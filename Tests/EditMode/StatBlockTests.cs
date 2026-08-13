using NUnit.Framework;

namespace DarkMagic.Tests
{
    public sealed class StatBlockTests
    {
        [Test]
        public void LookupIsCaseInsensitiveByNameOrAbbreviation()
        {
            var strength = new Stat("Strength", "STR", 12);
            var stats = new StatBlock(strength);

            Assert.That(stats["strength"], Is.SameAs(strength));
            Assert.That(stats["str"], Is.SameAs(strength));
        }

        [Test]
        public void CompoundAssignmentKeepsTheOriginalStatInTheBlock()
        {
            var strength = new Stat("Strength", "STR", 12);
            var stats = new StatBlock(strength);

            stats["STR"] += 5;

            Assert.That(stats["STR"], Is.SameAs(strength));
            Assert.That(stats.GetInt("STR"), Is.EqualTo(17));
            Assert.That(stats.All.Count, Is.EqualTo(1));
        }

        [Test]
        public void AddDoesNotDuplicateTheSameStatInstance()
        {
            var hp = new Stat("Health", "HP", 20, isResource: true);
            var stats = new StatBlock(hp);

            stats.Add(hp);

            Assert.That(stats.All.Count, Is.EqualTo(1));
        }

        [Test]
        public void SetBaseAndEquipmentHelpersUseStatBehavior()
        {
            var hp = new Stat("Health", "HP", 20, isResource: true);
            var stats = new StatBlock(hp);
            var baseChanges = 0;
            hp.OnBaseChanged += (_, oldValue, newValue) =>
            {
                Assert.That(oldValue, Is.EqualTo(20));
                Assert.That(newValue, Is.EqualTo(15));
                baseChanges++;
            };

            stats.SetBase("HP", 15);
            stats.AddEquipment("HP", 3);

            Assert.That(baseChanges, Is.EqualTo(1));
            Assert.That(hp.Base, Is.EqualTo(15));
            Assert.That(hp.EquipmentModifiers, Is.EqualTo(3));
            Assert.That(hp.Current, Is.EqualTo(15));
        }

        [Test]
        public void GetOrCreateUsesTheRequestedKey()
        {
            var stats = new StatBlock();

            var luck = stats.GetOrCreate("LUCK", 7);

            Assert.That(luck.Name, Is.EqualTo("LUCK"));
            Assert.That(stats.GetInt("luck"), Is.EqualTo(7));
        }
    }
}
