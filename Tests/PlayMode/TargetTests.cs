using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DarkMagic.Tests
{
    public sealed class TargetTests
    {
        [Test]
        public void AllModeReturnsFilteredTargetsWithoutACamera()
        {
            var first = new GameObject("First target");
            var second = new GameObject("Second target");

            try
            {
                var selection = U.Target.SelectMany(
                    new List<Transform> { first.transform, second.transform },
                    U.TargetMode.All,
                    filter: target => target != second.transform
                );
                var awaiter = selection.GetAwaiter();

                Assert.That(awaiter.IsCompleted, Is.True);
                var result = awaiter.GetResult();
                Assert.That(result.Cancelled, Is.False);
                Assert.That(result.Value, Is.EquivalentTo(new[] { first.transform }));
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
            }
        }

        [Test]
        public void TemporaryAllOptionDoesNotMutateCallerRules()
        {
            var rules = new U.TargetRules { AllowAll = false };
            var selection = U.Target.SelectMany(
                new List<Transform>(),
                all: true,
                rules: rules
            );
            var awaiter = selection.GetAwaiter();

            Assert.That(awaiter.IsCompleted, Is.True);
            Assert.That(awaiter.GetResult().Cancelled, Is.True);
            Assert.That(rules.AllowAll, Is.False);
        }
    }
}
