using System;
using System.Globalization;
using System.Threading;
using NUnit.Framework;

namespace Match3.Presentation.Tests
{
    public class HudFormatTests
    {
        [TestCase(0, "0")]
        [TestCase(7, "7")]
        [TestCase(22, "22")]
        [TestCase(222222, "222222")]
        [TestCase(1234567, "1234567")]
        public void Count_IsPlainDigits(int value, string expected)
        {
            Assert.That(HudFormat.Count(value), Is.EqualTo(expected));
        }

        [TestCase(6, "/ 6")]
        [TestCase(22, "/ 22")]
        [TestCase(1, "/ 1")]
        public void OutOf_PrefixesTheTotal(int total, string expected)
        {
            Assert.That(HudFormat.OutOf(total), Is.EqualTo(expected));
        }

        // Cultures that group digits or use another minus sign must not change the HUD.
        [TestCase("de-DE")]
        [TestCase("fr-FR")]
        [TestCase("tr-TR")]
        public void Formatting_IgnoresTheDeviceCulture(string cultureName)
        {
            CultureInfo previous = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo(cultureName);
                Assert.That(HudFormat.Count(1234567), Is.EqualTo("1234567"));
                Assert.That(HudFormat.OutOf(22), Is.EqualTo("/ 22"));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previous;
            }
        }

        [TestCase(0, 22, 0f)]
        [TestCase(11, 22, 0.5f)]
        [TestCase(21, 22, 21f / 22f)]
        [TestCase(22, 22, 1f)]
        [TestCase(30, 22, 1f)]
        [TestCase(1, 1, 1f)]
        public void GoalFill_IsTheClampedFraction(int progress, int target, float expected)
        {
            Assert.That(HudFormat.GoalFill(progress, target), Is.EqualTo(expected));
        }

        [Test]
        public void InvalidValues_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => HudFormat.Count(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => HudFormat.OutOf(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => HudFormat.OutOf(-3));
            Assert.Throws<ArgumentOutOfRangeException>(() => HudFormat.GoalFill(1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => HudFormat.GoalFill(-1, 22));
        }
    }
}
