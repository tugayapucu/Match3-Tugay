using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Match3.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Match3.Presentation.Tests
{
    // Core changes the Board first; these tests check that BoardView follows each stage with the right objects.
    public class BoardViewResolutionTests
    {
        private const float Spacing = 1.5f;
        private static readonly TileType[] Palette = { TileType.Red, TileType.Green, TileType.Blue };

        private GameObject boardObject;
        private GameObject tileTemplate;
        private BoardView boardView;

        [SetUp]
        public void SetUp()
        {
            var template = TestTiles.CreateTemplate("ResolutionTestTileTemplate");
            tileTemplate = template.gameObject;

            boardObject = new GameObject("ResolutionTestBoard");
            boardObject.transform.position = new Vector3(4f, -2f, 0f);
            boardView = boardObject.AddComponent<BoardView>();

            const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(BoardView).GetField("tilePrefab", fields).SetValue(boardView, template);
            typeof(BoardView).GetField("tileSpacing", fields).SetValue(boardView, Spacing);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(boardObject);
            Object.Destroy(tileTemplate);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AnimateSpecialEffects_BeamCrystalRow_PopsOnlyItsVisibleRow_ThenRestoresWithoutChangingBoard()
        {
            // The hole at (3,1) is inside the row footprint but shows nothing, so it must not pop.
            var board = CreateBoard(
                "RGBR",
                "GHB.",
                "BRGB");
            boardView.Render(board);
            var origin = new GridPosition(1, 1);
            var originView = boardView.GetTileView(origin);
            // Non-default starting states: the feedback must start from, and return to, each view's own values.
            originView.transform.localScale = new Vector3(0.7f, 0.8f, 1f);
            boardView.GetTileView(new GridPosition(0, 1)).GetComponent<SpriteRenderer>().color =
                new Color(0.3f, 0.6f, 0.9f, 0.5f);
            var colors = SnapshotColors(board);
            var scales = SnapshotScales(board);
            var before = ReadTiles(board);
            var plan = SpecialEffects.Plan(board, new[] { new SpecialActivation(origin) });

            var animation = boardView.AnimateSpecialEffects(plan);
            Assert.That(animation.MoveNext(), Is.True);
            AssertPoppedExactly(board, originView, ViewsAt(new GridPosition(0, 1), new GridPosition(2, 1)), colors, scales);
            // Core's footprint contains the origin too; it gets only its own pop, never the two multiplied (1.2 × 1.1).
            Assert.That(originView.transform.localScale.x, Is.EqualTo(scales[originView].x * 1.2f).Within(1e-5f));
            // Visual only: the Board and the lookup are untouched.
            Assert.That(ReadTiles(board), Is.EqualTo(before));
            Assert.That(boardView.GetTileView(origin), Is.SameAs(originView));
            do
            {
                // Scale only: no frame changes any view's RGB or alpha.
                AssertColorsUnchanged(board, colors);
                yield return null;
            }
            while (animation.MoveNext());

            AssertPoppedExactly(board, null, new TileView[0], colors, scales);
            Assert.That(ReadTiles(board), Is.EqualTo(before));
            AssertViewsMatchBoard(board);
        }

        [Test]
        public void AnimateSpecialEffects_BeamCrystalColumn_PopsOnlyItsColumn()
        {
            var board = CreateBoard(
                "RGB",
                "BVR",
                "GRB");
            boardView.Render(board);
            var origin = new GridPosition(1, 1);
            var colors = SnapshotColors(board);
            var scales = SnapshotScales(board);
            var plan = SpecialEffects.Plan(board, new[] { new SpecialActivation(origin) });

            Assert.That(boardView.AnimateSpecialEffects(plan).MoveNext(), Is.True);
            AssertPoppedExactly(board, boardView.GetTileView(origin),
                ViewsAt(new GridPosition(1, 0), new GridPosition(1, 2)), colors, scales);
        }

        [Test]
        public void AnimateSpecialEffects_NovaGem_PopsItsAreaClippedByTheBoard()
        {
            CheckNovaArea(new GridPosition(3, 3), 24); // Centre: the full 5×5 area.
            CheckNovaArea(new GridPosition(0, 3), 14); // Edge: 3 columns × 5 rows.
            CheckNovaArea(new GridPosition(0, 0), 8); // Corner: 3 × 3.
        }

        [UnityTest]
        public IEnumerator AnimateSpecialEffects_Chain_ShowsEachEffectInPlanOrder_FromTheOriginalAppearance()
        {
            // The Beam Crystal's row reaches the Nova Gem, so Core plans the row first and the 5×5 area second.
            var board = CreateBoard(
                "RRRRRRR",
                "RRRRRRR",
                "HRRRTRR",
                "RRRRRRR",
                "RRRRRRR");
            boardView.Render(board);
            var beam = new GridPosition(0, 2);
            var nova = new GridPosition(4, 2);
            var beamView = boardView.GetTileView(beam);
            var novaView = boardView.GetTileView(nova);
            var overlap = boardView.GetTileView(new GridPosition(3, 2));
            var novaOnly = boardView.GetTileView(new GridPosition(3, 0));
            // The same starting size on a cell in both footprints and a cell only in the area: during the area phase
            // they must match, so the overlap cannot carry any growth left over from the row phase.
            var shared = new Vector3(0.8f, 0.8f, 1f);
            overlap.transform.localScale = shared;
            novaOnly.transform.localScale = shared;
            overlap.GetComponent<SpriteRenderer>().color = new Color(0.3f, 0.6f, 0.9f, 0.5f);
            var colors = SnapshotColors(board);
            var scales = SnapshotScales(board);
            var before = ReadTiles(board);
            var plan = SpecialEffects.Plan(board, new[] { new SpecialActivation(beam) });
            Assert.That(plan.Effects.Count, Is.EqualTo(2));
            Assert.That(plan.Effects[0].Origin, Is.EqualTo(beam));
            Assert.That(plan.Effects[1].Origin, Is.EqualTo(nova));

            var animation = boardView.AnimateSpecialEffects(plan);
            Assert.That(animation.MoveNext(), Is.True);
            // Row phase: the Nova Gem pops as part of the row (footprint size, not origin size); its area is untouched.
            var row = new List<TileView>();
            for (int x = 1; x < board.Width; x++)
            {
                row.Add(boardView.GetTileView(new GridPosition(x, 2)));
            }

            AssertPoppedExactly(board, beamView, row, colors, scales);

            // The area phase starts when the Nova Gem takes its own, stronger origin pop.
            int frames = 0;
            while (Vector3.Distance(novaView.transform.localScale, scales[novaView] * 1.2f) > 1e-5f)
            {
                Assert.That(novaOnly.transform.localScale, Is.EqualTo(scales[novaOnly]), "Area-only cell in the row phase.");
                AssertColorsUnchanged(board, colors);
                yield return null;
                Assert.That(animation.MoveNext(), Is.True, "The area phase must follow the row phase.");
                Assert.That(++frames, Is.LessThan(1000));
            }

            // Area phase, first frame: the whole row (Beam Crystal included) is back, and the area pops.
            AssertPoppedExactly(board, novaView, ViewsInNovaArea(board, nova), colors, scales);
            yield return null;
            if (animation.MoveNext())
            {
                // Mid-settle, both cells that started at the same size are still the same size.
                Assert.That(Vector3.Distance(overlap.transform.localScale, novaOnly.transform.localScale), Is.LessThan(1e-5f),
                    "Overlap cell in the area phase.");
                AssertColorsUnchanged(board, colors);
                yield return animation;
            }

            AssertPoppedExactly(board, null, new TileView[0], colors, scales);
            Assert.That(ReadTiles(board), Is.EqualTo(before));
        }

        [Test]
        public void AnimateSpecialEffects_Wisp_KeepsTheSimpleOriginPulse()
        {
            CheckOriginOnlyPulse(CreateBoard("RFR"), new GridPosition(1, 0));
        }

        [Test]
        public void AnimateSpecialEffects_SpectrumCore_PopsEveryVisibleViewItsFootprintAddresses()
        {
            // Swapped with a Blue: Core targets every Blue. Other colors and the unrelated Beam Crystal stay still.
            var board = CreateBoard(
                "BRGB",
                "GLBR",
                "RBGH");
            boardView.Render(board);
            var origin = new GridPosition(1, 1);
            var colors = SnapshotColors(board);
            var scales = SnapshotScales(board);
            var before = ReadTiles(board);
            var plan = SpecialEffects.Plan(board, new[] { new SpecialActivation(origin, TileType.Blue) });
            Assert.That(plan.Effects.Count, Is.EqualTo(1));
            // An independent statement of the target cells: every Blue view on the board.
            var blues = AllViews(board).FindAll(view => view.Type == TileType.Blue);
            Assert.That(blues.Count, Is.EqualTo(4));

            Assert.That(boardView.AnimateSpecialEffects(plan).MoveNext(), Is.True);
            // The origin is in its own footprint too; AssertPoppedExactly requires 1.2x, never 1.2 x 1.1.
            AssertPoppedExactly(board, boardView.GetTileView(origin), blues, colors, scales);
            Assert.That(ReadTiles(board), Is.EqualTo(before));
        }

        [Test]
        public void AnimateSpecialEffects_ZeroDuration_ChangesNothing()
        {
            var board = CreateBoard("RHR");
            boardView.Render(board);
            typeof(BoardView).GetField("specialActivationDuration", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(boardView, 0f);
            var colors = SnapshotColors(board);
            var scales = SnapshotScales(board);
            var plan = SpecialEffects.Plan(board, new[] { new SpecialActivation(new GridPosition(1, 0)) });

            Assert.That(boardView.AnimateSpecialEffects(plan).MoveNext(), Is.False);
            AssertPoppedExactly(board, null, new TileView[0], colors, scales);
        }

        [Test]
        public void AnimateSpecialEffects_RejectsBadInput_WhenCalled_BeforeAnyViewChanges()
        {
            // H's row reaches T, so the plan has two effects; only the second one's view is made wrong.
            var board = CreateBoard("HRT");
            boardView.Render(board);
            var plan = SpecialEffects.Plan(board, new[] { new SpecialActivation(new GridPosition(0, 0)) });
            Assert.That(plan.Effects.Count, Is.EqualTo(2));
            var colors = SnapshotColors(board);
            var scales = SnapshotScales(board);

            Assert.Throws<ArgumentNullException>(() => boardView.AnimateSpecialEffects(null));
            boardView.GetTileView(new GridPosition(2, 0)).SetTileType(TileType.Red);
            Assert.Throws<InvalidOperationException>(() => boardView.AnimateSpecialEffects(plan));
            boardView.RemoveViews(new[] { new GridPosition(2, 0) });
            Assert.Throws<ArgumentOutOfRangeException>(() => boardView.AnimateSpecialEffects(plan));

            // Nothing was started, so the first effect's views were never touched.
            for (int x = 0; x < 2; x++)
            {
                var view = boardView.GetTileView(new GridPosition(x, 0));
                AssertColor(view.GetComponent<SpriteRenderer>().color, colors[view], $"({x},0)");
                Assert.That(view.transform.localScale, Is.EqualTo(scales[view]));
            }
        }

        [UnityTest]
        public IEnumerator AnimateClearViews_ShrinksAndFadesFromEachViewsOwnState_ThenEndsInvisible()
        {
            var board = CreateBoard("RGB");
            boardView.Render(board);
            var first = boardView.GetTileView(new GridPosition(0, 0));
            var second = boardView.GetTileView(new GridPosition(1, 0));
            var untouched = boardView.GetTileView(new GridPosition(2, 0));
            // Deliberately different starting states: the animation must use each view's own values.
            first.transform.localScale = new Vector3(0.7f, 0.8f, 1f);
            second.transform.localScale = new Vector3(1.1f, 0.9f, 1f);
            // Non-white tints: a fade that reset RGB to white (or touched RGB at all) would be caught.
            first.GetComponent<SpriteRenderer>().color = new Color(0.8f, 0.4f, 0.2f, 1f);
            second.GetComponent<SpriteRenderer>().color = new Color(0.3f, 0.6f, 0.9f, 0.5f);
            var views = new[] { first, second };
            var startScales = new[] { first.transform.localScale, second.transform.localScale };
            var startColors = new[] { first.GetComponent<SpriteRenderer>().color, second.GetComponent<SpriteRenderer>().color };
            Vector3 untouchedScale = untouched.transform.localScale;
            var before = ReadTiles(board);
            const float duration = 0.1f;

            var animation = boardView.AnimateClearViews(new[] { new GridPosition(0, 0), new GridPosition(1, 0) });
            float elapsed = 0f;
            bool more = animation.MoveNext();
            int frames = 0;
            while (more)
            {
                float remaining = 1f - TileEasing.EaseInOut(elapsed / duration);
                for (int i = 0; i < views.Length; i++)
                {
                    Assert.That(Vector3.Distance(views[i].transform.localScale, startScales[i] * remaining), Is.LessThan(1e-5f));
                    Color c = views[i].GetComponent<SpriteRenderer>().color;
                    Assert.That(c.r, Is.EqualTo(startColors[i].r), "RGB is kept exactly.");
                    Assert.That(c.g, Is.EqualTo(startColors[i].g), "RGB is kept exactly.");
                    Assert.That(c.b, Is.EqualTo(startColors[i].b), "RGB is kept exactly.");
                    Assert.That(c.a, Is.EqualTo(startColors[i].a * remaining).Within(1e-5f));
                }

                // Visual only: the lookup and Board are untouched, and so is every other view.
                Assert.That(boardView.GetTileView(new GridPosition(0, 0)), Is.SameAs(first));
                Assert.That(untouched.transform.localScale, Is.EqualTo(untouchedScale));
                frames++;
                yield return null;
                elapsed += Time.deltaTime;
                more = animation.MoveNext();
            }

            Assert.That(frames, Is.GreaterThan(0));
            foreach (var view in views)
            {
                Assert.That(view.transform.localScale, Is.EqualTo(Vector3.zero));
                Assert.That(view.GetComponent<SpriteRenderer>().color.a, Is.Zero);
            }

            for (int i = 0; i < views.Length; i++)
            {
                Color end = views[i].GetComponent<SpriteRenderer>().color;
                Assert.That(new Vector3(end.r, end.g, end.b), Is.EqualTo(new Vector3(startColors[i].r, startColors[i].g, startColors[i].b)),
                    "Even fully faded, only alpha changed.");
            }

            Assert.That(boardView.GetTileView(new GridPosition(1, 0)), Is.SameAs(second), "Removal is RemoveViews' job.");
            Assert.That(ReadTiles(board), Is.EqualTo(before));
        }

        [Test]
        public void AnimateClearViews_ZeroDuration_ChangesNothingAndFinishesImmediately()
        {
            var board = CreateBoard("RG");
            boardView.Render(board);
            typeof(BoardView).GetField("clearDuration", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(boardView, 0f);
            var view = boardView.GetTileView(new GridPosition(0, 0));
            Vector3 scale = view.transform.localScale;
            Color color = view.GetComponent<SpriteRenderer>().color;

            Assert.That(boardView.AnimateClearViews(new[] { new GridPosition(0, 0) }).MoveNext(), Is.False);

            Assert.That(view.transform.localScale, Is.EqualTo(scale));
            Assert.That(view.GetComponent<SpriteRenderer>().color, Is.EqualTo(color));
        }

        [Test]
        public void AnimateClearViews_InvalidRequest_ThrowsBeforeAnyVisualChange()
        {
            var board = CreateBoard("RG");
            boardView.Render(board);
            boardView.RemoveViews(new[] { new GridPosition(1, 0) });
            var view = boardView.GetTileView(new GridPosition(0, 0));
            Vector3 scale = view.transform.localScale;

            Assert.Throws<ArgumentNullException>(() => boardView.AnimateClearViews(null));
            // (1,0) has no view: the whole request is rejected, so (0,0) is never touched either.
            Assert.Throws<ArgumentOutOfRangeException>(() => boardView.AnimateClearViews(
                new[] { new GridPosition(0, 0), new GridPosition(1, 0) }));
            typeof(BoardView).GetField("clearDuration", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(boardView, -1f);
            Assert.Throws<ArgumentOutOfRangeException>(() => boardView.AnimateClearViews(new[] { new GridPosition(0, 0) }));

            Assert.That(view.transform.localScale, Is.EqualTo(scale));
        }

        [UnityTest]
        public IEnumerator Runner_MatchedViewsClearVisiblyAfterTheBoard_ThenTheSpecialAppearsAtItsAnchor()
        {
            // Four Reds: Core clears them and places the Rocket at (1,0) before any view changes.
            var board = CreateBoard("RRRR");
            boardView.Render(board);
            var consumed = new[]
            {
                boardView.GetTileView(new GridPosition(0, 0)), boardView.GetTileView(new GridPosition(1, 0)),
                boardView.GetTileView(new GridPosition(2, 0)), boardView.GetTileView(new GridPosition(3, 0))
            };
            Vector3 startScale = consumed[1].transform.localScale;
            int callbacks = 0;
            var runner = new BoardResolutionRunner(board, boardView, FullPalette,
                new ScriptedRandom(1, 2, 1), 0.05f, 0.05f, new System.Random(2));
            boardView.StartCoroutine(runner.ResolveUntilStable(result => callbacks++));

            bool sawClearing = false;
            float deadline = Time.realtimeSinceStartup + 5f;
            while (callbacks == 0)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Resolution did not finish.");
                // Removed views are deactivated at once and destroyed at frame end, so require it to be still shown.
                if (consumed[1] != null && consumed[1].gameObject.activeSelf && consumed[1].transform.localScale.x < startScale.x - 1e-4f)
                {
                    // Board-first: already cleared and the Rocket already placed, while the old Red still shrinks there.
                    sawClearing = true;
                    Assert.That(board.GetTile(new GridPosition(1, 0)), Is.EqualTo(TileType.HorizontalRocket));
                    Assert.That(board.GetTile(new GridPosition(0, 0)), Is.EqualTo(TileType.Empty));
                    Assert.That(boardView.GetTileView(new GridPosition(1, 0)), Is.SameAs(consumed[1]));
                    Assert.That(consumed[1].Type, Is.EqualTo(TileType.Red));
                }

                yield return null;
            }

            yield return null;
            Assert.That(sawClearing, Is.True, "The matched views should be seen shrinking before removal.");
            Assert.That(callbacks, Is.EqualTo(1));
            foreach (var view in consumed)
            {
                Assert.That(view == null, Is.True, "Every consumed view is destroyed after its animation.");
            }

            var rocket = boardView.GetTileView(new GridPosition(1, 0));
            Assert.That(rocket.Type, Is.EqualTo(TileType.HorizontalRocket));
            Assert.That(rocket.transform.localScale, Is.EqualTo(startScale), "The new special appears at full size.");
            Assert.That(rocket.GetComponent<SpriteRenderer>().color, Is.EqualTo(Color.white));
            AssertViewsMatchBoard(board);
        }

        [UnityTest]
        public IEnumerator Runner_BeamCrystalSwap_PopsItsRowOnTheUnchangedBoard_BeforeClearing()
        {
            // Already swapped: the Beam Crystal sits at (1,0), so Core's footprint is the bottom row.
            var board = CreateBoard(
                "RGB",
                "GBR",
                "RHG");
            boardView.Render(board);
            var rowViews = ViewsAt(new GridPosition(0, 0), new GridPosition(2, 0));
            var above = boardView.GetTileView(new GridPosition(0, 1));
            // Every rendered view starts at the template's size.
            Vector3 startScale = above.transform.localScale;
            var before = ReadTiles(board);
            var move = SwapValidator.Analyze(board, new GridPosition(0, 0), new GridPosition(1, 0));
            int callbacks = 0;
            boardView.StartCoroutine(new BoardResolutionRunner(board, boardView, FullPalette,
                new System.Random(7), 0.05f, 0.05f, new System.Random(2)).ResolveUntilStable(move, result => callbacks++));

            bool sawRowPop = false;
            float deadline = Time.realtimeSinceStartup + 5f;
            while (callbacks == 0)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Resolution did not finish.");
                // Growing is the effect feedback; the later clear only shrinks.
                if (rowViews[0] != null && rowViews[0].gameObject.activeSelf
                    && rowViews[0].transform.localScale.x > startScale.x + 1e-4f)
                {
                    // The feedback runs on the stable Board: nothing is cleared until it ends.
                    sawRowPop = true;
                    Assert.That(rowViews[1].transform.localScale.x, Is.GreaterThan(startScale.x + 1e-4f),
                        "The whole visible row pops together.");
                    Assert.That(above.transform.localScale, Is.EqualTo(startScale), "Cells outside the footprint stay as they are.");
                    foreach (var view in new[] { rowViews[0], rowViews[1], above })
                    {
                        AssertColor(view.GetComponent<SpriteRenderer>().color, Color.white, "the row and the cell above");
                    }

                    Assert.That(ReadTiles(board), Is.EqualTo(before));
                }

                yield return null;
            }

            yield return null;
            Assert.That(sawRowPop, Is.True, "The Beam Crystal's row should be seen popping before it clears.");
            Assert.That(callbacks, Is.EqualTo(1));
            Assert.That(rowViews[0] == null && rowViews[1] == null, Is.True, "The cleared row views are destroyed.");
            AssertViewsMatchBoard(board);
        }

        [UnityTest]
        public IEnumerator Runner_EffectClearedViews_ShrinkAfterTheBoardClears_ThenAreRemoved()
        {
            var board = CreateBoard(
                "RGB",
                "GBR",
                "RHG");
            boardView.Render(board);
            var red = boardView.GetTileView(new GridPosition(0, 0));
            var beam = boardView.GetTileView(new GridPosition(1, 0));
            Vector3 startScale = red.transform.localScale;
            var move = SwapValidator.Analyze(board, new GridPosition(0, 0), new GridPosition(1, 0));
            int callbacks = 0;
            boardView.StartCoroutine(new BoardResolutionRunner(board, boardView, FullPalette,
                new System.Random(7), 0.05f, 0.05f, new System.Random(2)).ResolveUntilStable(move, result => callbacks++));

            bool sawShrinking = false;
            float deadline = Time.realtimeSinceStartup + 5f;
            while (callbacks == 0)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Resolution did not finish.");
                // Removed views are deactivated at once and destroyed at frame end, so require it to be still shown.
                if (red != null && red.gameObject.activeSelf && red.transform.localScale.x < startScale.x - 1e-4f)
                {
                    // Board-first: the whole row is already Empty while its old views (the special too) still shrink.
                    sawShrinking = true;
                    for (int x = 0; x < 3; x++)
                    {
                        Assert.That(board.GetTile(new GridPosition(x, 0)), Is.EqualTo(TileType.Empty));
                    }

                    Assert.That(boardView.GetTileView(new GridPosition(0, 0)), Is.SameAs(red));
                    Assert.That(red.Type, Is.EqualTo(TileType.Red));
                    Assert.That(beam.transform.localScale.x, Is.LessThan(startScale.x), "The special clears the same way.");
                }

                yield return null;
            }

            yield return null;
            Assert.That(sawShrinking, Is.True, "Effect-cleared views should be seen shrinking before removal.");
            Assert.That(callbacks, Is.EqualTo(1));
            Assert.That(red == null && beam == null, Is.True, "Every effect-cleared view is destroyed.");
            AssertViewsMatchBoard(board);
        }

        [UnityTest]
        public IEnumerator AnimateFalls_SpecialsKeepTheirSpritesAndOrientation()
        {
            var board = CreateBoard("VTL", "HFB", "...");
            boardView.Render(board);
            boardView.RemoveViews(new[] { new GridPosition(0, 0), new GridPosition(1, 0), new GridPosition(2, 0) });
            var types = new Dictionary<TileView, TileType>();
            for (int y = 1; y < 3; y++)
                for (int x = 0; x < 3; x++)
                {
                    var view = boardView.GetTileView(new GridPosition(x, y));
                    types.Add(view, view.Type);
                }

            var moves = BoardResolution.ApplyGravity(board);
            yield return boardView.AnimateFalls(moves, 0.05f);

            foreach (var pair in types)
            {
                var view = pair.Key;
                Assert.That(boardView.GetTileView(view.Position), Is.SameAs(view));
                Assert.That(view.Type, Is.EqualTo(pair.Value));
                // Falling never swaps art: each view keeps its type's sprite, and orientation stays in the sprite.
                Assert.That(view.GetComponent<SpriteRenderer>().sprite, Is.SameAs(TestTiles.SpriteFor(pair.Value)));
                Assert.That(view.transform.localRotation, Is.EqualTo(Quaternion.identity));
            }
            AssertViewsMatchBoardWhereShown(board);
            Assert.That(CountActiveViews(), Is.EqualTo(6));
            for (int x = 0; x < 3; x++)
            {
                var empty = new GridPosition(x, 2);
                Assert.That(board.GetTile(empty), Is.EqualTo(TileType.Empty));
                Assert.Throws<ArgumentOutOfRangeException>(() => boardView.GetTileView(empty));
            }
        }

        [UnityTest]
        public IEnumerator RemoveViews_MatchedCells_AreRemovedAndOtherViewsSurvive()
        {
            // Column 0 has R R R from the bottom: a real match.
            var board = CreateBoard(
                "GBYP",
                "RBOY",
                "RGPB",
                "RYOG");
            boardView.Render(board);
            var matches = MatchFinder.FindMatches(board);
            var removedViews = new List<TileView>();
            foreach (var position in matches)
            {
                removedViews.Add(boardView.GetTileView(position));
            }

            var survivors = SnapshotViews(board, matches);
            BoardResolution.ClearCells(board, matches);
            var boardBefore = ReadTiles(board);

            boardView.RemoveViews(matches);

            foreach (var position in matches)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => boardView.GetTileView(position));
            }

            AssertSameViews(survivors);
            Assert.That(ReadTiles(board), Is.EqualTo(boardBefore), "BoardView must not change the Board.");

            yield return null;
            foreach (var view in removedViews)
            {
                Assert.That(view == null, Is.True, "Removed views must be destroyed.");
            }
        }

        [Test]
        public void RemoveViews_PositionWithoutView_ThrowsAndRemovesNothing()
        {
            var board = CreateBoard("RGB");
            boardView.Render(board);
            boardView.RemoveViews(new[] { new GridPosition(1, 0) });
            var views = SnapshotViews(board, new[] { new GridPosition(1, 0) });

            // (0,0) is valid but (1,0) no longer has a view: nothing may be removed.
            Assert.Throws<ArgumentOutOfRangeException>(
                () => boardView.RemoveViews(new[] { new GridPosition(0, 0), new GridPosition(1, 0) }));

            AssertSameViews(views);
        }

        [UnityTest]
        public IEnumerator AnimateFalls_MovesTheSameObjects_AndUpdatesLookupOnlyAtTheEnd()
        {
            // Column 0, bottom to top: G R R Y. Clearing G makes both reds and Y fall one cell.
            var board = CreateBoard(
                "Y",
                "R",
                "R",
                "G");
            boardView.Render(board);
            var lowerRed = boardView.GetTileView(new GridPosition(0, 1));
            var upperRed = boardView.GetTileView(new GridPosition(0, 2));
            var yellow = boardView.GetTileView(new GridPosition(0, 3));
            var cleared = new[] { new GridPosition(0, 0) };
            BoardResolution.ClearCells(board, cleared);
            boardView.RemoveViews(cleared);
            var moves = BoardResolution.ApplyGravity(board);
            var boardBefore = ReadTiles(board);

            // 0.5s exceeds Unity's default maximum frame step (~0.33s), so at least one in-between frame is observed.
            var animation = boardView.AnimateFalls(moves, 0.5f);
            bool sawMotion = false;
            while (animation.MoveNext())
            {
                // While falling, the lookup still describes where the tiles started.
                Assert.That(boardView.GetTileView(new GridPosition(0, 1)), Is.SameAs(lowerRed));
                Assert.That(boardView.GetTileView(new GridPosition(0, 3)), Is.SameAs(yellow));
                Assert.Throws<ArgumentOutOfRangeException>(() => boardView.GetTileView(new GridPosition(0, 0)));

                float y = lowerRed.transform.position.y;
                if (y < CellWorld(0, 1).y && y > CellWorld(0, 0).y)
                {
                    sawMotion = true;
                }

                yield return animation.Current;
            }

            Assert.That(sawMotion, Is.True, "The lower red should pass between its start and end cells.");
            // Two reds: identity comes from the move records, never from colour.
            Assert.That(boardView.GetTileView(new GridPosition(0, 0)), Is.SameAs(lowerRed));
            Assert.That(boardView.GetTileView(new GridPosition(0, 1)), Is.SameAs(upperRed));
            Assert.That(boardView.GetTileView(new GridPosition(0, 2)), Is.SameAs(yellow));
            Assert.Throws<ArgumentOutOfRangeException>(() => boardView.GetTileView(new GridPosition(0, 3)));
            Assert.That(ReadTiles(board), Is.EqualTo(boardBefore), "BoardView must not change the Board.");
            AssertViewsMatchBoardWhereShown(board);
        }

        [UnityTest]
        public IEnumerator AnimateRefill_CreatesNewViewsAboveTheBoard_AndRegistersThemOnLanding()
        {
            var board = CreateBoard(
                "R.",
                "GB",
                "BG");
            boardView.Render(board);
            boardView.RemoveViews(new[] { new GridPosition(1, 2) });
            var spawns = BoardResolution.Refill(board, Palette, new System.Random(5));
            Assert.That(spawns.Count, Is.EqualTo(1));
            var target = spawns[0].Position;
            var boardBefore = ReadTiles(board);

            var animation = boardView.AnimateRefill(spawns, 0.2f);
            Assert.That(animation.MoveNext(), Is.True);

            // Created on the first step, just above its cell, and not yet in the lookup.
            var created = FindViewNotInLookup(board);
            Assert.That(created, Is.Not.Null);
            Assert.That(created.Type, Is.EqualTo(spawns[0].Type));
            Assert.That(created.transform.position.y, Is.GreaterThan(CellWorld(target.X, target.Y).y));
            Assert.Throws<ArgumentOutOfRangeException>(() => boardView.GetTileView(target));

            yield return animation.Current;
            while (animation.MoveNext())
            {
                yield return animation.Current;
            }

            Assert.That(boardView.GetTileView(target), Is.SameAs(created));
            Assert.That(created.Position, Is.EqualTo(target));
            Assert.That(ReadTiles(board), Is.EqualTo(boardBefore), "BoardView must not change the Board.");
            AssertViewsMatchBoard(board);
        }

        [UnityTest]
        public IEnumerator AnimateFalls_FollowsTheEaseInOutCurve_AndEveryTileLandsTogether()
        {
            // Column 0: R falls two cells. Column 1: Y and G fall one cell each. Same duration for all.
            var board = CreateBoard(
                "RY",
                ".G",
                "..");
            boardView.Render(board);
            boardView.RemoveViews(new[] { new GridPosition(0, 1), new GridPosition(0, 0), new GridPosition(1, 0) });
            var moves = BoardResolution.ApplyGravity(board);
            var views = new TileView[moves.Count];
            for (int i = 0; i < moves.Count; i++)
            {
                views[i] = boardView.GetTileView(moves[i].From);
            }

            const float duration = 0.5f;
            var animation = boardView.AnimateFalls(moves, duration);
            Assert.That(animation.MoveNext(), Is.True);
            AssertEasedPositions(views, moves, 0f);

            // Drive by hand and mirror the routine's clock: on resume it adds this frame's deltaTime.
            float elapsed = 0f;
            int intermediateFrames = 0;
            while (true)
            {
                yield return null;
                elapsed += Time.deltaTime;
                if (!animation.MoveNext())
                {
                    break;
                }

                AssertEasedPositions(views, moves, TileEasing.EaseInOut(elapsed / duration));
                intermediateFrames++;
            }

            Assert.That(intermediateFrames, Is.GreaterThan(0), "At least one in-between frame was checked.");
            for (int i = 0; i < moves.Count; i++)
            {
                Assert.That(views[i].transform.position, Is.EqualTo(CellWorld(moves[i].To.X, moves[i].To.Y)));
                Assert.That(boardView.GetTileView(moves[i].To), Is.SameAs(views[i]));
            }
        }

        [UnityTest]
        public IEnumerator AnimateRefill_FollowsTheEaseInOutCurve_FromAboveEachColumn()
        {
            // Column 0 receives two new tiles, column 1 one: each starts that many cells above its target.
            var board = CreateBoard(
                "..",
                ".G",
                "RB");
            boardView.Render(board);
            boardView.RemoveViews(new[] { new GridPosition(0, 2), new GridPosition(0, 1), new GridPosition(1, 2) });
            var spawns = BoardResolution.Refill(board, Palette, new System.Random(5));
            Assert.That(spawns.Count, Is.EqualTo(3));
            var perColumn = new Dictionary<int, int>();
            foreach (TileSpawn spawn in spawns)
            {
                perColumn.TryGetValue(spawn.Position.X, out int count);
                perColumn[spawn.Position.X] = count + 1;
            }

            const float duration = 0.5f;
            var animation = boardView.AnimateRefill(spawns, duration);
            Assert.That(animation.MoveNext(), Is.True);
            // New views exist from the first step but stay out of the lookup until they land.
            var created = new Dictionary<GridPosition, TileView>();
            foreach (var view in boardObject.GetComponentsInChildren<TileView>())
            {
                bool registered;
                try { registered = boardView.GetTileView(view.Position) == view; }
                catch (ArgumentOutOfRangeException) { registered = false; }
                if (!registered)
                {
                    created.Add(view.Position, view);
                }
            }

            Assert.That(created.Count, Is.EqualTo(3));

            float elapsed = 0f;
            float eased = 0f;
            int intermediateFrames = 0;
            while (true)
            {
                foreach (TileSpawn spawn in spawns)
                {
                    Vector3 start = CellWorld(spawn.Position.X, spawn.Position.Y + perColumn[spawn.Position.X]);
                    Vector3 expected = Vector3.Lerp(start, CellWorld(spawn.Position.X, spawn.Position.Y), eased);
                    Assert.That(Vector3.Distance(created[spawn.Position].transform.position, expected), Is.LessThan(1e-4f));
                    // Still falling into place: the cell has no registered view until landing.
                    Assert.Throws<ArgumentOutOfRangeException>(() => boardView.GetTileView(spawn.Position),
                        "A refill view must not be registered before it lands.");
                }

                yield return null;
                elapsed += Time.deltaTime;
                if (!animation.MoveNext())
                {
                    break;
                }

                eased = TileEasing.EaseInOut(elapsed / duration);
                intermediateFrames++;
            }

            Assert.That(intermediateFrames, Is.GreaterThan(0), "At least one in-between frame was checked.");
            foreach (TileSpawn spawn in spawns)
            {
                Assert.That(created[spawn.Position].transform.position, Is.EqualTo(CellWorld(spawn.Position.X, spawn.Position.Y)));
                Assert.That(boardView.GetTileView(spawn.Position), Is.SameAs(created[spawn.Position]));
            }
        }

        // Each view sits at the same eased fraction of its own path, so all tiles land together.
        private void AssertEasedPositions(TileView[] views, IReadOnlyList<TileMove> moves, float eased)
        {
            for (int i = 0; i < moves.Count; i++)
            {
                Vector3 expected = Vector3.Lerp(CellWorld(moves[i].From.X, moves[i].From.Y), CellWorld(moves[i].To.X, moves[i].To.Y), eased);
                Assert.That(Vector3.Distance(views[i].transform.position, expected), Is.LessThan(1e-4f));
            }
        }

        [UnityTest]
        public IEnumerator FullRound_ClearFallRefill_LeavesBoardAndViewsInAgreement()
        {
            var board = CreateBoard(
                "GBYP",
                "RBOY",
                "RGPB",
                "RYOG");
            boardView.Render(board);
            var matches = MatchFinder.FindMatches(board);

            BoardResolution.ClearCells(board, matches);
            boardView.RemoveViews(matches);
            var moves = BoardResolution.ApplyGravity(board);
            yield return boardView.AnimateFalls(moves, 0.1f);
            var spawns = BoardResolution.Refill(board, Palette, new System.Random(11));
            yield return boardView.AnimateRefill(spawns, 0.1f);
            // Let deferred Destroy calls finish before counting objects.
            yield return null;

            AssertViewsMatchBoard(board);
            Assert.That(boardObject.GetComponentsInChildren<TileView>(true).Length, Is.EqualTo(board.Width * board.Height),
                "Exactly one view per cell: nothing leaked, nothing missing.");
        }

        [UnityTest]
        public IEnumerator Runner_DifferentGapDepths_RefillsEachColumnWithTheRightNewViews()
        {
            // Three staircase runs clear 1, 2, 3, 2, 1 cells in columns 0..4 within a single round.
            var board = CreateBoard(
                "POBBB",
                "YGGGO",
                "RRRYP");
            boardView.Render(board);
            var originalViews = new HashSet<TileView>(boardObject.GetComponentsInChildren<TileView>());
            var fullPalette = new[]
            {
                TileType.Red, TileType.Green, TileType.Blue, TileType.Yellow, TileType.Purple, TileType.Orange
            };
            // Refill order is column by column, bottom to top: (0,2) (1,1)(1,2) (2,0)(2,1)(2,2) (3,1)(3,2) (4,2).
            var runner = new BoardResolutionRunner(
                board, boardView, fullPalette, new ScriptedRandom(0, 2, 3, 1, 0, 5, 1, 4, 1), 0.05f, 0.05f, new System.Random(2));

            ResolutionResult result = null;
            yield return runner.ResolveUntilStable(r => result = r);
            // Let deferred Destroy calls finish before inspecting objects.
            yield return null;

            Assert.That(ReadTiles(board), Is.EqualTo(ReadTiles(CreateBoard(
                "RYOPG",
                "PBRGO",
                "YOGYP"))));
            AssertViewsMatchBoard(board);
            // The three staircase runs: 3 Red, 3 Green, 3 Blue.
            Assert.That(result.ClearedTiles, Is.EquivalentTo(new[]
            {
                TileType.Red, TileType.Red, TileType.Red,
                TileType.Green, TileType.Green, TileType.Green,
                TileType.Blue, TileType.Blue, TileType.Blue
            }));

            var newViewsPerColumn = new int[board.Width];
            foreach (var view in boardObject.GetComponentsInChildren<TileView>(true))
            {
                if (!originalViews.Contains(view))
                {
                    newViewsPerColumn[view.Position.X]++;
                }
            }

            // Every cleared cell got exactly one new view in its own column; survivors kept their objects.
            Assert.That(newViewsPerColumn, Is.EqualTo(new[] { 1, 2, 3, 2, 1 }));
            Assert.That(boardObject.GetComponentsInChildren<TileView>(true).Length, Is.EqualTo(board.Width * board.Height));
        }

        [UnityTest]
        public IEnumerator Runner_OneRound_ReportsExactlyTheClearedTypes()
        {
            // Column 0 already holds G G G; the refill (B, Y, O) creates no new match.
            var board = CreateBoard(
                "GBY",
                "GPO",
                "GRG");

            yield return RunAndCheckResult(board, new ScriptedRandom(2, 3, 5),
                new[] { TileType.Green, TileType.Green, TileType.Green },
                "OBY",
                "YPO",
                "BRG");
        }

        [UnityTest]
        public IEnumerator Runner_TwoCascadeRounds_AggregateIntoOneResult()
        {
            // Round 1 clears G G G and refills B B B (a new match); round 2 clears it and refills B, Y, O.
            var board = CreateBoard(
                "GBY",
                "GPO",
                "GRG");

            yield return RunAndCheckResult(board, new ScriptedRandom(2, 2, 2, 2, 3, 5),
                new[]
                {
                    TileType.Green, TileType.Green, TileType.Green,
                    TileType.Blue, TileType.Blue, TileType.Blue
                },
                "OBY",
                "YPO",
                "BRG");
        }

        [UnityTest]
        public IEnumerator Runner_ThreeCascadeRounds_AggregateIntoOneResult()
        {
            // G G G, then refill-made B B B, then refill-made Y Y Y, then a stable refill.
            var board = CreateBoard(
                "GBY",
                "GPO",
                "GRG");

            yield return RunAndCheckResult(board, new ScriptedRandom(2, 2, 2, 3, 3, 3, 2, 3, 5),
                new[]
                {
                    TileType.Green, TileType.Green, TileType.Green,
                    TileType.Blue, TileType.Blue, TileType.Blue,
                    TileType.Yellow, TileType.Yellow, TileType.Yellow
                },
                "OBY",
                "YPO",
                "BRG");
        }

        [UnityTest]
        public IEnumerator Runner_Intersection_CountsTheSharedCellOnce()
        {
            // A cross of Reds: row y=1 and column x=1 share (1,1), so 5 cells clear, not 6.
            var board = CreateBoard(
                "GRB",
                "RRR",
                "BRG");

            yield return RunAndCheckResult(board, new ScriptedRandom(0, 3, 4, 5),
                new[] { TileType.Red, TileType.Red, TileType.Red, TileType.Red, TileType.Red },
                "RPO",
                "GYB",
                "BTG");
        }

        [UnityTest]
        public IEnumerator Runner_FourHorizontal_CreatesRocketAndCreditsFourColors()
        {
            yield return CheckCreation(CreateBoard("RRRR"), new ScriptedRandom(1, 2, 1),
                TileType.HorizontalRocket, new GridPosition(1, 0), 4);
        }

        [UnityTest]
        public IEnumerator Runner_FourVertical_CreatedRocketFallsAndSurvivesRefill()
        {
            yield return CheckCreation(CreateBoard("R", "R", "R", "R"), new ScriptedRandom(1, 2, 1),
                TileType.VerticalRocket, new GridPosition(0, 0), 4);
        }

        [UnityTest]
        public IEnumerator Runner_FiveLine_CreatesLightBallAndCreditsFiveColors()
        {
            yield return CheckCreation(CreateBoard("RRRRR"), new ScriptedRandom(1, 2, 1, 2),
                TileType.LightBall, new GridPosition(2, 0), 5);
        }

        [UnityTest]
        public IEnumerator Runner_Square_CreatesPropellerAndCreditsFourColors()
        {
            yield return CheckCreation(CreateBoard("RR", "RR"), new ScriptedRandom(1, 2, 1),
                TileType.Propeller, new GridPosition(0, 0), 4);
        }

        [UnityTest]
        public IEnumerator Runner_LShape_CreatesTntAndKeepsUnrelatedViews()
        {
            var board = CreateBoard("RRR", "GBR", "YOR");
            boardView.Render(board);
            var survivor = boardView.GetTileView(new GridPosition(0, 1));
            int callbacks = 0;
            yield return new BoardResolutionRunner(board, boardView, FullPalette,
                new ScriptedRandom(1, 2, 1, 2), 0.05f, 0.05f, new System.Random(2)).ResolveUntilStable(result =>
                {
                    callbacks++;
                    Assert.That(result.ClearedCount, Is.EqualTo(5));
                    Assert.That(result.CountOf(TileType.Red), Is.EqualTo(5));
                });
            Assert.That(callbacks, Is.EqualTo(1));
            Assert.That(board.GetTile(new GridPosition(2, 0)), Is.EqualTo(TileType.TNT));
            Assert.That(boardView.GetTileView(new GridPosition(0, 1)), Is.SameAs(survivor));
            AssertViewsMatchBoard(board);
        }

        [UnityTest]
        public IEnumerator Runner_DisjointGroups_CreateTwoSpecialsAndKeepSurvivorIdentity()
        {
            var board = CreateBoard("RRRRBGGGG");
            boardView.Render(board);
            var survivor = boardView.GetTileView(new GridPosition(4, 0));
            int callbacks = 0;
            yield return new BoardResolutionRunner(board, boardView, FullPalette,
                new ScriptedRandom(1, 2, 3, 2, 0, 3), 0f, 0f, new System.Random(2)).ResolveUntilStable(result =>
                {
                    callbacks++;
                    Assert.That(result.ClearedCount, Is.EqualTo(8));
                    Assert.That(result.CountOf(TileType.Red), Is.EqualTo(4));
                    Assert.That(result.CountOf(TileType.Green), Is.EqualTo(4));
                });
            Assert.That(callbacks, Is.EqualTo(1));
            Assert.That(board.GetTile(new GridPosition(1, 0)), Is.EqualTo(TileType.HorizontalRocket));
            Assert.That(board.GetTile(new GridPosition(6, 0)), Is.EqualTo(TileType.HorizontalRocket));
            Assert.That(boardView.GetTileView(new GridPosition(4, 0)), Is.SameAs(survivor));
            AssertViewsMatchBoard(board);
        }

        [UnityTest]
        public IEnumerator Runner_CascadeSquare_UsesDefaultAnchorInsteadOfTheOriginalSelectedCell()
        {
            var board = CreateBoard("GB", "GR", "GR");
            boardView.Render(board);
            var move = SwapValidator.Analyze(board, new GridPosition(0, 1), new GridPosition(1, 1));
            var runner = new BoardResolutionRunner(board, boardView, FullPalette,
                new ScriptedRandom(0, 0, 2, 1, 1, 3), 0.05f, 0.05f, new System.Random(2));
            int callbacks = 0;

            yield return runner.ResolveUntilStable(move, result =>
            {
                callbacks++;
                Assert.That(result.CountOf(TileType.Green), Is.EqualTo(3));
                Assert.That(result.CountOf(TileType.Red), Is.EqualTo(4));
                Assert.That(result.ClearedCount, Is.EqualTo(7));
            });

            // The old second selection (1,1) belongs to the new square but has expired as an anchor preference.
            Assert.That(board.GetTile(new GridPosition(0, 0)), Is.EqualTo(TileType.Propeller));
            Assert.That(board.GetTile(new GridPosition(1, 0)), Is.EqualTo(TileType.Blue));
            Assert.That(callbacks, Is.EqualTo(1));
            Assert.That(MatchFinder.FindMatches(board), Is.Empty);
            AssertViewsMatchBoard(board);
        }

        private IEnumerator CheckCreation(Board board, System.Random random, TileType type, GridPosition finalCell, int count)
        {
            boardView.Render(board);
            var consumedView = boardView.GetTileView(finalCell);
            int callbacks = 0;
            yield return new BoardResolutionRunner(board, boardView, FullPalette, random, 0.05f, 0.05f, new System.Random(2))
                .ResolveUntilStable(result =>
                {
                    callbacks++;
                    Assert.That(result.ClearedCount, Is.EqualTo(count));
                    Assert.That(result.CountOf(TileType.Red), Is.EqualTo(count));
                    foreach (TileType color in result.ClearedTiles)
                        Assert.That(TileRules.IsNormalMatchItem(color), Is.True);
                });
            yield return null;
            Assert.That(callbacks, Is.EqualTo(1));
            Assert.That(consumedView == null, Is.True, "The consumed normal visual was removed.");
            Assert.That(board.GetTile(finalCell), Is.EqualTo(type));
            Assert.That(boardView.GetTileView(finalCell).Type, Is.EqualTo(type));
            Assert.That(boardView.GetTileView(finalCell).GetComponent<SpriteRenderer>().sprite, Is.SameAs(TestTiles.SpriteFor(type)));
            Assert.That(CountActiveViews(), Is.EqualTo(board.Width * board.Height));
            Assert.That(MatchFinder.FindMatches(board), Is.Empty);
            AssertViewsMatchBoard(board);
        }

        [UnityTest]
        public IEnumerator Runner_RocketChain_ReportsOnlyNormalColorsAndKeepsSurvivors()
        {
            var board = CreateBoard("RGB", "BGG", "HVR");
            boardView.Render(board);
            var oldRocket = boardView.GetTileView(new GridPosition(0, 0));
            var oldVertical = boardView.GetTileView(new GridPosition(1, 0));
            var blue = boardView.GetTileView(new GridPosition(0, 1));
            var green = boardView.GetTileView(new GridPosition(2, 1));
            var move = SwapValidator.Analyze(board, new GridPosition(0, 0), new GridPosition(1, 0));
            int callbacks = 0;
            yield return new BoardResolutionRunner(board, boardView, FullPalette,
                new ScriptedRandom(1, 0, 2, 3, 5), 0.05f, 0.05f, new System.Random(2)).ResolveUntilStable(move, result =>
                {
                    callbacks++;
                    Assert.That(result.ClearedCount, Is.EqualTo(3));
                    Assert.That(result.CountOf(TileType.Red), Is.EqualTo(1));
                    Assert.That(result.CountOf(TileType.Green), Is.EqualTo(2));
                });
            yield return null;
            Assert.That(callbacks, Is.EqualTo(1));
            Assert.That(oldRocket == null && oldVertical == null, Is.True);
            Assert.That(boardView.GetTileView(new GridPosition(0, 0)), Is.SameAs(blue));
            Assert.That(boardView.GetTileView(new GridPosition(2, 0)), Is.SameAs(green));
            Assert.That(ReadTiles(board), Is.EqualTo(ReadTiles(CreateBoard("GYO", "RBB", "BRG"))));
            AssertViewsMatchBoard(board);
        }

        [UnityTest]
        public IEnumerator Runner_ChainBreaksFourLineBeforeCreation_OnlyActualClearsCount()
        {
            var board = CreateBoard("RRVR", "GBRY");
            var first = new GridPosition(2, 1);
            var second = new GridPosition(2, 0);
            board.TrySwapAdjacent(first, second);
            Assert.That(MatchFinder.FindMatches(board).Count, Is.EqualTo(4));
            boardView.Render(board);
            var survivor = boardView.GetTileView(new GridPosition(1, 0));
            int callbacks = 0;
            var move = SwapValidator.Analyze(board, first, second);

            yield return new BoardResolutionRunner(board, boardView, FullPalette,
                new ScriptedRandom(1, 2), 0.05f, 0.05f, new System.Random(2)).ResolveUntilStable(move, result =>
                {
                    callbacks++;
                    Assert.That(result.ClearedCount, Is.EqualTo(1));
                    Assert.That(result.CountOf(TileType.Red), Is.EqualTo(1));
                });

            Assert.That(callbacks, Is.EqualTo(1));
            Assert.That(ReadTiles(board), Is.EqualTo(ReadTiles(CreateBoard("RRBR", "GBGY"))));
            Assert.That(boardView.GetTileView(new GridPosition(1, 0)), Is.SameAs(survivor));
            AssertViewsMatchBoard(board);
        }

        [UnityTest]
        public IEnumerator Runner_NewRocketFallsIntoOldActivationCell_WithoutRetroactiveActivation()
        {
            var board = CreateBoard("RRHR", "GBRY");
            var first = new GridPosition(2, 1);
            var second = new GridPosition(2, 0);
            board.TrySwapAdjacent(first, second);
            boardView.Render(board);
            var oldRocket = boardView.GetTileView(second);
            int callbacks = 0;
            var move = SwapValidator.Analyze(board, first, second);

            yield return new BoardResolutionRunner(board, boardView, FullPalette,
                new ScriptedRandom(1, 2, 2, 3, 1, 3, 5), 0.05f, 0.05f, new System.Random(2)).ResolveUntilStable(move, result =>
                {
                    callbacks++;
                    Assert.That(result.ClearedCount, Is.EqualTo(7));
                    Assert.That(result.CountOf(TileType.Red), Is.EqualTo(4));
                });
            yield return null;

            Assert.That(callbacks, Is.EqualTo(1));
            Assert.That(oldRocket == null, Is.True);
            Assert.That(board.GetTile(second), Is.EqualTo(TileType.HorizontalRocket));
            Assert.That(ReadTiles(board), Is.EqualTo(ReadTiles(CreateBoard("BYGO", "GBHY"))));
            Assert.That(CountActiveViews(), Is.EqualTo(8));
            AssertViewsMatchBoard(board);
        }

        [UnityTest]
        public IEnumerator Runner_EmptyFirstScanExpiresAnchor_CascadeTilesAtOldBlastCellsCountAgain()
        {
            var board = CreateBoard("RH", "GR");
            var first = new GridPosition(1, 1);
            var second = new GridPosition(1, 0);
            board.TrySwapAdjacent(first, second);
            boardView.Render(board);
            int callbacks = 0;
            var move = SwapValidator.Analyze(board, first, second);

            yield return new BoardResolutionRunner(board, boardView, FullPalette,
                new ScriptedRandom(0, 0, 1, 2, 3), 0.05f, 0.05f, new System.Random(2)).ResolveUntilStable(move, result =>
                {
                    callbacks++;
                    Assert.That(result.ClearedCount, Is.EqualTo(5));
                    Assert.That(result.CountOf(TileType.Green), Is.EqualTo(1));
                    Assert.That(result.CountOf(TileType.Red), Is.EqualTo(4));
                });

            Assert.That(callbacks, Is.EqualTo(1));
            Assert.That(ReadTiles(board), Is.EqualTo(ReadTiles(CreateBoard("GY", "FB"))));
            Assert.That(board.GetTile(new GridPosition(0, 0)), Is.EqualTo(TileType.Propeller),
                "The old second selection would wrongly put the cascade special in column 1.");
            AssertViewsMatchBoard(board);
        }

        [UnityTest]
        public IEnumerator Runner_OnlySpecialsRemoved_StillRefillsAndReportsAnEmptyColorResult()
        {
            var board = CreateBoard("HV");
            boardView.Render(board);
            var origin = new GridPosition(0, 0);
            var move = SwapValidator.Analyze(board, origin, new GridPosition(1, 0));
            int callbacks = 0;

            yield return new BoardResolutionRunner(board, boardView, FullPalette,
                new ScriptedRandom(1, 2), 0.05f, 0.05f, new System.Random(2)).ResolveUntilStable(move, result =>
                {
                    callbacks++;
                    Assert.That(result.ClearedTiles, Is.Empty);
                });

            Assert.That(callbacks, Is.EqualTo(1));
            Assert.That(board.GetTile(origin), Is.EqualTo(TileType.Green));
            Assert.That(board.GetTile(new GridPosition(1, 0)), Is.EqualTo(TileType.Blue));
            AssertViewsMatchBoard(board);
        }

        [UnityTest]
        public IEnumerator Runner_SameChainAndRefillSeed_ProduceIdenticalFinalBoardsAndColorFacts()
        {
            TileType[,] previousBoard = null;
            IReadOnlyList<TileType> previousColors = null;
            for (int run = 0; run < 2; run++)
            {
                var board = CreateBoard("RGB", "BGG", "HVT");
                boardView.Render(board);
                var move = SwapValidator.Analyze(board, new GridPosition(0, 0), new GridPosition(1, 0));
                ResolutionResult result = null;
                yield return new BoardResolutionRunner(board, boardView, FullPalette,
                    new System.Random(53), 0f, 0f, new System.Random(2)).ResolveUntilStable(move, r => result = r);

                Assert.That(result, Is.Not.Null);
                Assert.That(result.ClearedCount, Is.GreaterThanOrEqualTo(6));
                if (run > 0)
                {
                    Assert.That(ReadTiles(board), Is.EqualTo(previousBoard));
                    Assert.That(result.ClearedTiles, Is.EquivalentTo(previousColors));
                }
                previousBoard = ReadTiles(board);
                previousColors = result.ClearedTiles;
                AssertViewsMatchBoard(board);
            }
        }

        [Test]
        public void Runner_SharedRefillAndTargetingRandom_IsRejected()
        {
            var random = new System.Random(42);
            var error = Assert.Throws<ArgumentException>(() => new BoardResolutionRunner(
                CreateBoard("R"), boardView, FullPalette, random, 0f, 0f, random));
            Assert.That(error.ParamName, Is.EqualTo("targetingRandom"));
        }

        [UnityTest]
        public IEnumerator Runner_PropellerUsesSeparateTargetingStreamAndPreservesUntouchedView()
        {
            var board = CreateBoard("FRGB");
            boardView.Render(board);
            var survivor = boardView.GetTileView(new GridPosition(3, 0));
            int callbacks = 0;
            var origin = new GridPosition(0, 0);
            var move = SwapValidator.Analyze(board, origin, new GridPosition(1, 0));

            yield return new BoardResolutionRunner(board, boardView, FullPalette,
                new ScriptedRandom(0, 1, 3), 0.05f, 0.05f, new ScriptedRandom(0))
                .ResolveUntilStable(move, result =>
                {
                    callbacks++;
                    Assert.That(result.ClearedTiles, Is.EquivalentTo(new[] { TileType.Red, TileType.Green }));
                });

            Assert.That(callbacks, Is.EqualTo(1));
            Assert.That(ReadTiles(board), Is.EqualTo(ReadTiles(CreateBoard("RGYB"))));
            Assert.That(boardView.GetTileView(new GridPosition(3, 0)), Is.SameAs(survivor));
            AssertViewsMatchBoard(board);
        }

        [UnityTest]
        public IEnumerator Runner_LightBallExplicitColor_ReportsOnlyThoseNormalsAndKeepsOtherViews()
        {
            var board = CreateBoard("LRRGB");
            boardView.Render(board);
            var survivor = boardView.GetTileView(new GridPosition(3, 0));
            var origin = new GridPosition(0, 0);
            var move = SwapValidator.Analyze(board, origin, new GridPosition(1, 0));
            int callbacks = 0;

            yield return new BoardResolutionRunner(board, boardView, FullPalette,
                new ScriptedRandom(2, 3, 4), 0.05f, 0.05f, new System.Random(2)).ResolveUntilStable(move, result =>
                {
                    callbacks++;
                    Assert.That(result.ClearedTiles, Is.EquivalentTo(new[] { TileType.Red, TileType.Red }));
                });

            Assert.That(callbacks, Is.EqualTo(1));
            Assert.That(ReadTiles(board), Is.EqualTo(ReadTiles(CreateBoard("BYPGB"))));
            Assert.That(boardView.GetTileView(new GridPosition(3, 0)), Is.SameAs(survivor));
            AssertViewsMatchBoard(board);
        }

        [UnityTest]
        public IEnumerator Runner_MixedChain_ReportsExactDeduplicatedColorsAndRefillsEveryView()
        {
            var board = CreateBoard("RGB", "GFL", "HVT");
            boardView.Render(board);
            var origin = new GridPosition(0, 0);
            var move = SwapValidator.Analyze(board, origin, new GridPosition(1, 0));
            int callbacks = 0;

            yield return new BoardResolutionRunner(board, boardView, FullPalette,
                new ScriptedRandom(2, 1, 0, 3, 2, 1, 5, 3, 2), 0.05f, 0.05f, new ScriptedRandom())
                .ResolveUntilStable(move, result =>
                {
                    callbacks++;
                    Assert.That(result.ClearedTiles, Is.EquivalentTo(new[]
                        { TileType.Red, TileType.Green, TileType.Green, TileType.Blue }));
                });

            Assert.That(callbacks, Is.EqualTo(1));
            Assert.That(ReadTiles(board), Is.EqualTo(ReadTiles(CreateBoard("RGB", "GBY", "BYO"))));
            Assert.That(CountActiveViews(), Is.EqualTo(9));
            AssertViewsMatchBoard(board);
        }

        [UnityTest]
        public IEnumerator Runner_MixedChainWithBothFreshStreams_ReplaysFinalBoardAndColorFacts()
        {
            TileType[,] previousBoard = null;
            IReadOnlyList<TileType> previousColors = null;
            for (int run = 0; run < 2; run++)
            {
                var board = CreateBoard("RGBYOPGB", "HFVTLRGB");
                boardView.Render(board);
                var origin = new GridPosition(0, 0);
                var move = SwapValidator.Analyze(board, origin, new GridPosition(1, 0));
                int callbacks = 0;
                ResolutionResult result = null;
                yield return new BoardResolutionRunner(board, boardView, FullPalette,
                    new System.Random(53), 0f, 0f, new System.Random(53 ^ 0x5EED1234))
                    .ResolveUntilStable(move, r => { callbacks++; result = r; });

                Assert.That(callbacks, Is.EqualTo(1));
                Assert.That(result.ClearedCount, Is.GreaterThanOrEqualTo(9));
                Assert.That(MatchFinder.FindMatches(board), Is.Empty);
                Assert.That(ReadTiles(board), Has.No.Member(TileType.Empty));
                if (run > 0)
                {
                    Assert.That(ReadTiles(board), Is.EqualTo(previousBoard));
                    Assert.That(result.ClearedTiles, Is.EquivalentTo(previousColors));
                }
                previousBoard = ReadTiles(board);
                previousColors = result.ClearedTiles;
                AssertViewsMatchBoard(board);
            }
        }

        [UnityTest]
        public IEnumerator Runner_SameSeedTwice_ProducesTheSameResult()
        {
            var firstResult = new List<TileType>();
            var board = CreateBoard(
                "GBY",
                "GPO",
                "GRG");
            boardView.Render(board);
            yield return new BoardResolutionRunner(board, boardView, FullPalette, new System.Random(5), 0f, 0f, new System.Random(2))
                .ResolveUntilStable(r => firstResult.AddRange(r.ClearedTiles));
            var firstBoard = ReadTiles(board);

            var secondResult = new List<TileType>();
            board = CreateBoard(
                "GBY",
                "GPO",
                "GRG");
            boardView.Render(board);
            yield return new BoardResolutionRunner(board, boardView, FullPalette, new System.Random(5), 0f, 0f, new System.Random(2))
                .ResolveUntilStable(r => secondResult.AddRange(r.ClearedTiles));

            Assert.That(firstResult, Is.Not.Empty);
            Assert.That(secondResult, Is.EquivalentTo(firstResult));
            Assert.That(ReadTiles(board), Is.EqualTo(firstBoard));
        }

        // Runs the resolution and checks the result contract: the callback runs exactly once, only after the board
        // is stable (no matches, no Empty), the cleared types are exactly as expected (as a multiset, no Empty),
        // and the final board and views are the expected stable state.
        private IEnumerator RunAndCheckResult(
            Board board, System.Random refillRandom, TileType[] expectedCleared, params string[] finalRows)
        {
            boardView.Render(board);
            var runner = new BoardResolutionRunner(board, boardView, FullPalette, refillRandom, 0.05f, 0.05f, new System.Random(2));
            int callbacks = 0;
            ResolutionResult result = null;

            yield return runner.ResolveUntilStable(r =>
            {
                callbacks++;
                result = r;
                Assert.That(MatchFinder.FindMatches(board), Is.Empty, "Callback ran before the board was stable.");
                Assert.That(ReadTiles(board), Has.No.Member(TileType.Empty), "Callback ran before refill finished.");
            });
            // Let deferred Destroy calls finish before inspecting objects.
            yield return null;

            Assert.That(callbacks, Is.EqualTo(1));
            Assert.That(result.ClearedTiles, Is.EquivalentTo(expectedCleared));
            Assert.That(result.ClearedTiles, Has.No.Member(TileType.Empty));
            Assert.That(ReadTiles(board), Is.EqualTo(ReadTiles(CreateBoard(finalRows))));
            AssertViewsMatchBoard(board);
        }

        private static readonly TileType[] FullPalette =
        {
            TileType.Red, TileType.Green, TileType.Blue, TileType.Yellow, TileType.Purple, TileType.Orange
        };

        [Test]
        public void ZeroDuration_FallAndRefill_SettleInASingleStep()
        {
            var board = CreateBoard(
                ".",
                "R",
                ".");
            boardView.Render(board);
            boardView.RemoveViews(new[] { new GridPosition(0, 0), new GridPosition(0, 2) });
            var moves = BoardResolution.ApplyGravity(board);

            Assert.That(boardView.AnimateFalls(moves, 0f).MoveNext(), Is.False, "Zero-duration fall needs no frames.");
            var spawns = BoardResolution.Refill(board, Palette, new System.Random(3));
            Assert.That(boardView.AnimateRefill(spawns, 0f).MoveNext(), Is.False, "Zero-duration refill needs no frames.");

            AssertViewsMatchBoard(board);
        }

        [Test]
        public void AnimateFalls_InconsistentMoves_ThrowBeforeAnythingChanges()
        {
            var board = CreateBoard(
                "R",
                "G",
                "B");
            boardView.Render(board);
            boardView.RemoveViews(new[] { new GridPosition(0, 0) });
            var views = SnapshotViews(board, new[] { new GridPosition(0, 0) });

            // From has no view.
            Assert.Throws<ArgumentOutOfRangeException>(() => boardView.AnimateFalls(
                new[] { Move(TileType.Blue, 0, 0, 0, 1) }, 0f));
            // Type disagrees with the view at From.
            Assert.Throws<InvalidOperationException>(() => boardView.AnimateFalls(
                new[] { Move(TileType.Red, 0, 1, 0, 0) }, 0f));
            // Destination holds a view that is not moving.
            Assert.Throws<InvalidOperationException>(() => boardView.AnimateFalls(
                new[] { Move(TileType.Red, 0, 2, 0, 1) }, 0f));

            AssertSameViews(views);
        }

        [Test]
        public void AnimateRefill_OccupiedOrDuplicateTarget_ThrowsBeforeCreatingViews()
        {
            var board = CreateBoard("R.");
            boardView.Render(board);
            boardView.RemoveViews(new[] { new GridPosition(1, 0) });
            int viewCount = CountActiveViews();

            Assert.Throws<InvalidOperationException>(() => boardView.AnimateRefill(
                new[] { new TileSpawn(new GridPosition(0, 0), TileType.Green) }, 0f));
            Assert.Throws<ArgumentException>(() => boardView.AnimateRefill(
                new[]
                {
                    new TileSpawn(new GridPosition(1, 0), TileType.Green),
                    new TileSpawn(new GridPosition(1, 0), TileType.Blue)
                }, 0f));

            Assert.That(CountActiveViews(), Is.EqualTo(viewCount));
        }

        // Top row first: R/G/B/Y/P/O are colors, H/V/F/T/L are specials, '.' is Empty.
        private static Board CreateBoard(params string[] rows)
        {
            var board = new Board(rows[0].Length, rows.Length);
            for (int row = 0; row < rows.Length; row++)
            {
                int y = board.Height - 1 - row;
                for (int x = 0; x < board.Width; x++)
                {
                    board.SetTile(new GridPosition(x, y), ToTileType(rows[row][x]));
                }
            }

            return board;
        }

        private static TileType ToTileType(char symbol)
        {
            switch (symbol)
            {
                case '.': return TileType.Empty;
                case 'R': return TileType.Red;
                case 'G': return TileType.Green;
                case 'B': return TileType.Blue;
                case 'Y': return TileType.Yellow;
                case 'P': return TileType.Purple;
                case 'O': return TileType.Orange;
                case 'H': return TileType.HorizontalRocket;
                case 'V': return TileType.VerticalRocket;
                case 'F': return TileType.Propeller;
                case 'T': return TileType.TNT;
                case 'L': return TileType.LightBall;
                default: throw new ArgumentException($"Unknown tile symbol '{symbol}'.", nameof(symbol));
            }
        }

        private static TileMove Move(TileType type, int fromX, int fromY, int toX, int toY)
        {
            return new TileMove(new GridPosition(fromX, fromY), new GridPosition(toX, toY), type);
        }

        private Vector3 CellWorld(int x, int y)
        {
            return boardObject.transform.TransformPoint(new Vector3(x * Spacing, y * Spacing, 0f));
        }

        // Which view sits at each cell, skipping the given cells.
        private Dictionary<GridPosition, TileView> SnapshotViews(Board board, IEnumerable<GridPosition> skip)
        {
            var skipped = new HashSet<GridPosition>(skip);
            var views = new Dictionary<GridPosition, TileView>();
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    var position = new GridPosition(x, y);
                    if (!skipped.Contains(position))
                    {
                        views[position] = boardView.GetTileView(position);
                    }
                }
            }

            return views;
        }

        private void AssertSameViews(Dictionary<GridPosition, TileView> expected)
        {
            foreach (var entry in expected)
            {
                Assert.That(boardView.GetTileView(entry.Key), Is.SameAs(entry.Value),
                    $"A different view is now at ({entry.Key.X},{entry.Key.Y}).");
            }
        }

        // Every cell has a view that shows the Board's type, knows its cell, and sits at that cell's world position.
        private void AssertViewsMatchBoard(Board board)
        {
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    AssertViewAt(board, new GridPosition(x, y));
                }
            }
        }

        // The same, but for boards that still have Empty cells with no view (between stages).
        private void AssertViewsMatchBoardWhereShown(Board board)
        {
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    var position = new GridPosition(x, y);
                    if (board.GetTile(position) != TileType.Empty)
                    {
                        AssertViewAt(board, position);
                    }
                }
            }
        }

        private void AssertViewAt(Board board, GridPosition position)
        {
            var view = boardView.GetTileView(position);
            Assert.That(view.Type, Is.EqualTo(board.GetTile(position)), $"Type at ({position.X},{position.Y}).");
            Assert.That(view.Position, Is.EqualTo(position), $"Position of view at ({position.X},{position.Y}).");
            Assert.That(Vector3.Distance(view.transform.position, CellWorld(position.X, position.Y)), Is.LessThan(1e-4f),
                $"World position of view at ({position.X},{position.Y}).");
        }

        private TileView FindViewNotInLookup(Board board)
        {
            var known = new HashSet<TileView>();
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    try
                    {
                        known.Add(boardView.GetTileView(new GridPosition(x, y)));
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        // This cell has no view in the lookup right now.
                    }
                }
            }

            foreach (var view in boardObject.GetComponentsInChildren<TileView>())
            {
                if (!known.Contains(view))
                {
                    return view;
                }
            }

            return null;
        }

        private int CountActiveViews()
        {
            return boardObject.GetComponentsInChildren<TileView>().Length;
        }

        private void CheckNovaArea(GridPosition origin, int expectedPopped)
        {
            var board = new Board(7, 7);
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    var position = new GridPosition(x, y);
                    board.SetTile(position, position == origin ? TileType.TNT : TileType.Red);
                }
            }

            boardView.Render(board);
            var colors = SnapshotColors(board);
            var scales = SnapshotScales(board);
            var plan = SpecialEffects.Plan(board, new[] { new SpecialActivation(origin) });
            var expected = ViewsInNovaArea(board, origin);
            Assert.That(expected.Count, Is.EqualTo(expectedPopped));

            Assert.That(boardView.AnimateSpecialEffects(plan).MoveNext(), Is.True);
            AssertPoppedExactly(board, boardView.GetTileView(origin), expected, colors, scales);
        }

        // Core does give Wisp a wider footprint; until its flight feedback, only the special itself pulses.
        private void CheckOriginOnlyPulse(Board board, GridPosition origin)
        {
            boardView.Render(board);
            var colors = SnapshotColors(board);
            var scales = SnapshotScales(board);
            var plan = SpecialEffects.Plan(board, new[] { new SpecialActivation(origin) }, new System.Random(3));
            Assert.That(plan.Effects[0].Footprint.Count, Is.GreaterThan(1));

            Assert.That(boardView.AnimateSpecialEffects(plan).MoveNext(), Is.True);
            AssertPoppedExactly(board, boardView.GetTileView(origin), new TileView[0], colors, scales);
        }

        // An independent statement of the Nova Gem's area: visible views within two cells, the origin excluded.
        private List<TileView> ViewsInNovaArea(Board board, GridPosition origin)
        {
            var views = new List<TileView>();
            foreach (var view in AllViews(board))
            {
                if (view.Position != origin && view.Type != TileType.Empty
                    && Math.Abs(view.Position.X - origin.X) <= 2 && Math.Abs(view.Position.Y - origin.Y) <= 2)
                {
                    views.Add(view);
                }
            }

            return views;
        }

        private List<TileView> ViewsAt(params GridPosition[] positions)
        {
            var views = new List<TileView>();
            foreach (var position in positions)
            {
                views.Add(boardView.GetTileView(position));
            }

            return views;
        }

        // At full strength the origin is at 1.2x and the popped views at 1.1x their snapshot; every other view keeps
        // its size, and no view's color changes. With no origin and nothing popped, every view is back to its snapshot.
        private void AssertPoppedExactly(Board board, TileView origin, ICollection<TileView> popped,
            Dictionary<TileView, Color> colors, Dictionary<TileView, Vector3> scales)
        {
            AssertColorsUnchanged(board, colors);
            foreach (var view in AllViews(board))
            {
                bool isOrigin = origin != null && view == origin;
                float growth = isOrigin ? 1.2f : popped.Contains(view) ? 1.1f : 1f;
                Assert.That(Vector3.Distance(view.transform.localScale, scales[view] * growth), Is.LessThan(1e-5f),
                    $"Scale at ({view.Position.X},{view.Position.Y}).");
            }
        }

        private void AssertColorsUnchanged(Board board, Dictionary<TileView, Color> colors)
        {
            foreach (var view in AllViews(board))
            {
                AssertColor(view.GetComponent<SpriteRenderer>().color, colors[view], $"({view.Position.X},{view.Position.Y})");
            }
        }

        private static void AssertColor(Color actual, Color expected, string where)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(1e-5f), $"Red at {where}.");
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(1e-5f), $"Green at {where}.");
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(1e-5f), $"Blue at {where}.");
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(1e-5f), $"Alpha at {where}.");
        }

        // Only for freshly rendered boards, where every cell (Empty included) has a view.
        private List<TileView> AllViews(Board board)
        {
            var views = new List<TileView>();
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    views.Add(boardView.GetTileView(new GridPosition(x, y)));
                }
            }

            return views;
        }

        private Dictionary<TileView, Color> SnapshotColors(Board board)
        {
            var colors = new Dictionary<TileView, Color>();
            foreach (var view in AllViews(board))
            {
                colors.Add(view, view.GetComponent<SpriteRenderer>().color);
            }

            return colors;
        }

        private Dictionary<TileView, Vector3> SnapshotScales(Board board)
        {
            var scales = new Dictionary<TileView, Vector3>();
            foreach (var view in AllViews(board))
            {
                scales.Add(view, view.transform.localScale);
            }

            return scales;
        }

        private static TileType[,] ReadTiles(Board board)
        {
            var tiles = new TileType[board.Width, board.Height];
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    tiles[x, y] = board.GetTile(new GridPosition(x, y));
                }
            }

            return tiles;
        }
    }
}
