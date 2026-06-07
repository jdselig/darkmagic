using UnityEngine;

namespace DarkMagic
{
    /// <summary>
    /// Copy this file into Assets/Config if you want a clear place to declare typed StatBlocks.
    /// The "typed" pattern gives dot-access like player.Stats.HP and player.Stats.STR.
    /// </summary>
    public static class StatConfig
    {
        // Example typed statblocks for students to copy and customize:

        public sealed class PlayerStats : StatBlock
        {
            public Stat HP  = new("Health", "HP", 120, persists: true,  isLethal: true, isResource: true);
            public Stat MP  = new("Magic",  "MP", 30,  persists: true,  isLethal: false, isResource: true);
            public Stat STR = new("Strength","STR", 12);
            public Stat DEF = new("Defense", "DEF", 10);
            public Stat LVL = new("Level",   "LVL", 1) { Threshold = 99 };

            public PlayerStats()
            {
                AutoRegisterFields();
            }
        }

        public sealed class GoblinStats : StatBlock
        {
            public Stat HP  = new("Health", "HP", 35, persists: true, isLethal: true, isResource: true);
            public Stat STR = new("Strength","STR", 6);
            public Stat DEF = new("Defense", "DEF", 3);

            public GoblinStats()
            {
                AutoRegisterFields();
            }
        }
    }
}
