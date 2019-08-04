using System;
using System.Collections.Generic;
using System.Text;
using RoR2;

namespace ItemLib
{
    public sealed class CustomElite
    {
        public string Name;
        public EliteDef EliteDef;
        public CustomEquipment Equipment;
        public CustomBuff Buff;
        public int Tier;

        public CustomElite(string name, EliteDef eliteDef, CustomEquipment equipment, CustomBuff buff, int tier)
        {
            Name = name;
            EliteDef = eliteDef;
            Equipment = equipment;
            Buff = buff;
            Tier = tier;
        }
    }
}
