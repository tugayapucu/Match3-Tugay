using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Match3.Presentation.Tests
{
    public class MainMenuViewTests
    {
        private GameObject owner;
        private TestMainMenu menu;

        [SetUp]
        public void SetUp()
        {
            owner = new GameObject("MenuTestOwner");
            menu = TestMainMenu.Create(owner.transform);
            menu.View.Initialize(() => { });
        }

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(owner);

        [Test]
        public void ShowAndHide_UpdateLabelAndOnlyMenuObjects()
        {
            menu.View.Show("Continue");
            Assert.That(menu.Label.text, Is.EqualTo("Continue"));
            Assert.That(menu.Background.activeSelf && menu.Controls.activeSelf, Is.True);
            menu.View.Hide();
            Assert.That(menu.Background.activeSelf || menu.Controls.activeSelf, Is.False);
            Assert.That(owner.activeInHierarchy, Is.True);
            menu.View.Show("Play Again");
            Assert.That(menu.Label.text, Is.EqualTo("Play Again"));
            Assert.That(menu.Background.activeInHierarchy && menu.Controls.activeInHierarchy, Is.True);
        }

        [Test]
        public void Reinitialize_ReplacesCallback_WithoutAddingAnotherButtonListener()
        {
            int oldCalls = 0, newCalls = 0;
            menu.View.Initialize(() => oldCalls++);
            menu.View.Initialize(() => newCalls++);
            menu.Play.onClick.Invoke();
            Assert.That(oldCalls, Is.Zero);
            Assert.That(newCalls, Is.EqualTo(1));
        }

        [Test]
        public void Initialize_NullCallback_IsRejected()
        {
            Assert.Throws<ArgumentNullException>(() => menu.View.Initialize(null));
        }

        [TestCase("controls")]
        [TestCase("playButton")]
        [TestCase("playLabel")]
        public void Initialize_MissingWiring_IsRejected(string field)
        {
            typeof(MainMenuView).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(menu.View, null);
            Assert.Throws<InvalidOperationException>(() => menu.View.Initialize(() => { }));
        }
    }
}
