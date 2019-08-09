using System;
using System.Collections.Generic;
using System.Text;
using RoR2;
using UnityEngine;

namespace ItemLib
{
    /// <summary>
    /// Class that defines a custom buff type for use in the game;
    /// you may omit the index in the BuffDef, as that will
    /// be assigned by ItemLib.
    /// </summary>
    public sealed class CustomBuff
    {
        /// <summary>
        /// Name of the Buff for the purposes of looking up its index
        /// </summary>
        public string Name;

        /// <summary>
        /// Definition of the Buff
        /// </summary>
        public BuffDef BuffDef;

        /// <summary>
        /// Icon that will be displayed when this Buff is active
        /// </summary>
        public Sprite Icon;

        public CustomBuff(string name, BuffDef buffDef, Sprite icon)
        {
            Name = name;
            BuffDef = buffDef;
            Icon = icon;
        }
    }
}
