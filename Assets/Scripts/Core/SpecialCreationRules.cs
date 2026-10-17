using System;
using System.Collections.Generic;

namespace Match3.Core
{
    // Interprets shape facts and returns placements only; it never changes the Board or scores tiles.
    public static class SpecialCreationRules
    {
        // Pass swap cells only for the first normal-pattern scan; cascades use the defaults.
        public static IReadOnlyList<TileSpawn> FindCreations(
            MatchAnalysis analysis, GridPosition? first = null, GridPosition? second = null)
        {
            if (analysis == null)
            {
                throw new ArgumentNullException(nameof(analysis));
            }

            var creations = new List<TileSpawn>();
            foreach (MatchGroup group in analysis.Groups)
            {
                TileSpawn? best = null;
                int bestPriority = 0;
                int bestPreference = -1;
                foreach (Candidate candidate in GetCandidates(group))
                {
                    GridPosition anchor = candidate.DefaultAnchor;
                    int preference = 0;
                    if (second.HasValue && candidate.Contains(second.Value))
                    {
                        anchor = second.Value;
                        preference = 2;
                    }
                    else if (first.HasValue && candidate.Contains(first.Value))
                    {
                        anchor = first.Value;
                        preference = 1;
                    }

                    int priority = GetPriority(candidate.Type);
                    // Winning kind first, player preference second, then a geometric Y/X tie-break.
                    if (!best.HasValue || priority > bestPriority
                        || (priority == bestPriority && preference > bestPreference)
                        || (priority == bestPriority && preference == bestPreference
                            && ComesBefore(anchor, best.Value.Position)))
                    {
                        best = new TileSpawn(anchor, candidate.Type);
                        bestPriority = priority;
                        bestPreference = preference;
                    }
                }

                if (best.HasValue)
                {
                    creations.Add(best.Value);
                }
            }

            return creations.AsReadOnly();
        }

        private static List<Candidate> GetCandidates(MatchGroup group)
        {
            var candidates = new List<Candidate>();
            foreach (MatchPattern pattern in group.Patterns)
            {
                if (pattern.Kind == MatchPatternKind.Square)
                {
                    candidates.Add(new Candidate(TileType.Propeller, pattern.Positions[0], pattern));
                    continue;
                }

                // Runs are maximal, so a five-line isn't also reported as four-line subsets.
                int length = pattern.Positions.Count;
                GridPosition center = pattern.Positions[(length - 1) / 2];
                if (length >= 5)
                {
                    candidates.Add(new Candidate(TileType.LightBall, center, pattern));
                }
                else if (length == 4)
                {
                    TileType rocket = pattern.Kind == MatchPatternKind.HorizontalRun
                        ? TileType.HorizontalRocket : TileType.VerticalRocket;
                    candidates.Add(new Candidate(rocket, center, pattern));
                }

                if (pattern.Kind != MatchPatternKind.HorizontalRun)
                {
                    continue;
                }

                foreach (MatchPattern vertical in group.Patterns)
                {
                    if (vertical.Kind != MatchPatternKind.VerticalRun)
                    {
                        continue;
                    }

                    foreach (GridPosition cell in pattern.Positions)
                    {
                        if (vertical.Contains(cell))
                        {
                            // Each maximal run is already 3+, so a crossing qualifies as T/L/plus.
                            candidates.Add(new Candidate(TileType.TNT, cell, pattern, vertical));
                            break;
                        }
                    }
                }
            }

            return candidates;
        }

        private static int GetPriority(TileType type)
        {
            switch (type)
            {
                case TileType.LightBall: return 4;
                case TileType.TNT: return 3;
                case TileType.HorizontalRocket:
                case TileType.VerticalRocket: return 2;
                case TileType.Propeller: return 1;
                default: throw new ArgumentOutOfRangeException(nameof(type), type, "Not a creation candidate.");
            }
        }

        private static bool ComesBefore(GridPosition first, GridPosition second)
        {
            return first.Y < second.Y || (first.Y == second.Y && first.X < second.X);
        }

        // A qualifying shape is one run/square, or two crossing runs for TNT.
        private readonly struct Candidate
        {
            public TileType Type { get; }
            public GridPosition DefaultAnchor { get; }
            private readonly MatchPattern firstPattern;
            private readonly MatchPattern secondPattern;

            public Candidate(TileType type, GridPosition anchor, MatchPattern first, MatchPattern second = null)
            {
                Type = type;
                DefaultAnchor = anchor;
                firstPattern = first;
                secondPattern = second;
            }

            public bool Contains(GridPosition position)
            {
                return firstPattern.Contains(position) || (secondPattern != null && secondPattern.Contains(position));
            }
        }
    }
}
