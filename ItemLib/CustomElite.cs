using System;
using System.Collections.Generic;
using System.Text;
using RoR2;

namespace ItemLib
{
    /// <summary>
    /// Class that defines a custom Elite type for use in the game.
    /// All Elites consistent of an Elite definition, a <see cref="CustomEquipment"/>
    /// and a <see cref="CustomBuff"/>.  The equipment is automatically provided to
    /// the Elite when it spawns and is configured to passively apply the buff.
    /// Note that if Elite Spawning Overhaul is enabled, you'll also want to create a <see cref="EliteAffixCard"/>
    /// to allow the combat director to spawn your elite type.
    /// </summary>
    public sealed class CustomElite
    {
        /// <summary>
        /// Name of the Elite, for purposes of looking up its index
        /// </summary>
        public string Name;

        /// <summary>
        /// Elite definition (you can omit the index references, as those will be filled in automatically by ItemLib)
        /// </summary>
        public EliteDef EliteDef;

        /// <summary>
        /// Custom equipment that the Elite will carry; do note that this is something that may (rarely) drop from the Elite when killed,
        /// so players can also end up with this equipment
        /// </summary>
        public CustomEquipment Equipment;

        /// <summary>
        /// Custom buff that is applied passively by the equipment; note that this can be active on the player
        /// if they're using Wake of Vultures or pick up the equipment, so you'll need to decide what impact
        /// the elite buff should have on players.
        /// </summary>
        public CustomBuff Buff;

        /// <summary>
        /// Tier for the elite, where 1 is standard elites (Fire, Ice, Lightning) and 2 is currently just Poison (Malachite).
        /// If Elite Spawning Overhaul is disabled, it will use this tier to set cost/hp/dmg scaling.  Even if your mod is
        /// only intended to work with ESO enabled, this should still be set to a valid number 1-2 for compatibility with
        /// the underlying game code.
        /// </summary>
        public int Tier;

        public CustomElite(string name, EliteDef eliteDef, CustomEquipment equipment, CustomBuff buff, int tier = 1)
        {
            Name = name;
            EliteDef = eliteDef;
            Equipment = equipment;
            Buff = buff;
            Tier = tier;
        }
    }
}
