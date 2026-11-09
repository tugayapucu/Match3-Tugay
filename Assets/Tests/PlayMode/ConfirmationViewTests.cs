using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Match3.Presentation.Tests
{
    public class ConfirmationViewTests
    {
        private GameObject owner;
        private TestConfirmation prompt;

        [SetUp]
        public void SetUp()
        {
            owner = new GameObject("ConfirmationTestOwner");
            prompt = TestConfirmation.Create(owner.transform);
            prompt.View.Initialize(() => { }, () => { });
        }

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(owner);

        [TestCase(ConfirmationIntent.Restart, "Restart level?", "Restart")]
        [TestCase(ConfirmationIntent.Leave, "Leave level?", "Leave")]
        public void Show_UsesTheExplicitIntent_AndHideOnlyHidesThePrompt(ConfirmationIntent intent, string title, string label)
        {
            prompt.View.Hide();
            prompt.View.Show(intent);
            Assert.That(prompt.Title.text, Is.EqualTo(title));
            Assert.That(prompt.ConfirmLabel.text, Is.EqualTo(label));
            Assert.That(prompt.Message.text, Does.Contain("this level"));
            Assert.That(prompt.Background.activeSelf && prompt.Card.activeSelf, Is.True);
            prompt.View.Hide();
            Assert.That(prompt.Background.activeSelf || prompt.Card.activeSelf, Is.False);
            Assert.That(owner.activeInHierarchy, Is.True);
        }

        [Test]
        public void Reinitialize_ReplacesTheCallbacks_WithoutDuplicatingListeners()
        {
            int old = 0, confirms = 0, cancels = 0;
            prompt.View.Initialize(() => old++, () => old++);
            prompt.View.Initialize(() => confirms++, () => cancels++);
            prompt.View.Show(ConfirmationIntent.Restart);
            prompt.Confirm.onClick.Invoke();
            Assert.That(confirms, Is.EqualTo(1));
            Assert.That(cancels, Is.Zero);
            prompt.Cancel.onClick.Invoke();
            Assert.That(cancels, Is.EqualTo(1));
            Assert.That(old, Is.Zero);
        }

        [Test]
        public void InvalidArguments_FailBeforeShowingAnything()
        {
            Assert.Throws<ArgumentNullException>(() => prompt.View.Initialize(null, () => { }));
            Assert.Throws<ArgumentNullException>(() => prompt.View.Initialize(() => { }, null));
            prompt.View.Hide();
            Assert.Throws<ArgumentOutOfRangeException>(() => prompt.View.Show((ConfirmationIntent)99));
            Assert.That(prompt.Card.activeSelf || prompt.Background.activeSelf, Is.False);
        }

        [TestCase("card")]
        [TestCase("titleText")]
        [TestCase("messageText")]
        [TestCase("confirmText")]
        [TestCase("confirmButton")]
        [TestCase("cancelButton")]
        public void Initialize_MissingWiring_FailsAtStartup(string field)
        {
            typeof(ConfirmationView).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(prompt.View, null);
            Assert.Throws<InvalidOperationException>(() => prompt.View.Initialize(() => { }, () => { }));
        }
    }
}
