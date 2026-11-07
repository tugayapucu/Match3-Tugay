using System;
using UnityEngine;

namespace Match3.Presentation
{
    // Ordered configuration only. List index is level identity; session construction validates gameplay values.
    [CreateAssetMenu(fileName = "LevelCatalog", menuName = "Match3/Level Catalog")]
    public sealed class LevelCatalog : ScriptableObject
    {
        [SerializeField] private LevelDefinition[] levels = new LevelDefinition[0];

        public int Count => levels?.Length ?? 0;

        public LevelDefinition this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), index, "Level index must be inside the catalog.");
                }

                return levels[index];
            }
        }

        public void Validate()
        {
            if (Count == 0)
            {
                throw new InvalidOperationException("Assign at least one level to the catalog.");
            }
        }
    }
}
