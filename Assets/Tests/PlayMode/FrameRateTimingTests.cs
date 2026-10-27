using System.Collections;
using System.Reflection;
using Match3.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Match3.Presentation.Tests
{
    // Opt-in wall-clock checks: run with the Editor focused and without profiling/capture overhead.
    // These measure the real player loop, so machine load can legitimately fail the one-frame tolerance.
    public class FrameRateTimingTests
    {
        private int previousTarget;
        private int previousVSync;
        private GameObject root;
        private TileView template;
        private BoardView view;

        [SetUp]
        public void SetUp()
        {
            previousTarget = Application.targetFrameRate;
            previousVSync = QualitySettings.vSyncCount;
            Assert.That(Time.captureDeltaTime, Is.Zero, "Disable frame capture: it fakes deltaTime.");
            Assert.That(Time.timeScale, Is.EqualTo(1f), "Measure normal-speed animation.");
            template = TestTiles.CreateTemplate("TimingTileTemplate");
            root = new GameObject("TimingBoard");
            view = root.AddComponent<BoardView>();
            typeof(BoardView).GetField("tilePrefab", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(view, template);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Application.targetFrameRate = previousTarget;
            QualitySettings.vSyncCount = previousVSync;
            Object.Destroy(root);
            if (template != null)
            {
                Object.Destroy(template.gameObject);
            }
            yield return null;
        }

        [UnityTest, Explicit("Real-time hardware-dependent validation; select this test explicitly.")]
        public IEnumerator Animations_AtReal30Fps() => MeasureAt(30);

        [UnityTest, Explicit("Real-time hardware-dependent validation; select this test explicitly.")]
        public IEnumerator Animations_AtReal60Fps() => MeasureAt(60);

        [UnityTest, Explicit("Requires an Editor/display that can actually deliver 120 fps.")]
        public IEnumerator Animations_AtReal120Fps() => MeasureAt(120);

        private IEnumerator MeasureAt(int target)
        {
            if (target == 120 && Screen.currentResolution.refreshRateRatio.value < 120d)
            {
                Assert.Ignore("The current display does not support 120 Hz.");
            }

            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = target;
            // Let the real frame limiter settle; never change captureFramerate or deltaTime.
            for (int i = 0; i < 20; i++)
            {
                yield return null;
            }

            double start = Time.realtimeSinceStartupAsDouble;
            const int frames = 60;
            for (int i = 0; i < frames; i++)
            {
                yield return null;
            }
            double measuredFps = frames / (Time.realtimeSinceStartupAsDouble - start);
            TestContext.Out.WriteLine($"Requested {target} fps; actual cadence {measuredFps:F2} fps.");
            Assert.That(measuredFps, Is.InRange(target * 0.9d, target * 1.1d),
                "This machine did not deliver the requested cadence; animation timing is inconclusive.");

            var first = new GridPosition(0, 0);
            var second = new GridPosition(1, 0);
            RenderBoard();
            yield return Measure("swap", view.AnimateSwap(first, second, 0.2f), 0.2d, target);

            RenderBoard();
            yield return Measure("clear", view.AnimateClearViews(new[] { first }), 0.1d, target);

            foreach (TileType type in new[] { TileType.HorizontalRocket, TileType.TNT })
            {
                Board board = RenderBoard(type);
                var plan = SpecialEffects.Plan(board, new[] { new SpecialActivation(first) });
                yield return Measure(type.ToString(), view.AnimateSpecialEffects(plan), 0.12d, target);
            }

            RenderBoard();
            view.RemoveViews(new[] { first });
            var fall = new TileMove(new GridPosition(0, 1), first, TileType.Red);
            yield return Measure("fall", view.AnimateFalls(new[] { fall }, 0.15f), 0.15d, target);

            RenderBoard();
            view.RemoveViews(new[] { first });
            var spawn = new TileSpawn(first, TileType.Blue);
            yield return Measure("refill", view.AnimateRefill(new[] { spawn }, 0.15f), 0.15d, target);

            Board wispBoard = RenderBoard(TileType.Propeller);
            var wispPlan = SpecialEffects.Plan(wispBoard, new[] { new SpecialActivation(first) }, new System.Random(42));
            Assert.That(wispPlan.Effects[0].RemoteTarget.HasValue, Is.True);
            yield return MeasureWisp(view.AnimateSpecialEffects(wispPlan),
                view.GetTileView(wispPlan.Effects[0].RemoteTarget.Value).transform.position, target);
        }

        private Board RenderBoard(TileType origin = TileType.Red)
        {
            var board = new Board(3, 3);
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    board.SetTile(new GridPosition(x, y), TileType.Red);
                }
            }
            board.SetTile(new GridPosition(0, 0), origin);
            view.Render(board);
            return board;
        }

        private static IEnumerator Measure(string label, IEnumerator animation, double duration, int target)
        {
            double start = Time.realtimeSinceStartupAsDouble;
            int frames = 0;
            while (animation.MoveNext())
            {
                frames++;
                yield return animation.Current;
            }
            Assert.That(frames, Is.GreaterThan(0), "The production animation must actually run.");
            AssertDuration(label, Time.realtimeSinceStartupAsDouble - start, duration, target);
        }

        private IEnumerator MeasureWisp(IEnumerator animation, Vector3 destination, int target)
        {
            double phaseStart = Time.realtimeSinceStartupAsDouble;
            int phase = 0;
            Transform flyer = null;
            while (animation.MoveNext())
            {
                if (phase == 0)
                {
                    // The only non-TileView child is the production flight picture, created after the burst.
                    foreach (Transform child in root.transform)
                    {
                        if (child.GetComponent<TileView>() == null)
                        {
                            flyer = child;
                            break;
                        }
                    }
                    if (flyer != null)
                    {
                        AssertDuration("Wisp burst", Time.realtimeSinceStartupAsDouble - phaseStart, 0.12d, target);
                        phaseStart = Time.realtimeSinceStartupAsDouble;
                        phase = 1;
                    }
                }
                else if (phase == 1 && flyer.position == destination)
                {
                    AssertDuration("Wisp flight", Time.realtimeSinceStartupAsDouble - phaseStart, 0.25d, target);
                    phaseStart = Time.realtimeSinceStartupAsDouble;
                    phase = 2;
                }
                yield return animation.Current;
            }
            Assert.That(phase, Is.EqualTo(2), "Observe both flight and landing, not just the burst.");
            AssertDuration("Wisp landing", Time.realtimeSinceStartupAsDouble - phaseStart, 0.12d, target);
        }

        private static void AssertDuration(string label, double actual, double expected, int target)
        {
            TestContext.Out.WriteLine($"{target} fps | {label}: {actual * 1000d:F2} ms (expected {expected * 1000d:F0} ms).");
            Assert.That(actual, Is.EqualTo(expected).Within(1d / target),
                label + " must finish within one real target frame of its configured duration.");
        }
    }
}
