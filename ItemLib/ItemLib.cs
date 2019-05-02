using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using MonoMod.Cil;
using RoR2;
using UnityEngine;

namespace ItemLib
{
    public static class ItemLib
    {
        private static int originalItemCount;

        private static int customItemCount;

        private static List<ItemDef> itemDefList = new List<ItemDef>();

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
                            customItemCount++;
                            itemDefList.Add((ItemDef)methodInfo.Invoke(null, null)); // The method from the mod should return a new itemDef so the library can load it.

                        }
                    }
                }
            }

            InitHooks();

            MethodInfo DefineItems_MI = typeof(ItemCatalog).GetMethod("DefineItems", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            DefineItems_MI.Invoke(null, null);

            // Register the items into the game.
            for (int i = 0; i < customItemCount; i++)
            {
                MethodInfo RegisterItem_MI = typeof(ItemCatalog).GetMethod("RegisterItem", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                object[] para = { (ItemIndex)(i + originalItemCount + 1), itemDefList[i] };
                RegisterItem_MI.Invoke(null, para);
                Debug.Log("adding item");
            }
        }

        [ItemAttribute("NEWONE")]
        public static ItemDef test()
        {
            ItemDef newItemDef = new ItemDef
            {
                tier = ItemTier.NoTier,
                nameToken = "ITEM_AACANNON_NAME",
                pickupToken = "ITEM_AACANNON_PICKUP",
                descriptionToken = "ITEM_AACANNON_DESC",
                addressToken = ""
            };
            return newItemDef;
        }

        private static void InitHooks()
        {
            // Make it so itemDefs is large enough for all the new items.
            IL.RoR2.ItemCatalog.DefineItems += il =>
            {
                ILCursor cursor = new ILCursor(il);

                cursor.Index++;

                // ItemCatalog.itemDefs = new ItemDef[78+itemTotalCount];
                cursor.EmitDelegate<Func<int, int>>((self) =>
                {
                    originalItemCount = self;
                    self += customItemCount + 1;
                    return self;
                });
            };

            // Need to modifiy every methods that uses ItemIndex.Count as a check for the last item in the list.
        }
    }
}