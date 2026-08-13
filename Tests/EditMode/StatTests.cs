using NUnit.Framework;

namespace DarkMagic.Tests
{
    public sealed class StatTests
    {
        [Test]
        public void NormalStatCurrentUsesBaseAndModifiers()
        {
            var strength = new Stat("Strength", "STR", 12);

            strength.Remaining = 1;
            strength.Buff(5);
            strength.ModifyEquipment(2);

            Assert.That(strength.Current, Is.EqualTo(19));
        }

        [Test]
        public void ResourceCurrentUsesRemainingAndCapsAtEffectiveMax()
        {
            var hp = new Stat("Health", "HP", 20, isResource: true);

            hp.Damage(7);
            Assert.That(hp.Current, Is.EqualTo(13));

            hp.Remaining = 100;
            Assert.That(hp.Current, Is.EqualTo(20));
        }

        [Test]
        public void ResourceOperatorsHealAndDamageWhileNormalOperatorsModifyTemp()
        {
            var hp = new Stat("Health", "HP", 20, isResource: true);
            var strength = new Stat("Strength", "STR", 10);

            hp -= 8;
            hp += 3;
            strength += 4;
            strength -= 1;

            Assert.That(hp.Remaining, Is.EqualTo(15));
            Assert.That(strength.TempModifiers, Is.EqualTo(3));
            Assert.That(strength.Current, Is.EqualTo(13));
        }

        [Test]
        public void BuffDebuffAndEquipmentChangesStayBeginnerFriendly()
        {
            var defense = new Stat("Defense", "DEF", 10);

            defense.Buff(4);
            defense.Debuff(2);
            defense.ModifyEquipment(3);
            defense.Buff(0);
            defense.Debuff(-2);

            Assert.That(defense.TempModifiers, Is.EqualTo(2));
            Assert.That(defense.EquipmentModifiers, Is.EqualTo(3));
            Assert.That(defense.Current, Is.EqualTo(15));
        }

        [Test]
        public void ResourceLevelUpRefillsToNewEffectiveMax()
        {
            var hp = new Stat("Health", "HP", 20, isResource: true)
            {
                Delta = 5,
                EquipmentModifiers = 2,
            };
            hp.Damage(10);

            hp.LevelUp();

            Assert.That(hp.Base, Is.EqualTo(25));
            Assert.That(hp.Remaining, Is.EqualTo(27));
            Assert.That(hp.Current, Is.EqualTo(27));
        }

        [Test]
        public void PermanentChangesDoNotReplaceConfiguredLevelUpDelta()
        {
            var strength = new Stat("Strength", "STR", 10) { Delta = 3 };

            strength.ModifyBase(1);
            strength.LevelUp();

            Assert.That(strength.Delta, Is.EqualTo(3));
            Assert.That(strength.Base, Is.EqualTo(14));
        }

        [Test]
        public void ThresholdZeroIsDisabledAndPositiveThresholdRaisesEvent()
        {
            var xp = new Stat("Experience", "XP", 0);
            var events = 0;
            xp.OnThresholdMet += _ => events++;

            xp.Buff(10);
            Assert.That(events, Is.Zero);

            xp.Threshold = 12;
            xp.Buff(2);
            Assert.That(events, Is.EqualTo(1));
        }
    }
}
