using RoR2;
using UnityEngine;

namespace ItemLib
{
    public class CustomItem
    {
        public ItemDef ItemDef;
        public GameObject Prefab;
        public Object Icon;

        public ItemDisplayRule[] ItemDisplayRules;

        public CustomItem(ItemDef itemDef, GameObject prefab, Object icon, ItemDisplayRule[] itemDisplayRules)
        {
            ItemDef = itemDef;
            Prefab = prefab;
            Icon = icon;

            ItemDisplayRules = itemDisplayRules;
        }
    }
}
