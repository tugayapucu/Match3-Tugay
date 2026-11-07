#if UNITY_EDITOR
using System.Collections;
using Match3.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Match3.Presentation.Tests
{
    public partial class BoardBootstrapTests
    {
        private static readonly int[] CatalogIndices = { 0, 1, 2, 3, 4, 5 };
        // The real-scene test checks these fixture inputs against SampleScene, including palette order.
        internal const int CatalogBoardWidth = 8;
        internal const int CatalogBoardHeight = 8;
        internal static readonly TileType[] CatalogPalette =
        {
            TileType.Red, TileType.Green, TileType.Blue, TileType.Yellow, TileType.Purple, TileType.Orange
        };

        [UnityTest]
        public IEnumerator CatalogLevel_StartsWithItsConfiguration_StablePlayableAndDeterministic(
            [ValueSource(nameof(CatalogIndices))] int index)
        {
            // AssetDatabase is Editor-only; this is an Editor PlayMode content/session check.
            var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelCatalog>("Assets/Levels/InitialLevelCatalog.asset");
            Assert.That(catalog, Is.Not.Null);
            catalog.Validate();
            definition = catalog[index];
            Set("width", CatalogBoardWidth);
            Set("height", CatalogBoardHeight);
            Set("palette", CatalogPalette);
            Set("initialLevel", definition);
            inputCamera.transform.position = new Vector3(203.5f, 203.5f, -10f);
            inputCamera.orthographicSize = 8f;
            yield return StartBootstrap();

            var board = CurrentBoard();
            var progress = bootstrap.LevelProgress;
            var startingTiles = ReadTiles();
            Assert.That(progress.MovesRemaining, Is.EqualTo(definition.StartingMoveCount));
            Assert.That(progress.GoalTileType, Is.EqualTo(definition.GoalTileType));
            Assert.That(progress.GoalTarget, Is.EqualTo(definition.GoalTarget));
            Assert.That(progress.Score, Is.Zero);
            Assert.That(progress.GoalProgress, Is.Zero);
            Assert.That(progress.Status, Is.EqualTo(LevelStatus.Playing));
            Assert.That(MatchFinder.FindMatches(board), Is.Empty);
            Assert.That(BoardPlayability.HasAnyLegalMove(board), Is.True);
            bool goalPresent = false;
            for (int y = 0; y < board.Height; y++)
                for (int x = 0; x < board.Width; x++)
                    goalPresent |= board.GetTile(new GridPosition(x, y)) == definition.GoalTileType;
            Assert.That(goalPresent, Is.True);
            AssertViewsMatchBoard();

            StartConfiguredLevel();
            yield return null;
            Assert.That(CurrentBoard(), Is.Not.SameAs(board));
            Assert.That(bootstrap.LevelProgress, Is.Not.SameAs(progress));
            Assert.That(ReadTiles(), Is.EqualTo(startingTiles));
            Assert.That(controller.SelectedPosition, Is.Null);
            Assert.That(controller.enabled, Is.True);
            Assert.That(boardObject.GetComponentsInChildren<TileView>(true).Length, Is.EqualTo(64));
            AssertViewsMatchBoard();
        }
    }
}
#endif
