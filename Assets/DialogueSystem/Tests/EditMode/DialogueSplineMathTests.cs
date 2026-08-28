using System;
using DialogueSystem.UI;
using NUnit.Framework;
using UnityEngine;

namespace DialogueSystem.Tests
{
    public sealed class DialogueSplineMathTests
    {
        [Test]
        public void EvaluateSegment_UsesExactEndpoints()
        {
            var points = new[]
            {
                new Vector3(-2f, 0f, 0f),
                new Vector3(0f, 1f, 0f),
                new Vector3(3f, 1f, 2f),
                new Vector3(5f, 0f, 3f)
            };

            Assert.That(DialogueSplineMath.EvaluateSegment(points, 1, 2, 0f), Is.EqualTo(points[1]));
            Assert.That(DialogueSplineMath.EvaluateSegment(points, 1, 2, 1f), Is.EqualTo(points[2]));
        }

        [Test]
        public void EvaluateSegment_WithTwoPointsFallsBackToLinearInterpolation()
        {
            var points = new[] { Vector3.zero, new Vector3(10f, 0f, 0f) };

            Assert.That(
                DialogueSplineMath.EvaluateSegment(points, 0, 1, 0.25f),
                Is.EqualTo(new Vector3(2.5f, 0f, 0f)));
        }

        [Test]
        public void EvaluateSegment_WithOnePointReturnsTheOnlyPosition()
        {
            var point = new Vector3(2f, 3f, 4f);

            Assert.That(DialogueSplineMath.EvaluateSegment(new[] { point }, 0, 0, 0.5f), Is.EqualTo(point));
        }

        [Test]
        public void EvaluateSegment_ClampsProgressToTheSegment()
        {
            var points = new[] { Vector3.zero, Vector3.right * 4f };

            Assert.That(DialogueSplineMath.EvaluateSegment(points, 0, 1, -2f), Is.EqualTo(points[0]));
            Assert.That(DialogueSplineMath.EvaluateSegment(points, 0, 1, 3f), Is.EqualTo(points[1]));
        }

        [Test]
        public void EvaluateSegment_WithNoPointsThrowsHelpfulError()
        {
            Assert.That(
                () => DialogueSplineMath.EvaluateSegment(Array.Empty<Vector3>(), 0, 0, 0f),
                Throws.TypeOf<ArgumentException>().With.Message.Contains("控制点"));
        }

        [TestCase(-1, 0)]
        [TestCase(0, 2)]
        public void EvaluateSegment_WithInvalidIndexThrows(int fromIndex, int toIndex)
        {
            var points = new[] { Vector3.zero, Vector3.one };

            Assert.That(
                () => DialogueSplineMath.EvaluateSegment(points, fromIndex, toIndex, 0f),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }
    }
}
