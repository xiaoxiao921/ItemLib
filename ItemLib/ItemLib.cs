﻿using System;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using MonoMod.Cil;
using RoR2;
using UnityEngine;
using RoR2.Stats;
using Mono.Cecil.Cil;
using R2API;
using R2API.Utils;
using RoR2.UI;
using RoR2.UI.LogBook;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMember.Global

namespace ItemLib
{
    public static class ItemLib
    {
        private static int _originalItemCount;

        //public static int OriginalItemCount = (int) ItemIndex.Count;
        public static int OriginalItemCount
        {
            get
            {
                if (_originalItemCount == 0)
                    InitCatalogHook();
                return _originalItemCount;
            }
            private set => _originalItemCount = value;
        }

        private static int _customItemCount;
        public static int CustomItemCount
        {
            get
            {
                if (_customItemCount == 0)
                    GetAllCustomItemsAndEquipments();
                return _customItemCount;
            }
            private set => _customItemCount = value;
        }
        public static int TotalItemCount;

        private static int _originalEquipmentCount;
        public static int OriginalEquipmentCount
        {
            get
            {
                if (_originalEquipmentCount == 0)
                    InitCatalogHook();
                return _originalEquipmentCount;
            }
            private set => _originalEquipmentCount = value;
        }

        private static int _customEquipmentCount;
        public static int CustomEquipmentCount
        {
            get
            {
                if (_customEquipmentCount == 0)
                    GetAllCustomItemsAndEquipments();
                return _customEquipmentCount;
            }
            private set => _customEquipmentCount = value;
        }
        public static int TotalEquipmentCount;

        private static int _originalEliteCount;
        public static int OriginalEliteCount
        {
            get
            {
                if (_originalEliteCount == 0)
                    InitCatalogHook();
                return _originalEliteCount;
            }
            private set => _originalEliteCount = value;
        }

        private static int _customEliteCount;
        public static int CustomEliteCount
        {
            get
            {
                if (_customEliteCount == 0)
                    GetAllCustomItemsAndEquipments();
                return _customEliteCount;
            }
            private set => _customEliteCount = value;
        }
        public static int TotalEliteCount;
        public const int EliteMaxTier = 3;

        private static int _originalBuffCount;
        public static int OriginalBuffCount
        {
            get
            {
                if (_originalBuffCount == 0)
                    InitCatalogHook();
                return _originalBuffCount;
            }
            private set => _originalBuffCount = value;
        }

        private static int _customBuffCount;
        public static int CustomBuffCount
        {
            get
            {
                if (_customBuffCount == 0)
                    GetAllCustomItemsAndEquipments();
                return _customBuffCount;
            }
            private set => _customBuffCount = value;
        }
        public static int TotalBuffCount;

        public static readonly int CoinCount = 1;

        private static readonly HashSet<MethodInfo> CustomItemHashSet = new HashSet<MethodInfo>();
        private static readonly HashSet<MethodInfo> CustomEquipmentHashSet = new HashSet<MethodInfo>();
        private static readonly HashSet<MethodInfo> CustomEliteHashSet = new HashSet<MethodInfo>();
        private static readonly HashSet<MethodInfo> CustomBuffHashSet = new HashSet<MethodInfo>();

        public static readonly List<CustomItem> CustomItemList = new List<CustomItem>();
        public static readonly List<CustomEquipment> CustomEquipmentList = new List<CustomEquipment>();
        public static readonly List<CustomElite> CustomEliteList = new List<CustomElite>();
        public static readonly List<CustomBuff> CustomBuffList = new List<CustomBuff>();

        public static IReadOnlyDictionary<string, int> ItemReferences;
        public static IReadOnlyDictionary<string, int> EquipmentReferences;
        public static IReadOnlyDictionary<string, int> EliteReferences;
        public static IReadOnlyDictionary<string, int> BuffReferences;

        public static bool CountInitialized;
        public static bool CatalogInitialized;
        public static bool ItemDisplayInitialized;

        internal static void Initialize()
        {
            if (CatalogInitialized)
                return;

            // mod order don't matter : ItemDef are retrieved through MethodInfo and custom attributes. If they loaded before the Lib and cannot find their items on the Dictionary this get called, though all mods should have the BepinDependency in their header so it should never happen actually.

            ItemLibPlugin.Logger.LogInfo("Initializing");

            // Checking that the monomod patch is there.

            var methodImplementationFlag = typeof(ItemCatalog).GetMethodCached("GetItemDef").GetMethodImplementationFlags();
            if (!methodImplementationFlag.ToString().Equals("NoInlining"))
            {
                var info = new List<string> {
                    "The ItemLib monomod patch is not properly installed",
                    "Please make sure that a file called Assembly-CSharp.ItemLib.MM.dll ",
                    "is in the BepInEx\\monomod folder"
                };

                ItemLibPlugin.Logger.LogBlockError(info);
            }

            InitCatalogHook();

            GetAllCustomItemsAndEquipments();
            TotalItemCount = OriginalItemCount + CustomItemCount;
            TotalEquipmentCount = OriginalEquipmentCount + CustomEquipmentCount;
            TotalEliteCount = OriginalEliteCount + CustomEliteCount;
            TotalBuffCount = OriginalBuffCount + CustomBuffCount;

            // Call DefineItems because catalog is already made.
            // Also hooking on it execute body method, EmitDelegate not included.

            MethodInfo defineItems = typeof(ItemCatalog).GetMethodCached("DefineItems");
            if (defineItems != null)
            {
                defineItems.Invoke(null, null);
            }
            
            var defineEquipments = typeof(EquipmentCatalog).TypeInitializer;
            defineEquipments.Invoke(null, null);

            var defineElites = typeof(EliteCatalog).TypeInitializer;
            defineElites.Invoke(null, null);

            var defineBuffs = typeof(BuffCatalog).TypeInitializer;
            defineBuffs.Invoke(null, null);

            InitHooks();

            if (CustomEliteList.Count > 0)
            {
                var maxTier = CustomEliteList.Max(e => e.Tier);
                if (maxTier < 2)
                    maxTier = 2;
                if (maxTier > EliteMaxTier)
                    maxTier = EliteMaxTier;

                //Get CombatDirector's existing elite tier table
                var tiersField =
                    typeof(CombatDirector).GetFieldCached("eliteTiers");
                var tiers = (CombatDirector.EliteTierDef[])tiersField.GetValue(null);

                //We're going to replace it with an entirely new table
                var newTiers = new CombatDirector.EliteTierDef[maxTier + 1];
                for (int i = 0; i < newTiers.Length; i++)
                {
                    var newElites = CustomEliteList.Where(e => e.Tier == i).Select(e => e.EliteDef.eliteIndex).ToArray();
                    if (newElites.Length == 0)
                    {
                        newTiers[i] = tiers[i];
                        continue;
                    }

                    if (i < tiers.Length)
                    {
                        //Add new elites to this tier
                        var tier = tiers[i];
                        tier.eliteTypes = tier.eliteTypes.Concat(newElites).ToArray();
                        newTiers[i] = tier;
                    }
                    else
                    {
                        //Create a brand new tier
                        //TODO: Configure parameters
                        var tier = new CombatDirector.EliteTierDef
                        {
                            costMultiplier = 1,
                            damageBoostCoefficient = 1,
                            healthBoostCoefficient = 1,
                            isAvailable = () => Run.instance.loopClearCount > 0,
                            eliteTypes = newElites
                        };
                        newTiers[i] = tier;
                    }
                }

                tiersField.SetValue(null, newTiers);
            }
        }

        public static int GetItemId(string name)
        {
            if (!CatalogInitialized)
                Initialize();

            ItemReferences.TryGetValue(name, out var id);

            return id;
        }

        public static int GetEquipmentId(string name)
        {
            if (!CatalogInitialized)
                Initialize();

            EquipmentReferences.TryGetValue(name, out var id);

            return id - TotalItemCount;
        }

        public static int GetEliteId(string name)
        {
            if (!CatalogInitialized)
                Initialize();

            EliteReferences.TryGetValue(name, out var id);
            return id;
        }

        public static int GetBuffId(string name)
        {
            if (!CatalogInitialized)
                Initialize();

            BuffReferences.TryGetValue(name, out var id);
            return id;
        }

        public static CustomItem GetCustomItem(string name)
        {
            return CustomItemList.FirstOrDefault(x => x.ItemDef.nameToken.Equals(name));
        }

        public static CustomEquipment GetCustomEquipment(string name)
        {
            return CustomEquipmentList.FirstOrDefault(x => x.EquipmentDef.nameToken.Equals(name));
        }

        public static CustomElite GetCustomElite(string name)
        {
            return CustomEliteList.FirstOrDefault(x => string.Equals(x.Name, name));
        }

        public static CustomBuff GetCustomBuff(string name)
        {
            return CustomBuffList.FirstOrDefault(x => string.Equals(x.Name, name));
        }

        public static CustomItem GetCustomItem(int indexValue)
        {

            return GetCustomItem(ItemReferences.FirstOrDefault(x => x.Value == indexValue).Key);
        }

        public static CustomEquipment GetCustomEquipment(int indexValue)
        {
            return GetCustomEquipment(EquipmentReferences.FirstOrDefault(x => x.Value == indexValue).Key);
        }

        public static CustomElite GetCustomElite(int indexValue)
        {
            return GetCustomElite(EliteReferences.FirstOrDefault(x => x.Value == indexValue).Key);
        }

        public static CustomBuff GetCustomBuff(int indexValue)
        {
            return GetCustomBuff(BuffReferences.FirstOrDefault(x => x.Value == indexValue).Key);
        }

        public static void GetAllCustomItemsAndEquipments()
        {
            if (_customItemCount != 0 || _customEquipmentCount != 0 || _customEliteCount != 0 || _customBuffCount != 0)
                return;

            var allAssemblies = new List<Assembly>();

            var path = Directory.GetParent(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)).FullName;

            while (!path.EndsWith("ins"))
            {
                path = Directory.GetParent(path).FullName;
                ItemLibPlugin.Logger.LogWarning("ItemLib should be placed in its own folder in the BepinEx \\plugins folder. Path should be optimally looking like this : plugins\\ItemLib\\ItemLib.dll");
            }

            foreach (string dll in Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories))
            {
                if (!dll.ToLower().Contains("r2api") || !dll.ToLower().Contains("mmhook"))
                {
                    try // bepis code
                    {
                        allAssemblies.Add(Assembly.LoadFile(dll));
                    }
                    catch (BadImageFormatException) { } //unmanaged dll
                    catch (ReflectionTypeLoadException)
                    {
                        ItemLibPlugin.Logger.LogError($"Could not load \"{dll}\" as a plugin!");
                    }
                }
                    
            }
            foreach (var assembly in allAssemblies)
            {
                Type pluginType = typeof(BaseUnityPlugin);
                var types = new List<Type>();

                foreach (var type in assembly.GetTypes())
                {
                    if (!type.IsInterface && !type.IsAbstract && pluginType.IsAssignableFrom(type))
                    {
                        types.Add(type);
                    }   
                }
                
                for (int i = 0; i < types.Count; i++)
                {
                    foreach (var methodInfo in types.SelectMany(x => x.GetMethods()))
                    {
                        var customAttributes = methodInfo.GetCustomAttributes(false);
                        foreach (var attribute in customAttributes.OfType<ItemAttribute>())
                        {
                            switch (attribute.Type)
                            {
                                case ItemAttribute.ItemType.Item:
                                    CustomItemHashSet.Add(methodInfo);
                                    break;
                                case ItemAttribute.ItemType.Equipment:
                                    CustomEquipmentHashSet.Add(methodInfo);
                                    break;
                                case ItemAttribute.ItemType.Elite:
                                    CustomEliteHashSet.Add(methodInfo);
                                    break;
                                case ItemAttribute.ItemType.Buff:
                                    CustomBuffHashSet.Add(methodInfo);
                                    break;
                            }
                        }
                    }
                }
            }

            foreach (MethodInfo mi in CustomItemHashSet)
            {
                if (mi.Invoke(null, null) is CustomItem item)
                    CustomItemList.Add(item);
            }

            foreach (MethodInfo mi in CustomEquipmentHashSet)
            {
                if (mi.Invoke(null, null) is CustomEquipment equipment)
                    CustomEquipmentList.Add(equipment);
            }

            foreach (var mi in CustomBuffHashSet)
            {
                if (mi.Invoke(null, null) is CustomBuff buff)
                    CustomBuffList.Add(buff);
            }

            foreach (var mi in CustomEliteHashSet)
            {
                if (mi.Invoke(null, null) is CustomElite elite)
                {
                    CustomEliteList.Add(elite);
                    CustomEquipmentList.Add(elite.Equipment);
                    CustomBuffList.Add(elite.Buff);
                    elite.EliteDef.eliteEquipmentIndex = (EquipmentIndex)(OriginalEquipmentCount + CustomEquipmentList.Count - 1);
                    elite.Buff.BuffDef.eliteIndex = (EliteIndex)(OriginalEliteCount + CustomEliteList.Count - 1);
                    elite.Equipment.EquipmentDef.passiveBuff = (BuffIndex)(OriginalBuffCount + CustomBuffList.Count - 1);
                }
            }

            _customItemCount = CustomItemList.Count;
            _customEquipmentCount = CustomEquipmentList.Count;
            _customEliteCount = CustomEliteList.Count;
            _customBuffCount = CustomBuffList.Count;
        }

        private static void InitCatalogHook()
        {
            if (CatalogInitialized)
                return;

            var tmpItems = new Dictionary<string, int>();
            var tmpEquips = new Dictionary<string, int>();
            var tmpElites = new Dictionary<string, int>();
            var tmpBuffs = new Dictionary<string, int>();
            var size = 0;

            // Dynamically retrieve the Original Item Count on an all-in-one hook because of the impossibility to do multiple IL Hook on the catalog (i don't know why).
            // Make it so itemDefs is large enough for all the new items, with a Delegate.
            IL.RoR2.ItemCatalog.DefineItems += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                    i => i.MatchLdcI4(out size),
                    i => i.MatchNewarr<ItemDef>(),
                    i => i.MatchStsfld("RoR2.ItemCatalog", "itemDefs")
                );
                OriginalItemCount = size;

                cursor.Remove();
                cursor.EmitDelegate<Func<int>>(() => OriginalItemCount + CustomItemCount);

                cursor.GotoNext(
                        i => i.MatchLdcI4(0),
                        i => i.MatchStloc(1)
                );
                cursor.Index++;

                cursor.EmitDelegate<Action>(() =>
                {
                    // Register the items into the game and update ItemReferences so the mods know the id of their items.
                    for (int i = 0; i < CustomItemCount; i++)
                    {
                        MethodInfo registerItem = typeof(ItemCatalog).GetMethodCached("RegisterItem");
                        object[] para = { (ItemIndex)(i + OriginalItemCount), CustomItemList[i].ItemDef };
                        if (registerItem != null)
                            registerItem.Invoke(null, para);
                        //Debug.Log("adding custom item at index : " + (i + OriginalItemCount));

                        tmpItems.Add(CustomItemList[i].ItemDef.nameToken, i + OriginalItemCount);
                    }
                    ItemLibPlugin.Logger.LogInfo("Added " + _customItemCount + " custom items");
                    ItemReferences = new ReadOnlyDictionary<string, int>(tmpItems);
                });
            };

            IL.RoR2.EquipmentCatalog.cctor += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                    i => i.MatchLdcI4(out size),
                    i => i.MatchNewarr<EquipmentDef>(),
                    i => i.MatchStsfld("RoR2.EquipmentCatalog", "equipmentDefs")
                );
                OriginalEquipmentCount = size;

                cursor.Remove();
                cursor.EmitDelegate<Func<int>>(() => OriginalEquipmentCount + CustomEquipmentCount);

                cursor.GotoNext(
                    i => i.MatchLdcI4(0),
                    i => i.MatchStloc(0)
                );
                cursor.Index++;

                cursor.EmitDelegate<Action>(() =>
                {
                    // Register the items into the game and update equipmentReferences so the mods know the id of their equipments.
                    for (int i = 0; i < CustomEquipmentCount; i++)
                    {
                        MethodInfo registerEquipment = typeof(EquipmentCatalog).GetMethodCached("RegisterEquipment");
                        object[] para = { (EquipmentIndex)(i + OriginalEquipmentCount), CustomEquipmentList[i].EquipmentDef };
                        if (registerEquipment != null)
                            registerEquipment.Invoke(null, para);

                        tmpEquips.Add(CustomEquipmentList[i].EquipmentDef.nameToken, i + OriginalEquipmentCount + TotalItemCount);
                    }
                    ItemLibPlugin.Logger.LogInfo("Added " + _customEquipmentCount + " custom equipments");
                    EquipmentReferences = new ReadOnlyDictionary<string, int>(tmpEquips);
                });
            };

            MethodInfo registerElite = typeof(EliteCatalog).GetMethodCached("RegisterElite");
            if (registerElite == null)
            {
                Debug.LogError("Failed to bind RegisterElite method in EliteCatalog");
                return;
            }

            IL.RoR2.EliteCatalog.cctor += il =>
            {
                var cursor = new ILCursor(il);

                cursor.GotoNext(
                    i => i.MatchLdcI4(out size),
                    i => i.MatchNewarr<EliteDef>(),
                    i => i.MatchStsfld("RoR2.EliteCatalog", "eliteDefs")
                );
                OriginalEliteCount = size;

                cursor.Remove();
                cursor.EmitDelegate<Func<int>>(() => OriginalEliteCount + CustomEliteCount);

                //EliteCatalog doesn't do the check at the end for everything registered, so can just put the new registrations at the end
                cursor.GotoNext(i => i.MatchRet());
                cursor.EmitDelegate<Action>(() =>
                {
                    // Register the elites into the game and update eliteReferences so the mods know the id of their elites.
                    for (int i = 0; i < CustomEliteCount; i++)
                    {
                        object[] para = { (EliteIndex)(i + OriginalEliteCount), CustomEliteList[i].EliteDef };
                        registerElite.Invoke(null, para);

                        tmpElites.Add(CustomEliteList[i].Name, i + OriginalEliteCount);
                    }
                    ItemLibPlugin.Logger.LogInfo("Added " + _customEliteCount + " custom elites");
                    EliteReferences = new ReadOnlyDictionary<string, int>(tmpElites);
                });
            };

            MethodInfo registerBuff = typeof(BuffCatalog).GetMethodCached("RegisterBuff");
            if (registerBuff == null)
            {
                Debug.LogError("Failed to bind RegisterBuff method in BuffCatalog");
                return;
            }

            IL.RoR2.BuffCatalog.cctor += il =>
            {
                var cursor = new ILCursor(il);

                cursor.GotoNext(
                    i => i.MatchLdcI4(out size),
                    i => i.MatchNewarr<BuffDef>(),
                    i => i.MatchStsfld("RoR2.BuffCatalog", "buffDefs")
                );
                OriginalBuffCount = size;

                cursor.Remove();
                cursor.EmitDelegate<Func<int>>(() => OriginalBuffCount + CustomBuffCount);

                cursor.GotoNext(
                    i => i.MatchLdcI4(0),
                    i => i.MatchStloc(0)
                );
                cursor.Index++;

                cursor.EmitDelegate<Action>(() =>
                {
                    // Register the items into the game and update buffReferences so the mods know the id of their buffs.
                    for (int i = 0; i < CustomBuffCount; i++)
                    {
                        object[] para = {(BuffIndex) (i + OriginalBuffCount), CustomBuffList[i].BuffDef};
                        registerBuff.Invoke(null, para);

                        tmpBuffs.Add(CustomBuffList[i].Name, i + OriginalBuffCount);
                    }

                    ItemLibPlugin.Logger.LogInfo("Added " + _customBuffCount + " custom buffs");
                    BuffReferences = new ReadOnlyDictionary<string, int>(tmpBuffs);
                });
            };

            CatalogInitialized = true;
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

            // NetworkExtensions. Needed for GetItemCount / itemStacks on the inventory.

            IL.RoR2.NetworkExtensions.WriteItemStacks += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                    i => i.MatchLdcI4(OriginalItemCount)
                );
                cursor.Next.OpCode = OpCodes.Ldc_I4;
                cursor.Next.Operand = TotalItemCount;

                cursor.GotoNext(
                    i => i.MatchLdcI4(OriginalItemCount)
                );
                cursor.Next.OpCode = OpCodes.Ldc_I4;
                cursor.Next.Operand = TotalItemCount;
            };

            IL.RoR2.NetworkExtensions.ReadItemStacks += il =>
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

            IL.RoR2.Achievements.Discover5EquipmentAchievement.EquipmentDiscovered += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                    i => i.MatchLdcI4(OriginalEquipmentCount)
                );
                cursor.Next.OpCode = OpCodes.Ldc_I4;
                cursor.Next.Operand = TotalEquipmentCount;
            };

            /*IL.RoR2.Run.BuildDropTable += il =>
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
            };*/

            /*On.RoR2.Run.BuildDropTable += (orig, self) =>
            {
                self.availableTier1DropList.Clear();
                self.availableTier2DropList.Clear();
                self.availableTier3DropList.Clear();
                self.availableLunarDropList.Clear();
                self.availableEquipmentDropList.Clear();
                for (ItemIndex itemIndex = ItemIndex.Syringe; itemIndex < (ItemIndex)TotalItemCount; itemIndex++)
                {
                    if (self.availableItems.HasItem(itemIndex))
                    {
                        ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
                        List<PickupIndex> list = null;
                        switch (itemDef.tier)
                        {
                            case ItemTier.Tier1:
                                list = self.availableTier1DropList;
                                break;
                            case ItemTier.Tier2:
                                list = self.availableTier2DropList;
                                break;
                            case ItemTier.Tier3:
                                list = self.availableTier3DropList;
                                break;
                            case ItemTier.Lunar:
                                list = self.availableLunarDropList;
                                break;
                        }
                        if (list != null)
                        {
                            list.Add(new PickupIndex(itemIndex));
                        }
                    }
                }
                for (EquipmentIndex equipmentIndex = EquipmentIndex.CommandMissile; equipmentIndex < (EquipmentIndex)TotalEquipmentCount; equipmentIndex++)
                {
                    if (self.availableEquipment.HasEquipment(equipmentIndex))
                    {
                        EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
                        if (equipmentDef.canDrop)
                        {
                            if (!equipmentDef.isLunar)
                            {
                                self.availableEquipmentDropList.Add(new PickupIndex(equipmentIndex));
                            }
                            else
                            {
                                self.availableLunarDropList.Add(new PickupIndex(equipmentIndex));
                            }
                        }
                    }
                }
                self.smallChestDropTierSelector.Clear();
                self.smallChestDropTierSelector.AddChoice(self.availableTier1DropList, 0.8f);
                self.smallChestDropTierSelector.AddChoice(self.availableTier2DropList, 0.2f);
                self.smallChestDropTierSelector.AddChoice(self.availableTier3DropList, 0.01f);
                self.mediumChestDropTierSelector.Clear();
                self.mediumChestDropTierSelector.AddChoice(self.availableTier2DropList, 0.8f);
                self.mediumChestDropTierSelector.AddChoice(self.availableTier3DropList, 0.2f);
                self.largeChestDropTierSelector.Clear();
            };

            On.RoR2.ChestBehavior.RollItem += (orig, self) =>
            {
                if (!NetworkServer.active)
                {
                    Debug.LogWarning("[Server] function 'System.Void RoR2.ChestBehavior::RollItem()' called on client");
                    return;
                }
                WeightedSelection<List<PickupIndex>> weightedSelection = new WeightedSelection<List<PickupIndex>>(8);
                weightedSelection.AddChoice(Run.instance.availableTier1DropList, self.tier1Chance);
                weightedSelection.AddChoice(Run.instance.availableTier2DropList, self.tier2Chance);
                weightedSelection.AddChoice(Run.instance.availableTier3DropList, self.tier3Chance);
                weightedSelection.AddChoice(Run.instance.availableLunarDropList, self.lunarChance);
                List<PickupIndex> dropList = weightedSelection.Evaluate(Run.instance.treasureRng.nextNormalizedFloat);

                //PickFromList
                if (!NetworkServer.active)
                {
                    Debug.LogWarning("[Server] function 'System.Void RoR2.ChestBehavior::PickFromList(System.Collections.Generic.List`1<RoR2.PickupIndex>)' called on client");
                    return;
                }
                self.SetFieldValue("dropPickup", PickupIndex.none);
                if (dropList != null && dropList.Count > 0)
                {
                    self.SetFieldValue("dropPickup", Run.instance.treasureRng.NextElementUniform<PickupIndex>(dropList));
                }
            };*/


            // R2API.ItemDropAPI

            var t1Items = CustomItemList.Where(x => x.ItemDef.tier == ItemTier.Tier1).Select(x => (x.ItemDef.itemIndex)).ToArray();
            var t2Items = CustomItemList.Where(x => x.ItemDef.tier == ItemTier.Tier2).Select(x => (x.ItemDef.itemIndex)).ToArray();
            var t3Items = CustomItemList.Where(x => x.ItemDef.tier == ItemTier.Tier3).Select(x => (x.ItemDef.itemIndex)).ToArray();
            var lunarItems = CustomItemList.Where(x => x.ItemDef.tier == ItemTier.Lunar).Select(x => (x.ItemDef.itemIndex)).ToArray();
            var bossItems = CustomItemList.Where(x => x.ItemDef.tier == ItemTier.Boss).Select(x => (x.ItemDef.itemIndex)).ToArray();

            var equipments = CustomEquipmentList.Where(c => c.EquipmentDef.canDrop && !c.EquipmentDef.isLunar).Select(x => (x.EquipmentDef.equipmentIndex)).ToArray();
            List<EquipmentIndex> lunarEquipments = CustomEquipmentList.Where(c => c.EquipmentDef.canDrop && c.EquipmentDef.isLunar).Select(x => (x.EquipmentDef.equipmentIndex)).ToList();


            ItemDropAPI.AddToDefaultByTier(ItemTier.Tier1, t1Items);
            ItemDropAPI.AddToDefaultByTier(ItemTier.Tier2, t2Items);
            ItemDropAPI.AddToDefaultByTier(ItemTier.Tier3, t3Items);
            ItemDropAPI.AddToDefaultByTier(ItemTier.Lunar, lunarItems);
            ItemDropAPI.AddToDefaultByTier(ItemTier.Boss, bossItems);

            ItemDropAPI.AddToDefaultEquipment(equipments);
            ItemDropAPI.AddDrops(ItemDropLocation.LunarChest, ItemDropAPI.ToSelection(lunarEquipments));

            //ItemCatalog

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

            // EquipmentCatalog

            IL.RoR2.EquipmentCatalog.GetEquipmentDef += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                    i => i.MatchLdcI4(OriginalEquipmentCount)
                );
                cursor.Next.OpCode = OpCodes.Ldc_I4;
                cursor.Next.Operand = TotalEquipmentCount;
            };

            IL.RoR2.EquipmentCatalog.AllEquipmentEnumerator.MoveNext += il =>
            {
                ILCursor cursor = new ILCursor(il);


                cursor.GotoNext(
                    i => i.MatchLdcI4(OriginalEquipmentCount)
                );
                cursor.Next.OpCode = OpCodes.Ldc_I4;
                cursor.Next.Operand = TotalEquipmentCount;
            };

            // EliteCatalog

            IL.RoR2.EliteCatalog.GetEquipmentEliteIndex += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                    i => i.MatchLdcI4(OriginalEquipmentCount)
                );
                cursor.Next.OpCode = OpCodes.Ldc_I4;
                cursor.Next.Operand = TotalEquipmentCount;
            };

            IL.RoR2.EliteCatalog.GetEliteDef += il =>
            {
                var cursor = new ILCursor(il);

                cursor.GotoNext(i => i.MatchLdcI4(OriginalEliteCount));
                cursor.Next.OpCode = OpCodes.Ldc_I4;
                cursor.Next.Operand = TotalEliteCount;
            };

            // BuffCatalog

            IL.RoR2.BuffCatalog.GetBuffDef += il =>
            {
                var cursor = new ILCursor(il);
                cursor.GotoNext(i => i.MatchLdcI4(OriginalBuffCount));
                cursor.Next.OpCode = OpCodes.Ldc_I4;
                cursor.Next.Operand = TotalBuffCount;
            };

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
            typeof(PickupIndex).SetFieldValue("none", new PickupIndex((ItemIndex)(-1))); // needed apparently.

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

            // ReSharper disable once PossibleNullReferenceException
            var all = (ItemMask)typeof(ItemMask)
                .GetField("all", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(null);
            for (int i = OriginalItemCount; i < TotalItemCount; i++)
                all.AddItem((ItemIndex)i);

            // EquipmentMask

            IL.RoR2.EquipmentMask.HasEquipment += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                    i => i.MatchLdcI4(OriginalEquipmentCount)
                );
                cursor.Next.OpCode = OpCodes.Ldc_I4;
                cursor.Next.Operand = TotalEquipmentCount;
            };

            IL.RoR2.EquipmentMask.AddEquipment += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                    i => i.MatchLdcI4(OriginalEquipmentCount)
                );
                cursor.Next.OpCode = OpCodes.Ldc_I4;
                cursor.Next.Operand = TotalEquipmentCount;
            };

            IL.RoR2.EquipmentMask.RemoveEquipment += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                    i => i.MatchLdcI4(OriginalEquipmentCount)
                );
                cursor.Next.OpCode = OpCodes.Ldc_I4;
                cursor.Next.Operand = TotalEquipmentCount;
            };

            // ReSharper disable once PossibleNullReferenceException
            EquipmentMask all2 = (EquipmentMask)typeof(EquipmentMask)
                .GetField("all", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                ?.GetValue(null);
            for (int i = OriginalEquipmentCount; i < TotalEquipmentCount; i++)
                all2.AddEquipment((EquipmentIndex)i);

            // CharacterModel

            IL.RoR2.CharacterModel.UpdateItemDisplay += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                    i => i.MatchLdcI4(OriginalItemCount)
                );
                cursor.Next.OpCode = OpCodes.Ldc_I4;
                cursor.Next.Operand = TotalItemCount;
            };

            IL.RoR2.CharacterModel.UpdateMaterials += il =>
            {
                ILCursor cursor = new ILCursor(il);
                var forCounterLoc = 0;
                var itemDisplayLoc = 0;

                cursor.GotoNext(
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld("RoR2.CharacterModel", "parentedPrefabDisplays"),
                    i => i.MatchLdloc(out forCounterLoc)
                );

                cursor.GotoNext(
                    i => i.MatchCall("RoR2.CharacterModel/ParentedPrefabDisplay", "get_itemDisplay"),
                    i => i.MatchStloc(out itemDisplayLoc)
                );
                cursor.Index += 2;

                cursor.Emit(OpCodes.Ldloc, itemDisplayLoc);
                cursor.Emit(OpCodes.Call, typeof(UnityEngine.Object).GetMethodCached("op_Implicit"));
                cursor.Emit(OpCodes.Brfalse, cursor.MarkLabel());
                var brFalsePos = cursor.Index - 1;

                cursor.GotoNext(
                    i => i.MatchLdloc(forCounterLoc)
                );
                var label = cursor.MarkLabel();

                cursor.Index = brFalsePos;
                cursor.Next.Operand = label;

            };


            // CharacterBody

            IL.RoR2.CharacterBody.ctor += il =>
            {
                var cursor = new ILCursor(il);
                cursor.GotoNext(i => i.MatchLdcI4(OriginalBuffCount));
                cursor.Next.OpCode = OpCodes.Ldc_I4;
                cursor.Next.Operand = TotalBuffCount;

                //CharacterBody now has two separate arrays related to buffs
                cursor.GotoNext(i => i.MatchLdcI4(OriginalBuffCount));
                cursor.Next.OpCode = OpCodes.Ldc_I4;
                cursor.Next.Operand = TotalBuffCount;
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

            On.RoR2.ItemDisplayRuleSet.GetItemDisplayRuleGroup += (orig, self, index) =>
            {
                if ((int)index >= OriginalItemCount)
                {
                    var itemRuleGroups = self.GetFieldValue<DisplayRuleGroup[]>("itemRuleGroups");
                    var tmp = new DisplayRuleGroup[TotalItemCount];

                    if (itemRuleGroups.Length < TotalItemCount)
                    {
                        for (var i = 0; i < itemRuleGroups.Length; i++)
                        {
                            tmp[i] = new DisplayRuleGroup
                            {
                                rules = new ItemDisplayRule[itemRuleGroups[i].rules.Length]
                            };
                            itemRuleGroups[i].rules.CopyTo(tmp[i].rules, 0);
                        }

                        for (var i = OriginalItemCount; i < TotalItemCount; i++)
                        {
                            var customItem = GetCustomItem(i);
                            if (customItem?.ItemDisplayRules != null)
                            {
                                tmp[i] = new DisplayRuleGroup
                                {
                                    rules = customItem.ItemDisplayRules
                                };

                                continue;
                            }

                            tmp[i] = new DisplayRuleGroup
                            {
                                rules = new ItemDisplayRule[1]
                            };
                        }
                        self.SetFieldValue("itemRuleGroups", tmp);
                    }
                }

                return orig(self, index);
            };

            On.RoR2.ItemDisplayRuleSet.GetEquipmentDisplayRuleGroup += (orig, self, index) =>
            {
                if ((int)index >= OriginalEquipmentCount)
                {
                    var equipmentRuleGroups = self.GetFieldValue<DisplayRuleGroup[]>("equipmentRuleGroups");
                    var tmp = new DisplayRuleGroup[TotalEquipmentCount];

                    if (equipmentRuleGroups.Length < TotalEquipmentCount)
                    {
                        for (var i = 0; i < equipmentRuleGroups.Length; i++)
                        {
                            tmp[i] = new DisplayRuleGroup
                            {
                                rules = new ItemDisplayRule[equipmentRuleGroups[i].rules.Length]
                            };
                            equipmentRuleGroups[i].rules.CopyTo(tmp[i].rules, 0);
                        }

                        for (var i = OriginalEquipmentCount; i < TotalEquipmentCount; i++)
                        {
                            var customEquipment = GetCustomEquipment(i);
                            if (customEquipment?.ItemDisplayRules != null)
                            {
                                tmp[i] = new DisplayRuleGroup
                                {
                                    rules = customEquipment.ItemDisplayRules
                                };

                                continue;
                            }

                            tmp[i] = new DisplayRuleGroup
                            {
                                rules = new ItemDisplayRule[1]
                            };
                        }
                        self.SetFieldValue("equipmentRuleGroups", tmp);
                    }
                }

                return orig(self, index);
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

                cursor.GotoNext(
                    i => i.MatchLdcI4(OriginalEquipmentCount)
                );
                cursor.Next.OpCode = OpCodes.Ldc_I4;
                cursor.Next.Operand = TotalEquipmentCount;
            };

            ConstructorInfo ruleCatalogcctor = typeof(RuleCatalog).TypeInitializer;
            ruleCatalogcctor.Invoke(null, null);

            // Ok so this one is kinda a problem : extending the discoveredPickups bool array make it so Rewired cannot put any mouse / kb / game pad mapping into save files,
            // Why it does that, i don't know.
            // We probably won't ever need to fix it though if we don't want to mess up saves.

            /*IL.RoR2.UserProfile.ctor += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                    i => i.MatchLdcI4(OriginalItemCount + OriginalEquipmentCount + CoinCount)
                );
                cursor.Next.OpCode = OpCodes.Ldc_I4;
                cursor.Next.Operand = TotalItemCount + TotalEquipmentCount + CoinCount;
            };*/

            // Making sure nothing get saved to the UserProfile so if the user decide to remove the custom items he doesn't get a corrupted profile.

            // Disable DiscoveredPickups field in UserProfile for custom items
            On.RoR2.UserProfile.SaveFieldAttribute.SetupPickupsSet += (orig, self, fieldInfo) =>
            {
                self.getter = delegate (UserProfile userProfile)
                {
                    bool[] pickupsSet = (bool[])fieldInfo.GetValue(userProfile);
                    string result = "";
                    for (int i = 0; i < (OriginalItemCount + OriginalEquipmentCount + CoinCount); i++)
                    {
                        var pickupIndex = PickupIndex.allPickups.ToList()[i];
                        if (pickupsSet[pickupIndex.value] && GetCustomItem(pickupIndex.value) == null &&
                            GetCustomEquipment(pickupIndex.value) == null)
                        {
                            result += pickupIndex.ToString();
                            result += " ";
                        }
                    }

                    return result;
                };
                self.setter = delegate (UserProfile userProfile, string valueString)
                {
                    bool[] array = (bool[])fieldInfo.GetValue(userProfile);
                    Array.Clear(array, 0, 0);
                    string[] array2 = valueString.Split(' ');
                    foreach (var name in array2)
                    {
                        PickupIndex pickupIndex = PickupIndex.Find(name);
                        if (pickupIndex.isValid && GetCustomItem(pickupIndex.value) == null && GetCustomEquipment(pickupIndex.value) == null)
                        {
                            array[pickupIndex.value] = true;
                        }
                    }
                };
                self.copier = delegate (UserProfile srcProfile, UserProfile destProfile)
                {
                    Array sourceArray = (bool[])fieldInfo.GetValue(srcProfile);
                    bool[] array = (bool[])fieldInfo.GetValue(destProfile);
                    Array.Copy(sourceArray, array, array.Length);
                };
            };

            On.RoR2.UserProfile.DiscoverPickup += (orig, self, pickupIndex) =>
            {
                if (GetCustomItem(pickupIndex.value) == null && GetCustomEquipment(pickupIndex.value) == null)
                {
                    return;
                }

                orig(self, pickupIndex);
            };

            // Il is obviously cleaner here but who is seriously gonna mess with this method except for an item API?
            // Disable field making for totalCollected and highestCollected for custom items
            On.RoR2.Stats.PerItemStatDef.RegisterStatDefs += (orig) =>
            {
                var instancesList = typeof(PerItemStatDef).GetFieldValue<List<PerItemStatDef>>("instancesList");
                foreach (PerItemStatDef perItemStatDef in instancesList)
                {
                    var prefix = perItemStatDef.GetFieldValue<string>("prefix");
                    var recordType = perItemStatDef.GetFieldValue<StatRecordType>("recordType");
                    var dataType = perItemStatDef.GetFieldValue<StatDataType>("dataType");
                    var keyToStatDef = perItemStatDef.GetFieldValue<StatDef[]>("keyToStatDef");
                    foreach (ItemIndex itemIndex in ItemCatalog.allItems)
                    {
                        if ((int)itemIndex >= OriginalItemCount)
                            continue;
                        StatDef statDef = StatDef.Register(prefix + "." + itemIndex.ToString(), recordType, dataType, 0.0);
                        keyToStatDef[(int)itemIndex] = statDef;
                    }
                }
            };

            On.RoR2.Stats.PerEquipmentStatDef.RegisterStatDefs += (orig) =>
            {
                var instancesList = typeof(PerEquipmentStatDef).GetFieldValue<List<PerEquipmentStatDef>>("instancesList");
                foreach (PerEquipmentStatDef perEquipmentStatDef in instancesList)
                {
                    var prefix = perEquipmentStatDef.GetFieldValue<string>("prefix");
                    var recordType = perEquipmentStatDef.GetFieldValue<StatRecordType>("recordType");
                    var dataType = perEquipmentStatDef.GetFieldValue<StatDataType>("dataType");
                    var keyToStatDef = perEquipmentStatDef.GetFieldValue<StatDef[]>("keyToStatDef");
                    foreach (EquipmentIndex equipmentIndex in EquipmentCatalog.allEquipment)
                    {
                        if ((int)equipmentIndex >= OriginalEquipmentCount)
                            continue;
                        StatDef statDef = StatDef.Register(prefix + "." + equipmentIndex.ToString(), recordType, dataType, 0.0);
                        keyToStatDef[(int)equipmentIndex] = statDef;
                    }
                }
            };

            // Jump to the end of the loop if EquipmentIndex > OriginalEquipmentCount
            IL.RoR2.Stats.StatManager.ProcessCharacterUpdateEvents += il =>
            {
                var cursor = new ILCursor(il);
                var currentEquipmentLoc = 0;

                cursor.GotoNext(
                    i => i.MatchCallvirt<Inventory>("get_currentEquipmentIndex"),
                    i => i.MatchStloc(out currentEquipmentLoc)
                );
                cursor.GotoNext(
                    i => i.MatchLdloc(currentEquipmentLoc),
                    i => i.MatchLdcI4(-1),
                    i => i.MatchBeq(out _)
                );

                cursor.Index += 2;
                var label = cursor.Next.Operand;
                cursor.Index++;

                cursor.Emit(OpCodes.Ldloc, currentEquipmentLoc);
                cursor.Emit(OpCodes.Ldc_I4, OriginalEquipmentCount);
                cursor.Emit(OpCodes.Bge_S, label);
            };

            // Normally push values to the StatSheet about the item (totalCollected etc). Saves to UserProfile
            On.RoR2.Stats.StatManager.OnEquipmentActivated += (orig, activator, equipmentIndex) =>
            {
                if(equipmentIndex < EquipmentIndex.Count)
                    orig(activator, equipmentIndex);
            };

            On.RoR2.Stats.StatManager.OnServerItemGiven += (orig, inventory, itemIndex, quantity) =>
            {
                if (itemIndex < ItemIndex.Count)
                    orig(inventory, itemIndex, quantity);
            };

            // TODO : Custom Items LogBook Serialization  
            // LogBook. So for now we disable the progress part in the logbook for custom items, since logbook progression is linked to the data from the UserProfile,
            // the best solution would be to have a reserved data file for custom items somewhere so we never interact directly with the so fragile UserProfile of the users.
            // That way we could have a logbook working for custom items too
            // For now lets assume the user unlocked / discovered the item so we can see 3d models on it :)

            // It normally check if we have at least picked it up once / unlocked the linked achievement for it.
            // Default to available for custom items for now
            On.RoR2.UI.LogBook.LogBookController.GetPickupStatus += (orig, userProfile, entry) =>
            {
                if (GetCustomItem(((PickupIndex)entry.extraData).value) != null || GetCustomEquipment(((PickupIndex)entry.extraData).value) != null)
                {
                    return EntryStatus.Available;
                }
                return orig(userProfile, entry);
            };

            // This one normally interact with the UserProfile (PerItemStatDef) to get how many times we found the item and how far the user managed to stack it in a single run
            On.RoR2.UI.LogBook.PageBuilder.AddSimplePickup += (orig, self, pickupIndex) =>
            {
                if (GetCustomItem(pickupIndex.value) != null || GetCustomEquipment(pickupIndex.value) != null)
                {
                    ItemIndex itemIndex = pickupIndex.itemIndex;
                    EquipmentIndex equipmentIndex = pickupIndex.equipmentIndex;
                    string token = null;
                    if (itemIndex != ItemIndex.None)
                    {
                        ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
                        self.AddDescriptionPanel(Language.GetString(itemDef.descriptionToken));
                        token = itemDef.loreToken;
                        //ulong statValueULong = get from custom file;
                        //ulong statValueULong2 = get from custom file;
                        string stringFormatted = Language.GetStringFormatted("GENERIC_PREFIX_FOUND", "Unknown");
                        string stringFormatted2 = Language.GetStringFormatted("ITEM_PREFIX_STACKCOUNT", "Unknown");
                        self.AddSimpleTextPanel(stringFormatted, stringFormatted2);
                    }
                    else if (equipmentIndex != EquipmentIndex.None)
                    {
                        EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
                        self.AddDescriptionPanel(Language.GetString(equipmentDef.descriptionToken));
                        token = equipmentDef.loreToken;
                        // this.statSheet.GetStatDisplayValue(PerEquipmentStatDef.totalTimeHeld.FindStatDef(equipmentIndex)) Custom file too
                        // this.statSheet.GetStatDisplayValue(PerEquipmentStatDef.totalTimesFired.FindStatDef(equipmentIndex))
                        string stringFormatted3 = Language.GetStringFormatted("EQUIPMENT_PREFIX_TOTALTIMEHELD", "Unknown");
                        string stringFormatted4 = Language.GetStringFormatted("EQUIPMENT_PREFIX_USECOUNT", "Unknown");
                        self.AddSimpleTextPanel(stringFormatted3, stringFormatted4);
                    }
                    // ReSharper disable once AssignNullToNotNullAttribute
                    self.AddNotesPanel(Language.IsTokenInvalid(token) ? Language.GetString("EARLY_ACCESS_LORE") : Language.GetString(token));
                }
                else
                {
                    orig(self, pickupIndex);
                }
            };

            // make it unlocked no matter what for custom items
            On.RoR2.UI.LogBook.LogBookController.GetPickupTooltipContent += (orig, userProfile, entry, status) =>
            {
                if (GetCustomItem(((PickupIndex)entry.extraData).value) != null || GetCustomEquipment(((PickupIndex)entry.extraData).value) != null)
                {
                    UnlockableDef unlockableDef = UnlockableCatalog.GetUnlockableDef(((PickupIndex)entry.extraData).GetUnlockableName());
                    TooltipContent result = default;
                    result.titleToken = entry.nameToken;
                    result.titleColor = entry.color;
                    if (unlockableDef != null)
                    {
                        result.overrideBodyText = unlockableDef.getUnlockedString();
                    }
                    result.bodyToken = "LOGBOOK_CATEGORY_ITEM";
                    result.bodyColor = ColorCatalog.GetColor(ColorCatalog.ColorIndex.Unlockable);
                    return result;
                }
                return orig(userProfile, entry, status);
            };

            // IL is calling virt methods for the linq where lol
            // What we do here : Restrict first Entry IEnumerable to < originalItemCount and concat a new one to the first for custom items
            On.RoR2.UI.LogBook.LogBookController.BuildPickupEntries += (orig) =>
            {
                var getUnimplemented =
                    typeof(LogBookController).GetMethodCached("GetUnimplemented");
                var getWipTooltipContent =
                    typeof(LogBookController).GetMethodCached("GetWIPTooltipContent");
                var getPickupStatus =
                    typeof(LogBookController).GetMethodCached("GetPickupStatus");
                var getPickupTooltipContent =
                    typeof(LogBookController).GetMethodCached("GetPickupTooltipContent");

                Entry entry = new Entry
                {
                    nameToken = "TOOLTIP_WIP_CONTENT_NAME",
                    color = Color.white,
                    iconTexture = Resources.Load<Texture>("Textures/MiscIcons/texWIPIcon"),
                    getStatus = (Func<UserProfile, Entry, EntryStatus>)Delegate.CreateDelegate(
                        typeof(Func<UserProfile, Entry, EntryStatus>), getUnimplemented),
                    getTooltipContent = (Func<UserProfile, Entry, EntryStatus, TooltipContent>)Delegate.CreateDelegate(
                        typeof(Func<UserProfile, Entry, EntryStatus, TooltipContent>), getWipTooltipContent)
                };
                IEnumerable<Entry> first = from itemDef in PickupIndex.allPickups.Select(delegate (PickupIndex pickupIndex)
                {
                    PickupIndex pickupIndex2 = pickupIndex;
                    if (ItemCatalog.GetItemDef(pickupIndex2.itemIndex) != null && pickupIndex2.value < OriginalItemCount)
                    {
                        return ItemCatalog.GetItemDef(pickupIndex2.itemIndex);
                    }

                    return null;
                })
                where itemDef != null && itemDef.inDroppableTier && itemDef.itemIndex < ItemIndex.Count
                orderby (int)(itemDef.tier + ((itemDef.tier == ItemTier.Lunar) ? 100 : 0))
                select new Entry
                {
                    nameToken = itemDef.nameToken,
                    categoryTypeToken = "LOGBOOK_CATEGORY_ITEM",
                    color = ColorCatalog.GetColor(itemDef.darkColorIndex),
                    iconTexture = itemDef.pickupIconTexture,
                    bgTexture = itemDef.bgIconTexture,
                    extraData = new PickupIndex(itemDef.itemIndex),
                    modelPrefab = Resources.Load<GameObject>(itemDef.pickupModelPath),
                    getStatus = (Func<UserProfile, Entry, EntryStatus>)Delegate.CreateDelegate(typeof(Func<UserProfile, Entry, EntryStatus>), getPickupStatus),
                    getTooltipContent = (Func<UserProfile, Entry, EntryStatus, TooltipContent>)Delegate.CreateDelegate(
                    typeof(Func<UserProfile, Entry, EntryStatus, TooltipContent>), getPickupTooltipContent),
                    addEntries = PageBuilder.SimplePickup,
                    isWIP = Language.IsTokenInvalid(itemDef.loreToken)
                };
                IEnumerable<Entry> customItemsEntries = from itemDef in PickupIndex.allPickups.Select(delegate (PickupIndex pickupIndex)
                {
                    PickupIndex pickupIndex2 = pickupIndex;
                    if (ItemCatalog.GetItemDef(pickupIndex2.itemIndex) != null && pickupIndex2.value >= OriginalItemCount)
                    {
                        return ItemCatalog.GetItemDef(pickupIndex2.itemIndex);
                    }

                    return null;
                })
                where itemDef != null && itemDef.inDroppableTier && itemDef.itemIndex >= ItemIndex.Count
                orderby (int)(itemDef.tier + ((itemDef.tier == ItemTier.Lunar) ? 100 : 0))
                select new Entry
                {
                    nameToken = itemDef.nameToken,
                    categoryTypeToken = "LOGBOOK_CATEGORY_ITEM",
                    color = ColorCatalog.GetColor(itemDef.darkColorIndex),
                    iconTexture = (Texture)GetCustomItem(itemDef.nameToken).Icon,
                    bgTexture = itemDef.bgIconTexture,
                    extraData = new PickupIndex(itemDef.itemIndex),
                    modelPrefab = GetCustomItem(itemDef.nameToken).Prefab,
                    getStatus = (Func<UserProfile, Entry, EntryStatus>)Delegate.CreateDelegate(typeof(Func<UserProfile, Entry, EntryStatus>), getPickupStatus),
                    getTooltipContent = (Func<UserProfile, Entry, EntryStatus, TooltipContent>)Delegate.CreateDelegate(
                        typeof(Func<UserProfile, Entry, EntryStatus, TooltipContent>), getPickupTooltipContent),
                    addEntries = PageBuilder.SimplePickup,
                    isWIP = Language.IsTokenInvalid(itemDef.loreToken)
                };
                first = first.Concat(customItemsEntries);
                IEnumerable<Entry> second = from equipmentDef in PickupIndex.allPickups.Select(delegate (PickupIndex pickupIndex)
                {
                    PickupIndex pickupIndex2 = pickupIndex;
                    if (EquipmentCatalog.GetEquipmentDef(pickupIndex2.equipmentIndex) != null && pickupIndex2.value < TotalItemCount + OriginalEquipmentCount)
                    {
                        return EquipmentCatalog.GetEquipmentDef(pickupIndex2.equipmentIndex);
                    }

                    return null;
                })
                where equipmentDef != null && equipmentDef.canDrop
                orderby !equipmentDef.isLunar
                select new Entry
                {
                    nameToken = equipmentDef.nameToken,
                    categoryTypeToken = "LOGBOOK_CATEGORY_EQUIPMENT",
                    color = ColorCatalog.GetColor(equipmentDef.colorIndex),
                    iconTexture = equipmentDef.pickupIconTexture,
                    bgTexture = equipmentDef.bgIconTexture,
                    extraData = new PickupIndex(equipmentDef.equipmentIndex),
                    modelPrefab = Resources.Load<GameObject>(equipmentDef.pickupModelPath),
                    getStatus = (Func<UserProfile, Entry, EntryStatus>)Delegate.CreateDelegate(typeof(Func<UserProfile, Entry, EntryStatus>), getPickupStatus),
                    getTooltipContent = (Func<UserProfile, Entry, EntryStatus, TooltipContent>)Delegate.CreateDelegate(
                        typeof(Func<UserProfile, Entry, EntryStatus, TooltipContent>), getPickupTooltipContent),
                    addEntries = PageBuilder.SimplePickup,
                    isWIP = Language.IsTokenInvalid(equipmentDef.loreToken)
                };
                IEnumerable<Entry> customEquipmentsEntries = from equipmentDef in PickupIndex.allPickups.Select(delegate (PickupIndex pickupIndex)
                {
                    PickupIndex pickupIndex2 = pickupIndex;
                    if (EquipmentCatalog.GetEquipmentDef(pickupIndex2.equipmentIndex) != null && pickupIndex2.value >= TotalItemCount + OriginalEquipmentCount)
                    {
                        return EquipmentCatalog.GetEquipmentDef(pickupIndex2.equipmentIndex);
                    }

                    return null;
                })
                where equipmentDef != null && equipmentDef.canDrop
                orderby !equipmentDef.isLunar
                select new Entry
                {
                    nameToken = equipmentDef.nameToken,
                    categoryTypeToken = "LOGBOOK_CATEGORY_EQUIPMENT",
                    color = ColorCatalog.GetColor(equipmentDef.colorIndex),
                    iconTexture = (Texture)GetCustomEquipment(equipmentDef.nameToken).Icon,
                    bgTexture = equipmentDef.bgIconTexture,
                    extraData = new PickupIndex(equipmentDef.equipmentIndex),
                    modelPrefab = GetCustomEquipment(equipmentDef.nameToken).Prefab,
                    getStatus = (Func<UserProfile, Entry, EntryStatus>)Delegate.CreateDelegate(typeof(Func<UserProfile, Entry, EntryStatus>), getPickupStatus),
                    getTooltipContent = (Func<UserProfile, Entry, EntryStatus, TooltipContent>)Delegate.CreateDelegate(
                     typeof(Func<UserProfile, Entry, EntryStatus, TooltipContent>), getPickupTooltipContent),
                    addEntries = PageBuilder.SimplePickup,
                    isWIP = Language.IsTokenInvalid(equipmentDef.loreToken)
                };
                second = second.Concat(customEquipmentsEntries);
                IEnumerable<Entry> enumerable = first.Concat(second);
                var entries = enumerable as Entry[] ?? enumerable.ToArray();
                int count = Math.Max(120 - entries.Length, 0);
                IEnumerable<Entry> second2 = Enumerable.Repeat(entry, count);
                enumerable = entries.Concat(second2);
                return enumerable.ToArray();
            };

            // Icon and Prefab loading stuff

            IL.RoR2.Orbs.ItemTakenOrbEffect.Start += il =>
            {
                ILCursor cursor = new ILCursor(il);
                var id = 0;
                Sprite instance = null;

                cursor.GotoNext(
                    i => i.MatchCall("RoR2.ItemCatalog", "GetItemDef"),
                    i => i.MatchStloc(0)
                );

                cursor.Index++;

                cursor.EmitDelegate<Func<ItemDef, ItemDef>>((itemDef) =>
                {
                    id = (int)itemDef.itemIndex;
                    return itemDef;
                });

                cursor.GotoNext(
                    i => i.MatchLdnull(),
                    i => i.MatchStloc(2)
                );

                cursor.Index += 2;

                cursor.Emit(OpCodes.Ldloc_2);

                cursor.EmitDelegate<Action<Sprite>>((sprite) =>
                {
                    instance = sprite;
                });

                cursor.GotoNext(
                    i => i.MatchCallvirt("RoR2.ItemDef", "get_pickupIconSprite")
                );

                cursor.Index += 2;

                cursor.EmitDelegate<Action>(() =>
                {
                    if (id != 0)
                    {
                        var currentCustomItem = GetCustomEquipment(id);
                        if (currentCustomItem != null && currentCustomItem.Icon != null)
                        {
                            if (instance != null)
                                instance = (Sprite)currentCustomItem.Icon;
                        }
                    }
                });
            };

            IL.RoR2.UI.RuleChoiceController.SetChoice += il =>
            {
                ILCursor cursor = new ILCursor(il);
                string name = null;
                RuleChoiceController instance = null;

                cursor.Emit(OpCodes.Ldarg_0);

                cursor.EmitDelegate<Action<RuleChoiceController>>((self) =>
                {
                    instance = self;
                });

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
                    i => i.MatchCall("UnityEngine.Resources", "Load"),
                    i => i.MatchCallvirt("UnityEngine.UI.Image", "set_sprite")
                );

                cursor.Index += 2;

                cursor.EmitDelegate<Action>(() =>
                {
                    if (name != null)
                    {
                        var currentCustomItem = GetCustomItem(name);
                        if (currentCustomItem != null && currentCustomItem.Icon != null)
                        {
                            if (instance != null)
                                instance.image.sprite = (Sprite)currentCustomItem.Icon;
                        }
                    }
                });
            };

            IL.RoR2.UI.GenericNotification.SetItem += il =>
            {
                ILCursor cursor = new ILCursor(il);
                string name = null;
                GenericNotification instance = null;

                cursor.Emit(OpCodes.Ldarg_0);

                cursor.EmitDelegate<Action<GenericNotification>>((self) =>
                {
                    instance = self;
                });

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
                    i => i.MatchCall("UnityEngine.Resources", "Load"),
                    i => i.MatchCallvirt("UnityEngine.UI.RawImage", "set_texture")
                );

                cursor.Index += 2;

                cursor.EmitDelegate<Action>(() =>
                {
                    if (name != null)
                    {
                        var currentCustomItem = GetCustomItem(name);
                        if (currentCustomItem != null && currentCustomItem.Icon != null)
                        {
                            if (instance != null)
                                instance.iconImage.texture = (Texture)currentCustomItem.Icon;
                        }
                    }
                });
            };

            IL.RoR2.UI.GenericNotification.SetEquipment += il =>
            {
                ILCursor cursor = new ILCursor(il);
                string name = null;
                GenericNotification instance = null;

                cursor.Emit(OpCodes.Ldarg_0);

                cursor.EmitDelegate<Action<GenericNotification>>((self) =>
                {
                    instance = self;
                });

                cursor.GotoNext(
                    i => i.MatchLdfld("RoR2.EquipmentDef", "nameToken")
                );
                cursor.Index++;

                cursor.EmitDelegate<Func<string, string>>((nameToken) =>
                {
                    name = nameToken;
                    return nameToken;
                });

                cursor.GotoNext(
                    i => i.MatchCall("UnityEngine.Resources", "Load"),
                    i => i.MatchCallvirt("UnityEngine.UI.RawImage", "set_texture")
                );

                cursor.Index += 2;

                cursor.EmitDelegate<Action>(() =>
                {
                    if (name != null)
                    {
                        var currentCustomEquipment = GetCustomEquipment(name);
                        if (currentCustomEquipment != null && currentCustomEquipment.Icon != null)
                        {
                            if (instance != null)
                                instance.iconImage.texture = (Texture)currentCustomEquipment.Icon;
                        }
                    }
                });
            };

            IL.RoR2.UI.ItemIcon.SetItemIndex += il =>
            {
                ILCursor cursor = new ILCursor(il);
                string name = null;
                ItemIcon instance = null;

                cursor.Emit(OpCodes.Ldarg_0);

                cursor.EmitDelegate<Action<ItemIcon>>((self) =>
                {
                    instance = self;
                });

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
                    i => i.MatchCall("UnityEngine.Resources", "Load"),
                    i => i.MatchCallvirt("UnityEngine.UI.RawImage", "set_texture")
                );

                cursor.Index += 2;

                cursor.EmitDelegate<Action>(() =>
                {
                    if (name != null)
                    {
                        var currentCustomItem = GetCustomItem(name);
                        if (currentCustomItem != null && currentCustomItem.Icon != null)
                        {
                            if (instance != null)
                                instance.image.texture = (Texture)currentCustomItem.Icon;
                        }
                    }
                });
            };

            IL.RoR2.UI.EquipmentIcon.SetDisplayData += il =>
            {
                ILCursor cursor = new ILCursor(il);
                string name = null;
                EquipmentDef equipDefArg = null;
                EquipmentIcon instance = null;

                cursor.GotoNext(
                    i => i.MatchLdarg(1),
                    i => i.MatchLdfld("RoR2.UI.EquipmentIcon/DisplayData", "equipmentDef")
                );

                cursor.Index += 2;

                cursor.EmitDelegate<Func<EquipmentDef, EquipmentDef>>((self) =>
                {
                    equipDefArg = self;
                    if (equipDefArg != null)
                        name = equipDefArg.nameToken;
                    return self;
                });

                cursor.Emit(OpCodes.Ldarg_0);

                cursor.EmitDelegate<Action<EquipmentIcon>>((self) =>
                {
                    instance = self;
                });

                cursor.GotoNext(
                    i => i.MatchLdloc(0),
                    i => i.MatchCallvirt("UnityEngine.UI.RawImage", "set_texture")
                );

                cursor.Index += 2;

                cursor.EmitDelegate<Action>(() =>
                {
                    if (name != null)
                    {
                        var currentCustomEquipment = GetCustomEquipment(name);
                        if (currentCustomEquipment != null && currentCustomEquipment.Icon != null)
                        {
                            if (equipDefArg != null)
                                instance.iconImage.texture = (Texture)currentCustomEquipment.Icon;
                        }
                    }
                });
            };

            // ItemDisplayRule on CharacterModel. Manually setting the followerprefab

            // BuffDisplay

            IL.RoR2.UI.BuffDisplay.AllocateIcons += il =>
            {
                var cursor = new ILCursor(il);
                cursor.GotoNext(i => i.MatchLdcI4(OriginalBuffCount));
                cursor.Next.OpCode = OpCodes.Ldc_I4;
                cursor.Next.Operand = TotalBuffCount;
            };

            IL.RoR2.UI.BuffDisplay.UpdateLayout += il =>
            {
                var cursor = new ILCursor(il);
                cursor.GotoNext(i => i.MatchLdcI4(OriginalBuffCount));
                cursor.Next.OpCode = OpCodes.Ldc_I4;
                cursor.Next.Operand = TotalBuffCount;
            };

            // BuffMask

            IL.RoR2.BuffMask.cctor += il =>
            {
                var cursor = new ILCursor(il);
                cursor.GotoNext(i => i.MatchLdcI4(OriginalBuffCount));
                cursor.Next.OpCode = OpCodes.Ldc_I4;
                cursor.Next.Operand = TotalBuffCount;
            };

            // Icon injection for buff

            IL.RoR2.UI.BuffIcon.UpdateIcon += il =>
            {
                var cursor = new ILCursor(il);
                cursor.GotoNext(i => i.MatchLdfld("RoR2.BuffDef", "iconPath"));
                cursor.Index--;

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldfld,
                            typeof(BuffIcon).GetField("buffIndex", BindingFlags.Instance | BindingFlags.Public));
                cursor.Emit(OpCodes.Ldc_I4, OriginalBuffCount);

                var caseOriginal = cursor.DefineLabel();
                var setSprite = cursor.DefineLabel();
                cursor.Emit(OpCodes.Blt, caseOriginal);

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldfld,
                            typeof(BuffIcon).GetField("buffIndex", BindingFlags.Instance | BindingFlags.Public));
                cursor.EmitDelegate<Func<BuffIndex, Sprite>>(buffIndex => GetCustomBuff((int)buffIndex).Icon);
                cursor.Emit(OpCodes.Br, setSprite);
                cursor.MarkLabel(caseOriginal);

                cursor.GotoNext(i => i.MatchCallvirt("UnityEngine.UI.Image", "set_sprite"));
                cursor.MarkLabel(setSprite);
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