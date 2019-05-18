using RoR2;
using UnityEngine;

namespace ItemLib
{
    public class CustomEquipment
    {
        public EquipmentDef EquipmentDef;
        public GameObject Prefab;
        public Object Icon;

        public CustomEquipment(EquipmentDef equipmentDef, GameObject prefab, Object icon)
        {
            EquipmentDef = equipmentDef;
            Prefab = prefab;
            Icon = icon;
        }
    }
}