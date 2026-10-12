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
            controller.Initialize(board, boardView, inputCamera);
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

            Assert.Throws<ArgumentNullException>(() => controller.Initialize(null, boardView, inputCamera));
            Assert.Throws<ArgumentNullException>(() => controller.Initialize(board, null, inputCamera));
            Assert.Throws<ArgumentNullException>(() => controller.Initialize(board, boardView, null));

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
        public IEnumerator HandlePointerDown_HorizontalNeighbor_SwapsOnceAndLocksUntilAnimationFinishes()
        {
            UseBoardWithValidSwapsAtOrigin();
            yield return VerifySuccessfulSwap(new GridPosition(1, 0));
        }

        [UnityTest]
        public IEnumerator HandlePointerDown_VerticalNeighbor_SwapsOnceAndLocksUntilAnimationFinishes()
        {
            UseBoardWithValidSwapsAtOrigin();
            yield return VerifySuccessfulSwap(new GridPosition(0, 1));
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
        public IEnumerator Swap_InvalidThenValid_KeepsOnlyTheValidSwap()
        {
            SetBoard(
                "GBY",
                "GPO",
                "RGG");
            var expected = ReadTiles();
            var views = SnapshotViews();

            // Y and O exchange without forming a match: rolled back.
            yield return Swap(new GridPosition(2, 2), new GridPosition(2, 1));
            AssertSettled();
            Assert.That(ReadTiles(), Is.EqualTo(expected));
            Assert.That(SnapshotViews(), Is.EqualTo(views));

            // R and G exchange and complete the left column: kept, using the restored lookup.
            yield return Swap(new GridPosition(0, 0), new GridPosition(1, 0));
            AssertSettled();
            Exchange(expected, new GridPosition(0, 0), new GridPosition(1, 0));
            Exchange(views, new GridPosition(0, 0), new GridPosition(1, 0));
            Assert.That(ReadTiles(), Is.EqualTo(expected));
            Assert.That(SnapshotViews(), Is.EqualTo(views));
        }

        [UnityTest]
        public IEnumerator Swap_ValidThenInvalid_RollsBackToTheNewBoardState()
        {
            SetBoard(
                "GBY",
                "GPO",
                "RGG");

            yield return Swap(new GridPosition(0, 0), new GridPosition(1, 0));
            AssertSettled();
            // The kept swap is the new baseline; its G G G column stays because nothing clears yet.
            var afterValid = ReadTiles();
            var viewsAfterValid = SnapshotViews();

            // That leftover column is an unrelated match, so this swap must still roll back.
            yield return Swap(new GridPosition(2, 2), new GridPosition(2, 1));
            AssertSettled();
            Assert.That(ReadTiles(), Is.EqualTo(afterValid));
            Assert.That(SnapshotViews(), Is.EqualTo(viewsAfterValid));
        }

        [UnityTest]
        public IEnumerator Swap_OnlyAnUnrelatedMatchOnTheBoard_RollsBack()
        {
            SetBoard(
                "RRR",
                "GBY",
                "PGO");
            var original = ReadTiles();
            var views = SnapshotViews();

            // The top row already matches, but swapping G and O matches nothing involving them.
            yield return Swap(new GridPosition(1, 0), new GridPosition(2, 0));

            AssertSettled();
            Assert.That(ReadTiles(), Is.EqualTo(original));
            Assert.That(SnapshotViews(), Is.EqualTo(views));
        }

        [UnityTest]
        public IEnumerator Swap_CreatingAnIntersectionAtASwappedCell_IsKept()
        {
            // Moving the top R down to (1,2) completes both row y=2 and column x=1 (a T shape).
            SetBoard(
                "GRB",
                "RYR",
                "BRG",
                "PRO");
            var expected = ReadTiles();
            var views = SnapshotViews();

            yield return Swap(new GridPosition(1, 3), new GridPosition(1, 2));

            AssertSettled();
            Exchange(expected, new GridPosition(1, 3), new GridPosition(1, 2));
            Exchange(views, new GridPosition(1, 3), new GridPosition(1, 2));
            Assert.That(ReadTiles(), Is.EqualTo(expected));
            Assert.That(SnapshotViews(), Is.EqualTo(views));
        }

        private IEnumerator VerifySuccessfulSwap(GridPosition second)
        {
            var first = new GridPosition(0, 0);
            var expected = ReadTiles();
            TileType originalFirst = expected[first.X, first.Y];
            expected[first.X, first.Y] = expected[second.X, second.Y];
            expected[second.X, second.Y] = originalFirst;
            var firstView = boardView.GetTileView(first);
            var secondView = boardView.GetTileView(second);
            Vector3 firstStart = firstView.transform.position;
            Vector3 secondStart = secondView.transform.position;
            Vector2 rapidClick = ScreenPosition(new GridPosition(2, 2));
            Click(first);

            Click(second);

            Assert.That(controller.IsInputLocked, Is.True);
            Assert.That(controller.SelectedPosition, Is.EqualTo(first));
            Assert.That(ReadTiles(), Is.EqualTo(expected));
            Assert.Throws<InvalidOperationException>(() => controller.Initialize(board, boardView, inputCamera));

            // Additional pointer calls are rejected both immediately and on later animation frames.
            for (int i = 0; i < 10; i++)
            {
                controller.HandlePointerDown(rapidClick);
            }

            float deadline = Time.realtimeSinceStartup + 5f;
            while (controller.IsInputLocked)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Swap did not finish.");
                controller.HandlePointerDown(rapidClick);
                Assert.That(controller.SelectedPosition, Is.EqualTo(first));
                Assert.That(ReadTiles(), Is.EqualTo(expected));
                yield return null;
            }

            Assert.That(controller.SelectedPosition, Is.Null);
            Assert.That(ReadTiles(), Is.EqualTo(expected));
            Assert.That(boardView.GetTileView(first), Is.SameAs(secondView));
            Assert.That(boardView.GetTileView(second), Is.SameAs(firstView));
            Assert.That(firstView.transform.position, Is.EqualTo(secondStart));
            Assert.That(secondView.transform.position, Is.EqualTo(firstStart));
            AssertViewsMatchBoard();

            // A new click after completion starts a fresh selection, rather than another swap.
            Click(first);
            Assert.That(controller.SelectedPosition, Is.EqualTo(first));
            Assert.That(controller.IsInputLocked, Is.False);
            Assert.That(ReadTiles(), Is.EqualTo(expected));
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

        // Rewrites the 3x3 board so that swapping (0,0) with (1,0) or with (0,1) creates a match:
        //   G B Y
        //   G P O
        //   R G G
        // Horizontal swap completes the left column (G G G); vertical swap completes the bottom row.
        private void UseBoardWithValidSwapsAtOrigin()
        {
            SetBoard(
                "GBY",
                "GPO",
                "RGG");
        }

        // Replaces the board with one built from text (rows[0] is the top row) and re-wires the controller.
        private void SetBoard(params string[] rows)
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
            controller.Initialize(board, boardView, inputCamera);
            Physics2D.SyncTransforms();
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
