using System;
using System.Collections;
using System.Collections.Generic;
using Match3.Core;
using UnityEngine;

namespace Match3.Presentation
{
    public sealed class BoardView : MonoBehaviour
    {
        // The tile template to copy for each cell; assign it in the Inspector.
        [SerializeField] private TileView tilePrefab;
        // Optional parent and coordinate origin for the visual grid.
        [SerializeField] private Transform tileRoot;
        // Distance between cell centers in the root's local space.
        [SerializeField] private float tileSpacing = 1f;
        [SerializeField] private float specialActivationDuration = 0.12f;
        // Seconds matched views take to shrink and fade before they are removed; 0 removes them instantly.
        [SerializeField] private float clearDuration = 0.1f;

        // Find a visual by cell address and track only our own clones for cleanup.
        private readonly Dictionary<GridPosition, TileView> tileViews = new Dictionary<GridPosition, TileView>();

        // Display the supplied board by reading its cells, without changing its data.
        public void Render(Board board)
        {
            // Validate inputs before removing the current display.
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (tilePrefab == null)
            {
                throw new InvalidOperationException("Assign a TileView prefab before rendering.");
            }

            if (tileSpacing <= 0f || float.IsNaN(tileSpacing) || float.IsInfinity(tileSpacing))
            {
                throw new InvalidOperationException("Tile spacing must be a positive finite value.");
            }

            // Remove the previous grid before creating its replacement.
            ClearViews();
            // If no root is assigned, parent tiles to this GameObject instead.
            Transform root = tileRoot != null ? tileRoot : transform;

            // Visit every cell one row at a time.
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    // This is a logical cell address, not a Unity world position.
                    var position = new GridPosition(x, y);
                    var tileType = board.GetTile(position);
                    // Clone under the root; false selects local-space parenting.
                    var view = Instantiate(tilePrefab, root, false);
                    // Register which visual currently represents this cell.
                    tileViews.Add(position, view);

                    // Give the view the cell address and type it should display.
                    view.Initialize(position, tileType);
                    view.SetWorldPosition(GetWorldPosition(position));
                    // Activate inactive-template clones; TileView hides Empty through its renderer.
                    view.gameObject.SetActive(true);
                }
            }
        }

        public TileView GetTileView(GridPosition position)
        {
            if (!tileViews.TryGetValue(position, out var view) || view == null)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(position), position, "No tile view is rendered at this position.");
            }

            return view;
        }

        // The caller swaps the Board first, then waits for this coroutine before another swap or render.
        public IEnumerator AnimateSwap(GridPosition first, GridPosition second, float duration)
        {
            ValidateDuration(duration);

            var firstView = GetTileView(first);
            var secondView = GetTileView(second);
            if (first == second)
            {
                throw new ArgumentException("Swap animation requires two different positions.", nameof(second));
            }

            var firstStart = firstView.transform.position;
            var secondStart = secondView.transform.position;
            var firstDestination = GetWorldPosition(second);
            var secondDestination = GetWorldPosition(first);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                // Both visuals use the same eased progress so they travel together, starting and settling gently.
                float progress = TileEasing.EaseInOut(elapsed / duration);
                firstView.SetWorldPosition(Vector3.Lerp(firstStart, firstDestination, progress));
                secondView.SetWorldPosition(Vector3.Lerp(secondStart, secondDestination, progress));
                yield return null;
                elapsed += Time.deltaTime;
            }

            // Snap exactly to the destinations, including when a zero duration skips movement.
            firstView.SetWorldPosition(firstDestination);
            secondView.SetWorldPosition(secondDestination);

            // The visuals keep their tile types; only the cells they represent change.
            tileViews[first] = secondView;
            tileViews[second] = firstView;
            firstView.SetGridPosition(second);
            secondView.SetGridPosition(first);
        }

        // Core supplies the whole chain; effects are shown one at a time in the plan's order. Beam Crystal and Nova Gem
        // light the visible views their Core Footprint addresses (holes are skipped; the footprint is never recomputed);
        // other specials keep the simple pulse until their own feedback. Everything is checked before anything changes.
        public IEnumerator AnimateSpecialEffects(SpecialEffectPlan plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            ValidateDuration(specialActivationDuration);

            var steps = new SpecialEffectFeedback.Step[plan.Effects.Count];
            for (int i = 0; i < steps.Length; i++)
            {
                var effect = plan.Effects[i];
                var origin = GetTileView(effect.Origin);
                if (origin.Type != effect.Type)
                {
                    throw new InvalidOperationException("The activation view must match the planned special.");
                }

                var highlighted = new List<TileView>();
                if (ShowsFootprint(effect.Type))
                {
                    foreach (var position in effect.Footprint)
                    {
                        // A hole has no view (cleared) or a hidden Empty view (initial render); neither is lit.
                        if (position != effect.Origin && tileViews.TryGetValue(position, out var view) && view != null
                            && view.Type != TileType.Empty)
                        {
                            highlighted.Add(view);
                        }
                    }
                }

                steps[i] = new SpecialEffectFeedback.Step(origin, highlighted.ToArray());
            }

            return SpecialEffectFeedback.Animate(steps, specialActivationDuration);
        }

        // Step 7a covers the line and area specials; Wisp and Spectrum Core feedback arrives in Step 7b.
        private static bool ShowsFootprint(TileType type)
        {
            return type == TileType.HorizontalRocket || type == TileType.VerticalRocket || type == TileType.TNT;
        }

        // Core has already cleared these cells; their old views shrink and fade, then RemoveViews destroys them.
        // Every position is checked before anything changes; the lookup itself is not touched here.
        public IEnumerator AnimateClearViews(IReadOnlyCollection<GridPosition> positions)
        {
            if (positions == null)
            {
                throw new ArgumentNullException(nameof(positions));
            }

            ValidateDuration(clearDuration);

            // A repeated position names the same view once, matching RemoveViews.
            var unique = new HashSet<GridPosition>(positions);
            var views = new TileView[unique.Count];
            int index = 0;
            foreach (var position in unique)
            {
                views[index++] = GetTileView(position);
            }

            return ClearAnimation.Animate(views, clearDuration);
        }

        // Clear visuals: Core has already set these cells to Empty, so their views disappear immediately.
        // Empty cells then have no view in the lookup until refill gives them one.
        public void RemoveViews(IReadOnlyCollection<GridPosition> positions)
        {
            if (positions == null)
            {
                throw new ArgumentNullException(nameof(positions));
            }

            // Check every position first, so a bad call removes nothing.
            foreach (var position in positions)
            {
                GetTileView(position);
            }

            foreach (var position in positions)
            {
                // TryGetValue makes a repeated position harmless: its view is already gone.
                if (tileViews.TryGetValue(position, out var view))
                {
                    tileViews.Remove(position);
                    DestroyView(view);
                }
            }
        }

        // Consumed normal views have already gone. Created specials appear at their anchors, without falling in.
        public void PlaceSpecialViews(IReadOnlyList<TileSpawn> creations)
        {
            if (creations == null)
            {
                throw new ArgumentNullException(nameof(creations));
            }

            if (tilePrefab == null)
            {
                throw new InvalidOperationException("Assign a TileView prefab before placing specials.");
            }

            var targets = new HashSet<GridPosition>();
            foreach (TileSpawn creation in creations)
            {
                if (!TileRules.IsSpecial(creation.Type) || !targets.Add(creation.Position))
                {
                    throw new ArgumentException("Each placement must be a special at a distinct cell.", nameof(creations));
                }

                if (tileViews.ContainsKey(creation.Position))
                {
                    throw new InvalidOperationException("Remove the consumed view before placing a special.");
                }
            }

            Transform root = tileRoot != null ? tileRoot : transform;
            foreach (TileSpawn creation in creations)
            {
                var view = Instantiate(tilePrefab, root, false);
                view.Initialize(creation.Position, creation.Type);
                view.SetWorldPosition(GetWorldPosition(creation.Position));
                tileViews.Add(creation.Position, view);
                view.gameObject.SetActive(true);
            }
        }

        // Gravity visuals: slides the existing views named by Core's moves. The same objects survive, so
        // identity is never guessed from colour. Inputs are checked now; the animation runs when iterated.
        public IEnumerator AnimateFalls(IReadOnlyList<TileMove> moves, float duration)
        {
            if (moves == null)
            {
                throw new ArgumentNullException(nameof(moves));
            }

            ValidateDuration(duration);

            var views = new TileView[moves.Count];
            var origins = new HashSet<GridPosition>();
            for (int i = 0; i < moves.Count; i++)
            {
                TileMove move = moves[i];
                var view = GetTileView(move.From);
                if (view.Type != move.Type)
                {
                    throw new InvalidOperationException(
                        $"The view at ({move.From.X},{move.From.Y}) shows {view.Type}, but the move is for {move.Type}.");
                }

                if (!origins.Add(move.From))
                {
                    throw new ArgumentException("A tile can only move once per fall.", nameof(moves));
                }

                views[i] = view;
            }

            // A destination may only be free, or be the old cell of another falling tile.
            foreach (TileMove move in moves)
            {
                if (tileViews.ContainsKey(move.To) && !origins.Contains(move.To))
                {
                    throw new InvalidOperationException(
                        $"({move.To.X},{move.To.Y}) already shows a tile that is not moving.");
                }
            }

            return AnimateFallsRoutine(moves, views, duration);
        }

        // Refill visuals: creates a new view for each spawn, just above its column, and slides it into its cell.
        // Inputs are checked now; the views are created and animated when iterated.
        public IEnumerator AnimateRefill(IReadOnlyList<TileSpawn> spawns, float duration)
        {
            if (spawns == null)
            {
                throw new ArgumentNullException(nameof(spawns));
            }

            ValidateDuration(duration);
            if (tilePrefab == null)
            {
                throw new InvalidOperationException("Assign a TileView prefab before refilling.");
            }

            var targets = new HashSet<GridPosition>();
            foreach (TileSpawn spawn in spawns)
            {
                if (tileViews.ContainsKey(spawn.Position))
                {
                    throw new InvalidOperationException(
                        $"({spawn.Position.X},{spawn.Position.Y}) already shows a tile.");
                }

                if (!targets.Add(spawn.Position))
                {
                    throw new ArgumentException("Each cell can only be refilled once.", nameof(spawns));
                }
            }

            return AnimateRefillRoutine(spawns, duration);
        }

        private IEnumerator AnimateFallsRoutine(IReadOnlyList<TileMove> moves, TileView[] views, float duration)
        {
            var starts = new Vector3[views.Length];
            var destinations = new Vector3[views.Length];
            for (int i = 0; i < views.Length; i++)
            {
                starts[i] = views[i].transform.position;
                destinations[i] = GetWorldPosition(moves[i].To);
            }

            // Forward each frame of the shared movement (rather than nesting a coroutine), so a zero
            // duration finishes in the same step and this reads as one flat sequence.
            var movement = MoveTogether(views, starts, destinations, duration);
            while (movement.MoveNext())
            {
                yield return movement.Current;
            }

            // The lookup changes only once everything has landed. Remove every old cell first: a cell can be
            // one tile's From and another tile's To, and must end up holding the tile that arrived.
            foreach (TileMove move in moves)
            {
                tileViews.Remove(move.From);
            }

            for (int i = 0; i < views.Length; i++)
            {
                tileViews[moves[i].To] = views[i];
                views[i].SetGridPosition(moves[i].To);
            }
        }

        private IEnumerator AnimateRefillRoutine(IReadOnlyList<TileSpawn> spawns, float duration)
        {
            Transform root = tileRoot != null ? tileRoot : transform;

            // How many new tiles each column receives, so they start stacked just above their cells, in order.
            var spawnsPerColumn = new Dictionary<int, int>();
            foreach (TileSpawn spawn in spawns)
            {
                spawnsPerColumn.TryGetValue(spawn.Position.X, out int count);
                spawnsPerColumn[spawn.Position.X] = count + 1;
            }

            var views = new TileView[spawns.Count];
            var starts = new Vector3[spawns.Count];
            var destinations = new Vector3[spawns.Count];
            for (int i = 0; i < spawns.Count; i++)
            {
                TileSpawn spawn = spawns[i];
                var view = Instantiate(tilePrefab, root, false);
                view.Initialize(spawn.Position, spawn.Type);
                // Presentation picks the start: the same distance above the target as the column's spawn count.
                var start = new GridPosition(spawn.Position.X, spawn.Position.Y + spawnsPerColumn[spawn.Position.X]);
                starts[i] = GetWorldPosition(start);
                destinations[i] = GetWorldPosition(spawn.Position);
                view.SetWorldPosition(starts[i]);
                view.gameObject.SetActive(true);
                views[i] = view;
            }

            var movement = MoveTogether(views, starts, destinations, duration);
            while (movement.MoveNext())
            {
                yield return movement.Current;
            }

            // Register the new views only once they have landed, matching how falls update the lookup.
            for (int i = 0; i < spawns.Count; i++)
            {
                tileViews.Add(spawns[i].Position, views[i]);
            }
        }

        // Moves all views with the same eased progress (the swap curve), then snaps them exactly onto their destinations.
        // Every view finishes together, so a tile with farther to go simply moves faster.
        private static IEnumerator MoveTogether(TileView[] views, Vector3[] starts, Vector3[] destinations, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float progress = TileEasing.EaseInOut(elapsed / duration);
                for (int i = 0; i < views.Length; i++)
                {
                    views[i].SetWorldPosition(Vector3.Lerp(starts[i], destinations[i], progress));
                }

                yield return null;
                elapsed += Time.deltaTime;
            }

            // Snap exactly, including when a zero duration skips movement entirely.
            for (int i = 0; i < views.Length; i++)
            {
                views[i].SetWorldPosition(destinations[i]);
            }
        }

        private static void ValidateDuration(float duration)
        {
            if (duration < 0f || float.IsNaN(duration) || float.IsInfinity(duration))
            {
                throw new ArgumentOutOfRangeException(nameof(duration), duration, "Duration must be finite and nonnegative.");
            }
        }

        private Vector3 GetWorldPosition(GridPosition position)
        {
            Transform root = tileRoot != null ? tileRoot : transform;
            // Convert a cell address into an offset, then apply the root's position, rotation, and scale.
            var localPosition = new Vector3(position.X * tileSpacing, position.Y * tileSpacing, 0f);
            return root.TransformPoint(localPosition);
        }

        private void OnDestroy()
        {
            // Clean up owned tiles when Unity invokes this component's destruction callback.
            ClearViews();
        }

        private void ClearViews()
        {
            foreach (var view in tileViews.Values)
            {
                // Unity treats an already-destroyed tile object as null.
                if (view == null)
                {
                    continue;
                }

                DestroyView(view);
            }

            // Forget removed views; clearing the dictionary alone does not destroy GameObjects.
            tileViews.Clear();
        }

        private static void DestroyView(TileView view)
        {
            // Runtime destruction is deferred to the end of the frame, so hide the tile immediately.
            view.gameObject.SetActive(false);
            if (Application.isPlaying)
            {
                Destroy(view.gameObject);
            }
            else
            {
                // Outside Play Mode, remove the object immediately for Editor validation.
                DestroyImmediate(view.gameObject);
            }
        }
    }
}
