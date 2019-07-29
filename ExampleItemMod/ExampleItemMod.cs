﻿using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using BepInEx;
using ItemLib;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

// ReSharper disable UnusedMember.Global

namespace ExampleItemMod
{
    [BepInDependency(ItemLibPlugin.ModGuid)]
    [BepInPlugin(ModGuid, ModName, ModVer)]
    public class ExampleItemMod : BaseUnityPlugin
    {
        private const string ModVer = "0.0.1";
        private const string ModName = "ExampleItemMod";
        private const string ModGuid = "dev.iDeathHD.ExampleItemMod";

        private static int _myCustomItemId;

        private static AssetBundle _exampleAssetBundle;
        private static GameObject _prefab;
        private static Object _icon;

        private static ItemDisplayRule[] _itemDisplayRules;

        public ExampleItemMod()
        {
            _myCustomItemId = ItemLib.ItemLib.GetEquipmentId("Custom Equipment Example");

            // Need to hook in here so the item actually proc, the orig method is a switch case on the equipmentIndex

            /*On.RoR2.EquipmentSlot.PerformEquipmentAction += (orig, self, equipmentIndex) =>
            {
                Debug.Log((int)equipmentIndex);
                Debug.Log(_myCustomItemId);
                if ((int) equipmentIndex == _myCustomItemId)
                {
                    DetonateAlive(100);
                    return true; // must
                }
                return orig(self, equipmentIndex); // must
            };*/
        }

        private static void DetonateAlive(float radius)
        {
            var currentBody = CameraRigController.readOnlyInstancesList.First().viewer.masterController.master.GetBody();
            var stickyBomb = (GameObject)Resources.Load("Prefabs/Projectiles/StickyBomb");

            ReadOnlyCollection<TeamComponent> teamMembers = TeamComponent.GetTeamMembers(TeamIndex.Monster);
            foreach (var t in teamMembers)
            {
                float sqrDistance = Vector3.SqrMagnitude(t.transform.position - currentBody.transform.position);
                if (sqrDistance <= (radius * radius))
                {
                    Vector3 position = currentBody.transform.position;
                    Vector3 forward = t.transform.position - position;
                    Quaternion rotation = forward.magnitude != 0f
                        ? Util.QuaternionSafeLookRotation(forward)
                        : Random.rotationUniform;
#pragma warning disable 618
                    ProjectileManager.instance.FireProjectile(stickyBomb,
#pragma warning restore 618
                        t.transform.position, rotation, currentBody.gameObject, currentBody.damage * 40, 100f,
                        Util.CheckRoll(currentBody.crit, currentBody.master), DamageColorIndex.Item, null,
                        forward.magnitude * 60f);
                }
            }
        }

        [Item(ItemAttribute.ItemType.Item)]
        public static CustomItem Test()
        {
            // Load the AssetBundle you made with the Unity Editor

            _exampleAssetBundle = AssetBundle.LoadFromFile(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/Rampage_data");

            _prefab = _exampleAssetBundle.LoadAsset<GameObject>("Assets/Import/belt/belt.prefab");
            _icon = _exampleAssetBundle.LoadAsset<Object>("Assets/Import/belt_icon/belt_icon.png");

            ItemDef newItemDef = new ItemDef
            {
                tier = ItemTier.Tier3,
                pickupModelPath = "", // leave it empty and give directly the prefab / icon on the return but you can also use an already made prefab by putting a path in there.
                pickupIconPath = "",
                nameToken = "Custom Item Example",
                pickupToken = "i'm a custom item. i do sticky bomb on kill",
                descriptionToken = "yes"
            };

            _itemDisplayRules = new ItemDisplayRule[1]; // keep it null if you don't want the item to show up on the survivor 3d model
            _itemDisplayRules[0].followerPrefab = _prefab;
            _itemDisplayRules[0].childName = "Chest";
            _itemDisplayRules[0].localScale = new Vector3(0.15f,0.15f,0.15f);
            _itemDisplayRules[0].localAngles = new Vector3(0f, 180f, 0f);
            _itemDisplayRules[0].localPos = new Vector3(-0.35f, -0.1f, 0f);

            return new CustomItem(newItemDef, _prefab, _icon, _itemDisplayRules);
        }
    }
}