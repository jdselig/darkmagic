using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DarkMagic.Tests
{
    public sealed class UIFontSmokeTests
    {
        [UnityTest]
        public IEnumerator BundledDynamicFontBuildsVisibleTextMesh()
        {
            const string sample = "DarkMagic 123!?";
            var handle = U.Display(() => sample, U.Placements.MiddleCenter);

            try
            {
                yield return null;
                var display = GameObject.Find("U_Display");
                Assert.That(display, Is.Not.Null);

                var text = display.GetComponentInChildren<TextMeshProUGUI>();
                Assert.That(text, Is.Not.Null);
                Assert.That(text.font, Is.Not.Null);
                text.ForceMeshUpdate();

                Assert.That(text.font.HasCharacters(sample), Is.True);
                Assert.That(text.textInfo.characterCount, Is.EqualTo(sample.Length));
                Assert.That(text.textInfo.meshInfo[0].vertexCount, Is.GreaterThan(0));
                Assert.That(text.preferredWidth, Is.GreaterThan(0));

                var liberation = Resources.Load<TMP_FontAsset>(
                    "Fonts/Liberation/LiberationSans SDF"
                );
                Assert.That(liberation, Is.Not.Null);
                liberation.TryAddCharacters(sample);
                Assert.That(liberation.HasCharacters(sample), Is.True);
            }
            finally
            {
                handle.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator PopOutcomeCreatesAndReleasesVisibleCombatText()
        {
            var cameraObject = new GameObject("DarkMagic outcome camera", typeof(Camera));
            var target = new GameObject("DarkMagic outcome target");
            cameraObject.transform.position = new Vector3(0, 0, -10);
            var oldDuration = UConfig.OutcomeDuration;
            UConfig.OutcomeDuration = 0.05f;

            try
            {
                _ = U.PopOutcome(target.transform, "12", camera: cameraObject.GetComponent<Camera>());
                yield return null;

                var outcome = GameObject.Find("U_Outcome");
                Assert.That(outcome, Is.Not.Null);
                Assert.That(outcome.activeInHierarchy, Is.True);
                Assert.That(outcome.GetComponentInChildren<TMP_Text>().text, Is.EqualTo("12"));

                yield return new WaitForSeconds(0.1f);
                Assert.That(outcome.activeInHierarchy, Is.False);
            }
            finally
            {
                UConfig.OutcomeDuration = oldDuration;
                Object.Destroy(cameraObject);
                Object.Destroy(target);
            }
        }
    }
}
