using System;
using System.Collections;
using System.Collections.Generic;
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
        // Every result the controller reported, in order: one per finished valid move.
        private List<ResolutionResult> reportedResults;
        // What the controller calls after a valid move; level tests switch it to ApplyToLevel via UseLevel.
        private Action<ResolutionResult> onValidMoveResolved;
        private LevelProgress level;
        // Every busy notification in order: "busy", "free", or "free-early" if the controller was not yet settled.
        private List<string> busyChanges;

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
            reportedResults = new List<ResolutionResult>();
            busyChanges = new List<string>();
            onValidMoveResolved = reportedResults.Add;
            level = null;
            controller = boardObject.AddComponent<BoardInputController>();
            controller.Initialize(board, boardView, inputCamera, CreateRunner(new System.Random(1)), onValidMoveResolved, RecordBusy);
            Physics2D.SyncTransforms();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
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
            Assert.That(busyChanges, Is.Empty, "A rejected click is not a move.");
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
            Action<ResolutionResult> report = reportedResults.Add;
            Action<bool> busy = RecordBusy;
            Assert.Throws<ArgumentNullException>(() => controller.Initialize(null, boardView, inputCamera, runner, report, busy));
            Assert.Throws<ArgumentNullException>(() => controller.Initialize(board, null, inputCamera, runner, report, busy));
            Assert.Throws<ArgumentNullException>(() => controller.Initialize(board, boardView, null, runner, report, busy));
            Assert.Throws<ArgumentNullException>(() => controller.Initialize(board, boardView, inputCamera, null, report, busy));
            Assert.Throws<ArgumentNullException>(() => controller.Initialize(board, boardView, inputCamera, runner, null, busy));
            Assert.Throws<ArgumentNullException>(() => controller.Initialize(board, boardView, inputCamera, runner, report, null));

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

            // The whole left column clears from the top, so no surviving tile falls.
            yield return VerifyValidMove(new GridPosition(0, 0), new GridPosition(1, 0), 1, false,
                "OBY",
                "YPO",
                "BRG");

            // A new click after completion starts a fresh selection, rather than another swap.
            Click(new GridPosition(0, 0));
            Assert.That(controller.SelectedPosition, Is.EqualTo(new GridPosition(0, 0)));
            Assert.That(controller.IsInputLocked, Is.False);
        }

        [UnityTest]
        public IEnumerator HandlePointerDown_VerticalValidSwap_ResolvesOneRoundThenUnlocks()
        {
            // The swap completes the bottom row G G G; the rows above fall and the top row refills with B, Y, R.
            SetBoard(new ScriptedRandom(2, 3, 0),
                "GBY",
                "GPO",
                "RGG");

            yield return VerifyValidMove(new GridPosition(0, 0), new GridPosition(0, 1), 1, true,
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

            yield return VerifyValidMove(new GridPosition(0, 0), new GridPosition(1, 0), 2, false,
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

            yield return VerifyValidMove(new GridPosition(0, 0), new GridPosition(1, 0), 3, false,
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
            Assert.That(reportedResults, Is.Empty, "An invalid move is never reported.");
            Assert.That(busyChanges, Is.EqualTo(new[] { "busy", "free" }), "An invalid move is still busy until rolled back.");
        }

        [UnityTest]
        public IEnumerator Update_MousePress_UsesPointerHandlingAndDoesNotSwapOnHeldButton()
        {
            // Isolated Input System for this test only: no real devices, and input is delivered regardless of
            // Editor/Game view focus. Without it, an unfocused Editor drops the simulated mouse events.
            // TearDown restores the real Input System state, so project settings are never changed.
            var input = new InputTestFixture();
            input.Setup();
            try
            {
                var mouse = InputSystem.AddDevice<Mouse>();
                var first = new GridPosition(0, 0);
                var second = new GridPosition(1, 0);
                var before = ReadTiles();

                SendMouseState(mouse, ScreenPosition(first), 1);
                Assert.That(controller.SelectedPosition, Is.EqualTo(first));

                SendMouseState(mouse, ScreenPosition(second), 1);
                Assert.That(controller.SelectedPosition, Is.EqualTo(first));
                Assert.That(controller.IsInputLocked, Is.False);
                Assert.That(ReadTiles(), Is.EqualTo(before));

                SendMouseState(mouse, ScreenPosition(second), 0);
                SendMouseState(mouse, ScreenPosition(second), 1);
                Assert.That(controller.IsInputLocked, Is.True);
                Assert.That(board.GetTile(first), Is.EqualTo(before[second.X, second.Y]));
                Assert.That(board.GetTile(second), Is.EqualTo(before[first.X, first.Y]));

                yield return WaitForSwap();

                Assert.That(controller.SelectedPosition, Is.Null);
                AssertViewsMatchBoard();
            }
            finally
            {
                input.TearDown();
            }
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
            Assert.That(reportedResults, Is.Empty, "An invalid move is never reported.");
        }

        [UnityTest]
        public IEnumerator Swap_CreatingAnIntersectionAtASwappedCell_IsResolved()
        {
            // Five Reds count; TNT appears at the selected (1,2), then falls to (1,0).
            SetBoard(new ScriptedRandom(0, 0, 4, 5),
                "GRB",
                "RYR",
                "BRG",
                "PRO");

            yield return VerifyValidMove(new GridPosition(1, 3), new GridPosition(1, 2), 1, true,
                "RPO",
                "GRB",
                "BYG",
                "PTO");
            Assert.That(reportedResults[0].CountOf(TileType.Red), Is.EqualTo(5));
        }

        [UnityTest]
        public IEnumerator Swap_FourLine_UsesSecondSelectionAndReportsOneScoredMove()
        {
            UseLevel(1, TileType.Red, 4);
            SetBoard(new ScriptedRandom(1, 2, 3), "RRRBR");
            var survivingBlue = boardView.GetTileView(new GridPosition(3, 0));

            yield return Swap(new GridPosition(4, 0), new GridPosition(3, 0));

            AssertStable();
            Assert.That(board.GetTile(new GridPosition(3, 0)), Is.EqualTo(TileType.HorizontalRocket));
            Assert.That(boardView.GetTileView(new GridPosition(4, 0)), Is.SameAs(survivingBlue));
            Assert.That(reportedResults.Count, Is.EqualTo(1));
            Assert.That(reportedResults[0].CountOf(TileType.Red), Is.EqualTo(4));
            Assert.That(level.Score, Is.EqualTo(400));
            Assert.That(level.MovesRemaining, Is.Zero);
            Assert.That(level.Status, Is.EqualTo(LevelStatus.Won));
            Assert.That(busyChanges, Is.EqualTo(new[] { "busy", "free" }));
        }

        [UnityTest]
        public IEnumerator Sequence_ValidMoveThenAnotherValidMove_BothResolveFromTheCurrentBoard()
        {
            // Move 1 completes G G G in column 0 (refilled Y, R, O).
            // Move 2, on the resolved board, completes B B B in column 3 (refilled G, Y, P).
            SetBoard(new ScriptedRandom(3, 0, 5, 1, 3, 4),
                "GBYB",
                "GPOB",
                "RGBR");
            var untouched = new[]
            {
                new GridPosition(1, 1), new GridPosition(1, 2), new GridPosition(2, 1), new GridPosition(2, 2)
            };
            var untouchedViews = ViewsAt(untouched);
            var firstRed = boardView.GetTileView(new GridPosition(0, 0));
            var secondRed = boardView.GetTileView(new GridPosition(3, 0));

            yield return VerifyValidMove(new GridPosition(0, 0), new GridPosition(1, 0), 1, false,
                "OBYB",
                "RPOB",
                "YRBR");
            yield return VerifyValidMove(new GridPosition(2, 0), new GridPosition(3, 0), 1, false,
                "OBYP",
                "RPOY",
                "YRRG");

            // Tiles no move cleared are still the same objects; each swapped red survived where it landed.
            Assert.That(ViewsAt(untouched), Is.EqualTo(untouchedViews));
            Assert.That(boardView.GetTileView(new GridPosition(1, 0)), Is.SameAs(firstRed));
            Assert.That(boardView.GetTileView(new GridPosition(2, 0)), Is.SameAs(secondRed));
        }

        [UnityTest]
        public IEnumerator Resolve_SameColourSurvivorsFallTogether_KeepTheirIdentity()
        {
            // Completing Y Y Y on the bottom row makes every column fall one cell, including two reds in column 0.
            SetBoard(new ScriptedRandom(1, 3, 2),
                "RBG",
                "RGP",
                "BYO",
                "YPY");
            var lowerRed = boardView.GetTileView(new GridPosition(0, 2));
            var upperRed = boardView.GetTileView(new GridPosition(0, 3));
            var blue = boardView.GetTileView(new GridPosition(0, 1));
            var orange = boardView.GetTileView(new GridPosition(2, 1));

            yield return VerifyValidMove(new GridPosition(1, 1), new GridPosition(1, 0), 1, true,
                "GYB",
                "RBG",
                "RGP",
                "BPO");

            // Colour cannot tell the reds apart; the move records can. Three columns collapsed at once.
            Assert.That(boardView.GetTileView(new GridPosition(0, 1)), Is.SameAs(lowerRed));
            Assert.That(boardView.GetTileView(new GridPosition(0, 2)), Is.SameAs(upperRed));
            Assert.That(boardView.GetTileView(new GridPosition(0, 0)), Is.SameAs(blue));
            Assert.That(boardView.GetTileView(new GridPosition(2, 0)), Is.SameAs(orange));
        }

        [UnityTest]
        public IEnumerator Resolve_CascadeRoundIsAnIntersection_ResolvesBothRunsThenStabilises()
        {
            // Round 1 clears column 0 and refills it with P P P. That column, plus the P P already in row 1,
            // forms an intersection at (0,1): five Purples count and TNT falls to (0,0).
            SetBoard(new ScriptedRandom(4, 4, 4, 2, 1, 0, 5),
                "GBY",
                "GPP",
                "RGO");

            yield return VerifyValidMove(new GridPosition(0, 0), new GridPosition(1, 0), 2, true,
                "GRO",
                "BBY",
                "TRO");
            Assert.That(reportedResults[0].CountOf(TileType.Purple), Is.EqualTo(5));
        }

        [UnityTest]
        public IEnumerator Resolve_SeveralRealMoves_LeaveNoStaleLookupState()
        {
            // A generated board and a real seeded refill: play every valid move found, several times in a row.
            board = BoardGenerator.Generate(5, 4, new[] { TileType.Red, TileType.Green, TileType.Blue, TileType.Yellow }, 7);
            boardView.Render(board);
            controller.Initialize(board, boardView, inputCamera, CreateRunner(new System.Random(7)), onValidMoveResolved, RecordBusy);
            Physics2D.SyncTransforms();

            int movesPlayed = 0;
            for (int attempt = 0; attempt < 6; attempt++)
            {
                if (!TryFindValidMove(out var first, out var second))
                {
                    break;
                }

                yield return Swap(first, second);
                AssertStable();
                // One view per cell: nothing leaked from earlier rounds, nothing missing.
                Assert.That(boardObject.GetComponentsInChildren<TileView>(true).Length, Is.EqualTo(board.Width * board.Height));
                movesPlayed++;
            }

            Assert.That(movesPlayed, Is.GreaterThanOrEqualTo(3), "Expected several moves on this seeded board.");
        }

        [UnityTest]
        public IEnumerator Level_InvalidSwap_CostsNothing()
        {
            UseLevel(5, TileType.Green, 10);
            SetBoard(new ScriptedRandom(2, 3, 5),
                "GBY",
                "GPO",
                "RGG");

            // Y and O exchange without forming a match: rolled back, so the move is free.
            yield return Swap(new GridPosition(2, 2), new GridPosition(2, 1));

            AssertSettled();
            AssertLevel(5, 0, 0, LevelStatus.Playing);
            Assert.That(reportedResults, Is.Empty);
            Assert.That(controller.enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator Level_ValidOneRoundMove_CostsOneMoveAndScoresTheClearedTiles()
        {
            UseLevel(5, TileType.Green, 10);
            SetBoard(new ScriptedRandom(2, 3, 5),
                "GBY",
                "GPO",
                "RGG");

            // Clears G G G; the refilled B, Y, O count for nothing.
            yield return VerifyValidMove(new GridPosition(0, 0), new GridPosition(1, 0), 1, false,
                "OBY",
                "YPO",
                "BRG");

            AssertLevel(4, 300, 3, LevelStatus.Playing);
            Assert.That(controller.enabled, Is.True);
            // Still Playing: the next click starts a fresh selection.
            Click(new GridPosition(0, 0));
            Assert.That(controller.SelectedPosition, Is.EqualTo(new GridPosition(0, 0)));
        }

        [UnityTest]
        public IEnumerator Level_ValidCascadeMove_CostsOneMoveAndScoresEveryRound()
        {
            UseLevel(5, TileType.Blue, 10);
            // Three rounds clear G G G, then B B B, then Y Y Y: 9 tiles, 3 of them Blue.
            SetBoard(new ScriptedRandom(2, 2, 2, 3, 3, 3, 2, 3, 5),
                "GBY",
                "GPO",
                "RGG");

            yield return VerifyValidMove(new GridPosition(0, 0), new GridPosition(1, 0), 3, false,
                "OBY",
                "YPO",
                "BRG");

            Assert.That(reportedResults.Count, Is.EqualTo(1));
            Assert.That(reportedResults[0].ClearedCount, Is.EqualTo(9));
            AssertLevel(4, 900, 3, LevelStatus.Playing);
        }

        [UnityTest]
        public IEnumerator Level_GoalReached_WinsAndStopsNewMoves()
        {
            UseLevel(5, TileType.Green, 3);
            SetBoard(new ScriptedRandom(2, 3, 5),
                "GBY",
                "GPO",
                "RGG");

            yield return VerifyValidMove(new GridPosition(0, 0), new GridPosition(1, 0), 1, false,
                "OBY",
                "YPO",
                "BRG");

            AssertLevel(4, 300, 3, LevelStatus.Won);
            AssertLevelOver();
        }

        [UnityTest]
        public IEnumerator Level_LastMoveWithoutGoal_LosesAndStopsNewMoves()
        {
            UseLevel(1, TileType.Blue, 10);
            SetBoard(new ScriptedRandom(2, 3, 5),
                "GBY",
                "GPO",
                "RGG");

            // Clears only Green; the Blue that refill creates does not count.
            yield return VerifyValidMove(new GridPosition(0, 0), new GridPosition(1, 0), 1, false,
                "OBY",
                "YPO",
                "BRG");

            AssertLevel(0, 300, 0, LevelStatus.Lost);
            AssertLevelOver();
        }

        [UnityTest]
        public IEnumerator Level_LastMoveReachesTheGoal_WinsInsteadOfLosing()
        {
            UseLevel(1, TileType.Green, 3);
            SetBoard(new ScriptedRandom(2, 3, 5),
                "GBY",
                "GPO",
                "RGG");

            yield return Swap(new GridPosition(0, 0), new GridPosition(1, 0));

            // Moves reached 0 in the same move that completed the goal: Won has priority.
            AssertLevel(0, 300, 3, LevelStatus.Won);
            AssertLevelOver();
        }

        [UnityTest]
        public IEnumerator Level_LastMoveReachesTheGoalOnlyInACascade_StaysPlayingUntilStableThenWins()
        {
            // Yellow is cleared only in the third round, by a cascade.
            UseLevel(1, TileType.Yellow, 3);
            SetBoard(new ScriptedRandom(2, 2, 2, 3, 3, 3, 2, 3, 5),
                "GBY",
                "GPO",
                "RGG");
            Click(new GridPosition(0, 0));
            Click(new GridPosition(1, 0));

            float deadline = Time.realtimeSinceStartup + 10f;
            while (controller.IsInputLocked)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Resolution did not finish.");
                // Progress is applied once, after the last cascade: never a premature Lost in between.
                AssertLevel(1, 0, 0, LevelStatus.Playing);
                yield return null;
            }

            AssertLevel(0, 900, 3, LevelStatus.Won);
            AssertLevelOver();
        }

        [UnityTest]
        public IEnumerator SpecialSwap_RocketUsesNewRow_WinsOnLastMoveAndCleansUp()
        {
            UseLevel(1, TileType.Yellow, 1);
            SetBoard(new ScriptedRandom(2, 3, 1), "HGB", "RYO");

            yield return VerifyValidMove(new GridPosition(0, 1), new GridPosition(0, 0), 1, true,
                "BYG", "RGB");

            AssertLevel(0, 200, 1, LevelStatus.Won);
            Assert.That(reportedResults[0].ClearedTiles, Is.EquivalentTo(new[] { TileType.Yellow, TileType.Orange }));
            AssertLevelOver();
        }

        [UnityTest]
        public IEnumerator SpecialSwap_VerticalRocketUsesNewColumn_LosesOnlyAfterFinishing()
        {
            UseLevel(1, TileType.Green, 2);
            SetBoard(new ScriptedRandom(2, 3), "RGB", "VYO");

            yield return VerifyValidMove(new GridPosition(0, 0), new GridPosition(1, 0), 1, false,
                "RYB", "YBO");

            AssertLevel(0, 100, 1, LevelStatus.Lost);
            AssertLevelOver();
        }

        [UnityTest]
        public IEnumerator SpecialSwap_PropellerAcceptsMouseMoveAndUsesProductionResolutionPath()
        {
            UseLevel(4, TileType.Red, 9);
            SetBoard(new ScriptedRandom(0, 1, 2, 3), "FRGB");

            yield return VerifyValidMove(new GridPosition(0, 0), new GridPosition(1, 0), 1, false, "RGBY");

            AssertLevel(3, 300, 1, LevelStatus.Playing);
            Assert.That(reportedResults[0].ClearedCount, Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator SpecialSwap_TwoSpecialsAndLongMixedChain_CostOneMoveWithoutBonus()
        {
            UseLevel(3, TileType.Green, 10);
            SetBoard(new ScriptedRandom(2, 1, 0, 3, 2, 1, 5, 3, 2), "RGB", "GFL", "HVT");

            yield return VerifyValidMove(new GridPosition(0, 0), new GridPosition(1, 0), 1, false,
                "RGB", "GBY", "BYO");

            AssertLevel(2, 400, 2, LevelStatus.Playing);
            Assert.That(reportedResults[0].ClearedCount, Is.EqualTo(4), "Five activated specials give no color bonus.");
        }

        // Plays one valid move and follows it to the end. Checks that the Board changes first, that input stays
        // locked and clicks are ignored through every stage, that exactly `expectedRounds` resolution rounds
        // happen (each round empties cells, then refills them), and the exact final stable board.
        // Intermediate stages are recognised by state, never by frame counts:
        //   Empty cells present → the Board is already collapsed; while tiles fall their visuals lag behind.
        //   after a round started, no Empty but still locked → refill: Board full, new visuals not yet registered.
        // `expectFalls` is false when every cleared column was emptied from the top, so no surviving tile falls.
        private IEnumerator VerifyValidMove(
            GridPosition first, GridPosition second, int expectedRounds, bool expectFalls, params string[] finalRows)
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
                () => controller.Initialize(board, boardView, inputCamera, CreateRunner(new System.Random(1)), onValidMoveResolved, RecordBusy));
            int reportsBefore = reportedResults.Count;
            int busyBefore = busyChanges.Count;
            Assert.That(busyChanges[busyBefore - 1], Is.EqualTo("busy"), "Busy as soon as the move is accepted.");

            int rounds = 0;
            bool hadEmpty = false;
            bool sawFallCatchingUp = false;
            bool sawRefillCatchingUp = false;
            float deadline = Time.realtimeSinceStartup + 10f;
            while (controller.IsInputLocked)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Resolution did not finish.");

                // Clicks during the swap, clear, fall, refill and between cascade rounds must all be ignored.
                controller.HandlePointerDown(rapidClick);
                Assert.That(controller.SelectedPosition, Is.EqualTo(first));
                Assert.That(controller.IsInputLocked, Is.True);
                Assert.That(reportedResults.Count, Is.EqualTo(reportsBefore), "Nothing is reported before the board is stable.");
                Assert.That(busyChanges.Count, Is.EqualTo(busyBefore), "Still busy through every stage.");

                // A round starts when cleared (Empty) cells appear and ends when refill fills them again.
                bool hasEmpty = BoardHasEmpty();
                if (hasEmpty && !hadEmpty)
                {
                    rounds++;
                }

                if (hasEmpty)
                {
                    // Core cleared and collapsed in the same step, so no hole can sit below a tile.
                    AssertBoardCollapsed();
                    sawFallCatchingUp |= SomeTileVisualLagsBehind();
                }
                else if (rounds > 0)
                {
                    // Board already holds the refilled types; the new visuals are registered only on landing.
                    sawRefillCatchingUp |= SomeTileVisualLagsBehind();
                }

                hadEmpty = hasEmpty;
                yield return null;
            }

            Assert.That(rounds, Is.EqualTo(expectedRounds), "Resolution rounds before unlock.");
            Assert.That(sawRefillCatchingUp, Is.True, "Refilled tiles should be seen catching up with the Board.");
            if (expectFalls)
            {
                Assert.That(sawFallCatchingUp, Is.True, "Falling tiles should be seen catching up with the Board.");
            }

            AssertStable();
            Assert.That(ReadTiles(), Is.EqualTo(Tiles(finalRows)));
            Assert.That(reportedResults.Count, Is.EqualTo(reportsBefore + 1), "One report per valid move, after every round.");
            Assert.That(busyChanges.Count, Is.EqualTo(busyBefore + 1));
            Assert.That(busyChanges[busyBefore], Is.EqualTo("free"), "Free only after the complete cleanup.");
        }

        private TileView[] ViewsAt(GridPosition[] positions)
        {
            var views = new TileView[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                views[i] = boardView.GetTileView(positions[i]);
            }

            return views;
        }

        // Finds the first adjacent swap that SwapValidator accepts, trying it on a copy so the real Board is untouched.
        private bool TryFindValidMove(out GridPosition first, out GridPosition second)
        {
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    var candidates = new[] { new GridPosition(x + 1, y), new GridPosition(x, y + 1) };
                    foreach (var neighbour in candidates)
                    {
                        if (!board.IsInBounds(neighbour))
                        {
                            continue;
                        }

                        var copy = new Board(board.Width, board.Height);
                        for (int cy = 0; cy < board.Height; cy++)
                        {
                            for (int cx = 0; cx < board.Width; cx++)
                            {
                                var cell = new GridPosition(cx, cy);
                                copy.SetTile(cell, board.GetTile(cell));
                            }
                        }

                        var origin = new GridPosition(x, y);
                        copy.TrySwapAdjacent(origin, neighbour);
                        if (SwapValidator.IsValidResult(copy, origin, neighbour))
                        {
                            first = origin;
                            second = neighbour;
                            return true;
                        }
                    }
                }
            }

            first = default;
            second = default;
            return false;
        }

        // In every column, no Empty cell sits below a tile: gravity has been applied.
        private void AssertBoardCollapsed()
        {
            for (int x = 0; x < board.Width; x++)
            {
                bool seenEmpty = false;
                for (int y = 0; y < board.Height; y++)
                {
                    bool isEmpty = board.GetTile(new GridPosition(x, y)) == TileType.Empty;
                    Assert.That(!isEmpty && seenEmpty, Is.False, $"Tile above a hole in column {x}: gravity not applied yet.");
                    seenEmpty |= isEmpty;
                }
            }
        }

        // True when some non-empty Board cell has no registered view yet, or its view is not at that cell yet.
        private bool SomeTileVisualLagsBehind()
        {
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    var position = new GridPosition(x, y);
                    if (board.GetTile(position) == TileType.Empty)
                    {
                        continue;
                    }

                    TileView view;
                    try
                    {
                        view = boardView.GetTileView(position);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        return true;
                    }

                    Vector3 expected = boardObject.transform.TransformPoint(new Vector3(x * 1.5f, y * 1.5f, 0f));
                    if (view.Type != board.GetTile(position) || Vector3.Distance(view.transform.position, expected) > 1e-4f)
                    {
                        return true;
                    }
                }
            }

            return false;
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

        private void SendMouseState(Mouse mouse, Vector2 position, ushort buttons)
        {
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position, buttons = buttons });
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
            controller.Initialize(board, boardView, inputCamera, CreateRunner(refillRandom), onValidMoveResolved, RecordBusy);
            Physics2D.SyncTransforms();
        }

        private void RecordBusy(bool isBusy)
        {
            bool isSettled = !controller.IsInputLocked && !controller.SelectedPosition.HasValue;
            busyChanges.Add(isBusy ? "busy" : isSettled ? "free" : "free-early");
        }

        // Makes the next SetBoard wire the controller to a fresh level (100 points per cleared tile).
        private void UseLevel(int startingMoveCount, TileType goalTileType, int goalTarget)
        {
            level = new LevelProgress(startingMoveCount, goalTileType, goalTarget, 100);
            onValidMoveResolved = ApplyToLevel;
        }

        // The same rule BoardBootstrap applies; here it lets level tests use exact, scripted boards.
        private void ApplyToLevel(ResolutionResult result)
        {
            reportedResults.Add(result);
            level.ApplyValidMove(result);
            if (level.Status != LevelStatus.Playing)
            {
                controller.enabled = false;
            }
        }

        private void AssertLevel(int movesRemaining, int score, int goalProgress, LevelStatus status)
        {
            Assert.That(level.MovesRemaining, Is.EqualTo(movesRemaining), "Moves remaining");
            Assert.That(level.Score, Is.EqualTo(score), "Score");
            Assert.That(level.GoalProgress, Is.EqualTo(goalProgress), "Goal progress");
            Assert.That(level.Status, Is.EqualTo(status), "Status");
        }

        // Won or Lost: the last move still finished cleanly, but no new move can start.
        private void AssertLevelOver()
        {
            AssertStable();
            Assert.That(controller.enabled, Is.False);
            var tiles = ReadTiles();

            Click(new GridPosition(0, 0));
            Click(new GridPosition(1, 0));

            Assert.That(controller.SelectedPosition, Is.Null);
            Assert.That(controller.IsInputLocked, Is.False);
            Assert.That(ReadTiles(), Is.EqualTo(tiles));
            Assert.That(reportedResults.Count, Is.EqualTo(1));
            Assert.That(busyChanges, Is.EqualTo(new[] { "busy", "free" }), "A finished level still reports its last move as done.");
        }

        private BoardResolutionRunner CreateRunner(System.Random refillRandom)
        {
            return new BoardResolutionRunner(board, boardView, Palette, refillRandom, ResolutionDuration, ResolutionDuration, new System.Random(2));
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
