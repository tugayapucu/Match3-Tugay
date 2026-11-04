using System;
using System.Collections;
using Match3.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace Match3.Presentation.Tests
{
    // Shares the existing fixture and board assertions; gesture cases stay separate from resolution regressions.
    public partial class BoardInputControllerTests
    {
        [Test]
        public void Tap_UsesPressStartCell_WhenSmallDriftCrossesACellBorder()
        {
            typeof(BoardView).GetField("tileSpacing", Fields).SetValue(boardView, 1f);
            boardView.Render(board);
            var cell = new GridPosition(1, 1);
            Vector2 start = inputCamera.WorldToScreenPoint(
                boardView.GetTileView(cell).transform.position + Vector3.right * 0.49f);
            Vector2 end = start + Vector2.right * CellPixels() * 0.04f;

            controller.HandlePointerDown(start);
            Assert.That(controller.SelectedPosition, Is.Null, "Press alone must not select.");
            Assert.That(Physics2D.GetRayIntersection(inputCamera.ScreenPointToRay(end)).collider,
                Is.SameAs(boardView.GetTileView(new GridPosition(2, 1)).GetComponent<Collider2D>()));
            controller.HandlePointerUp(end);

            Assert.That(controller.SelectedPosition, Is.EqualTo(cell), "Release must not pick the neighbouring cell.");
            Assert.That(busyChanges, Is.Empty);
        }

        [TestCase(0.249f, false)]
        [TestCase(0.25f, true)]
        public void Swipe_ThresholdBoundary_UsesInclusiveDominantDistance(float fraction, bool starts)
        {
            // Exact binary-friendly pixel spacing makes the equality assertion independent of rounding.
            inputCamera.pixelRect = new Rect(0f, 0f, 800f, 800f);
            Vector2 start = ScreenPosition(new GridPosition(1, 1));
            controller.HandlePointerDown(start);
            controller.HandlePointerMove(start + Vector2.right * CellPixels() * fraction);

            Assert.That(controller.IsInputLocked, Is.EqualTo(starts));
            Assert.That(busyChanges.Count, Is.EqualTo(starts ? 1 : 0));
            if (!starts)
            {
                controller.HandlePointerUp(start + Vector2.right * CellPixels() * fraction);
                Assert.That(controller.SelectedPosition, Is.EqualTo(new GridPosition(1, 1)));
            }
        }

        [TestCase(0.5f, 0f, 1, 0)]
        [TestCase(-0.5f, 0f, -1, 0)]
        [TestCase(0f, 0.5f, 0, 1)]
        [TestCase(0f, -0.5f, 0, -1)]
        [TestCase(0.6f, 0.4f, 1, 0)]
        [TestCase(0.4f, 0.6f, 0, 1)]
        [TestCase(0.5f, 0.5f, 1, 0)]
        public void Swipe_MapsScreenDirectionToExactlyOneAdjacentCell(float dx, float dy, int x, int y)
        {
            var first = new GridPosition(1, 1);
            var second = new GridPosition(1 + x, 1 + y);
            var expected = ReadTiles();
            Exchange(expected, first, second);
            Vector2 start = ScreenPosition(first);

            controller.HandlePointerDown(start);
            controller.HandlePointerMove(start + new Vector2(dx, dy) * CellPixels());

            Assert.That(controller.IsInputLocked, Is.True);
            Assert.That(controller.SelectedPosition, Is.EqualTo(first));
            Assert.That(ReadTiles(), Is.EqualTo(expected), "Screen up must target grid y+1, not y-1.");
            Assert.That(busyChanges, Is.EqualTo(new[] { "busy" }));
        }

        [TestCase(0, 0, -1, 0)]
        [TestCase(0, 0, 0, -1)]
        [TestCase(2, 0, 1, 0)]
        [TestCase(2, 0, 0, -1)]
        [TestCase(0, 2, -1, 0)]
        [TestCase(0, 2, 0, 1)]
        [TestCase(2, 2, 1, 0)]
        [TestCase(2, 2, 0, 1)]
        [TestCase(0, 1, -1, 0)]
        [TestCase(2, 1, 1, 0)]
        [TestCase(1, 0, 0, -1)]
        [TestCase(1, 2, 0, 1)]
        public void Swipe_OutwardFromEdgesAndCorners_KeepsSelectionWithoutLockOrRetry(int x, int y, int dx, int dy)
        {
            var previous = new GridPosition(1, 1);
            Click(previous);
            var before = ReadTiles();
            Vector2 start = ScreenPosition(new GridPosition(x, y));
            Vector2 displacement = new Vector2(dx, dy) * CellPixels();

            Assert.DoesNotThrow(() =>
            {
                controller.HandlePointerDown(start);
                controller.HandlePointerMove(start + displacement);
                // Reversing into the board must not retry this rejected gesture.
                controller.HandlePointerMove(start - displacement);
                controller.HandlePointerUp(start - displacement);
            });

            Assert.That(controller.SelectedPosition, Is.EqualTo(previous));
            Assert.That(controller.IsInputLocked, Is.False);
            Assert.That(ReadTiles(), Is.EqualTo(before));
            Assert.That(busyChanges, Is.Empty);
        }

        [Test]
        public void Swipe_EmptyNeighbour_IsRejectedWithoutChangingBoardOrSelection()
        {
            var previous = new GridPosition(0, 0);
            Click(previous);
            var empty = new GridPosition(2, 1);
            board.SetTile(empty, TileType.Empty);
            boardView.GetTileView(empty).SetTileType(TileType.Empty);
            var before = ReadTiles();
            Vector2 start = ScreenPosition(new GridPosition(1, 1));

            controller.HandlePointerDown(start);
            controller.HandlePointerMove(start + Vector2.right * CellPixels());
            controller.HandlePointerUp(start + Vector2.right * CellPixels());

            Assert.That(controller.SelectedPosition, Is.EqualTo(previous));
            Assert.That(controller.IsInputLocked, Is.False);
            Assert.That(busyChanges, Is.Empty);
            Assert.That(ReadTiles(), Is.EqualTo(before));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Gesture_StartingOutsideOrOnEmpty_CannotBecomeATapOrSwipe(bool empty)
        {
            var cell = new GridPosition(0, 0);
            Vector2 start = ScreenPosition(cell);
            if (empty)
            {
                board.SetTile(cell, TileType.Empty);
                boardView.GetTileView(cell).SetTileType(TileType.Empty);
            }
            else
            {
                start = inputCamera.WorldToScreenPoint(new Vector3(110f, 110f, 0f));
            }
            var before = ReadTiles();
            controller.HandlePointerDown(start);
            controller.HandlePointerMove(ScreenPosition(new GridPosition(1, 1)));
            controller.HandlePointerUp(ScreenPosition(new GridPosition(1, 1)));

            Assert.That(controller.SelectedPosition, Is.Null);
            Assert.That(controller.IsInputLocked, Is.False);
            Assert.That(ReadTiles(), Is.EqualTo(before));
            Assert.That(busyChanges, Is.Empty);
        }

        [Test]
        public void Swipe_ReplacesPreviousSelectionInsideSharedPathBeforeBusyCallback()
        {
            var previous = new GridPosition(0, 0);
            var origin = new GridPosition(1, 1);
            Click(previous);
            var expected = ReadTiles();
            Exchange(expected, origin, new GridPosition(2, 1));
            typeof(BoardInputController).GetField("onBusyChanged", Fields).SetValue(controller,
                (Action<bool>)(busy =>
                {
                    if (busy)
                    {
                        Assert.That(controller.SelectedPosition, Is.EqualTo(origin));
                        Assert.That(ReadTiles(), Is.EqualTo(expected));
                    }
                    RecordBusy(busy);
                }));

            Vector2 start = ScreenPosition(origin);
            controller.HandlePointerDown(start);
            Assert.That(controller.SelectedPosition, Is.EqualTo(previous));
            controller.HandlePointerMove(start + Vector2.right * CellPixels());
            Assert.That(busyChanges, Is.EqualTo(new[] { "busy" }));
        }

        [TestCase(0f)]
        [TestCase(-0.1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void Initialize_InvalidSwipeFraction_ThrowsBeforeLosingSelection(float fraction)
        {
            var cell = new GridPosition(0, 0);
            Click(cell);
            typeof(BoardInputController).GetField("swipeThresholdFraction", Fields).SetValue(controller, fraction);
            Assert.Throws<InvalidOperationException>(() => controller.Initialize(board, boardView, inputCamera,
                CreateRunner(new System.Random(1)), onValidMoveResolved, RecordBusy));
            Assert.That(controller.SelectedPosition, Is.EqualTo(cell));
            Assert.That(controller.IsInputLocked, Is.False);
        }

        [TestCase("Initialize")]
        [TestCase("Disable")]
        [TestCase("Focus")]
        public void Gesture_Cancellation_DiscardsPendingInputButAllowsANewPress(string cause)
        {
            Vector2 start = ScreenPosition(new GridPosition(0, 0));
            controller.HandlePointerDown(start);
            if (cause == "Initialize")
            {
                controller.Initialize(board, boardView, inputCamera, CreateRunner(new System.Random(1)),
                    onValidMoveResolved, RecordBusy);
            }
            else if (cause == "Disable")
            {
                controller.enabled = false;
                controller.HandlePointerMove(start + Vector2.right * CellPixels());
                controller.HandlePointerUp(start);
                controller.enabled = true;
            }
            else
            {
                typeof(BoardInputController).GetMethod("OnApplicationFocus", Fields).Invoke(controller, new object[] { false });
            }
            controller.HandlePointerMove(start + Vector2.right * CellPixels());
            controller.HandlePointerUp(start);
            Assert.That(controller.SelectedPosition, Is.Null);
            Assert.That(controller.IsInputLocked, Is.False);
            Assert.That(busyChanges, Is.Empty);
            Click(new GridPosition(0, 0));
            Assert.That(controller.SelectedPosition, Is.EqualTo(new GridPosition(0, 0)));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Update_InitializedPointerSameFrameTap_ProcessesBothEdgesExactlyOnce(bool mouseInput)
        {
            var touch = CreateTouchscreen();
            var mouse = mouseInput ? InputSystem.AddDevice<Mouse>() : null;
            if (mouse != null)
            {
                _ = mouse.press.wasPressedThisFrame;
            }
            for (int tap = 0; tap < 2; tap++)
            {
                Vector2 point = ScreenPosition(new GridPosition(tap, 0));
                if (mouse != null)
                {
                    InputSystem.QueueStateEvent(mouse, new MouseState { position = point, buttons = 1 });
                    InputSystem.QueueStateEvent(mouse, new MouseState { position = point, buttons = 0 });
                }
                else
                {
                    InputSystem.QueueStateEvent(touch, new TouchState { touchId = tap + 1, phase = TouchPhase.Began, position = point });
                    InputSystem.QueueStateEvent(touch, new TouchState { touchId = tap + 1, phase = TouchPhase.Ended, position = point });
                }
                InputSystem.Update();
                PollGestureController();
                if (tap == 0)
                {
                    Assert.That(controller.SelectedPosition, Is.EqualTo(new GridPosition(0, 0)));
                    Assert.That(busyChanges, Is.Empty);
                }
            }
            Assert.That(controller.IsInputLocked, Is.True);
            Assert.That(busyChanges, Is.EqualTo(new[] { "busy" }));
        }

        [UnityTest]
        public IEnumerator Update_TouchDisplacedReleaseWithoutMovementPoll_UsesTrueOriginAndRollsBack()
        {
            var touch = CreateTouchscreen();
            UseLevel(5, TileType.Red, 25);
            SetBoard(new System.Random(1), "RGB", "YPO", "RGB");
            var before = ReadTiles();
            var first = new GridPosition(0, 0);
            var second = new GridPosition(1, 0);
            var views = SnapshotViews();
            var movingView = boardView.GetTileView(first);
            Vector3 worldStart = movingView.transform.position;
            float fullDistance = Vector3.Distance(worldStart, boardView.GetTileView(second).transform.position);
            Vector2 start = ScreenPosition(first);
            Vector2 end = start + Vector2.right * CellPixels() * 1.4f;
            InputSystem.QueueStateEvent(touch, new TouchState { touchId = 1, phase = TouchPhase.Began, position = start });
            InputSystem.QueueStateEvent(touch, new TouchState { touchId = 1, phase = TouchPhase.Ended, position = end });
            InputSystem.Update();
            PollGestureController();

            var expected = ReadTiles();
            Assert.That(expected[0, 0], Is.EqualTo(before[1, 0]));
            Assert.That(expected[1, 0], Is.EqualTo(before[0, 0]));
            Assert.That(controller.SelectedPosition, Is.EqualTo(new GridPosition(0, 0)));
            Assert.That(controller.IsInputLocked, Is.True);
            bool sawForward = false;
            bool sawRollback = false;
            float deadline = Time.realtimeSinceStartup + 5f;
            while (controller.IsInputLocked)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline));
                AssertLevel(5, 0, 0, LevelStatus.Playing);
                float distance = Vector3.Distance(movingView.transform.position, worldStart);
                if (distance > 0.001f && distance < fullDistance - 0.001f)
                {
                    sawForward |= movingView.Position == first;
                    sawRollback |= movingView.Position == second;
                }
                yield return null;
            }
            Assert.That(sawForward && sawRollback, Is.True, "Both animation directions must be observed.");
            Assert.That(ReadTiles(), Is.EqualTo(before));
            Assert.That(SnapshotViews(), Is.EqualTo(views), "Rollback keeps the original tile objects.");
            AssertLevel(5, 0, 0, LevelStatus.Playing);
            AssertStable();
            Assert.That(reportedResults, Is.Empty);
            Assert.That(busyChanges, Is.EqualTo(new[] { "busy", "free" }));
        }

        [UnityTest]
        public IEnumerator Swipe_ValidCascade_ChargesOneMoveAndReportsEveryRoundExactlyOnce()
        {
            var touch = CreateTouchscreen();
            UseLevel(5, TileType.Blue, 10);
            // Clear GGG, then refill BBB, then YYY: three rounds, nine normal tiles in one move.
            SetBoard(new ScriptedRandom(2, 2, 2, 3, 3, 3, 2, 3, 5), "GBY", "GPO", "RGG");
            var red = boardView.GetTileView(new GridPosition(0, 0));
            Vector2 start = ScreenPosition(new GridPosition(0, 0));
            Vector2 end = start + Vector2.right * CellPixels() * 0.4f;
            SendGestureTouch(touch, 1, TouchPhase.Began, start);
            Assert.That(controller.SelectedPosition, Is.Null);
            SendGestureTouch(touch, 1, TouchPhase.Moved, end);
            Assert.That(controller.IsInputLocked, Is.True);
            Assert.That(busyChanges, Is.EqualTo(new[] { "busy" }));

            float deadline = Time.realtimeSinceStartup + 10f;
            while (controller.IsInputLocked)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline));
                AssertLevel(5, 0, 0, LevelStatus.Playing);
                SendGestureTouch(touch, 1, TouchPhase.Stationary, end);
                yield return null;
            }
            // Still held at unlock: neither it nor its release may create a second attempt/tap.
            SendGestureTouch(touch, 1, TouchPhase.Stationary, end);
            SendGestureTouch(touch, 1, TouchPhase.Ended, end);

            AssertLevel(4, 900, 3, LevelStatus.Playing);
            Assert.That(reportedResults.Count, Is.EqualTo(1));
            Assert.That(reportedResults[0].ClearedCount, Is.EqualTo(9));
            Assert.That(reportedResults[0].CountOf(TileType.Blue), Is.EqualTo(3));
            Assert.That(ReadTiles(), Is.EqualTo(Tiles("OBY", "YPO", "BRG")));
            Assert.That(boardView.GetTileView(new GridPosition(1, 0)), Is.SameAs(red));
            Assert.That(controller.SelectedPosition, Is.Null);
            Assert.That(busyChanges, Is.EqualTo(new[] { "busy", "free" }));
            AssertStable();
        }

        [UnityTest]
        public IEnumerator Swipe_SpecialPair_UsesNormalChainRulesAndChargesOneMove()
        {
            var touch = CreateTouchscreen();
            UseLevel(5, TileType.Red, 10);
            // After F swaps right: LFRRB. Light Ball targets the two Reds; Wisp's only remote target is Blue.
            SetBoard(new ScriptedRandom(0, 0, 1, 0, 2), "FLRRB");
            Vector2 start = ScreenPosition(new GridPosition(0, 0));
            Vector2 end = ScreenPosition(new GridPosition(1, 0));
            SendGestureTouch(touch, 1, TouchPhase.Began, start);
            SendGestureTouch(touch, 1, TouchPhase.Moved, end);
            Assert.That(controller.IsInputLocked, Is.True);
            AssertLevel(5, 0, 0, LevelStatus.Playing);
            SendGestureTouch(touch, 1, TouchPhase.Ended, end);
            yield return WaitForSwap();

            AssertLevel(4, 300, 2, LevelStatus.Playing);
            Assert.That(reportedResults.Count, Is.EqualTo(1));
            Assert.That(reportedResults[0].ClearedCount, Is.EqualTo(3), "The two specials give no color credit.");
            Assert.That(reportedResults[0].CountOf(TileType.Red), Is.EqualTo(2));
            Assert.That(reportedResults[0].CountOf(TileType.Blue), Is.EqualTo(1));
            Assert.That(ReadTiles(), Is.EqualTo(Tiles("RRGRB")));
            Assert.That(busyChanges, Is.EqualTo(new[] { "busy", "free" }));
            AssertStable();
        }

        [UnityTest]
        public IEnumerator Update_TapThenSwipeThenTap_UsesSwipeOriginAndStartsAFreshSelection()
        {
            var touch = CreateTouchscreen();
            int id = 0;
            var swipeOrigin = new GridPosition(0, 0);
            // Same tile; adjacent tile (the original press-first conflict); distant tile.
            foreach (var tapped in new[] { swipeOrigin, new GridPosition(0, 1), new GridPosition(2, 2) })
            {
                UseLevel(5, TileType.Green, 10);
                SetBoard(new ScriptedRandom(2, 3, 5), "GBY", "GPO", "RGG");
                reportedResults.Clear();
                busyChanges.Clear();
                var before = ReadTiles();
                Vector2 tapPoint = ScreenPosition(tapped);
                SendGestureTouch(touch, ++id, TouchPhase.Began, tapPoint);
                Assert.That(controller.SelectedPosition, Is.Null);
                SendGestureTouch(touch, id, TouchPhase.Ended, tapPoint);
                Assert.That(controller.SelectedPosition, Is.EqualTo(tapped));

                Vector2 start = ScreenPosition(swipeOrigin);
                Vector2 end = start + Vector2.right * CellPixels() * 0.4f;
                SendGestureTouch(touch, ++id, TouchPhase.Began, start);
                Assert.That(controller.SelectedPosition, Is.EqualTo(tapped), "Pressing the swipe origin must retain A until classification.");
                Assert.That(controller.IsInputLocked, Is.False, "No premature A-to-B swap, even when A is adjacent to B.");
                Assert.That(ReadTiles(), Is.EqualTo(before));
                Assert.That(busyChanges, Is.Empty);
                SendGestureTouch(touch, id, TouchPhase.Moved, end);
                Assert.That(controller.IsInputLocked, Is.True);
                Assert.That(controller.SelectedPosition, Is.EqualTo(swipeOrigin));
                SendGestureTouch(touch, id, TouchPhase.Ended, end);
                yield return WaitForSwap();

                AssertLevel(4, 300, 3, LevelStatus.Playing);
                Assert.That(reportedResults.Count, Is.EqualTo(1));
                Assert.That(ReadTiles(), Is.EqualTo(Tiles("OBY", "YPO", "BRG")));
                Assert.That(controller.SelectedPosition, Is.Null, "Swipe release must not create an extra tap.");
                Assert.That(busyChanges, Is.EqualTo(new[] { "busy", "free" }));
                AssertStable();

                var nextSelection = new GridPosition(2, 1);
                tapPoint = ScreenPosition(nextSelection);
                var settled = ReadTiles();
                SendGestureTouch(touch, ++id, TouchPhase.Began, tapPoint);
                Assert.That(controller.SelectedPosition, Is.Null);
                Assert.That(controller.IsInputLocked, Is.False);
                SendGestureTouch(touch, id, TouchPhase.Ended, tapPoint);
                Assert.That(controller.SelectedPosition, Is.EqualTo(nextSelection));
                Assert.That(ReadTiles(), Is.EqualTo(settled));
                AssertLevel(4, 300, 3, LevelStatus.Playing);
                Assert.That(reportedResults.Count, Is.EqualTo(1));
                Assert.That(busyChanges, Is.EqualTo(new[] { "busy", "free" }));
            }
        }

        [UnityTest]
        public IEnumerator Update_IgnoredTapsAndRejectedSwipe_PreserveSelectionForTheNextAdjacentTap()
        {
            var touch = CreateTouchscreen();
            UseLevel(5, TileType.Green, 10);
            SetBoard(new ScriptedRandom(2, 3, 5), "GBY", "GPO", "RGG");
            var first = new GridPosition(0, 0);
            var before = ReadTiles();
            SendTouchState(touch, 1, TouchPhase.Began, first);
            SendTouchState(touch, 1, TouchPhase.Ended, first);

            // A non-adjacent tap and a tap outside the board must both keep the original selection.
            Vector2 outside = inputCamera.WorldToScreenPoint(new Vector3(110f, 110f, 0f));
            int id = 1;
            foreach (Vector2 point in new[] { ScreenPosition(new GridPosition(2, 2)), outside })
            {
                SendGestureTouch(touch, ++id, TouchPhase.Began, point);
                Assert.That(controller.SelectedPosition, Is.EqualTo(first));
                Assert.That(controller.IsInputLocked, Is.False);
                SendGestureTouch(touch, id, TouchPhase.Ended, point);
                Assert.That(controller.SelectedPosition, Is.EqualTo(first));
            }

            Vector2 start = ScreenPosition(new GridPosition(2, 2));
            SendGestureTouch(touch, ++id, TouchPhase.Began, start);
            SendGestureTouch(touch, id, TouchPhase.Moved, start + Vector2.right * CellPixels());
            SendGestureTouch(touch, id, TouchPhase.Ended, start + Vector2.right * CellPixels());
            Assert.That(controller.SelectedPosition, Is.EqualTo(first));
            Assert.That(controller.IsInputLocked, Is.False);
            Assert.That(ReadTiles(), Is.EqualTo(before));
            Assert.That(busyChanges, Is.Empty);
            AssertLevel(5, 0, 0, LevelStatus.Playing);

            var second = new GridPosition(1, 0);
            SendTouchState(touch, ++id, TouchPhase.Began, second);
            Assert.That(controller.IsInputLocked, Is.False, "An adjacent second tap also waits for release.");
            Assert.That(controller.SelectedPosition, Is.EqualTo(first));
            SendTouchState(touch, id, TouchPhase.Ended, second);
            Assert.That(controller.IsInputLocked, Is.True);
            yield return WaitForSwap();

            AssertLevel(4, 300, 3, LevelStatus.Playing);
            Assert.That(reportedResults.Count, Is.EqualTo(1));
            Assert.That(ReadTiles(), Is.EqualTo(Tiles("OBY", "YPO", "BRG")));
            Assert.That(controller.SelectedPosition, Is.Null);
            Assert.That(busyChanges, Is.EqualTo(new[] { "busy", "free" }));
            AssertStable();
        }

        [UnityTest]
        public IEnumerator Update_HeldSwipePastUnlock_DoesNotRepeatOrBecomeATap()
        {
            var touch = CreateTouchscreen();
            Vector2 start = ScreenPosition(new GridPosition(0, 0));
            Vector2 end = start + Vector2.right * CellPixels();
            SendGestureTouch(touch, 1, TouchPhase.Began, start);
            SendGestureTouch(touch, 1, TouchPhase.Moved, end);
            Assert.That(controller.IsInputLocked, Is.True);
            yield return WaitForSwap();
            SendGestureTouch(touch, 1, TouchPhase.Stationary, end);
            SendGestureTouch(touch, 1, TouchPhase.Ended, end);
            Assert.That(controller.SelectedPosition, Is.Null);
            Assert.That(controller.IsInputLocked, Is.False);
            Assert.That(busyChanges, Is.EqualTo(new[] { "busy", "free" }));
            SendGestureTouch(touch, 2, TouchPhase.Began, start);
            SendGestureTouch(touch, 2, TouchPhase.Ended, start);
            Assert.That(controller.SelectedPosition, Is.EqualTo(new GridPosition(0, 0)));
        }

        [Test]
        public void Update_PointerDeviceChange_CancelsWithoutRearmingHeldInput()
        {
            var touch = CreateTouchscreen();
            Vector2 point = ScreenPosition(new GridPosition(0, 0));
            SendGestureTouch(touch, 1, TouchPhase.Began, point);
            var mouse = InputSystem.AddDevice<Mouse>();
            SendMouseState(mouse, point, 0);
            SendGestureTouch(touch, 1, TouchPhase.Ended, point);
            Assert.That(controller.SelectedPosition, Is.Null);
            Assert.That(busyChanges, Is.Empty);
            SendGestureTouch(touch, 2, TouchPhase.Began, point);
            SendGestureTouch(touch, 2, TouchPhase.Ended, point);
            Assert.That(controller.SelectedPosition, Is.EqualTo(new GridPosition(0, 0)));
        }

        [Test]
        public void Update_CancelledTouch_DoesNotCommitATap()
        {
            var touch = CreateTouchscreen();
            Vector2 point = ScreenPosition(new GridPosition(0, 0));
            SendGestureTouch(touch, 1, TouchPhase.Began, point);
            SendGestureTouch(touch, 1, TouchPhase.Canceled, point);
            Assert.That(controller.SelectedPosition, Is.Null);
            Assert.That(busyChanges, Is.Empty);
        }

        [UnityTest]
        public IEnumerator Swipe_ThresholdIsCapturedAtPress_AndNextPressUsesChangedCamera()
        {
            var cell = new GridPosition(1, 1);
            Vector2 start = ScreenPosition(cell);
            float oldSpacing = CellPixels();
            controller.HandlePointerDown(start);
            inputCamera.orthographicSize *= 0.5f;
            controller.HandlePointerMove(start + Vector2.right * oldSpacing * 0.3f);
            Assert.That(controller.IsInputLocked, Is.True, "The held gesture retains its press-time threshold.");
            yield return WaitForSwap();

            start = ScreenPosition(cell);
            controller.HandlePointerDown(start);
            controller.HandlePointerMove(start + Vector2.right * oldSpacing * 0.3f);
            Assert.That(controller.IsInputLocked, Is.False, "A new press measures the now-larger cells.");
            controller.HandlePointerUp(start + Vector2.right * oldSpacing * 0.3f);
            Assert.That(controller.SelectedPosition, Is.EqualTo(cell));
        }

        private float CellPixels()
        {
            return Vector2.Distance(ScreenPosition(new GridPosition(0, 0)), ScreenPosition(new GridPosition(1, 0)));
        }

        private void SendGestureTouch(Touchscreen touch, int id, TouchPhase phase, Vector2 point)
        {
            InputSystem.QueueStateEvent(touch, new TouchState { touchId = id, phase = phase, position = point });
            InputSystem.Update();
            PollGestureController();
        }

        private void PollGestureController()
        {
            typeof(BoardInputController).GetMethod("Update", Fields).Invoke(controller, null);
        }
    }
}
