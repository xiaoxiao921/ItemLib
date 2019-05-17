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

        private static int myCustomItemID_1;

        public ExampleItemMod()
        {
            // retrieve your item id from the lib.
            // if you can't find the id of your item, means your mod loaded before the lib, so call ItemLib.Initialize

            myCustomItemID_1 = ItemLib.ItemLib.GetItemID("Custom Item Example");

            On.RoR2.CharacterBody.OnKilledOther += (orig, self, damageReport) =>
            {
                orig(self, damageReport);

                if (self.inventory.GetItemCount((ItemIndex) myCustomItemID_1) > 0)
                {
                    DetonateAlive(100);
                }
            };
        }

        public static void DetonateAlive(float radius)
        {
            var currBody = RoR2.CameraRigController.readOnlyInstancesList.First().viewer.masterController.master.GetBody();
            var StickyBomb = (GameObject)Resources.Load("Prefabs/Projectiles/StickyBomb");

            ReadOnlyCollection<TeamComponent> teamMembers = TeamComponent.GetTeamMembers(TeamIndex.Monster);
            for (int i = 0; i < teamMembers.Count; i++)
            {
                float sqr_distance = Vector3.SqrMagnitude(teamMembers[i].transform.position - currBody.transform.position);
                if (sqr_distance <= (radius * radius))
                {
                    Vector3 position = currBody.transform.position;
                    Vector3 forward = teamMembers[i].transform.position - position;
                    Quaternion rotation = (forward.magnitude != 0f) ? Util.QuaternionSafeLookRotation(forward) : UnityEngine.Random.rotationUniform;
                    RoR2.Projectile.ProjectileManager.instance.FireProjectile(StickyBomb, teamMembers[i].transform.position, rotation, currBody.gameObject, currBody.damage * 40, 100f, RoR2.Util.CheckRoll(currBody.crit, currBody.master), DamageColorIndex.Item, null, forward.magnitude * 60f);
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