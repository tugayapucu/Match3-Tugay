using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Match3.Core.Tests
{
    public class MatchAnalysisTests
    {
        [Test]
        public void Analyze_MaximalRuns_DoNotReportOverlappingShorterSubsets()
        {
            var analysis = MatchFinder.Analyze(TestBoards.Create("RRRRRR"));

            Assert.That(analysis.Groups.Count, Is.EqualTo(1));
            var patterns = analysis.Groups[0].Patterns;
            Assert.That(patterns.Count, Is.EqualTo(1));
            Assert.That(patterns[0].Kind, Is.EqualTo(MatchPatternKind.HorizontalRun));
            Assert.That(patterns[0].Positions.Count, Is.EqualTo(6));
            Assert.That(patterns[0].Positions[0], Is.EqualTo(new GridPosition(0, 0)));
            Assert.That(patterns[0].Positions[5], Is.EqualTo(new GridPosition(5, 0)));
        }

        [Test]
        public void Analyze_OverlappingSquares_ArePatternsInOneGroupWithoutDuplicateCells()
        {
            var analysis = MatchFinder.Analyze(TestBoards.Create("RR", "RR", "RR"));

            Assert.That(analysis.MatchedPositions.Count, Is.EqualTo(6));
            var group = analysis.Groups[0];
            Assert.That(group.Color, Is.EqualTo(TileType.Red));
            Assert.That(group.Patterns.Count, Is.EqualTo(4), "Two vertical runs and two squares.");
            int squares = 0;
            foreach (MatchPattern pattern in group.Patterns)
            {
                if (pattern.Kind == MatchPatternKind.Square)
                {
                    squares++;
                    Assert.That(pattern.Positions.Count, Is.EqualTo(4));
                }
            }
            Assert.That(squares, Is.EqualTo(2));
            Assert.That(new HashSet<GridPosition>(group.Positions).Count, Is.EqualTo(6));
        }

        [TestCase("RRR/GGG", 2)]
        [TestCase("RRRR/RRRR", 1)]
        [TestCase("RRR.RRR", 2)]
        [TestCase("RRR....../...RRR.../......RRR", 3)]
        public void Analyze_GroupsOnlyOrthogonallyTouchingMatchedCellsOfTheSameColor(string layout, int groups)
        {
            var analysis = MatchFinder.Analyze(TestBoards.Create(layout.Split('/')));

            Assert.That(analysis.Groups.Count, Is.EqualTo(groups));
        }

        [Test]
        public void Analyze_UnmatchedSameColorBridge_DoesNotJoinGroups()
        {
            // The connecting middle row is too short to be a pattern and makes no vertical three.
            var analysis = MatchFinder.Analyze(TestBoards.Create("...RRR", "..RR..", "RRR..."));

            Assert.That(analysis.Groups.Count, Is.EqualTo(2));
            Assert.That(analysis.MatchedPositions.Count, Is.EqualTo(6));
        }

        [TestCase("RG/GR")]
        [TestCase("RR/RG")]
        [TestCase("HH/HH")]
        [TestCase("VV/VV")]
        [TestCase("FF/FF")]
        [TestCase("TT/TT")]
        [TestCase("LL/LL")]
        [TestCase("RR/RH")]
        [TestCase("RHR/..R/..R")]
        public void Analyze_MixedColorsAndSpecials_DoNotMakeFalseShapes(string layout)
        {
            var analysis = MatchFinder.Analyze(TestBoards.Create(layout.Split('/')));

            // The final example has one ordinary vertical three, but no T/L or square.
            Assert.That(SpecialCreationRules.FindCreations(analysis), Is.Empty);
            Assert.That(analysis.MatchedPositions.Count, Is.EqualTo(layout.StartsWith("RHR") ? 3 : 0));
        }

        [Test]
        public void Analysis_IsReadOnlyAndDoesNotTrackLaterBoardChanges()
        {
            var board = TestBoards.Create("RR", "RR");
            var analysis = MatchFinder.Analyze(board);
            var group = analysis.Groups[0];
            var pattern = group.Patterns[0];

            Assert.Throws<NotSupportedException>(() => ((IList<GridPosition>)analysis.MatchedPositions).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<MatchGroup>)analysis.Groups).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<GridPosition>)group.Positions).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<MatchPattern>)group.Patterns).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<GridPosition>)pattern.Positions).Clear());
            BoardResolution.ClearCells(board, analysis.MatchedPositions);

            Assert.That(analysis.MatchedPositions.Count, Is.EqualTo(4));
            Assert.That(group.Color, Is.EqualTo(TileType.Red));
            Assert.That(SpecialCreationRules.FindCreations(analysis)[0].Type, Is.EqualTo(TileType.Propeller));
        }

        [Test]
        public void Analyze_AndCompatibilityWrapper_ReturnSameOrderedUniqueCells()
        {
            var board = TestBoards.Create("GGG..", "..RR.", "..RR.");
            var analysis = MatchFinder.Analyze(board);

            Assert.That(MatchFinder.FindMatches(board), Is.EqualTo(analysis.MatchedPositions));
            Assert.That(analysis.MatchedPositions, Is.EqualTo(new[]
            {
                new GridPosition(2, 0), new GridPosition(3, 0),
                new GridPosition(2, 1), new GridPosition(3, 1),
                new GridPosition(0, 2), new GridPosition(1, 2), new GridPosition(2, 2)
            }));
            Assert.That(analysis.Groups[0].Color, Is.EqualTo(TileType.Red));
            Assert.That(analysis.Groups[1].Color, Is.EqualTo(TileType.Green));
        }

        [Test]
        public void Analyze_NullBoard_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => MatchFinder.Analyze(null));
        }
    }
}
