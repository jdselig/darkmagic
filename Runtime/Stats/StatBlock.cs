using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace DarkMagic
{
    /// <summary>
    /// A collection of stats with ergonomic access and safe lookup.
    /// DarkMagic ethos: easy to use first, more structure later.
    /// </summary>
    [Serializable]
    public class StatBlock
    {
        [SerializeField] private List<Stat> _stats = new();
        private readonly Dictionary<string, Stat> _byKey = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<Stat> All => _stats;

        public StatBlock() { }

        public StatBlock(params Stat[] stats)
        {
            if (stats == null) return;
            foreach (var s in stats) Add(s);
        }

        /// <summary>
        /// Add a stat. Keys are Name and Abbreviation (case-insensitive).
        /// </summary>
        public void Add(Stat s)
        {
            if (s == null) return;

            _stats.Add(s);
            ReindexStat(s);
        }

        /// <summary>
        /// Get a stat by Name or Abbreviation (case-insensitive).
        /// </summary>
        public Stat Get(string nameOrAbbr)
        {
            if (string.IsNullOrWhiteSpace(nameOrAbbr)) return null;
            EnsureIndex();
            _byKey.TryGetValue(nameOrAbbr.Trim(), out var s);
            return s;
        }

        /// <summary>
        /// Indexer sugar: stats["HP"] or stats["Strength"].
        /// </summary>
        /// <summary>
        /// Indexer sugar: stats["HP"] or stats["Strength"].
        /// Setter exists so compound assignments like stats["HP"] -= 10 compile.
        /// The setter is a safe no-op for existing stats, but will Add() if you assign a new Stat instance.
        /// </summary>
        public Stat this[string nameOrAbbr]
        {
            get => Get(nameOrAbbr);
            set
            {
                if (string.IsNullOrWhiteSpace(nameOrAbbr) || value == null) return;
                EnsureIndex();

                // If this key already points to this stat, nothing to do.
                if (_byKey.TryGetValue(nameOrAbbr.Trim(), out var existing) && ReferenceEquals(existing, value))
                    return;

                // Ensure the stat is in the block, then (re)index it.
                if (!_stats.Contains(value))
                    Add(value);
                else
                    ReindexStat(value);
            }
        }

        public int GetInt(string nameOrAbbr) => Get(nameOrAbbr)?.Current ?? 0;

        public void SetBase(string nameOrAbbr, int value)
        {
            var s = GetOrCreate(nameOrAbbr);
            s.Base = value;
            s.Remaining = Mathf.Min(s.Remaining, s.Base + s.TempModifiers + s.EquipmentModifiers);
        }

        public void AddTemp(string nameOrAbbr, int amount) => GetOrCreate(nameOrAbbr).ModifyTemp(amount);

        public void AddBase(string nameOrAbbr, int amount) => GetOrCreate(nameOrAbbr).ModifyBase(amount);

        public void Refresh(bool force = false)
        {
            EnsureIndex();
            foreach (var s in _stats)
                s.Refresh(force);
        }

        /// <summary>
        /// Calls LevelUp() on every stat in the block.
        /// </summary>
        public void LevelUp()
        {
            EnsureIndex();
            foreach (var s in _stats)
                s.LevelUp();
        }

        /// <summary>
        /// Creates a stat if it doesn't exist. If you pass "Strength" it will use "Strength" as name and also as abbreviation.
        /// For better naming, create stats explicitly with name+abbr.
        /// </summary>

        /// <summary>
        /// Typed StatBlock pattern: in a derived class, declare public Stat fields (HP, STR, etc.)
        /// then call AutoRegisterFields() in the derived constructor to register them into the block.
        /// </summary>
        protected void AutoRegisterFields()
        {
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var fields = GetType().GetFields(flags);
            foreach (var f in fields)
            {
                if (f.FieldType != typeof(Stat)) continue;
                var s = (Stat)f.GetValue(this);
                if (s == null) continue;
                if (!_stats.Contains(s))
                    Add(s);
            }
        }
        public Stat GetOrCreate(string nameOrAbbr, int initial = 0)
        {
            var s = Get(nameOrAbbr);
            if (s != null) return s;

            s = new Stat(nameOrAbbr, nameOrAbbr, initial);
            Add(s);
            return s;
        }

        private void EnsureIndex()
        {
            if (_byKey.Count > 0 && _byKey.Count >= _stats.Count * 2) return;

            _byKey.Clear();
            foreach (var s in _stats)
                ReindexStat(s);
        }

        private void ReindexStat(Stat s)
        {
            if (s == null) return;

            if (!string.IsNullOrWhiteSpace(s.Name))
                _byKey[s.Name.Trim()] = s;

            if (!string.IsNullOrWhiteSpace(s.Abbreviation))
                _byKey[s.Abbreviation.Trim()] = s;
        }
    }
}