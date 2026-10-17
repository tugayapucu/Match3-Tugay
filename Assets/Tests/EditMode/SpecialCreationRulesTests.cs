using System;
using NUnit.Framework;

namespace Match3.Core.Tests
{
    public class SpecialCreationRulesTests
    {
        // The execution plan's concrete shapes, plus basic orientation and priority cases.
        [TestCase("RRR", 3, TileType.Empty, 0, 0)]
        [TestCase("RRRR", 4, TileType.HorizontalRocket, 1, 0)]
        [TestCase("R/R/R/R", 4, TileType.VerticalRocket, 0, 1)]
        [TestCase("RRRRR", 5, TileType.LightBall, 2, 0)]
        [TestCase("R/R/R/R/R", 5, TileType.LightBall, 0, 2)]
        [TestCase("RRRRRR", 6, TileType.LightBall, 2, 0)]
        [TestCase("R/R/R/R/R/R", 6, TileType.LightBall, 0, 2)]
        [TestCase("RRRR/RRRR", 8, TileType.HorizontalRocket, 1, 0)]
        [TestCase(".R./RRR/.R.", 5, TileType.TNT, 1, 1)]
        [TestCase("RRR/.R./.R.", 5, TileType.TNT, 1, 2)]
        [TestCase("RRR/..R/..R", 5, TileType.TNT, 2, 2)]
        [TestCase("RR/RR", 4, TileType.Propeller, 0, 0)]
        [TestCase("RR/RR/RR", 6, TileType.Propeller, 0, 0)]
        [TestCase("RR./RRR", 5, TileType.Propeller, 0, 0)]
        [TestCase("RRR./.R../.RRR/..R./..R.", 9, TileType.TNT, 1, 2)]
        [TestCase("RRRR/RR../R...", 7, TileType.TNT, 0, 2)]
        [TestCase("RRRRR/..R../..R..", 7, TileType.LightBall, 2, 2)]
        [TestCase("RRRR/RR..", 6, TileType.HorizontalRocket, 1, 1)]
        [TestCase("RRR/RRR/RRR", 9, TileType.TNT, 0, 0)]
        public void ApprovedShapes_ChooseOneHighestPriorityCreation(
            string layout, int matchedCount, TileType type, int x, int y)
        {
            var board = TestBoards.Create(layout.Split('/'));
            var analysis = MatchFinder.Analyze(board);

            var creations = SpecialCreationRules.FindCreations(analysis);

            Assert.That(analysis.MatchedPositions.Count, Is.EqualTo(matchedCount));
            Assert.That(analysis.Groups.Count, Is.EqualTo(1));
            if (type == TileType.Empty)
            {
                Assert.That(creations, Is.Empty);
            }
            else
            {
                Assert.That(creations, Is.EqualTo(new[] { new TileSpawn(new GridPosition(x, y), type) }));
            }

            // Policy reports a placement; applying it belongs to the next step.
            Assert.That(board.GetTile(new GridPosition(x, y)), Is.EqualTo(TileType.Red));
        }

        [TestCase("RRRR.GGGG")]
        [TestCase("RRRR.RRRR")]
        public void DisjointGroups_EachGetACreation(string layout)
        {
            var analysis = MatchFinder.Analyze(TestBoards.Create(layout));

            var creations = SpecialCreationRules.FindCreations(analysis);

            Assert.That(analysis.Groups.Count, Is.EqualTo(2));
            Assert.That(analysis.MatchedPositions.Count, Is.EqualTo(8));
            Assert.That(creations, Is.EqualTo(new[]
            {
                new TileSpawn(new GridPosition(1, 0), TileType.HorizontalRocket),
                new TileSpawn(new GridPosition(6, 0), TileType.HorizontalRocket)
            }));
        }

        [TestCase("RRRR", 0, 0, 3, 0, 3, 0)]
        [TestCase("RRRR", 3, 0, 4, 0, 3, 0)]
        [TestCase("RRRR", 4, 0, 5, 0, 1, 0)]
        [TestCase("RR/RR", 0, 0, 1, 1, 1, 1)]
        [TestCase(".R./RRR/.R.", 1, 0, 2, 1, 2, 1)]
        [TestCase("RRRR/RRRR", 0, 0, 3, 1, 3, 1)]
        public void PlayerAnchors_PreferEligibleSecondThenFirstThenDefault(
            string layout, int firstX, int firstY, int secondX, int secondY, int expectedX, int expectedY)
        {
            var analysis = MatchFinder.Analyze(TestBoards.Create(layout.Split('/')));

            var creations = SpecialCreationRules.FindCreations(analysis,
                new GridPosition(firstX, firstY), new GridPosition(secondX, secondY));

            Assert.That(creations.Count, Is.EqualTo(1));
            Assert.That(creations[0].Position, Is.EqualTo(new GridPosition(expectedX, expectedY)));
        }

        [Test]
        public void SelectedCellInLosingPattern_CannotAnchorWinningLightBall()
        {
            var analysis = MatchFinder.Analyze(TestBoards.Create("RRRRR", "..R..", "..R.."));

            var creations = SpecialCreationRules.FindCreations(analysis,
                new GridPosition(0, 2), new GridPosition(2, 0));

            Assert.That(creations, Is.EqualTo(new[]
            {
                new TileSpawn(new GridPosition(0, 2), TileType.LightBall)
            }));
        }

        [Test]
        public void OnlyLosingPatternsContainSelectedCells_UsesWinningDefaultAnchor()
        {
            var analysis = MatchFinder.Analyze(TestBoards.Create("RRRRR", "..R..", "..R.."));

            var creations = SpecialCreationRules.FindCreations(analysis,
                new GridPosition(2, 1), new GridPosition(2, 0));

            Assert.That(creations[0], Is.EqualTo(new TileSpawn(new GridPosition(2, 2), TileType.LightBall)));
        }

        [Test]
        public void CascadeScan_HasNoLeakedPlayerPreference()
        {
            var analysis = MatchFinder.Analyze(TestBoards.Create("RRRR"));

            var player = SpecialCreationRules.FindCreations(analysis, new GridPosition(2, 0), new GridPosition(3, 0));
            var cascade = SpecialCreationRules.FindCreations(analysis);

            Assert.That(player[0].Position, Is.EqualTo(new GridPosition(3, 0)));
            Assert.That(cascade[0].Position, Is.EqualTo(new GridPosition(1, 0)));
        }

        [Test]
        public void EqualPriorityAnchors_AreOrderedByYThenX()
        {
            var board = TestBoards.Create("RRR", "R.R", "RRR");
            var analysis = MatchFinder.Analyze(board);
            for (int scan = 0; scan < 10; scan++)
            {
                var creations = SpecialCreationRules.FindCreations(MatchFinder.Analyze(board));
                Assert.That(creations[0], Is.EqualTo(new TileSpawn(new GridPosition(0, 0), TileType.TNT)));
            }

            // Any equally winning crossing may accept the selected arm cell, not only the default crossing.
            var selected = SpecialCreationRules.FindCreations(analysis, second: new GridPosition(2, 2));
            Assert.That(selected[0].Position, Is.EqualTo(new GridPosition(2, 2)));
        }

        [Test]
        public void NoPatterns_ProducesNoCreations()
        {
            Assert.That(SpecialCreationRules.FindCreations(MatchFinder.Analyze(TestBoards.Create("RGB"))), Is.Empty);
        }

        [Test]
        public void NullAnalysis_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => SpecialCreationRules.FindCreations(null));
        }
    }
}
