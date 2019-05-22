using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using BepInEx;
using ItemLib;
using RoR2;
using UnityEngine;

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

        public ExampleItemMod()
        {
            _myCustomItemId = ItemLib.ItemLib.GetEquipmentId("Custom Equipment Example");

            // Need to hook in here so the item actually proc, the orig method is a switch case on the equipmentIndex

            On.RoR2.EquipmentSlot.PerformEquipmentAction += (orig, self, equipmentIndex) =>
            {
                Debug.Log((int)equipmentIndex);
                Debug.Log((int)_myCustomItemId);
                if ((int) equipmentIndex == _myCustomItemId)
                {
                    DetonateAlive(100);
                    return true; // must
                }
                return orig(self, equipmentIndex); // must
            };
        }

        private static void DetonateAlive(float radius)
        {
            var currBody = RoR2.CameraRigController.readOnlyInstancesList.First().viewer.masterController.master.GetBody();
            var stickyBomb = (GameObject)Resources.Load("Prefabs/Projectiles/StickyBomb");

            ReadOnlyCollection<TeamComponent> teamMembers = TeamComponent.GetTeamMembers(TeamIndex.Monster);
            for (int i = 0; i < teamMembers.Count; i++)
            {
                float sqrDistance = Vector3.SqrMagnitude(teamMembers[i].transform.position - currBody.transform.position);
                if (sqrDistance <= (radius * radius))
                {
                    Vector3 position = currBody.transform.position;
                    Vector3 forward = teamMembers[i].transform.position - position;
                    Quaternion rotation = (forward.magnitude != 0f) ? Util.QuaternionSafeLookRotation(forward) : UnityEngine.Random.rotationUniform;
                    RoR2.Projectile.ProjectileManager.instance.FireProjectile(stickyBomb, teamMembers[i].transform.position, rotation, currBody.gameObject, currBody.damage * 40, 100f, RoR2.Util.CheckRoll(currBody.crit, currBody.master), DamageColorIndex.Item, null, forward.magnitude * 60f);
                }
            }
        }

        [Item(ItemAttribute.ItemType.Equipment)]
        public static ItemLib.CustomEquipment Test()
        {
            // Load the AssetBundle you made with the Unity Editor

            _exampleAssetBundle = AssetBundle.LoadFromFile(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/Rampage_data");

            _prefab = _exampleAssetBundle.LoadAsset<GameObject>("Assets/Import/belt/belt.prefab");
            _icon = _exampleAssetBundle.LoadAsset<Object>("Assets/Import/belt_icon/belt_icon.png");

            EquipmentDef newEquipmentDef = new EquipmentDef
            {
                cooldown = 45f,
                pickupModelPath = "",
                pickupIconPath = "",
                nameToken = "Custom Equipment Example",
                pickupToken = "pickup sample text",
                descriptionToken = "description in logbook",
                addressToken = "",
                canDrop = true,
                enigmaCompatible = true
            };

            return new CustomEquipment(newEquipmentDef, _prefab, _icon);
        }
    }
}