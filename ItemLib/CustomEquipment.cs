using RoR2;
using UnityEngine;

namespace ItemLib
{
    public class CustomEquipment
    {
        public EquipmentDef EquipmentDef;
        public GameObject Prefab;
        public Object Icon;

        public ItemDisplayRule[] ItemDisplayRules;

        public CustomEquipment(EquipmentDef equipmentDef, GameObject prefab, Object icon, ItemDisplayRule[] itemDisplayRules)
        {
            EquipmentDef = equipmentDef;
            Prefab = prefab;
            Icon = icon;

            ItemDisplayRules = itemDisplayRules;
        }
    }
}