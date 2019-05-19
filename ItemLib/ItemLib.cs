using System;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using MonoMod.Cil;
using RoR2;
using UnityEngine;
using RoR2.Stats;
using Mono.Cecil.Cil;

namespace ItemLib
{
    public static class ItemLib
    {
        public static int OriginalItemCount = (int)ItemIndex.Count;
        private static int _customItemCount;
        public static int CustomItemCount
        {
            get
            {
                if(_customItemCount == 0)
                    GetAllCustomItemsAndEquipments();
                return _customItemCount;
            }
            private set { _customItemCount = value; }
        }
        public static int TotalItemCount;

        public static int OriginalEquipmentCount = (int)EquipmentIndex.Count;
        private static int _customEquipmentCount;
        public static int CustomEquipmentCount
        {
            get
            {
                if (_customEquipmentCount == 0)
                    GetAllCustomItemsAndEquipments();
                return _customEquipmentCount;
            }
            private set { _customEquipmentCount = value; }
        }
        private static int _totalEquipmentCount;

        public static readonly int CoinCount = 1;

        private static readonly HashSet<MethodInfo> CustomItemHashSet = new HashSet<MethodInfo>();
        private static readonly HashSet<MethodInfo> CustomEquipmentHashSet = new HashSet<MethodInfo>();

        public static readonly List<CustomItem> CustomItemList = new List<CustomItem>();
        public static readonly List<CustomEquipment> CustomEquipmentList = new List<CustomEquipment>();

        public static IReadOnlyDictionary<string, int> _itemReferences;
        public static IReadOnlyDictionary<string, int> _equipmentReferences;

        public static bool Initialized;

        internal static void Initialize()
        {
            if (Initialized)
                return;

            // https://discordapp.com/channels/562704639141740588/562704639569428506/575081634898903040 ModRecalc implementation ?

            // mod order don't matter : ItemDef are retrieved through MethodInfo and custom attributes. If they loaded before the Lib and cannot find their items on the Dictionary this get called.

            Debug.Log("[ItemLib] Initializing");

            GetAllCustomItemsAndEquipments();
            TotalItemCount = OriginalItemCount + CustomItemCount;
            _totalEquipmentCount = OriginalEquipmentCount + CustomEquipmentCount;

            InitCatalogHook();

            // Call DefineItems because catalog is already made...
            // Also hooking on it execute body method, EmitDelegate not included.

            MethodInfo defineItems = typeof(ItemCatalog).GetMethod("DefineItems", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            defineItems.Invoke(null, null);

            InitHooks();

            Initialized = true;
        }

        public static int GetItemId(string name)
        {
            if (!Initialized)
                Initialize();

            _itemReferences.TryGetValue(name, out var id);

            return id;
        }

        public static int GetEquipmentId(string name)
        {
            if (!Initialized)
                Initialize();

            _equipmentReferences.TryGetValue(name, out var id);

            return id;
        }

        public static void GetAllCustomItemsAndEquipments()
        {
            if (_customItemCount != 0 || _customEquipmentCount != 0)
                return;

            List<Assembly> allAssemblies = new List<Assembly>();

            string path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            foreach (string dll in Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories))
            {
                if (!dll.Contains("R2API"))
                    allAssemblies.Add(Assembly.LoadFile(dll));
            }
            foreach (Assembly assembly in allAssemblies)
            {
                Type[] types = assembly.GetTypes();

                for (int i = 0; i < types.Length; i++)
                {
                    foreach (var methodInfo in types.SelectMany(x => x.GetMethods()))
                    {
                        var customAttributes = methodInfo.GetCustomAttributes(false);
                        foreach (var attribute in customAttributes.OfType<ItemAttribute>())
                        {
                            if(attribute.Type == ItemAttribute.ItemType.Item)
                                CustomItemHashSet.Add(methodInfo);
                            else
                            {
                                CustomEquipmentHashSet.Add(methodInfo);
                            }
                        }
                    }
                }
            }

            foreach (MethodInfo mi in CustomItemHashSet)
            {
                CustomItemList.Add((CustomItem)mi.Invoke(null, null));
            }

            foreach (MethodInfo mi in CustomEquipmentHashSet)
            {
                CustomEquipmentList.Add((CustomEquipment)mi.Invoke(null, null));
            }

            _customItemCount = CustomItemHashSet.Count;
            _customEquipmentCount = CustomEquipmentHashSet.Count;
        }

        private static void InitCatalogHook()
        {
            var tmp = new Dictionary<string, int>();

            // Make it so itemDefs is large enough for all the new items.
            IL.RoR2.ItemCatalog.DefineItems += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                        i => i.MatchLdcI4(OriginalItemCount)
                );

                cursor.Next.OpCode = OpCodes.Ldc_I4;
                cursor.Next.Operand = TotalItemCount;

                cursor.GotoNext(
                        i => i.MatchLdcI4(0),
                        i => i.MatchStloc(0)
                );
                cursor.Index++;

                cursor.EmitDelegate<Action>(() =>
                {
                    // Register the items into the game and update ItemReferences so the mods know the id of their items.
                    for (int i = 0; i < CustomItemCount; i++)
                    {
                        MethodInfo registerItem = typeof(ItemCatalog).GetMethod("RegisterItem", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                        object[] para = { (ItemIndex)(i + OriginalItemCount), CustomItemList[i].ItemDef };
                        registerItem.Invoke(null, para);
                        //Debug.Log("adding custom item at index : " + (i + OriginalItemCount));

                        tmp.Add(CustomItemList[i].ItemDef.nameToken , i + OriginalItemCount);
                    }
                    Debug.Log("[ItemLib] Added " + _customItemCount + " custom items");
                    _itemReferences = new ReadOnlyDictionary<string, int>(tmp);
                });
            };
        }

        private static void InitHooks()
        {
            IL.RoR2.Inventory.ctor += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                    i => i.MatchLdcI4(OriginalItemCount)
                );
                cursor.Next.OpCode = OpCodes.Ldc_I4;
                cursor.Next.Operand = TotalItemCount;
            };

            IL.RoR2.Inventory.HasAtLeastXTotalItemsOfTier += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                        i => i.MatchLdcI4(OriginalItemCount)
                );
                cursor.Next.OpCode = OpCodes.Ldc_I4;
                cursor.Next.Operand = TotalItemCount;
            };

            IL.RoR2.Inventory.GetTotalItemCountOfTier += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                        i => i.MatchLdcI4(OriginalItemCount)
                );
                cursor.Next.OpCode = OpCodes.Ldc_I4;
                cursor.Next.Operand = TotalItemCount;
            };

            IL.RoR2.Achievements.Discover10UniqueTier1Achievement.UniqueTier1Discovered += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                        i => i.MatchLdcI4(OriginalItemCount)
                );
                cursor.Next.OpCode = OpCodes.Ldc_I4;
                cursor.Next.Operand = TotalItemCount;
            };

            IL.RoR2.Run.BuildDropTable += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                        i => i.MatchLdcI4(OriginalItemCount)
                );
                cursor.Next.OpCode = OpCodes.Ldc_I4;
                cursor.Next.Operand = TotalItemCount;
            };

            IL.RoR2.ItemCatalog.GetItemDef += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                    i => i.MatchLdcI4(OriginalItemCount)
                );
                cursor.Next.OpCode = OpCodes.Ldc_I4;
                cursor.Next.Operand = TotalItemCount;
            };

            IL.RoR2.ItemCatalog.AllItemsEnumerator.MoveNext += il =>
            {
                ILCursor cursor = new ILCursor(il);


                cursor.GotoNext(
                        i => i.MatchLdcI4(OriginalItemCount)
                );
                cursor.Next.OpCode = OpCodes.Ldc_I4;
                cursor.Next.Operand = TotalItemCount;
            };

            IL.RoR2.ItemCatalog.RequestItemOrderBuffer += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                        i => i.MatchLdcI4(OriginalItemCount)
                );
                cursor.Next.OpCode = OpCodes.Ldc_I4;
                cursor.Next.Operand = TotalItemCount; // ble IL instruction, need to do it this way (not removing and emitting lines, but instead changing the line directly)
            };

            IL.RoR2.ItemCatalog.RequestItemStackArray += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                        i => i.MatchLdcI4(OriginalItemCount)
                );
                cursor.Next.OpCode = OpCodes.Ldc_I4;
                cursor.Next.Operand = TotalItemCount;

            };

            /*IL.RoR2.Stats.PerItemStatDef.ctor += il => // no work ? inlined ?
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                        i => i.MatchLdcI4(OriginalItemCount)
                );
                cursor.Next.OpCode = OpCodes.Ldc_I4;
                cursor.Next.Operand = _totalItemCount;
            };*/

            var instancesList = (List<PerItemStatDef>)typeof(PerItemStatDef)
                .GetField("instancesList", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .GetValue(null);
            foreach (PerItemStatDef instance in instancesList)
            {
                typeof(PerItemStatDef).GetField("keyToStatDef", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(instance, new StatDef[78 + CustomItemCount]);
            }

            IL.RoR2.RunReport.PlayerInfo.ctor += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                        i => i.MatchLdcI4(OriginalItemCount)
                );
                cursor.Next.OpCode = OpCodes.Ldc_I4;
                cursor.Next.Operand = TotalItemCount;
            };

            IL.RoR2.RunReport.PlayerInfo.Generate += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                    i => i.MatchLdcI4(OriginalItemCount)
                );
                cursor.Next.OpCode = OpCodes.Ldc_I4;
                cursor.Next.Operand = TotalItemCount;
            };

            // PickupIndex : Some methods are inlined, monomod for that and reflection for some of its fields.

            typeof(PickupIndex).SetFieldValue("lunarCoin1", new PickupIndex((ItemIndex)TotalItemCount + _totalEquipmentCount));
            typeof(PickupIndex).SetFieldValue("last", new PickupIndex((ItemIndex)TotalItemCount + _totalEquipmentCount));
            typeof(PickupIndex).SetFieldValue("end", new PickupIndex((ItemIndex)TotalItemCount + _totalEquipmentCount + CoinCount));
            typeof(PickupIndex).SetFieldValue("none", new PickupIndex((ItemIndex)(-1))); // this is real fucking fuckery fuck.

            // ItemMask

            IL.RoR2.ItemMask.HasItem += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                        i => i.MatchLdcI4(OriginalItemCount)
                );
                cursor.Next.OpCode = OpCodes.Ldc_I4;
                cursor.Next.Operand = TotalItemCount;
            };

            IL.RoR2.ItemMask.AddItem += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                        i => i.MatchLdcI4(OriginalItemCount)
                );
                cursor.Next.OpCode = OpCodes.Ldc_I4;
                cursor.Next.Operand = TotalItemCount;
            };

            IL.RoR2.ItemMask.RemoveItem += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                        i => i.MatchLdcI4(OriginalItemCount)
                );
                cursor.Next.OpCode = OpCodes.Ldc_I4;
                cursor.Next.Operand = TotalItemCount;
            };

            ItemMask all = (ItemMask) typeof(ItemMask)
                .GetField("all", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);
            for (int i = OriginalItemCount; i < TotalItemCount; i++)
                all.AddItem((ItemIndex)i);

            // CharacterModel

            IL.RoR2.CharacterModel.ctor += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                    i => i.MatchLdcI4(OriginalItemCount)
                );
                cursor.Next.OpCode = OpCodes.Ldc_I4;
                cursor.Next.Operand = TotalItemCount;
            };

            IL.RoR2.CharacterModel.UpdateItemDisplay += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                    i => i.MatchLdcI4(OriginalItemCount)
                );
                cursor.Next.OpCode = OpCodes.Ldc_I4;
                cursor.Next.Operand = TotalItemCount;
            };

            // ItemDisplayRuleSet

            IL.RoR2.ItemDisplayRuleSet.ctor += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                    i => i.MatchLdcI4(OriginalItemCount)
                );
                cursor.Next.OpCode = OpCodes.Ldc_I4;
                cursor.Next.Operand = TotalItemCount;

                cursor.GotoNext(
                    i => i.MatchLdcI4(OriginalEquipmentCount)
                );
                cursor.Next.OpCode = OpCodes.Ldc_I4;
                cursor.Next.Operand = _totalEquipmentCount;
            };

            IL.RoR2.ItemDisplayRuleSet.Reset += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                    i => i.MatchLdcI4(OriginalItemCount)
                );
                cursor.Next.OpCode = OpCodes.Ldc_I4;
                cursor.Next.Operand = TotalItemCount;

                cursor.GotoNext(
                    i => i.MatchLdcI4(OriginalEquipmentCount)
                );
                cursor.Next.OpCode = OpCodes.Ldc_I4;
                cursor.Next.Operand = _totalEquipmentCount;
            };

            // RuleCatalog

            IL.RoR2.RuleCatalog.cctor += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                        i => i.MatchLdcI4(OriginalItemCount)
                );
                cursor.Next.OpCode = OpCodes.Ldc_I4;
                cursor.Next.Operand = TotalItemCount;
            };

            List<RuleDef> allRuleDefs = (List<RuleDef>) typeof(RuleCatalog)
                .GetField("allRuleDefs", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .GetValue(null);
            for (int i = OriginalItemCount; i < TotalItemCount; i++)
                allRuleDefs.Add(RuleDef.FromItem((ItemIndex)i));

            // bug UserProfile

            // Ok so this one is kinda a problem : extending the discoveredPickups bool array make it so Rewired cannot put any mouse / kb / gamepad mapping into save files,
            // Why it does that, i don't know.
            // so the band-aid fix is killing SetupPickupsSet by emptying getter and setter.
            // Consequence : Item Logbook game menu get emptied at each game restart, though tracking of unlocked items is still working. (tested it only with the crowbar unlock)

            /*IL.RoR2.UserProfile.ctor += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                    i => i.MatchLdcI4(OriginalItemCount + OriginalEquipmentCount + CoinCount)
                );
                cursor.Next.OpCode = OpCodes.Ldc_I4;
                cursor.Next.Operand = TotalItemCount + TotalEquipmentCount + CoinCount;
            };*/

            On.RoR2.UserProfile.SaveFieldAttribute.SetupPickupsSet += (orig, self, fieldInfo) =>
            {
                self.getter = delegate (UserProfile userProfile)
                {
                    return " ";
                };
                self.setter = delegate (UserProfile userProfile, string valueString)
                {

                };
                self.copier = delegate (UserProfile srcProfile, UserProfile destProfile)
                {
                    Array sourceArray = (bool[])fieldInfo.GetValue(srcProfile);
                    bool[] array = (bool[])fieldInfo.GetValue(destProfile);
                    Array.Copy(sourceArray, array, array.Length);
                };
            };

            IL.RoR2.Orbs.ItemTakenOrbEffect.Start += il =>
            {
                ILCursor cursor = new ILCursor(il);
                var id = 0;

                cursor.GotoNext(
                    i => i.MatchStloc(0)
                );

                cursor.EmitDelegate<Func<ItemDef, ItemDef>>((itemDef) =>
                {
                    id = (int)itemDef.itemIndex;
                    return itemDef;
                });

                cursor.GotoNext(
                    i => i.MatchLdloc(0),
                    i => i.MatchCallvirt<ItemDef>("get_pickupIconSprite"),
                    i => i.MatchStloc(2)
                );
                cursor.Index += 2;

                cursor.EmitDelegate<Func<Sprite, Sprite>>((sprite) =>
                {
                    // check if the item has a custom sprite.
                    if (id != 0)
                    {
                        var itemName = _itemReferences.FirstOrDefault(x => x.Value == id).Key;

                        if (itemName != null)
                        {
                            CustomItem currentCustomItem = CustomItemList.FirstOrDefault(x => x.ItemDef.nameToken.Equals(itemName));
                            if (currentCustomItem != null && currentCustomItem.Icon != null)
                            {
                                return (Sprite)currentCustomItem.Icon;
                            }
                        }
                    }

                    return sprite;
                });
            };

            IL.RoR2.UI.RuleChoiceController.SetChoice += il =>
            {
                ILCursor cursor = new ILCursor(il);
                string name = null;

                cursor.GotoNext(
                    i => i.MatchLdarg(0),
                    i => i.MatchLdarg(1)
                );
                cursor.Index++;

                cursor.EmitDelegate<Func<RuleChoiceDef, RuleChoiceDef>>((ruleChoiceDef) =>
                {
                    name = ruleChoiceDef.tooltipNameToken;
                    return ruleChoiceDef;
                });

                cursor.GotoNext(
                    i => i.MatchLdfld("RoR2.RuleChoiceDef", "spritePath")
                );

                if (name != null)
                {
                    CustomItem currentCustomItem = CustomItemList.FirstOrDefault(x => x.ItemDef.nameToken.Equals(name));
                    if (currentCustomItem != null)
                    {
                        cursor.Index -= 2;
                        cursor.RemoveRange(3);
                        cursor.Index++;
                    }
                }

                cursor.EmitDelegate<Func<Sprite, Sprite>>((sprite) =>
                {
                    if (name != null)
                    {
                        CustomItem currentCustomItem = CustomItemList.FirstOrDefault(x => x.ItemDef.nameToken.Equals(name));
                        if (currentCustomItem != null && currentCustomItem.Icon != null)
                        {
                            return (Sprite) currentCustomItem.Icon;
                        }
                    }

                    return sprite;
                });
            };

            IL.RoR2.UI.GenericNotification.SetItem += il =>
            {
                ILCursor cursor = new ILCursor(il);
                string name = null;

                cursor.GotoNext(
                    i => i.MatchLdfld("RoR2.ItemDef", "nameToken")
                );
                cursor.Index++;

                cursor.EmitDelegate<Func<string, string>>((nameToken) =>
                {
                    name = nameToken;
                    return nameToken;
                });

                cursor.GotoNext(
                    i => i.MatchLdfld("RoR2.UI.GenericNotification", "iconImage")
                );

                if (name != null)
                {
                    CustomItem currentCustomItem = CustomItemList.FirstOrDefault(x => x.ItemDef.nameToken.Equals(name));
                    if (currentCustomItem != null)
                    {
                        cursor.Index++;
                        cursor.RemoveRange(2);
                        cursor.Index++;
                    }
                }

                cursor.EmitDelegate<Func<Texture, Texture>>((texture) =>
                {
                    if (name != null)
                    {
                        CustomItem currentCustomItem = CustomItemList.FirstOrDefault(x => x.ItemDef.nameToken.Equals(name));
                        if (currentCustomItem != null && currentCustomItem.Icon != null)
                        {
                            return (Texture)currentCustomItem.Icon;
                        }
                    }

                    return texture;
                });
            };

            IL.RoR2.UI.ItemIcon.SetItemIndex += il =>
            {
                ILCursor cursor = new ILCursor(il);
                string name = null;

                cursor.GotoNext(
                    i => i.MatchLdfld("RoR2.UI.ItemIcon", "itemIndex"),
                    i => i.MatchCall("RoR2.ItemCatalog", "GetItemDef")
                );
                cursor.Index += 2;

                cursor.EmitDelegate<Func<ItemDef, ItemDef>>((itemDef) =>
                {
                    name = itemDef.nameToken;
                    return itemDef;
                });

                cursor.GotoNext(
                    i => i.MatchLdloc(4),
                    i => i.MatchLdfld("RoR2.ItemDef", "pickupIconPath")
                );

                if (name != null)
                {
                    CustomItem currentCustomItem = CustomItemList.FirstOrDefault(x => x.ItemDef.nameToken.Equals(name));
                    if (currentCustomItem != null)
                    {
                        cursor.Index--;
                        cursor.RemoveRange(2);
                        cursor.Index++;
                    }
                }

                cursor.EmitDelegate<Func<Texture, Texture>>((texture) =>
                {
                    if (name != null)
                    {
                        CustomItem currentCustomItem = CustomItemList.FirstOrDefault(x => x.ItemDef.nameToken.Equals(name));
                        if (currentCustomItem != null && currentCustomItem.Icon != null)
                        {
                            return (Texture)currentCustomItem.Icon;
                        }
                    }

                    return texture;
                });
            };


        }
#if DEBUG
        /*[Item(ItemAttribute.ItemType.Item)]
        public static CustomItem Test()
        {
            ItemDef newItemDef = new ItemDef
            {
                tier = ItemTier.Tier1,
                pickupModelPath = "", // leave it empty and give directly the prefab / icon on the return but you can also use an already made prefab by putting a path in there.
                pickupIconPath = "",
                nameToken = "Item ItemLib DEBUG",
                pickupToken = "",
                descriptionToken = "yes",
                addressToken = ""
            };

            return new CustomItem(newItemDef, null, null);
        }*/
#endif
    }
}