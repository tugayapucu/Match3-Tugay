using System;
using System.Linq;
using Match3.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Match3.Presentation.Tests
{
    public class LevelCatalogTests
    {
        private const string CatalogPath = "Assets/Levels/InitialLevelCatalog.asset";

        [Test]
        public void EmptyCatalog_IsRejected()
        {
            var catalog = ScriptableObject.CreateInstance<LevelCatalog>();
            try
            {
                Assert.Throws<InvalidOperationException>(() => catalog.Validate());
            }
            finally { Object.DestroyImmediate(catalog); }
        }

        [Test]
        public void ProductionCatalog_HasSixDistinctStartsAndGoals_AndKeepsLevelOne()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(CatalogPath);
            Assert.That(catalog, Is.Not.Null, CatalogPath);
            Assert.DoesNotThrow(() => catalog.Validate());
            Assert.That(catalog.Count, Is.EqualTo(6));
            Assert.That(catalog[0].StartingMoveCount, Is.EqualTo(20));
            Assert.That(catalog[0].GoalTileType, Is.EqualTo(TileType.Red));
            Assert.That(catalog[0].GoalTarget, Is.EqualTo(25));
            Assert.That(catalog[0].Seed, Is.EqualTo(42));
            Assert.That(Enumerable.Range(0, catalog.Count).Select(i => catalog[i].Seed).Distinct().Count(), Is.EqualTo(6));
            Assert.That(Enumerable.Range(0, catalog.Count).Select(i => catalog[i].GoalTileType).Distinct().Count(), Is.EqualTo(6));
        }

        [TestCase(-1)]
        [TestCase(6)]
        public void IndexOutsideProductionCatalog_IsRejected(int index)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(CatalogPath);
            Assert.That(catalog, Is.Not.Null);
            Assert.Throws<ArgumentOutOfRangeException>(() => { var definition = catalog[index]; });
        }

        [Test]
        public void InlineEntry_StillUsesLevelProgressForGameplayValidation()
        {
            var catalog = ScriptableObject.CreateInstance<LevelCatalog>();
            try
            {
                // Unity creates an inline entry with default values when the author expands the list.
                var serialized = new SerializedObject(catalog);
                serialized.FindProperty("levels").arraySize = 1;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.DoesNotThrow(() => catalog.Validate(), "The catalog owns structure, not gameplay rules.");
                var definition = catalog[0];
                Assert.That(definition, Is.Not.Null);
                Assert.Throws<ArgumentOutOfRangeException>(() => new LevelProgress(definition.StartingMoveCount,
                    definition.GoalTileType, definition.GoalTarget, 100));
            }
            finally { Object.DestroyImmediate(catalog); }
        }
    }
}
