using System;
using System.Collections;
using System.Reflection;
using Match3.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Match3.Presentation.Tests
{
    public class BoardInputControllerTests
    {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
        // Short but non-zero, so every resolution stage lasts at least one observable frame.
        private const float ResolutionDuration = 0.05f;
        // Palette indices for ScriptedRandom: R=0 G=1 B=2 Y=3 P=4 O=5.
        private static readonly TileType[] Palette =
        {
            TileType.Red, TileType.Green, TileType.Blue,
            TileType.Yellow, TileType.Purple, TileType.Orange
        };

        private GameObject boardObject;
        private GameObject tileTemplate;
        private GameObject cameraObject;
        private GameObject foreignObject;
        private Board board;
        private BoardView boardView;
        private BoardInputController controller;
        private Camera inputCamera;
        private Mouse testMouse;

        [SetUp]
        public void SetUp()
        {
            tileTemplate = new GameObject("InputTestTileTemplate");
            tileTemplate.SetActive(false);
            var template = tileTemplate.AddComponent<TileView>();
            // Sprite-free test tiles need an explicit size; Unity otherwise autosizes this collider to zero.
            tileTemplate.AddComponent<BoxCollider2D>().size = Vector2.one;

            // Keep test colliders away from any objects in the project's scene.
            boardObject = new GameObject("InputTestBoard");
            boardObject.transform.position = new Vector3(100f, 100f, 0f);
            boardView = boardObject.AddComponent<BoardView>();
            typeof(BoardView).GetField("tilePrefab", Fields).SetValue(boardView, template);
            typeof(BoardView).GetField("tileSpacing", Fields).SetValue(boardView, 1.5f);

            cameraObject = new GameObject("InputTestCamera");
            cameraObject.transform.position = new Vector3(101f, 101f, -10f);
            inputCamera = cameraObject.AddComponent<Camera>();
            inputCamera.orthographic = true;
            inputCamera.orthographicSize = 4f;

            board = new Board(3, 3);
            var types = new[]
            {
                TileType.Red, TileType.Green, TileType.Blue,
                TileType.Yellow, TileType.Purple, TileType.Orange
            };
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    board.SetTile(new GridPosition(x, y), types[(x + y * board.Width) % types.Length]);
                }
            }

            boardView.Render(board);
            controller = boardObject.AddComponent<BoardInputController>();
            controller.Initialize(board, boardView, inputCamera, CreateRunner(new System.Random(1)));
            Physics2D.SyncTransforms();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (testMouse != null)
            {
                InputSystem.RemoveDevice(testMouse);
            }

            Object.Destroy(boardObject);
            Object.Destroy(tileTemplate);
            Object.Destroy(cameraObject);
            if (foreignObject != null)
            {
                Object.Destroy(foreignObject);
            }

            yield return null;
        }

        [Test]
        public void HandlePointerDown_FirstClick_PicksTransformedTileWithoutChangingBoard()
        {
            boardObject.transform.rotation = Quaternion.Euler(0f, 0f, 15f);
            boardObject.transform.localScale = new Vector3(1.2f, 0.8f, 1f);
            var position = new GridPosition(1, 1);
            var before = ReadTiles();

            Click(position);

            Assert.That(controller.SelectedPosition, Is.EqualTo(position));
            Assert.That(controller.IsInputLocked, Is.False);
            Assert.That(ReadTiles(), Is.EqualTo(before));
        }

        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(2, 0)]
        [TestCase(0, 2)]
        public void HandlePointerDown_InvalidSecondSelection_KeepsFirstSelectionAndBoard(int x, int y)
        {
            var first = new GridPosition(0, 0);
            var before = ReadTiles();
            Click(first);

            Click(new GridPosition(x, y));

            Assert.That(controller.SelectedPosition, Is.EqualTo(first));
            Assert.That(controller.IsInputLocked, Is.False);
            Assert.That(ReadTiles(), Is.EqualTo(before));
            AssertViewsMatchBoard();
        }

        [Test]
        public void HandlePointerDown_OutsideBoard_LeavesSelectionIntact()
        {
            var first = new GridPosition(0, 0);
            Click(first);

            controller.HandlePointerDown(inputCamera.WorldToScreenPoint(new Vector3(110f, 110f, 0f)));

            Assert.That(controller.SelectedPosition, Is.EqualTo(first));
            Assert.That(controller.IsInputLocked, Is.False);
        }

        [Test]
        public void HandlePointerDown_EmptyCell_IsIgnored()
        {
            var empty = new GridPosition(1, 0);
            board.SetTile(empty, TileType.Empty);
            boardView.GetTileView(empty).SetTileType(TileType.Empty);

            Click(empty);
            Assert.That(controller.SelectedPosition, Is.Null);

            var first = new GridPosition(0, 0);
            Click(first);
            Click(empty);

            Assert.That(controller.SelectedPosition, Is.EqualTo(first));
            Assert.That(controller.IsInputLocked, Is.False);
            Assert.That(board.GetTile(empty), Is.EqualTo(TileType.Empty));
        }

        [Test]
        public void HandlePointerDown_ForeignTileWithValidAddress_IsIgnored()
        {
            foreignObject = new GameObject("ForeignTile");
            foreignObject.transform.position = new Vector3(98f, 100f, 0f);
            var foreignView = foreignObject.AddComponent<TileView>();
            foreignView.Initialize(new GridPosition(0, 0), TileType.Red);
            foreignObject.AddComponent<BoxCollider2D>().size = Vector2.one;
            Physics2D.SyncTransforms();

            controller.HandlePointerDown(inputCamera.WorldToScreenPoint(foreignObject.transform.position));

            Assert.That(controller.SelectedPosition, Is.Null);
            Assert.That(controller.IsInputLocked, Is.False);
        }

        [Test]
        public void HandlePointerDown_DisabledOrUninitializedController_IgnoresInput()
        {
            var first = new GridPosition(0, 0);
            var before = ReadTiles();
            Click(first);
            controller.enabled = false;

            Click(new GridPosition(1, 0));
            var uninitialized = boardObject.AddComponent<BoardInputController>();
            uninitialized.HandlePointerDown(ScreenPosition(first));

            Assert.That(controller.SelectedPosition, Is.EqualTo(first));
            Assert.That(uninitialized.SelectedPosition, Is.Null);
            Assert.That(ReadTiles(), Is.EqualTo(before));
        }

        [Test]
        public void Initialize_NullDependency_ThrowsWithoutLosingExistingSelection()
        {
            var first = new GridPosition(0, 0);
            Click(first);

            var runner = CreateRunner(new System.Random(1));
            Assert.Throws<ArgumentNullException>(() => controller.Initialize(null, boardView, inputCamera, runner));
            Assert.Throws<ArgumentNullException>(() => controller.Initialize(board, null, inputCamera, runner));
            Assert.Throws<ArgumentNullException>(() => controller.Initialize(board, boardView, null, runner));
            Assert.Throws<ArgumentNullException>(() => controller.Initialize(board, boardView, inputCamera, null));

            Assert.That(controller.SelectedPosition, Is.EqualTo(first));
        }

        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void HandlePointerDown_InvalidDuration_ThrowsBeforeChangingBoard(float duration)
        {
            var first = new GridPosition(0, 0);
            var before = ReadTiles();
            Click(first);
            typeof(BoardInputController).GetField("swapDuration", Fields).SetValue(controller, duration);

            Assert.Throws<InvalidOperationException>(() => Click(new GridPosition(1, 0)));

            Assert.That(controller.SelectedPosition, Is.EqualTo(first));
            Assert.That(controller.IsInputLocked, Is.False);
            Assert.That(ReadTiles(), Is.EqualTo(before));
        }

        [UnityTest]
        public IEnumerator HandlePointerDown_HorizontalValidSwap_ResolvesOneRoundThenUnlocks()
        {
            // The swap completes the left column G G G; it is cleared and refilled with B, Y, O (no new match).
            SetBoard(new ScriptedRandom(2, 3, 5),
                "GBY",
                "GPO",
                "RGG");

            yield return VerifyValidMove(new GridPosition(0, 0), new GridPosition(1, 0), 1,
                "OBY",
                "YPO",
                "BRG");
        }

        [UnityTest]
        public IEnumerator HandlePointerDown_VerticalValidSwap_ResolvesOneRoundThenUnlocks()
        {
            // The swap completes the bottom row G G G; the rows above fall and the top row refills with B, Y, R.
            SetBoard(new ScriptedRandom(2, 3, 0),
                "GBY",
                "GPO",
                "RGG");

            yield return VerifyValidMove(new GridPosition(0, 0), new GridPosition(0, 1), 1,
                "BYR",
                "GBY",
                "RPO");
        }

        [UnityTest]
        public IEnumerator Resolve_RefillCreatesAMatch_RunsASecondRoundBeforeUnlocking()
        {
            // Round 1 refills the left column with B B B, which is itself a match; round 2 refills B, Y, O.
            SetBoard(new ScriptedRandom(2, 2, 2, 2, 3, 5),
                "GBY",
                "GPO",
                "RGG");

            yield return VerifyValidMove(new GridPosition(0, 0), new GridPosition(1, 0), 2,
                "OBY",
                "YPO",
                "BRG");
        }

        [UnityTest]
        public IEnumerator Resolve_SeveralCascades_AllFinishBeforeUnlocking()
        {
            // Refills B B B, then Y Y Y (both matches), then B, Y, O: three rounds, clicks ignored throughout.
            SetBoard(new ScriptedRandom(2, 2, 2, 3, 3, 3, 2, 3, 5),
                "GBY",
                "GPO",
                "RGG");

            yield return VerifyValidMove(new GridPosition(0, 0), new GridPosition(1, 0), 3,
                "OBY",
                "YPO",
                "BRG");
        }

        [UnityTest]
        public IEnumerator HandlePointerDown_SwapWithoutMatch_AnimatesForwardThenRollsBack()
        {
            // The default board has no match after swapping (0,0) and (1,0), so this move is invalid.
            var first = new GridPosition(0, 0);
            var second = new GridPosition(1, 0);
            var original = ReadTiles();
            var swapped = ReadTiles();
            swapped[first.X, first.Y] = original[second.X, second.Y];
            swapped[second.X, second.Y] = original[first.X, first.Y];
            var firstView = boardView.GetTileView(first);
            var secondView = boardView.GetTileView(second);
            Vector3 firstStart = firstView.transform.position;
            Vector3 secondStart = secondView.transform.position;
            Vector2 rapidClick = ScreenPosition(new GridPosition(2, 2));
            Click(first);

            Click(second);

            // The Board changes first, before any movement.
            Assert.That(controller.IsInputLocked, Is.True);
            Assert.That(ReadTiles(), Is.EqualTo(swapped));

            bool sawRollbackPhase = false;
            float deadline = Time.realtimeSinceStartup + 5f;
            while (controller.IsInputLocked)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Rollback did not finish.");

                // Clicks during either animation must not select, swap, or unlock anything.
                controller.HandlePointerDown(rapidClick);
                Assert.That(controller.SelectedPosition, Is.EqualTo(first));

                var tiles = ReadTiles();
                bool isSwapped = AreEqual(tiles, swapped);
                Assert.That(isSwapped || AreEqual(tiles, original), Is.True, "Board must be either swapped or restored.");

                if (!isSwapped)
                {
                    // Restored Board while still locked: the forward animation has finished
                    // (its lookup update happened), and the backward animation is still running.
                    sawRollbackPhase = true;
                    Assert.That(boardView.GetTileView(first), Is.SameAs(secondView));
                }

                yield return null;
            }

            Assert.That(sawRollbackPhase, Is.True, "Input unlocked before the rollback animation ran.");
            Assert.That(controller.SelectedPosition, Is.Null);
            Assert.That(ReadTiles(), Is.EqualTo(original));
            Assert.That(boardView.GetTileView(first), Is.SameAs(firstView));
            Assert.That(boardView.GetTileView(second), Is.SameAs(secondView));
            Assert.That(firstView.Position, Is.EqualTo(first));
            Assert.That(secondView.Position, Is.EqualTo(second));
            Assert.That(firstView.transform.position, Is.EqualTo(firstStart));
            Assert.That(secondView.transform.position, Is.EqualTo(secondStart));
            AssertViewsMatchBoard();
        }

        [UnityTest]
        public IEnumerator Update_MousePress_UsesPointerHandlingAndDoesNotSwapOnHeldButton()
        {
            testMouse = InputSystem.AddDevice<Mouse>();
            var first = new GridPosition(0, 0);
            var second = new GridPosition(1, 0);
            var before = ReadTiles();

            SendMouseState(ScreenPosition(first), 1);
            Assert.That(controller.SelectedPosition, Is.EqualTo(first));

            SendMouseState(ScreenPosition(second), 1);
            Assert.That(controller.SelectedPosition, Is.EqualTo(first));
            Assert.That(controller.IsInputLocked, Is.False);
            Assert.That(ReadTiles(), Is.EqualTo(before));

            SendMouseState(ScreenPosition(second), 0);
            SendMouseState(ScreenPosition(second), 1);
            Assert.That(controller.IsInputLocked, Is.True);
            Assert.That(board.GetTile(first), Is.EqualTo(before[second.X, second.Y]));
            Assert.That(board.GetTile(second), Is.EqualTo(before[first.X, first.Y]));

            yield return WaitForSwap();

            Assert.That(controller.SelectedPosition, Is.Null);
            AssertViewsMatchBoard();
        }

        [UnityTest]
        public IEnumerator Swap_RepeatedInvalidAttempt_BehavesExactlyLikeTheFirst()
        {
            // Default board: swapping (0,0) and (1,0) creates no match.
            var first = new GridPosition(0, 0);
            var second = new GridPosition(1, 0);
            var originalTiles = ReadTiles();
            var originalViews = SnapshotViews();

            yield return Swap(first, second);
            AssertSettled();
            Assert.That(ReadTiles(), Is.EqualTo(originalTiles));
            Assert.That(SnapshotViews(), Is.EqualTo(originalViews));

            yield return Swap(first, second);
            AssertSettled();
            Assert.That(ReadTiles(), Is.EqualTo(originalTiles));
            Assert.That(SnapshotViews(), Is.EqualTo(originalViews));
        }

        [UnityTest]
        public IEnumerator Swap_InvalidThenValid_RollsBackThenResolves()
        {
            SetBoard(new ScriptedRandom(2, 3, 5),
                "GBY",
                "GPO",
                "RGG");
            var expected = ReadTiles();
            var views = SnapshotViews();

            // Y and O exchange without forming a match: rolled back, and nothing is resolved.
            yield return Swap(new GridPosition(2, 2), new GridPosition(2, 1));
            AssertSettled();
            Assert.That(ReadTiles(), Is.EqualTo(expected));
            Assert.That(SnapshotViews(), Is.EqualTo(views));

            // R and G exchange and complete the left column: resolved, using the restored lookup.
            var red = boardView.GetTileView(new GridPosition(0, 0));
            yield return Swap(new GridPosition(0, 0), new GridPosition(1, 0));
            AssertStable();
            Assert.That(ReadTiles(), Is.EqualTo(Tiles(
                "OBY",
                "YPO",
                "BRG")));
            // The swapped red survived (only the left column was cleared), still the same object.
            Assert.That(boardView.GetTileView(new GridPosition(1, 0)), Is.SameAs(red));
            Assert.That(boardView.GetTileView(new GridPosition(2, 2)), Is.SameAs(views[2, 2]));
        }

        [UnityTest]
        public IEnumerator Swap_ValidThenInvalid_RollsBackToTheResolvedBoard()
        {
            SetBoard(new ScriptedRandom(2, 3, 5),
                "GBY",
                "GPO",
                "RGG");

            yield return Swap(new GridPosition(0, 0), new GridPosition(1, 0));
            AssertStable();
            // The resolved board is the new baseline, not the original layout.
            var afterValid = ReadTiles();
            var viewsAfterValid = SnapshotViews();

            yield return Swap(new GridPosition(2, 2), new GridPosition(2, 1));
            AssertStable();
            Assert.That(ReadTiles(), Is.EqualTo(afterValid));
            Assert.That(SnapshotViews(), Is.EqualTo(viewsAfterValid));
        }

        [UnityTest]
        public IEnumerator Swap_OnlyAnUnrelatedMatchOnTheBoard_RollsBack()
        {
            SetBoard(new System.Random(1),
                "RRR",
                "GBY",
                "PGO");
            var original = ReadTiles();
            var views = SnapshotViews();

            // The top row already matches, but swapping G and O matches nothing involving them.
            // An invalid move must only roll back: the unrelated R R R is not resolved either.
            yield return Swap(new GridPosition(1, 0), new GridPosition(2, 0));

            AssertSettled();
            Assert.That(ReadTiles(), Is.EqualTo(original));
            Assert.That(SnapshotViews(), Is.EqualTo(views));
        }

        [UnityTest]
        public IEnumerator Swap_CreatingAnIntersectionAtASwappedCell_IsResolved()
        {
            // Moving the top R down to (1,2) completes row y=2 and column x=1 (a T shape): 5 cells clear.
            SetBoard(new ScriptedRandom(0, 0, 4, 5, 3),
                "GRB",
                "RYR",
                "BRG",
                "PRO");

            yield return VerifyValidMove(new GridPosition(1, 3), new GridPosition(1, 2), 1,
                "ROY",
                "GPB",
                "BRG",
                "PYO");
        }

        // Plays one valid move and follows it to the end. Checks that the Board changes first, that input stays
        // locked and clicks are ignored through every stage, that exactly `expectedRounds` resolution rounds
        // happen (each round empties cells, then refills them), and the exact final stable board.
        private IEnumerator VerifyValidMove(
            GridPosition first, GridPosition second, int expectedRounds, params string[] finalRows)
        {
            var swapped = ReadTiles();
            Exchange(swapped, first, second);
            Vector2 rapidClick = ScreenPosition(new GridPosition(0, board.Height - 1));
            Click(first);

            Click(second);

            Assert.That(controller.IsInputLocked, Is.True);
            Assert.That(controller.SelectedPosition, Is.EqualTo(first));
            Assert.That(ReadTiles(), Is.EqualTo(swapped), "The Board swaps before anything animates.");
            Assert.Throws<InvalidOperationException>(
                () => controller.Initialize(board, boardView, inputCamera, CreateRunner(new System.Random(1))));

            int rounds = 0;
            bool hadEmpty = false;
            float deadline = Time.realtimeSinceStartup + 10f;
            while (controller.IsInputLocked)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Resolution did not finish.");

                // Clicks during the swap, clear, fall, refill and between cascade rounds must all be ignored.
                controller.HandlePointerDown(rapidClick);
                Assert.That(controller.SelectedPosition, Is.EqualTo(first));
                Assert.That(controller.IsInputLocked, Is.True);

                // A round starts when cleared (Empty) cells appear and ends when refill fills them again.
                bool hasEmpty = BoardHasEmpty();
                if (hasEmpty && !hadEmpty)
                {
                    rounds++;
                }

                hadEmpty = hasEmpty;
                yield return null;
            }

            Assert.That(rounds, Is.EqualTo(expectedRounds), "Resolution rounds before unlock.");
            AssertStable();
            Assert.That(ReadTiles(), Is.EqualTo(Tiles(finalRows)));

            // A new click after completion starts a fresh selection, rather than another swap.
            Click(first);
            Assert.That(controller.SelectedPosition, Is.EqualTo(first));
            Assert.That(controller.IsInputLocked, Is.False);
        }

        private IEnumerator WaitForSwap()
        {
            float deadline = Time.realtimeSinceStartup + 5f;
            while (controller.IsInputLocked)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Swap did not finish.");
                yield return null;
            }
        }

        private void Click(GridPosition position)
        {
            Physics2D.SyncTransforms();
            controller.HandlePointerDown(ScreenPosition(position));
        }

        private Vector2 ScreenPosition(GridPosition position)
        {
            return inputCamera.WorldToScreenPoint(boardView.GetTileView(position).transform.position);
        }

        private void SendMouseState(Vector2 position, ushort buttons)
        {
            InputSystem.QueueStateEvent(testMouse, new MouseState { position = position, buttons = buttons });
            InputSystem.Update();
            // Exercise the actual mouse poll deterministically after processing the simulated event.
            typeof(BoardInputController).GetMethod("Update", Fields).Invoke(controller, null);
        }

        // Replaces the board with one built from text (rows[0] is the top row) and re-wires the controller.
        // `refillRandom` decides which tiles refill creates, so resolution tests are fully deterministic.
        private void SetBoard(System.Random refillRandom, params string[] rows)
        {
            board = new Board(rows[0].Length, rows.Length);
            for (int row = 0; row < rows.Length; row++)
            {
                int y = board.Height - 1 - row;
                for (int x = 0; x < board.Width; x++)
                {
                    board.SetTile(new GridPosition(x, y), ToTileType(rows[row][x]));
                }
            }

            boardView.Render(board);
            controller.Initialize(board, boardView, inputCamera, CreateRunner(refillRandom));
            Physics2D.SyncTransforms();
        }

        private BoardResolutionRunner CreateRunner(System.Random refillRandom)
        {
            return new BoardResolutionRunner(board, boardView, Palette, refillRandom, ResolutionDuration, ResolutionDuration);
        }

        // The tile layout described by text rows (rows[0] is the top row), for comparing with ReadTiles().
        private static TileType[,] Tiles(params string[] rows)
        {
            var tiles = new TileType[rows[0].Length, rows.Length];
            for (int row = 0; row < rows.Length; row++)
            {
                for (int x = 0; x < rows[row].Length; x++)
                {
                    tiles[x, rows.Length - 1 - row] = ToTileType(rows[row][x]);
                }
            }

            return tiles;
        }

        private bool BoardHasEmpty()
        {
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    if (board.GetTile(new GridPosition(x, y)) == TileType.Empty)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // The Phase 7 stable contract: settled (unlocked, no selection, views agree), no matches, no Empty cells.
        private void AssertStable()
        {
            AssertSettled();
            Assert.That(MatchFinder.FindMatches(board), Is.Empty, "A stable board has no matches.");
            Assert.That(BoardHasEmpty(), Is.False, "A stable board has no Empty cells.");
        }

        // A Random whose results are scripted palette indices, so refills (and cascades) are chosen by the test.
        private sealed class ScriptedRandom : System.Random
        {
            private readonly System.Collections.Generic.Queue<int> values;

            public ScriptedRandom(params int[] values)
            {
                this.values = new System.Collections.Generic.Queue<int>(values);
            }

            public override int Next()
            {
                return NextScripted(int.MaxValue);
            }

            public override int Next(int maxValue)
            {
                return NextScripted(maxValue);
            }

            public override int Next(int minValue, int maxValue)
            {
                return minValue + NextScripted(maxValue - minValue);
            }

            private int NextScripted(int range)
            {
                Assert.That(values.Count, Is.GreaterThan(0), "ScriptedRandom ran out of scripted values.");
                int value = values.Dequeue();
                Assert.That(value, Is.InRange(0, range - 1), "Scripted value outside the requested range.");
                return value;
            }
        }

        private static TileType ToTileType(char symbol)
        {
            switch (symbol)
            {
                case 'R': return TileType.Red;
                case 'G': return TileType.Green;
                case 'B': return TileType.Blue;
                case 'Y': return TileType.Yellow;
                case 'P': return TileType.Purple;
                case 'O': return TileType.Orange;
                default: throw new ArgumentException($"Unknown tile symbol '{symbol}'.", nameof(symbol));
            }
        }

        // One complete player move: select, select, then wait until input unlocks.
        private IEnumerator Swap(GridPosition first, GridPosition second)
        {
            Click(first);
            Click(second);
            Assert.That(controller.IsInputLocked, Is.True, "An adjacent second click should start a move.");
            yield return WaitForSwap();
        }

        // After a move: unlocked, nothing selected, and every view agrees with the Board,
        // including its lookup entry, represented cell, and on-screen position.
        private void AssertSettled()
        {
            Assert.That(controller.IsInputLocked, Is.False);
            Assert.That(controller.SelectedPosition, Is.Null);
            AssertViewsMatchBoard();
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    var view = boardView.GetTileView(new GridPosition(x, y));
                    Vector3 expected = boardObject.transform.TransformPoint(new Vector3(x * 1.5f, y * 1.5f, 0f));
                    Assert.That(Vector3.Distance(view.transform.position, expected), Is.LessThan(1e-4f));
                }
            }
        }

        // Which view object sits at each cell, so tests can check that the same objects end up in the same places.
        private TileView[,] SnapshotViews()
        {
            var views = new TileView[board.Width, board.Height];
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    views[x, y] = boardView.GetTileView(new GridPosition(x, y));
                }
            }

            return views;
        }

        private static void Exchange<T>(T[,] cells, GridPosition first, GridPosition second)
        {
            T temporary = cells[first.X, first.Y];
            cells[first.X, first.Y] = cells[second.X, second.Y];
            cells[second.X, second.Y] = temporary;
        }

        private static bool AreEqual(TileType[,] first, TileType[,] second)
        {
            for (int y = 0; y < first.GetLength(1); y++)
            {
                for (int x = 0; x < first.GetLength(0); x++)
                {
                    if (first[x, y] != second[x, y])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private TileType[,] ReadTiles()
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

        private void AssertViewsMatchBoard()
        {
            foreach (var view in boardObject.GetComponentsInChildren<TileView>(true))
            {
                Assert.That(boardView.GetTileView(view.Position), Is.SameAs(view));
                Assert.That(view.Type, Is.EqualTo(board.GetTile(view.Position)));
            }
        }
    }
}
