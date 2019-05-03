using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using MonoMod.Cil;
using RoR2;
using UnityEngine;
using RoR2.Stats;

namespace ItemLib
{
    public static class ItemLib
    {
        private static int originalItemCount;

        private static int customItemCount;

        private static List<ItemDef> itemDefList = new List<ItemDef>();

        private static MethodInfo lastMI;

        public static void Initialize()
        {
            // Get all the custom items (ItemDef) from all mods that are using ItemLib.
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
                            if (lastMI != null) // all this ugly stuff is because i'm getting duplicates attributes and idk why
                            {
                                if (lastMI.Name.Equals(methodInfo.Name))
                                {
                                    lastMI = methodInfo;
                                    continue;
                                }
                                else
                                {
                                    customItemCount++;
                                    itemDefList.Add((ItemDef)methodInfo.Invoke(null, null)); // The method from the mod should return a new itemDef so the library can load it.
                                    lastMI = methodInfo;
                                }
                            }
                            else
                            {
                                customItemCount++;
                                itemDefList.Add((ItemDef)methodInfo.Invoke(null, null));
                                lastMI = methodInfo;
                            }
                        }
                    }
                }
            }

            Debug.Log("ItemCatalog Hooking");
            InitCatalogHook();

            // Call DefineItems because catalog is already made...
            Debug.Log("ItemCatalog.DefineItems()");
            MethodInfo DefineItems_MI = typeof(ItemCatalog).GetMethod("DefineItems", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            DefineItems_MI.Invoke(null, null);


            Debug.Log("Hooking all others things (itemmask etc)");
            InitHooks();

        }

        [ItemAttribute("NEWONE")]
        public static ItemDef test()
        {
            ItemDef newItemDef = new ItemDef
            {
                tier = ItemTier.Tier1,
                pickupModelPath = "Prefabs/PickupModels/PickupWolfPelt",
                pickupIconPath = "Textures/ItemIcons/texWolfPeltIcon",
                nameToken = "THE WOO ITEM",
                pickupToken = "PFGHKOPFGHKPOH",
                descriptionToken = "LETS GO",
                addressToken = ""
            };
            return newItemDef;
        }

        private static void InitCatalogHook()
        {
            // Make it so itemDefs is large enough for all the new items.
            IL.RoR2.ItemCatalog.DefineItems += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                        i => i.MatchLdcI4(78) // ItemCatalog.itemDefs = new ItemDef[78 + itemTotalCount];
                );
                cursor.Index++;

                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    originalItemCount = self;
                    self += customItemCount;
                    return self;
                });

                cursor.GotoNext(
                        i => i.MatchLdcI4(0),
                        i => i.MatchStloc(0)
                );
                cursor.Index++;

                cursor.EmitDelegate<Action>(() =>
                {
                    // Register the items into the game.
                    Debug.Log("Starting Custom RegisterItem() Invoke");
                    for (int i = 0; i < customItemCount; i++)
                    {
                        MethodInfo RegisterItem_MI = typeof(ItemCatalog).GetMethod("RegisterItem", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                        object[] para = { (ItemIndex)(i + originalItemCount), itemDefList[i] };
                        RegisterItem_MI.Invoke(null, para);
                        Debug.Log("adding item at index : " + (i + originalItemCount));
                    }
                });
            };
        }

        private static void InitHooks()
        {
            // Need to modifiy every methods that uses ItemIndex.Count as a check for the last item in the list.

            IL.RoR2.CharacterModel.UpdateItemDisplay += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                        i => i.MatchLdcI4(originalItemCount)
                );
                cursor.Index++;

                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });
            };

            IL.RoR2.Inventory.HasAtLeastXTotalItemsOfTier += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                        i => i.MatchLdcI4(78)
                );
                cursor.Index++;
                //cursor.Remove();
                //cursor.Emit(OpCodes.Ldc_I4, 79); // thats another way to do it i guess

                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount - 1;
                    return self;
                });
            };

            IL.RoR2.Inventory.GetTotalItemCountOfTier += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                        i => i.MatchLdcI4(originalItemCount)
                );
                cursor.Index++;

                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });
            };

            IL.RoR2.RunReport.PlayerInfo.Generate += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                        i => i.MatchLdcI4(originalItemCount)
                );
                cursor.Index++;

                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });
            };

            IL.RoR2.Achievements.Discover10UniqueTier1Achievement.UniqueTier1Discovered += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                        i => i.MatchLdcI4(originalItemCount)
                );
                cursor.Index++;

                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });
            };

            IL.RoR2.Run.BuildDropTable += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                        i => i.MatchLdcI4(originalItemCount)
                );
                cursor.Index++;

                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });
            };

            IL.RoR2.ItemCatalog.AllItemsEnumerator.MoveNext += il => // exception if no extended array size for DefineItems()
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                        i => i.MatchLdcI4(originalItemCount)
                );
                cursor.Index++;

                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });
            };

            IL.RoR2.ItemCatalog.GetItemDef += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                        i => i.MatchLdcI4(originalItemCount)
                );
                cursor.Index++;

                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });
            };

            IL.RoR2.ItemCatalog.RequestItemOrderBuffer += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                        i => i.MatchLdcI4(originalItemCount)
                );
                cursor.Index++;

                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });
            };

            IL.RoR2.ItemCatalog.RequestItemStackArray += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                        i => i.MatchLdcI4(originalItemCount)
                );
                cursor.Index++;

                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });
            };

            // Changing all hard coded array

            // Reflection showcase kek
            var instancesList = (List<PerItemStatDef>)typeof(PerItemStatDef).GetField("instancesList", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);
            foreach (PerItemStatDef instance in instancesList)
            {
                typeof(PerItemStatDef).GetField("keyToStatDef", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(instance, new StatDef[78 + customItemCount]);
            }

            /*IL.RoR2.Stats.PerItemStatDef.ctor += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                        i => i.MatchLdcI4(originalItemCount)
                );
                cursor.Index++;

                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });
            };*/

            IL.RoR2.RunReport.PlayerInfo.ctor += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                        i => i.MatchLdcI4(originalItemCount)
                );
                cursor.Index++;

                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });
            };

            IL.RoR2.Inventory.ctor += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                        i => i.MatchLdcI4(originalItemCount)
                );
                cursor.Index++;

                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });
            };

            IL.RoR2.CharacterModel.ctor += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                        i => i.MatchLdcI4(originalItemCount)
                );
                cursor.Index++;

                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });
            };

            // PickupIndex, another set of hardcoded shit

            IL.RoR2.PickupIndex.cctor += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                        i => i.MatchLdcI4(105) // hitting lunarcoin1. this == lunarcoinStart originalitemcount + originalequipmentcount
                );
                cursor.Index++;
                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });

                cursor.GotoNext(
                        i => i.MatchLdcI4(105) // pickupindex last
                );
                cursor.Index++;
                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });

                cursor.GotoNext(
                        i => i.MatchLdcI4(106) // pickupindex end
                );
                cursor.Index++;
                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });

                cursor.GotoNext(
                        i => i.MatchLdcI4(107) // allpickupnames string array
                );
                cursor.Index++;
                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });

                cursor.GotoNext(
                        i => i.MatchLdcI4(originalItemCount) // ItemIndex.count
                );
                cursor.Index++;
                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });

                cursor.GotoNext(
                        i => i.MatchLdcI4(79) // EquipmentIndex index start for allPickupNames array . originalItemCount + 1
                );
                cursor.Index++;
                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });

                cursor.GotoNext(
                        i => i.MatchLdcI4(105) // lunar coin loop 
                );
                cursor.Index++;
                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });

                cursor.GotoNext(
                        i => i.MatchLdcI4(105) // lunar coin loop pt 2
                );
                cursor.Index++;
                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });
            };

            IL.RoR2.PickupIndex.ctor_EquipmentIndex += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                        i => i.MatchLdcI4(originalItemCount)
                );
                cursor.Index++;
                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });
            };

            IL.RoR2.PickupIndex.GetPickupDisplayPrefab += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                        i => i.MatchLdcI4(originalItemCount)
                );
                cursor.Index++;
                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });

                cursor.GotoNext(
                        i => i.MatchLdcI4(105)
                );
                cursor.Index++;
                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });

                cursor.GotoNext(
                        i => i.MatchLdcI4(originalItemCount)
                );
                cursor.Index++;
                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });

                cursor.GotoNext(
                        i => i.MatchLdcI4(106)
                );
                cursor.Index++;
                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });
            };

            IL.RoR2.PickupIndex.GetPickupDropletDisplayPrefab += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                        i => i.MatchLdcI4(originalItemCount)
                );
                cursor.Index++;
                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });

                cursor.GotoNext(
                        i => i.MatchLdcI4(105)
                );
                cursor.Index++;
                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });

                cursor.GotoNext(
                        i => i.MatchLdcI4(106)
                );
                cursor.Index++;
                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });
            };

            IL.RoR2.PickupIndex.GetPickupColor += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                        i => i.MatchLdcI4(originalItemCount)
                );
                cursor.Index++;
                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });

                cursor.GotoNext(
                        i => i.MatchLdcI4(105)
                );
                cursor.Index++;
                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });

                cursor.GotoNext(
                        i => i.MatchLdcI4(originalItemCount)
                );
                cursor.Index++;
                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });

                cursor.GotoNext(
                        i => i.MatchLdcI4(106)
                );
                cursor.Index++;
                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });
            };

            IL.RoR2.PickupIndex.GetPickupColorDark += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                        i => i.MatchLdcI4(originalItemCount)
                );
                cursor.Index++;
                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });

                cursor.GotoNext(
                        i => i.MatchLdcI4(105)
                );
                cursor.Index++;
                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });

                cursor.GotoNext(
                        i => i.MatchLdcI4(originalItemCount)
                );
                cursor.Index++;
                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });

                cursor.GotoNext(
                        i => i.MatchLdcI4(106)
                );
                cursor.Index++;
                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });
            };

            IL.RoR2.PickupIndex.GetPickupNameToken += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                        i => i.MatchLdcI4(originalItemCount)
                );
                cursor.Index++;
                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });

                cursor.GotoNext(
                        i => i.MatchLdcI4(105)
                );
                cursor.Index++;
                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });

                cursor.GotoNext(
                        i => i.MatchLdcI4(originalItemCount)
                );
                cursor.Index++;
                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });

                cursor.GotoNext(
                        i => i.MatchLdcI4(106)
                );
                cursor.Index++;
                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });
            };

            IL.RoR2.PickupIndex.GetUnlockableName += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                        i => i.MatchLdcI4(originalItemCount)
                );
                cursor.Index++;
                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });

                cursor.GotoNext(
                        i => i.MatchLdcI4(105)
                );
                cursor.Index++;
                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });

                cursor.GotoNext(
                        i => i.MatchLdcI4(originalItemCount)
                );
                cursor.Index++;
                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });
            };

            IL.RoR2.PickupIndex.IsLunar += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                        i => i.MatchLdcI4(originalItemCount)
                );
                cursor.Index++;
                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });

                cursor.GotoNext(
                        i => i.MatchLdcI4(105)
                );
                cursor.Index++;
                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });

                cursor.GotoNext(
                        i => i.MatchLdcI4(originalItemCount)
                );
                cursor.Index++;
                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });
            };

            IL.RoR2.PickupIndex.IsBoss += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                        i => i.MatchLdcI4(originalItemCount)
                );
                cursor.Index++;
                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });

                cursor.GotoNext(
                        i => i.MatchLdcI4(105)
                );
                cursor.Index++;
                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });

                cursor.GotoNext(
                        i => i.MatchLdcI4(originalItemCount)
                );
                cursor.Index++;
                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });
            };

            // ItemMask

            

            IL.RoR2.ItemMask.HasItem += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                        i => i.MatchLdcI4(originalItemCount)
                );
                cursor.Index++;
                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount + 1;
                    return self;
                });
            };

            IL.RoR2.ItemMask.AddItem += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                        i => i.MatchLdcI4(originalItemCount)
                );
                cursor.Index++;
                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });
            };

            IL.RoR2.ItemMask.RemoveItem += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                        i => i.MatchLdcI4(originalItemCount)
                );
                cursor.Index++;
                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });
            };

            // RuleCatalog

            IL.RoR2.RuleCatalog.cctor += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.GotoNext(
                        i => i.MatchLdcI4(originalItemCount)
                );
                cursor.Index++;
                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    self += customItemCount;
                    return self;
                });
            };
        }
    }
}