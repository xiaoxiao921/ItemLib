using BepInEx;
using MonoMod.Cil;
using RoR2;
using System;
using System.Reflection;
using UnityEngine;

namespace ItemLib
{
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin(ModGuid, ModName, ModVer)]
    public class ItemLibPlugin : BaseUnityPlugin
    {
        private const string ModVer = "0.0.1";
        private const string ModName = "ItemLib";
        private const string ModGuid = "dev.iDeathHD.ItemLib";

        public ItemLibPlugin()
        {
            ItemLib.Initialize();
            On.RoR2.RoR2Application.UnitySystemConsoleRedirector.Redirect += orig => { };

            On.RoR2.Console.Awake += (orig, self) =>
            {
                CommandHelper.RegisterCommands(self);
                orig(self);
            };
        }

        [ConCommand(commandName = "custom_item", flags = ConVarFlags.ExecuteOnServer, helpText = "Give custom item")]
        private static void customitem(ConCommandArgs args)
        {

            string indexString = args.userArgs[0];
            string countString = args.userArgs[1];

            Inventory inventory = args.sender.master.inventory;

            int itemCount = 1;
            if (!int.TryParse(countString, out itemCount))
            {
                itemCount = 1;
            }

            int itemIndex = 0;
            ItemIndex itemType = ItemIndex.Syringe;
            if (int.TryParse(indexString, out itemIndex))
            {
                if (itemIndex < 100 && itemIndex >= 0) // need proper range check
                {
                    itemType = (ItemIndex)itemIndex;
                    inventory.GiveItem(itemType, itemCount);
                }
            }
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.F3))
            {
                // JUST TO BE SURE. This call "should" put the custom item at index 78 of the array. its just getItemDef returning null for some reason
                Debug.Log("ItemCatalog.DefineItems()");
                MethodInfo DefineItems_MI = typeof(ItemCatalog).GetMethod("DefineItems", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                DefineItems_MI.Invoke(null, null);

                ItemDef[] array = (ItemDef[])typeof(ItemCatalog).GetField("itemDefs", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);
                Debug.Log(array.Length);
                array[78] = ItemLib.test();
                Debug.Log(ItemCatalog.GetItemDef((ItemIndex)78)); // null
                Debug.Log(array[78]); // not null...
            }
        }
    }
}
