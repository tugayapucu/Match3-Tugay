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
            tileTemplate = new GameObject("ResolutionTestTileTemplate");
            tileTemplate.SetActive(false);
            var template = tileTemplate.AddComponent<TileView>();

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
                board, boardView, fullPalette, new ScriptedRandom(0, 2, 3, 1, 0, 5, 1, 4, 1), 0.05f, 0.05f);

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
        public IEnumerator Runner_SameSeedTwice_ProducesTheSameResult()
        {
            var firstResult = new List<TileType>();
            var board = CreateBoard(
                "GBY",
                "GPO",
                "GRG");
            boardView.Render(board);
            yield return new BoardResolutionRunner(board, boardView, FullPalette, new System.Random(5), 0f, 0f)
                .ResolveUntilStable(r => firstResult.AddRange(r.ClearedTiles));
            var firstBoard = ReadTiles(board);

            var secondResult = new List<TileType>();
            board = CreateBoard(
                "GBY",
                "GPO",
                "GRG");
            boardView.Render(board);
            yield return new BoardResolutionRunner(board, boardView, FullPalette, new System.Random(5), 0f, 0f)
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
            var runner = new BoardResolutionRunner(board, boardView, FullPalette, refillRandom, 0.05f, 0.05f);
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
