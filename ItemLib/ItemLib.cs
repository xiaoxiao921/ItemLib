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
        public static int TotalEquipmentCount;

        public static readonly int CoinCount = 1;

        private static readonly HashSet<MethodInfo> ItemDefHashSet = new HashSet<MethodInfo>();
        private static readonly HashSet<MethodInfo> EquipmentDefHashSet = new HashSet<MethodInfo>();

        private static readonly List<ItemDef> ItemDefList = new List<ItemDef>();
        private static readonly List<EquipmentDef> EquipmentDefList = new List<EquipmentDef>();

        public static IReadOnlyDictionary<string, int> ItemReferences;

        private static bool loaded = false;

        public static void Initialize()
        {
            if (loaded)
                return;

            // https://discordapp.com/channels/562704639141740588/562704639569428506/575081634898903040 ModRecalc implementation ?

            // mod order kind of matters : ItemDef are retrieved through MethodInfo and custom attributes. If they loaded before the Lib and cannot find their items on the Dictionary they should call this method.

            GetAllCustomItemsAndEquipments();
            TotalItemCount = OriginalItemCount + CustomItemCount;
            TotalEquipmentCount = OriginalEquipmentCount + CustomEquipmentCount;

            InitCatalogHook();

            // Call DefineItems because catalog is already made...
            // Also hooking on it execute body method, EmitDelegate not included.

            MethodInfo defineItems = typeof(ItemCatalog).GetMethod("DefineItems", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            defineItems.Invoke(null, null);

            InitHooks();

            loaded = true;
        }

        public static int GetItemID(string name)
        {
            if (ItemReferences == null)
                Initialize();

            int id;
            ItemReferences.TryGetValue(name, out id);

            return id;
        }

        public static void GetAllCustomItemsAndEquipments()
        {
            List<Assembly> allAssemblies = new List<Assembly>();
            string path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            foreach (string dll in Directory.GetFiles(path, "*.dll"))
                allAssemblies.Add(Assembly.LoadFile(dll));

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
                                ItemDefHashSet.Add(methodInfo);
                            else
                            {
                                EquipmentDefHashSet.Add(methodInfo);
                            }
                        }
                    }
                }
            }

            foreach (MethodInfo mi in ItemDefHashSet)
            {
                ItemDefList.Add((ItemDef)mi.Invoke(null, null)); // Mods should return an itemDef so we can load it.
            }

            foreach (MethodInfo mi in EquipmentDefHashSet)
            {
                EquipmentDefList.Add((EquipmentDef)mi.Invoke(null, null));
            }

            CustomItemCount = ItemDefHashSet.Count;
            CustomEquipmentCount = EquipmentDefHashSet.Count;
        }

        public static ItemDef GetItemDef(ItemIndex itemIndex)
        {
            int index = (int)itemIndex;
            ItemDef[] array = (ItemDef[]) typeof(ItemCatalog)
                .GetField("itemDefs", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .GetValue(null); // reflection for each getitemdef call ? delete this 

            if (index < 0 || index > TotalItemCount)
                return null;
            return array[index];
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
                        object[] para = { (ItemIndex)(i + OriginalItemCount), ItemDefList[i] };
                        registerItem.Invoke(null, para);
                        //Debug.Log("adding custom item at index : " + (i + OriginalItemCount));

                        tmp.Add(ItemDefList[i].nameToken , i + OriginalItemCount);
                    }
                    Debug.Log("[ItemLib] Added " + _customItemCount + " custom items");
                    ItemReferences = new ReadOnlyDictionary<string, int>(tmp);
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
                        i => i.MatchLdcI4(_originalItemCount)
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

            typeof(PickupIndex).SetFieldValue("lunarCoin1", new PickupIndex((ItemIndex)TotalItemCount + TotalEquipmentCount));
            typeof(PickupIndex).SetFieldValue("last", new PickupIndex((ItemIndex)TotalItemCount + TotalEquipmentCount));
            typeof(PickupIndex).SetFieldValue("end", new PickupIndex((ItemIndex)TotalItemCount + TotalEquipmentCount + CoinCount));

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
                cursor.Next.Operand = TotalEquipmentCount;
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
                cursor.Next.Operand = TotalEquipmentCount;
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
        }
#if DEBUG
        [Item(ItemAttribute.ItemType.Item)]
        public static ItemDef Test()
        {
            ItemDef newItemDef = new ItemDef
            {
                tier = ItemTier.Tier1,
                pickupModelPath = "Prefabs/PickupModels/PickupWolfPelt",
                pickupIconPath = "Textures/ItemIcons/texWolfPeltIcon",
                nameToken = "AAAAAAAAAAAAAA",
                pickupToken = "PFGHKOPFGHKPOH",
                descriptionToken = "LETS GO",
                addressToken = ""
            };
            return newItemDef;
        }
#endif
    }
}