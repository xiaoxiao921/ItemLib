using RoR2;
using UnityEngine;

namespace ItemLib
{
    public class CustomItem
    {
        public ItemDef ItemDef;
        public GameObject Prefab;
        public Object Icon;

        public CustomItem(ItemDef itemDef, GameObject prefab, Object icon)
        {
            ItemDef = itemDef;
            Prefab = prefab;
            Icon = icon;
        }
    }
}
