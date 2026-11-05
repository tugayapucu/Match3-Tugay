using System;
using System.Collections;
using Match3.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace Match3.Presentation.Tests
{
    public partial class BoardInputControllerTests
    {
        [Test]
        public void SelectionCue_FirstTouchReleaseShowsCue_AndDoesNotChangePickingFootprint()
        {
            var touch = CreateTouchscreen();
            var cell = new GridPosition(1, 1);
            var view = boardView.GetTileView(cell);
            var collider = view.GetComponent<BoxCollider2D>();
            var bounds = collider.bounds;
            Vector2 edge = inputCamera.WorldToScreenPoint(view.transform.position + Vector3.right * 0.49f);
            var beforeHit = Physics2D.GetRayIntersection(inputCamera.ScreenPointToRay(edge)).collider;
            SendTouchState(touch, 1, TouchPhase.Began, cell);
            AssertNoSelectionCues();
            SendTouchState(touch, 1, TouchPhase.Ended, cell);
            Assert.That(Cue(view).activeInHierarchy, Is.True);
            Assert.That(controller.SelectedPosition, Is.EqualTo(cell));
            Physics2D.SyncTransforms();
            Assert.That(collider.bounds, Is.EqualTo(bounds));
            Assert.That(Physics2D.GetRayIntersection(inputCamera.ScreenPointToRay(edge)).collider, Is.SameAs(beforeHit));
            Assert.That(beforeHit, Is.SameAs(collider));
        }

        [Test]
        public void SelectionCue_IgnoredTapsAndRejectedSwipesRetainTheSameCue()
        {
            var first = new GridPosition(0, 0);
            var empty = new GridPosition(1, 0);
            board.SetTile(empty, TileType.Empty);
            boardView.GetTileView(empty).SetTileType(TileType.Empty);
            Click(first);
            var selected = boardView.GetTileView(first);
            foreach (var tapped in new[] { new GridPosition(2, 2), empty })
            {
                Click(tapped);
                Assert.That(Cue(selected).activeInHierarchy, Is.True);
            }
            Vector2 outside = inputCamera.WorldToScreenPoint(new Vector3(110f, 110f, 0f));
            controller.HandlePointerDown(outside); controller.HandlePointerUp(outside);
            // Empty neighbour, then off-board neighbour from a different origin.
            foreach (var origin in new[] { first, new GridPosition(2, 2) })
            {
                Vector2 start = ScreenPosition(origin);
                controller.HandlePointerDown(start);
                controller.HandlePointerMove(start + Vector2.right * CellPixels());
                controller.HandlePointerUp(start + Vector2.right * CellPixels());
                Assert.That(controller.SelectedPosition, Is.EqualTo(first));
                Assert.That(Cue(selected).activeInHierarchy, Is.True);
                Assert.That(controller.IsInputLocked, Is.False);
            }
            Assert.That(busyChanges, Is.Empty);
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void SelectionCue_SameTileTapCancelsOnRelease_WithoutMove_AndNextTapCanSelectAnywhere(bool mouseInput, bool special)
        {
            var touch = CreateTouchscreen();
            var mouse = mouseInput ? InputSystem.AddDevice<Mouse>() : null;
            if (mouse != null)
            {
                _ = mouse.press.wasPressedThisFrame;
            }
            UseLevel(5, TileType.Green, 10);
            SetBoard(new ScriptedRandom(2, 3, 5), "GBY", "GPO", special ? "TGG" : "RGG");
            var first = new GridPosition(0, 0);
            var view = boardView.GetTileView(first);
            var before = ReadTiles();
            int touchId = 0;
            Action<GridPosition, bool> send = (cell, pressed) =>
            {
                if (mouse != null)
                {
                    SendMouseState(mouse, ScreenPosition(cell), (ushort)(pressed ? 1 : 0));
                }
                else
                {
                    if (pressed) { touchId++; }
                    SendTouchState(touch, touchId, pressed ? TouchPhase.Began : TouchPhase.Ended, cell);
                }
            };

            send(first, true);
            send(first, false);
            Assert.That(controller.SelectedPosition, Is.EqualTo(first));
            Assert.That(Cue(view).activeInHierarchy, Is.True);

            send(first, true);
            Assert.That(controller.SelectedPosition, Is.EqualTo(first), "Press alone must not cancel.");
            Assert.That(Cue(view).activeInHierarchy, Is.True);
            if (mouse != null) { SendMouseState(mouse, ScreenPosition(first), 1); }
            else { SendTouchState(touch, touchId, TouchPhase.Stationary, first); }
            Assert.That(Cue(view).activeInHierarchy, Is.True, "Holding below the threshold keeps the cue.");

            send(first, false);
            Assert.That(controller.SelectedPosition, Is.Null);
            AssertNoSelectionCues();
            Assert.That(controller.IsInputLocked, Is.False);
            Assert.That(busyChanges, Is.Empty);
            Assert.That(reportedResults, Is.Empty);
            Assert.That(ReadTiles(), Is.EqualTo(before));
            Assert.That(boardView.GetTileView(first), Is.SameAs(view));
            AssertLevel(5, 0, 0, LevelStatus.Playing);

            // A duplicate release cannot reselect the old tile. A fresh, distant tap can select freely.
            controller.HandlePointerUp(ScreenPosition(first));
            Assert.That(controller.SelectedPosition, Is.Null);
            var next = new GridPosition(2, 2);
            send(next, true);
            Assert.That(controller.SelectedPosition, Is.Null);
            send(next, false);
            Assert.That(controller.SelectedPosition, Is.EqualTo(next));
            Assert.That(Cue(boardView.GetTileView(next)).activeInHierarchy, Is.True);
            Assert.That(Cue(view).activeSelf, Is.False);
            Assert.That(controller.IsInputLocked, Is.False);
            Assert.That(ReadTiles(), Is.EqualTo(before));
            Assert.That(busyChanges, Is.Empty);
            Assert.That(reportedResults, Is.Empty);
            AssertLevel(5, 0, 0, LevelStatus.Playing);
        }

        [UnityTest]
        public IEnumerator SelectionCue_AcceptedTapSwipeAndSpecialAttemptHideBeforeBusyAndStayHidden()
        {
            for (int scenario = 0; scenario < 3; scenario++)
            {
                // Invalid tap (full rollback), valid swipe with cascades, and special-pair swipe.
                if (scenario == 0) { SetBoard(new System.Random(1), "RGB", "YPO", "RGB"); }
                if (scenario == 1) { SetBoard(new ScriptedRandom(2, 2, 2, 3, 3, 3, 2, 3, 5), "GBY", "GPO", "RGG"); }
                if (scenario == 2) { SetBoard(new ScriptedRandom(0, 0, 1, 0, 2), "FLRRB"); }
                var first = new GridPosition(0, 0);
                var priorSelection = scenario == 1 ? new GridPosition(2, 2) : first;
                Click(priorSelection);
                var oldView = boardView.GetTileView(priorSelection);
                Assert.That(Cue(oldView).activeInHierarchy, Is.True);
                bool observedBusy = false;
                bool hiddenAtBusy = false;
                typeof(BoardInputController).GetField("onBusyChanged", Fields).SetValue(controller, (Action<bool>)(busy =>
                {
                    if (busy)
                    {
                        observedBusy = true;
                        hiddenAtBusy = !Cue(oldView).activeSelf && !Cue(boardView.GetTileView(first)).activeSelf;
                    }
                    RecordBusy(busy);
                }));
                if (scenario == 0) { Click(new GridPosition(1, 0)); }
                else
                {
                    Vector2 start = ScreenPosition(first);
                    controller.HandlePointerDown(start);
                    controller.HandlePointerMove(start + Vector2.right * CellPixels() * 0.4f);
                    controller.HandlePointerUp(start + Vector2.right * CellPixels() * 0.4f);
                }
                Assert.That(observedBusy, Is.True);
                Assert.That(hiddenAtBusy, Is.True, "Cue must disappear before the busy callback and first animation frame.");
                Assert.That(controller.IsInputLocked, Is.True);
                float deadline = Time.realtimeSinceStartup + 10f;
                while (controller.IsInputLocked)
                {
                    AssertNoSelectionCues();
                    Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline));
                    yield return null;
                }
                AssertNoSelectionCues();
                Assert.That(controller.SelectedPosition, Is.Null);
                AssertStable();
            }
        }

        [Test]
        public void SelectionCue_DisableHides_EnableRederives_InitializeClears()
        {
            var cell = new GridPosition(0, 0);
            Click(cell);
            var view = boardView.GetTileView(cell);
            controller.enabled = false;
            Assert.That(Cue(view).activeSelf, Is.False);
            Assert.That(controller.SelectedPosition, Is.EqualTo(cell));
            controller.enabled = true;
            Assert.That(Cue(view).activeInHierarchy, Is.True);
            controller.Initialize(board, boardView, inputCamera, CreateRunner(new System.Random(1)), onValidMoveResolved, RecordBusy);
            AssertNoSelectionCues();
            Assert.That(controller.SelectedPosition, Is.Null);
            Assert.That(typeof(BoardInputController).GetField("highlightedView", Fields).GetValue(controller), Is.Null);
        }

        [UnityTest]
        public IEnumerator SelectionCue_ReplacedAndDestroyedVisualsAreDiscardedBeforeRederiving()
        {
            var cell = new GridPosition(0, 0);
            Click(cell);
            var old = boardView.GetTileView(cell);
            boardView.Render(board);
            Assert.That(old.gameObject.activeSelf, Is.False, "Re-render immediately hides old views before deferred destruction.");
            AssertNoSelectionCues();
            typeof(BoardInputController).GetMethod("RefreshSelectionCue", Fields).Invoke(controller, null);
            var replacement = boardView.GetTileView(cell);
            Assert.That(Cue(old).activeSelf, Is.False);
            Assert.That(Cue(replacement).activeInHierarchy, Is.True);
            Assert.That(typeof(BoardInputController).GetField("highlightedView", Fields).GetValue(controller), Is.SameAs(replacement));

            boardView.Render(board);
            yield return null;
            Assert.That(replacement == null, Is.True, "Now the cache contains a destroyed Unity object.");
            typeof(BoardInputController).GetMethod("RefreshSelectionCue", Fields).Invoke(controller, null);
            Assert.That(Cue(boardView.GetTileView(cell)).activeInHierarchy, Is.True);
            Assert.That(controller.SelectedPosition, Is.EqualTo(cell));

            var destroyed = boardView.GetTileView(cell);
            Object.Destroy(destroyed.gameObject);
            boardView.Render(board);
            yield return null;
            controller.Initialize(board, boardView, inputCamera, CreateRunner(new System.Random(1)), onValidMoveResolved, RecordBusy);
            AssertNoSelectionCues();
            Assert.That(ReferenceEquals(typeof(BoardInputController).GetField("highlightedView", Fields).GetValue(controller), null), Is.True,
                "Discard the managed reference too, not only Unity's fake-null result.");
        }

        private static GameObject Cue(TileView view) => view.transform.Find("SelectionHighlight").gameObject;

        private void AssertNoSelectionCues()
        {
            foreach (var view in boardObject.GetComponentsInChildren<TileView>(true))
            {
                Assert.That(Cue(view).activeInHierarchy, Is.False, view.Position.ToString());
            }
        }
    }
}
