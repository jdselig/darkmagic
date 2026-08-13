using NUnit.Framework;
using UnityEngine;

namespace DarkMagic.Tests
{
    public sealed class EventAndStateTests
    {
        private bool _guardrails;

        private sealed class NumberEvent : V.Event<int> { }
        private sealed class OnceEvent : V.Event { }
        private sealed class OwnedEvent : V.Event<int> { }

        private static class TestStates
        {
            public sealed class Idle { }
            public sealed class Acting { }
        }

        [SetUp]
        public void DisableGuardrailWarnings()
        {
            _guardrails = V.Guardrails;
            V.Guardrails = false;
        }

        [TearDown]
        public void RestoreGuardrailWarnings()
        {
            V.Guardrails = _guardrails;
        }

        [Test]
        public void TypedEventSubscriptionCanBeDisposed()
        {
            var received = 0;
            using (V.OnDisposable<NumberEvent, int>(value => received += value))
            {
                V.Broadcast<NumberEvent, int>(7);
            }
            V.Broadcast<NumberEvent, int>(7);

            Assert.That(received, Is.EqualTo(7));
        }

        [Test]
        public void OnceSubscriptionRunsOnlyOnce()
        {
            var calls = 0;
            V.Once<OnceEvent>(() => calls++);

            V.Broadcast<OnceEvent>();
            V.Broadcast<OnceEvent>();

            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void DestroyedOwnerStopsReceivingEvents()
        {
            var owner = new GameObject("DarkMagic test owner");
            var calls = 0;
            V.On<OwnedEvent, int>(_ => calls++, owner);

            Object.DestroyImmediate(owner);
            V.Broadcast<OwnedEvent, int>(1);

            Assert.That(calls, Is.Zero);
        }

        [Test]
        public void StateRegistryReturnsOneMachinePerOwner()
        {
            var firstOwner = new GameObject("First state owner");
            var secondOwner = new GameObject("Second state owner");

            try
            {
                var first = StateMachineRegistry.For(firstOwner);
                var firstAgain = StateMachineRegistry.For(firstOwner);
                var second = StateMachineRegistry.For(secondOwner);

                Assert.That(firstAgain, Is.SameAs(first));
                Assert.That(second, Is.Not.SameAs(first));
                Assert.That(first.StartIn<TestStates.Idle>(), Is.True);
                Assert.That(first.Go<TestStates.Acting>(), Is.True);
                Assert.That(first.Is<TestStates.Acting>(), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(firstOwner);
                Object.DestroyImmediate(secondOwner);
            }
        }
    }
}
