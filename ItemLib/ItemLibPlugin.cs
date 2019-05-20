using BepInEx;
using RoR2;
using UnityEngine;

namespace ItemLib
{
    [BepInPlugin(ModGuid, ModName, ModVer)]
    public class ItemLibPlugin : BaseUnityPlugin
    {
        private const string ModVer = "0.0.1";
        private const string ModName = "ItemLib";
        private const string ModGuid = "dev.iDeathHD.ItemLib";

        public ItemLibPlugin()
        {
#if DEBUG
            Debug.Log("[ItemLib] Debug");
#endif
            ItemLib.Initialize();

            On.RoR2.RoR2Application.UnitySystemConsoleRedirector.Redirect += orig => { };

            On.RoR2.Console.Awake += (orig, self) =>
            {
                CommandHelper.RegisterCommands(self);
                orig(self);
            };
        }

        [ConCommand(commandName = "custom_item", flags = ConVarFlags.ExecuteOnServer,
            helpText = "Give custom item, id only. /custom_item 78 1")]
        private static void Customitem(ConCommandArgs args)
        {
            string indexString = args.userArgs[0];
            string countString = args.userArgs[1];

            Inventory inventory = args.sender.master.inventory;

            if (!int.TryParse(countString, out var itemCount))
            {
                itemCount = 1;
            }

            if (int.TryParse(indexString, out var itemIndex))
            {
                if (itemIndex >= 0 && itemIndex < ItemLib.TotalItemCount)
                {
                    var itemType = (ItemIndex) itemIndex;
                    inventory.GiveItem(itemType, itemCount);
                }
            }
        }
#if DEBUG
        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.F3))
            {
                // drop every t1 items. for trying prefabs pickup.
                var dropList = Run.instance.availableTier3DropList;
                Debug.Log(dropList.Count);
                var trans = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;
                for (int i = 0; i != dropList.Count; i++)
                    PickupDropletController.CreatePickupDroplet(dropList[i], trans.position, trans.forward * 20f);
            }
        }
#endif
    }
}