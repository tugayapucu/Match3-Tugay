using System.Collections.Generic;

namespace Match3.Core
{
    public enum SwapKind
    {
        Invalid,
        NormalMatch,
        SpecialSwap
    }

    // Facts about an already-applied swap. Special activation behavior is introduced in later steps.
    public sealed class SwapAnalysis
    {
        public SwapKind Kind { get; }
        public GridPosition First { get; }
        public GridPosition Second { get; }
        public IReadOnlyList<SpecialActivation> InitialActivations { get; }

        internal SwapAnalysis(SwapKind kind, GridPosition first, GridPosition second,
            IEnumerable<SpecialActivation> initialActivations = null)
        {
            Kind = kind;
            First = first;
            Second = second;
            InitialActivations = new List<SpecialActivation>(
                initialActivations ?? System.Array.Empty<SpecialActivation>()).AsReadOnly();
        }
    }
}
