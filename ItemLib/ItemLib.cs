using System;
using System.Reflection;
using System.Linq;
using MonoMod.Cil;
using RoR2;
using System.Collections.Generic;

namespace ItemLib
{
    public static class ItemLib
    {
        private static int originalItemCount;

        private static int customItemCount;

        private static List<ItemDef> itemDefList = new List<ItemDef>();

        public static void Initialize()
        {
            // Get all the custom items from all mods that are using ItemLib.
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                Type[] types = assembly.GetTypes();

                for (int i = 0; i < types.Length; i++)
                {
                    // that take way too long
                    foreach (var methodInfo in types.SelectMany(x => x.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)))
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

            // Register the items into the game.
            for (int i = 0; i < customItemCount; i++)
            {
                MethodInfo RegisterItem_MI = typeof(ItemCatalog).GetMethod("RegisterItem");
                object[] para = { (ItemIndex)(i + originalItemCount + 1), itemDefList[i] };
                RegisterItem_MI.Invoke(null, para);
            }
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
                    self += customItemCount;
                    return self;
                });
            };
        }
    }
}