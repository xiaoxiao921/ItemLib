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
        public const string ModVer = "0.0.7";
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
            helpText = "Give custom item, id only for items. Get all custom item's id : /il_getids in console. Usage Example : /give_custom_item 78 1. Usage : /give_custom_item [itemName/custom_itemID] (optional)[count] (optional)[playerName/playerID]")]
        private static void GiveCustomItem(ConCommandArgs args)
        {
            string countString = null;
            NetworkUser player = null;

            string indexString = args.userArgs[0];
            if(args.userArgs.Count > 1)
                countString = args.userArgs[1];
            if (args.userArgs.Count > 2)
            {
                var playerString = args.userArgs[2];
                player = GetNetUserFromString(playerString);
            }

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
                if (itemIndex < (ItemLib.TotalItemCount + ItemLib.TotalEquipmentCount) && itemIndex >= 0)
                {
                    itemType = (ItemIndex) itemIndex;
                    inventory.GiveItem(itemType, itemCount);
                }
            }
            else
            {
                Debug.Log("Incorrect arguments. Usage Example : /give_custom_item 78 1. Usage : /give_custom_item [itemName/custom_itemID] (optional)[count] (optional)[playerName/playerID]");
            }
        }

        [ConCommand(commandName = "il_getids", flags = ConVarFlags.None,
            helpText = "Give custom item, id only for items. Get all custom item's id : /il_getids in console. Usage Example : /give_custom_item 78 1. Usage : /give_custom_item [itemName/custom_itemID] (optional)[count] (optional)[playerName/playerID]")]
        private static void GetIds(ConCommandArgs args)
        {
            string list = "Custom Items IDs: \n";

            for (int i = 0; i < ItemLib.CustomItemList.Count; i++)
            {
                list += ItemLib.CustomItemList[i].ItemDef.nameToken + " | " + (i + ItemLib.OriginalItemCount)+"\n";
            }

            list += "Custom Equipments IDs : \n";

            for (int i = 0; i < ItemLib.CustomEquipmentList.Count; i++)
            {
                list += ItemLib.CustomEquipmentList[i].EquipmentDef.nameToken + " | " + (i + ItemLib.OriginalEquipmentCount+ItemLib.TotalItemCount) + "\n";
            }

            Debug.Log(list);
        }
#if DEBUG
        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.F3))
            {
                //var dropList = Run.instance.availableEquipmentDropList;

                var trans = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;
                var chest = Resources.Load<SpawnCard>("SpawnCards/InteractableSpawnCard/iscGoldChest");
                
                
                /*foreach (var pi in ItemDropAPI.Selection[ItemDropLocation.LargeChest])
                {
                    Debug.Log("dropchance : "+pi.DropChance);
                    foreach (var pu in pi.Pickups)
                    {
                        Debug.Log("itemIndex : "+(int)pu.itemIndex);
                        //PickupDropletController.CreatePickupDroplet(pu, trans.position, trans.forward * 20f);
                    }
                }*/

                var go = chest.DoSpawn(trans.position, trans.rotation);
                //var chestbeha = go.GetComponent<ChestBehavior>();
                //chestbeha.Open();
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