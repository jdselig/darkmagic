using System.Threading;
using UnityEngine;

namespace DarkMagic
{
    /// <summary>
    /// A: animation awaitables (Animator helpers).
    ///
    /// Goal:
    ///     await animator.PlayAndWait("Attack");
    ///     await A.WaitForAnimation(animator, "Attack");
    ///
    /// Notes:
    /// - This waits until the Animator is playing the named state on a layer and that state's normalizedTime >= 1,
    ///   and the Animator is not in transition on that layer.
    /// - "stateName" must match the Animator STATE name (or full path), not necessarily the clip name.
    /// </summary>
    public static class A
    {
        /// <summary>Play a state, then wait for it to finish.</summary>
        public static async Awaitable PlayAndWait(this Animator animator, string stateName, int layer = 0, float normalizedTime = 0f, CancellationToken cancellationToken = default)
        {
            if (animator == null) return;

            animator.Play(stateName, layer, normalizedTime);

            // Give the animator a chance to enter the state immediately this frame.
            animator.Update(0f);

            await WaitForAnimation(animator, stateName, layer, cancellationToken);
        }

        /// <summary>Wait for an Animator state to finish (normalizedTime >= 1) on a given layer.</summary>
        public static async Awaitable WaitForAnimation(Animator animator, string stateName, int layer = 0, CancellationToken cancellationToken = default)
        {
            if (animator == null) return;

            // Wait until we're actually in the named state (in case caller didn't Play first).
            while (true)
            {
                if (cancellationToken.IsCancellationRequested) return;

                var st = animator.GetCurrentAnimatorStateInfo(layer);
                if (st.IsName(stateName)) break;

                await Awaitable.NextFrameAsync(cancellationToken);
            }

            // Then wait until it completes.
            while (true)
            {
                if (cancellationToken.IsCancellationRequested) return;

                var st = animator.GetCurrentAnimatorStateInfo(layer);

                if (st.IsName(stateName) && st.normalizedTime >= 1f && !animator.IsInTransition(layer))
                    return;

                await Awaitable.NextFrameAsync(cancellationToken);
            }
        }

        /// <summary>Hash overload for avoiding string mistakes.</summary>
        public static async Awaitable PlayAndWait(this Animator animator, int stateHash, int layer = 0, float normalizedTime = 0f, CancellationToken cancellationToken = default)
        {
            if (animator == null) return;

            animator.Play(stateHash, layer, normalizedTime);
            animator.Update(0f);

            await WaitForAnimation(animator, stateHash, layer, cancellationToken);
        }

        public static async Awaitable WaitForAnimation(Animator animator, int stateHash, int layer = 0, CancellationToken cancellationToken = default)
        {
            if (animator == null) return;

            while (true)
            {
                if (cancellationToken.IsCancellationRequested) return;

                var st = animator.GetCurrentAnimatorStateInfo(layer);
                if (st.shortNameHash == stateHash || st.fullPathHash == stateHash) break;

                await Awaitable.NextFrameAsync(cancellationToken);
            }

            while (true)
            {
                if (cancellationToken.IsCancellationRequested) return;

                var st = animator.GetCurrentAnimatorStateInfo(layer);

                bool match = (st.shortNameHash == stateHash || st.fullPathHash == stateHash);
                if (match && st.normalizedTime >= 1f && !animator.IsInTransition(layer))
                    return;

                await Awaitable.NextFrameAsync(cancellationToken);
            }
        }
    }
}
