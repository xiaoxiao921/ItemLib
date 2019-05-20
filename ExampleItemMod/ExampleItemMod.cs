using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using BepInEx;
using ItemLib;
using RoR2;
using UnityEngine;

namespace ExampleItemMod
{
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
            // retrieve your item id from the lib.
            _myCustomItemId = ItemLib.ItemLib.GetItemId("Custom Item Example");

            On.RoR2.CharacterBody.OnKilledOther += (orig, self, damageReport) =>
            {
                orig(self, damageReport);

                if (self.inventory.GetItemCount((ItemIndex) _myCustomItemId) > 0)
                {
                    DetonateAlive(100);
                }
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

        [Item(ItemAttribute.ItemType.Item)]
        public static ItemLib.CustomItem Test()
        {
            // Load the AssetBundle you made with the Unity Editor

            _exampleAssetBundle = AssetBundle.LoadFromFile(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/exampleitemmod");

            _prefab = _exampleAssetBundle.LoadAsset<GameObject>("Assets/Import/belt/belt.prefab");
            _icon = _exampleAssetBundle.LoadAsset<Object>("Assets/Import/belt_icon/belt_icon.png");

            ItemDef newItemDef = new ItemDef
            {
                tier = ItemTier.Tier1,
                pickupModelPath = "", // leave it empty and give directly the prefab / icon on the return but you can also use an already made prefab by putting a path in there.
                pickupIconPath = "",
                nameToken = "Custom Item Example",
                pickupToken = "i'm a custom item. i do sticky bomb on kill",
                descriptionToken = "yes",
                addressToken = ""
            };

            return new CustomItem(newItemDef, _prefab, _icon);
        }
    }
}