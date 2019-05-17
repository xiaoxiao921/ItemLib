using System;
using System.Collections.Generic;
using MonoMod;
using UnityEngine;

namespace RoR2
{
    internal class patch_ItemCatalog
    {
        public static ItemDef GetItemDef(ItemIndex itemIndex)
        {
            return ItemLib.ItemLib.GetItemDef(itemIndex);
        }
    }

    [Serializable]
    internal struct patch_PickupIndex
    {
        private static readonly int OriginalItemCount = ItemLib.ItemLib.OriginalItemCount;
        private static readonly int CustomItemCount = ItemLib.ItemLib.CustomItemCount;
        private static readonly int TotalItemCount = OriginalItemCount + CustomItemCount;

        private static readonly int OriginalEquipmentCount = ItemLib.ItemLib.OriginalEquipmentCount;
        private static readonly int CustomEquipmentCount = ItemLib.ItemLib.CustomEquipmentCount;
        private static readonly int TotalEquipmentCount = OriginalEquipmentCount + CustomEquipmentCount;

        private static readonly int CoinCount = ItemLib.ItemLib.CoinCount;

        [MonoModConstructor]
        public patch_PickupIndex(EquipmentIndex equipmentIndex)
        {
            value = (int)((equipmentIndex < EquipmentIndex.CommandMissile) ? EquipmentIndex.None : ((TotalItemCount) + equipmentIndex));
        }

        [MonoModConstructor]
        static patch_PickupIndex()
        {
            allPickupNames[0] = "None";
            for (ItemIndex itemIndex = ItemIndex.Syringe; itemIndex < (ItemIndex)TotalItemCount; itemIndex++)
            {
                allPickupNames[(int)(1 + itemIndex)] = "ItemIndex." + itemIndex.ToString();
            }
            for (EquipmentIndex equipmentIndex = EquipmentIndex.CommandMissile; equipmentIndex < (EquipmentIndex)TotalEquipmentCount; equipmentIndex++)
            {
                allPickupNames[(int)(TotalItemCount + 1 + equipmentIndex)] = "EquipmentIndex." + equipmentIndex.ToString();
            }
            for (int i = TotalItemCount + TotalEquipmentCount; i < TotalItemCount + TotalEquipmentCount + CoinCount; i++)
            {
                allPickupNames[1 + i] = "LunarCoin.Coin" + (i - TotalItemCount + TotalEquipmentCount);
            }
            stringToPickupIndexTable = new Dictionary<string, PickupIndex>();
            for (int j = 0; j < allPickupNames.Length; j++)
            {
                stringToPickupIndexTable.Add(allPickupNames[j], new PickupIndex((ItemIndex)j - 1));
            }
        }

        // no use until UserProfile fix

        /*public bool isValid
        {
            get
            {
                return value < TotalItemCount + TotalEquipmentCount + CoinCount;
            }
        }*/

        public GameObject GetPickupDisplayPrefab()
        {
            if (value >= 0)
            {
                if (value < TotalItemCount)
                {
                    return Resources.Load<GameObject>(ItemLib.ItemLib.GetItemDef((ItemIndex)value).pickupModelPath);
                }
                if (value < TotalItemCount + TotalEquipmentCount)
                {
                    return Resources.Load<GameObject>(EquipmentCatalog.GetEquipmentDef((EquipmentIndex)(value - TotalItemCount)).pickupModelPath);
                }
                if (value < TotalItemCount + TotalEquipmentCount + CoinCount)
                {
                    return Resources.Load<GameObject>("Prefabs/PickupModels/PickupLunarCoin");
                }
            }
            return null;
        }

        public GameObject GetPickupDropletDisplayPrefab()
        {
            if (value >= 0)
            {
                if (value < TotalItemCount)
                {
                    ItemDef itemDef = ItemLib.ItemLib.GetItemDef((ItemIndex)value);
                    string path = null;
                    switch (itemDef.tier)
                    {
                        case ItemTier.Tier1:
                            path = "Prefabs/ItemPickups/Tier1Orb";
                            break;
                        case ItemTier.Tier2:
                            path = "Prefabs/ItemPickups/Tier2Orb";
                            break;
                        case ItemTier.Tier3:
                            path = "Prefabs/ItemPickups/Tier3Orb";
                            break;
                        case ItemTier.Lunar:
                            path = "Prefabs/ItemPickups/LunarOrb";
                            break;
                    }
                    if (!string.IsNullOrEmpty(path))
                    {
                        return Resources.Load<GameObject>(path);
                    }
                    return null;
                }
                else
                {
                    if (value < TotalItemCount + TotalEquipmentCount)
                    {
                        return Resources.Load<GameObject>("Prefabs/ItemPickups/EquipmentOrb");
                    }
                    if (value < TotalItemCount + TotalEquipmentCount + CoinCount)
                    {
                        return Resources.Load<GameObject>("Prefabs/ItemPickups/LunarOrb");
                    }
                }
            }
            return null;
        }

        public Color GetPickupColor()
        {
            if (value >= 0)
            {
                if (value < TotalItemCount)
                {
                    return ColorCatalog.GetColor(ItemLib.ItemLib.GetItemDef((ItemIndex)value).colorIndex);
                }
                if (value < TotalItemCount + TotalEquipmentCount)
                {
                    return ColorCatalog.GetColor(EquipmentCatalog.GetEquipmentDef((EquipmentIndex)(value - TotalItemCount)).colorIndex);
                }
                if (value < TotalItemCount + TotalEquipmentCount + CoinCount)
                {
                    return ColorCatalog.GetColor(ColorCatalog.ColorIndex.LunarItem);
                }
            }
            return Color.black;
        }

        public Color GetPickupColorDark()
        {
            if (value >= 0)
            {
                if (value < TotalItemCount)
                {
                    return ColorCatalog.GetColor(ItemLib.ItemLib.GetItemDef((ItemIndex)value).darkColorIndex);
                }
                if (value < TotalItemCount + TotalEquipmentCount)
                {
                    return ColorCatalog.GetColor(EquipmentCatalog.GetEquipmentDef((EquipmentIndex)(value - TotalItemCount)).colorIndex);
                }
                if (value < TotalItemCount + TotalEquipmentCount + CoinCount)
                {
                    return ColorCatalog.GetColor(ColorCatalog.ColorIndex.LunarItem);
                }
            }
            return Color.black;
        }

        public string GetPickupNameToken()
        {
            if (value >= 0)
            {
                if (value < TotalItemCount)
                {
                    return ItemLib.ItemLib.GetItemDef((ItemIndex)value).nameToken;
                }
                if (value < TotalItemCount + TotalEquipmentCount)
                {
                    return EquipmentCatalog.GetEquipmentDef((EquipmentIndex)(value - TotalItemCount)).nameToken;
                }
                if (value < TotalItemCount + TotalEquipmentCount + CoinCount)
                {
                    return "PICKUP_LUNAR_COIN";
                }
            }
            return "???";
        }

        public string GetUnlockableName()
        {
            if (value >= 0)
            {
                if (value < TotalItemCount)
                {
                    return ItemLib.ItemLib.GetItemDef((ItemIndex)value).unlockableName;
                }
                if (value < TotalItemCount + TotalEquipmentCount)
                {
                    return EquipmentCatalog.GetEquipmentDef((EquipmentIndex)(value - TotalItemCount)).unlockableName;
                }
            }
            return "";
        }

        public bool IsLunar()
        {
            if (value >= 0)
            {
                if (value < TotalItemCount)
                {
                    return ItemLib.ItemLib.GetItemDef((ItemIndex)value).tier == ItemTier.Lunar;
                }
                if (value < TotalItemCount + TotalEquipmentCount)
                {
                    return EquipmentCatalog.GetEquipmentDef((EquipmentIndex)(value - TotalItemCount)).isLunar;
                }
            }
            return false;
        }

        public bool IsBoss()
        {
            if (value >= 0)
            {
                if (value < TotalItemCount)
                {
                    return ItemLib.ItemLib.GetItemDef((ItemIndex)value).tier == ItemTier.Boss;
                }
                if (value < TotalItemCount + TotalEquipmentCount)
                {
                    return EquipmentCatalog.GetEquipmentDef((EquipmentIndex)(value - TotalItemCount)).isBoss;
                }
            }
            return false;
        }

        public ItemIndex itemIndex
        {
            get
            {
                if (value < 0 || value >= TotalItemCount)
                {
                    return ItemIndex.None;
                }
                return (ItemIndex)value;
            }
        }

        public EquipmentIndex equipmentIndex
        {
            get
            {
                if (value < TotalItemCount || value >= TotalItemCount + TotalEquipmentCount)
                {
                    return EquipmentIndex.None;
                }
                return (EquipmentIndex)(value - TotalItemCount);
            }
        }

        public int coinIndex
        {
            get
            {
                if (value < TotalItemCount + TotalEquipmentCount || value >= TotalItemCount + TotalEquipmentCount + CoinCount)
                {
                    return -1;
                }
                return value - TotalItemCount + TotalEquipmentCount;
            }
        }

        [SerializeField]
        public readonly int value;

        public static readonly string[] allPickupNames = new string[TotalItemCount + TotalEquipmentCount + CoinCount + 1];
        public static readonly Dictionary<string, PickupIndex> stringToPickupIndexTable;
    }
}