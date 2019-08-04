using System;
using System.Collections.Generic;
using System.Text;
using RoR2;
using UnityEngine;

namespace ItemLib
{
    public sealed class CustomBuff
    {
        public string Name;
        public BuffDef BuffDef;
        public Sprite Icon;

        public CustomBuff(string name, BuffDef buffDef, Sprite icon)
        {
            Name = name;
            BuffDef = buffDef;
            Icon = icon;
        }
    }
}
