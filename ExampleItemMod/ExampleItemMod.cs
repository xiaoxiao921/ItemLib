using System.Collections.ObjectModel;
using System.Linq;
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

        private static int _myCustomItemId1;

        public ExampleItemMod()
        {
            // retrieve your item id from the lib.

            _myCustomItemId1 = ItemLib.ItemLib.GetItemId("Custom Item Example");

            On.RoR2.CharacterBody.OnKilledOther += (orig, self, damageReport) =>
            {
                orig(self, damageReport);

                if (self.inventory.GetItemCount((ItemIndex) _myCustomItemId1) > 0)
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
        public static ItemDef Test()
        {
            ItemDef newItemDef = new ItemDef
            {
                tier = ItemTier.Tier1,
                pickupModelPath = "Prefabs/PickupModels/PickupWolfPelt",
                pickupIconPath = "Textures/ItemIcons/texWolfPeltIcon",
                nameToken = "Custom Item Example",
                pickupToken = "i'm a custom item. i do sticky bomb on kill",
                descriptionToken = "yes",
                addressToken = ""
            };
            return newItemDef;
        }
    }
}