using System;
using NUnit.Framework;
using UnityEngine;

namespace Match3.Presentation.Tests
{
    public class ProgressionStoreTests
    {
        private const int LevelCount = 6;
        private string key;
        private ProgressionStore store;

        [SetUp]
        public void SetUp()
        {
            key = "Match3.Tests.Progression." + Guid.NewGuid().ToString("N");
            store = new ProgressionStore(LevelCount, key);
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_NonpositiveLevelCount_IsRejected(int levelCount)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ProgressionStore(levelCount, key));
            Assert.That(PlayerPrefs.HasKey(key), Is.False);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void Constructor_EmptyKey_IsRejected(string invalidKey)
        {
            Assert.Throws<ArgumentException>(() => new ProgressionStore(LevelCount, invalidKey));
        }

        [Test]
        public void Load_MissingKey_ReturnsZeroWithoutCreatingAKey()
        {
            Assert.That(store.Load(), Is.Zero);
            Assert.That(PlayerPrefs.HasKey(key), Is.False);
        }

        [TestCase(int.MinValue, 0)]
        [TestCase(-1, 0)]
        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(5, 5)]
        [TestCase(6, 6)]
        [TestCase(7, 6)]
        [TestCase(int.MaxValue, 6)]
        public void Load_StoredValue_IsClampedWithoutWriting(int stored, int expected)
        {
            PlayerPrefs.SetInt(key, stored);
            Assert.That(store.Load(), Is.EqualTo(expected));
            Assert.That(PlayerPrefs.GetInt(key), Is.EqualTo(stored), "Loading must not write preferences.");
        }

        [Test]
        public void Save_AdvancesProgress_AndANewStoreReadsIt()
        {
            Assert.That(store.Save(1), Is.EqualTo(1));
            Assert.That(store.Save(3), Is.EqualTo(3));
            Assert.That(PlayerPrefs.GetInt(key), Is.EqualTo(3));
            Assert.That(new ProgressionStore(LevelCount, key).Load(), Is.EqualTo(3));
        }

        [TestCase(0)]
        [TestCase(2)]
        [TestCase(4)]
        public void Save_EarlierOrSameProgress_NeverLowersIt(int requested)
        {
            PlayerPrefs.SetInt(key, 4);
            Assert.That(store.Save(requested), Is.EqualTo(4));
            Assert.That(new ProgressionStore(LevelCount, key).Load(), Is.EqualTo(4));
        }

        [TestCase(1)]
        [TestCase(6)]
        public void Save_FinalCompletion_RemainsCompleteDuringReplay(int levelCount)
        {
            var catalogStore = new ProgressionStore(levelCount, key);
            Assert.That(catalogStore.Save(levelCount), Is.EqualTo(levelCount));
            Assert.That(catalogStore.Save(0), Is.EqualTo(levelCount));
            Assert.That(catalogStore.Save(1), Is.EqualTo(levelCount));
            Assert.That(new ProgressionStore(levelCount, key).Load(), Is.EqualTo(levelCount));
        }

        [TestCase(-20, 1)]
        [TestCase(99, 6)]
        public void Save_NormalizesInvalidStoredData_UsingTheLoadPolicy(int stored, int expected)
        {
            PlayerPrefs.SetInt(key, stored);
            Assert.That(store.Save(1), Is.EqualTo(expected));
            Assert.That(PlayerPrefs.GetInt(key), Is.EqualTo(expected));
        }

        [TestCase(-1)]
        [TestCase(7)]
        public void Save_InvalidRequest_ThrowsBeforeChangingStoredProgress(int requested)
        {
            PlayerPrefs.SetInt(key, 3);
            Assert.Throws<ArgumentOutOfRangeException>(() => store.Save(requested));
            Assert.That(PlayerPrefs.GetInt(key), Is.EqualTo(3));
        }
    }
}
