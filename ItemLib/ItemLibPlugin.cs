using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using R2API;
using RoR2;
using UnityEngine;

namespace ItemLib
{
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInPlugin(ModGuid, ModName, ModVer)]
    public class ItemLibPlugin : BaseUnityPlugin
    {
        public const string ModVer = "0.0.6";
        public const string ModName = "ItemLib";
        public const string ModGuid = "dev.iDeathHD.ItemLib";

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

        [ConCommand(commandName = "give_custom_item", flags = ConVarFlags.ExecuteOnServer,
            helpText = "Give custom item, id only for items. Example : /give_custom_item 78 1. Usage : /give_custom_item [itemName/custom_itemID] (optional)[count] (optional)[playerName/playerID]")]
        private static void GiveCustomItem(ConCommandArgs args)
        {
            string indexString = args.userArgs[0];
            string countString = args.userArgs[1];
            string playerString = args.userArgs[2];

            NetworkUser player = GetNetUserFromString(playerString);

            Inventory inventory = player != null ? player.master.inventory : args.sender.master.inventory;


            int itemCount = 1;
            if (!int.TryParse(countString, out itemCount))
            {
                itemCount = 1;
            }

            int itemIndex = 0;
            ItemIndex itemType = ItemIndex.Syringe;
            if (int.TryParse(indexString, out itemIndex))
            {
                if (itemIndex < (int)ItemIndex.Count && itemIndex >= 0)
                {
                    itemType = (ItemIndex)itemIndex;
                    inventory.GiveItem(itemType, itemCount);
                }
            }
            else if (Enum.TryParse<ItemIndex>(indexString, true, out itemType))
            {
                inventory.GiveItem(itemType, itemCount);
            }
            else
            {
                Debug.Log("Incorrect arguments. Usage : /give_custom_item [itemName/custom_itemID] [count] [playerName/playerID]. Example : /give_custom_item 78 1");
            }
        }
#if DEBUG
        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.F3))
            {
                //var dropList = Run.instance.availableEquipmentDropList;

                var trans = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;
                var chest = Resources.Load<SpawnCard>("SpawnCards/InteractableSpawnCard/iscGoldChest");
                var chestbeha = chest.prefab.GetComponent<ChestBehavior>();
                foreach (var pi in ItemDropAPI.Selection[ItemDropLocation.LargeChest])
                {
                    Debug.Log("dropchance : "+pi.DropChance);
                    foreach (var pu in pi.Pickups)
                    {
                        Debug.Log("itemIndex : "+(int)pu.itemIndex);
                        PickupDropletController.CreatePickupDroplet(pu, trans.position, trans.forward * 20f);
                    }
                }

                chest.DoSpawn(trans.position, trans.rotation);
            }
        }
#endif
        private static NetworkUser GetNetUserFromString(string playerString)
        {
            int result = 0;

            if (playerString != "")
            {
                if (int.TryParse(playerString, out result))
                {
                    if (result < NetworkUser.readOnlyInstancesList.Count && result >= 0)
                    {

                        return NetworkUser.readOnlyInstancesList[result];
                    }
                    Debug.Log("Specified player index does not exist");
                    return null;
                }
                else
                {
                    foreach (NetworkUser n in NetworkUser.readOnlyInstancesList)
                    {
                        if (n.userName.Equals(playerString, StringComparison.CurrentCultureIgnoreCase))
                        {
                            return n;
                        }
                    }
                    Debug.Log("Specified player does not exist");
                    return null;
                }
            }

            return null;
        }
    }
}