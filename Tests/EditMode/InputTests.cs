using NUnit.Framework;

namespace DarkMagic.Tests
{
    public sealed class InputTests
    {
        [TearDown]
        public void RemoveTestMappings()
        {
            I.Buttons.Remove("DarkMagicTestButton");
            I.Axes.Remove("DarkMagicTestAxis");
            I.AxesRaw.Remove("DarkMagicTestRawAxis");
        }

        [Test]
        public void CustomMappingsOverrideInputBackends()
        {
            I.Buttons["DarkMagicTestButton"] = () => (true, false, true);
            I.Axes["DarkMagicTestAxis"] = () => 0.25f;
            I.AxesRaw["DarkMagicTestRawAxis"] = () => -1f;

            Assert.That(I.GetButton("darkmagictestbutton"), Is.True);
            Assert.That(I.GetButtonDown("DarkMagicTestButton"), Is.False);
            Assert.That(I.GetButtonUp("DarkMagicTestButton"), Is.True);
            Assert.That(I.GetAxis("DARKMAGICTESTAXIS"), Is.EqualTo(0.25f));
            Assert.That(I.GetAxisRaw("darkmagictestrawaxis"), Is.EqualTo(-1f));
        }
    }
}
