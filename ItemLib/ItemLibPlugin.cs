using BepInEx;
using MonoMod.Cil;
using RoR2;
using System;
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
            On.RoR2.RoR2Application.UnitySystemConsoleRedirector.Redirect += orig => { };

            On.RoR2.Console.Awake += (orig, self) =>
            {
                CommandHelper.RegisterCommands(self);
                orig(self);
            };

            ItemLib.Initialize();
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
                var dropList = Run.instance.availableTier1DropList;
                Chat.AddMessage(dropList.Count.ToString());
                var nextItem = Run.instance.treasureRng.RangeInt(0, dropList.Count);
                var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;
                //PickupDropletController.CreatePickupDroplet(dropList[78], transform.position, transform.forward * 20f);
            }
        }
    }
}
