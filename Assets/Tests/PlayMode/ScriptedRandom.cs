using System.Collections.Generic;
using NUnit.Framework;

namespace Match3.Presentation.Tests
{
    // A Random whose results are scripted palette indices, so refills (and cascades) are chosen by the test.
    // Overrides every integer overload, so tests do not depend on which one the code under test calls.
    internal sealed class ScriptedRandom : System.Random
    {
        private readonly Queue<int> values;

        public ScriptedRandom(params int[] values)
        {
            this.values = new Queue<int>(values);
        }

        public override int Next()
        {
            return NextScripted(int.MaxValue);
        }

        public override int Next(int maxValue)
        {
            return NextScripted(maxValue);
        }

        public override int Next(int minValue, int maxValue)
        {
            return minValue + NextScripted(maxValue - minValue);
        }

        private int NextScripted(int range)
        {
            Assert.That(values.Count, Is.GreaterThan(0), "ScriptedRandom ran out of scripted values.");
            int value = values.Dequeue();
            Assert.That(value, Is.InRange(0, range - 1), "Scripted value outside the requested range.");
            return value;
        }
    }
}
