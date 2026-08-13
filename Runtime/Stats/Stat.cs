using System;
using UnityEngine;

namespace DarkMagic
{
    /// <summary>
    /// A simple JRPG-friendly stat: Base + temp (buff/debuff) + equipment,
    /// with an optional Remaining value for "resource" stats (HP/MP/etc).
    /// </summary>
    [Serializable]
    public class Stat
    {
        // Identity
        public string Name;          // e.g. "Strength"
        public string Abbreviation;  // e.g. "STR"

        // Core values
        public int Initial;     // starting base at creation
        public int Base;        // persistent base (max for resources)
        public int Remaining;   // current remaining (for resources). Ignored by normal stats.
        public int Delta = 1;   // amount applied by LevelUp()
        public int Threshold = 0; // e.g. XP needed to level; 0 disables threshold logic

        // Modifiers
        public int TempModifiers;       // buffs/debuffs for the encounter/session
        public int EquipmentModifiers;  // gear modifiers

        // Behavior flags
        public bool IsResource; // if true, +=/-= affect Remaining (heal/damage) instead of TempModifiers
        public bool Persists;   // if false, Remaining resets to Base when Refresh() is called
        public bool IsLethal;   // if Remaining/Current hits 0 => disabled
        public bool IsDisabled;

        // Events (optional, lightweight)
        public event Action<Stat, int, int> OnBaseChanged;      // (stat, oldBase, newBase)
        public event Action<Stat> OnThresholdMet;

        /// <summary>
        /// Current practical value. Normal stats use Base + modifiers;
        /// resource stats use Remaining, capped at that effective max.
        /// </summary>
        public int Current => CalculateCurrent();

        public Stat() { }

        public Stat(string name, string abbr, int initial, bool persists = false, bool isLethal = false, bool isResource = false)
        {
            Name = name;
            Abbreviation = abbr;

            Initial = initial;
            Base = initial;
            Remaining = initial;

            Persists = persists;
            IsLethal = isLethal;
            IsResource = isResource;
            IsDisabled = false;

            TempModifiers = 0;
            EquipmentModifiers = 0;

            Delta = 1;
            Threshold = 0;
        }

        /// <summary>
        /// Normal stats use their effective value. Resource stats use Remaining,
        /// capped at the effective max.
        /// </summary>
        public int CalculateCurrent()
        {
            int effectiveMax = Base + TempModifiers + EquipmentModifiers;
            if (!IsResource) return effectiveMax;
            return Remaining < effectiveMax ? Remaining : effectiveMax;
        }

        /// <summary>
        /// Refresh at end of encounter/session. If force==true, refill even persistent stats (e.g. Inn).
        /// </summary>
        public void Refresh(bool force = false)
        {
            TempModifiers = 0;

            if (!Persists || force)
                Remaining = Base;

            ClampRemainingToMax();
            ClampAndCheckLethal();
        }

        public void Heal(int amount)
        {
            if (amount <= 0) return;

            int effectiveMax = Base + TempModifiers + EquipmentModifiers;
            Remaining = Mathf.Min(Remaining + amount, effectiveMax);

            CheckThreshold();
            ClampRemainingToMax();
            ClampAndCheckLethal();
        }

        public void Damage(int amount)
        {
            if (amount <= 0) return;

            Remaining -= amount;
            CheckThreshold();
            ClampRemainingToMax();
            ClampAndCheckLethal();
        }

        public void ModifyTemp(int amount)
        {
            TempModifiers += amount;
            CheckThreshold();
            ClampRemainingToMax();
            ClampAndCheckLethal();
        }

        /// <summary>Adds a temporary bonus. Non-positive amounts are ignored.</summary>
        public void Buff(int amount)
        {
            if (amount <= 0) return;
            ModifyTemp(amount);
        }

        /// <summary>Adds a temporary penalty. Non-positive amounts are ignored.</summary>
        public void Debuff(int amount)
        {
            if (amount <= 0) return;
            ModifyTemp(-amount);
        }

        /// <summary>Changes the equipment bonus, then keeps resource values in range.</summary>
        public void ModifyEquipment(int amount)
        {
            EquipmentModifiers += amount;
            CheckThreshold();
            ClampRemainingToMax();
            ClampAndCheckLethal();
        }

        /// <summary>
        /// Persistent base change (like Pokemon vitamins, level-ups, permanent debuffs).
        /// </summary>
        public void ModifyBase(int amount)
        {
            int old = Base;
            Base += amount;

            // Clamp Remaining to the new effective max.
            int effectiveMax = Base + TempModifiers + EquipmentModifiers;
            Remaining = Mathf.Min(Remaining, effectiveMax);

            OnBaseChanged?.Invoke(this, old, Base);
            CheckThreshold();
            ClampRemainingToMax();
            ClampAndCheckLethal();
        }

        /// <summary>
        /// Applies Delta to Base. Common for leveling systems.
        /// For resource stats, also refills Remaining to the new effective max.
        /// </summary>
        public void LevelUp()
        {
            ModifyBase(Delta);
            if (IsResource)
            {
                int effectiveMax = Base + TempModifiers + EquipmentModifiers;
                Remaining = effectiveMax;
            }
        }

        /// <summary>
        /// For stats like XP: checks whether Current has reached Threshold.
        /// Threshold values of zero or less disable the check.
        /// </summary>
        public void CheckThreshold()
        {
            if (Threshold <= 0) return;
            if (Current >= Threshold)
                OnThresholdMet?.Invoke(this);
        }


        /// <summary>
        /// Ensures Remaining does not exceed the current effective max (Base + modifiers).
        /// </summary>
        public void ClampRemainingToMax()
        {
            int effectiveMax = Base + TempModifiers + EquipmentModifiers;
            Remaining = Mathf.Min(Remaining, effectiveMax);
        }

        private void ClampAndCheckLethal()
        {
            int effectiveMax = Base + TempModifiers + EquipmentModifiers;
            Remaining = Mathf.Clamp(Remaining, IsLethal ? 0 : int.MinValue, effectiveMax);

            if (IsLethal && Remaining <= 0)
                IsDisabled = true;
        }

        // ------------------------
        // Syntactic sugar
        // ------------------------

        /// <summary>Use a Stat in an int context to get Current.</summary>
        public static implicit operator int(Stat s) => s?.Current ?? 0;

        /// <summary>Nice in Debug.Log: prints Current.</summary>
        public override string ToString() => Current.ToString();

        /// <summary>
        /// Adds a TEMP modifier and returns the same stat (allows: stat += 5 when the stat is a field).
        /// </summary>
        public static Stat operator +(Stat s, int amt)
        {
            if (s == null) return null;
            if (s.IsResource) s.Heal(amt);
            else s.ModifyTemp(amt);
            return s;
        }

        /// <summary>
        /// Adds a TEMP modifier and returns the same stat (allows: stat -= 5 when the stat is a field).
        /// </summary>
        public static Stat operator -(Stat s, int amt)
        {
            if (s == null) return null;
            if (s.IsResource) s.Damage(amt);
            else s.ModifyTemp(-amt);
            return s;
        }

        /// <summary>
        /// Applies a BASE change (persistent). Example: (stats.STR >> -4) for permanent -4 STR.
        /// Note: C# does not allow overloading a custom '>>>' operator; we use >> as the "base" operator.
        /// </summary>
        public static Stat operator >>(Stat s, int baseDelta)
        {
            if (s == null) return null;
            s.ModifyBase(baseDelta);
            return s;
        }
    }
}
