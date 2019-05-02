using BepInEx;
using RoR2;

namespace ItemLib
{
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin(ModGuid, ModName, ModVer)]
    public class ItemLibPlugin : BaseUnityPlugin
    {
        private const string ModVer    = "0.0.1";
        private const string ModName   = "ItemLib";
        private const string ModGuid   = "dev.iDeathHD.ItemLib";

        public ItemLibPlugin()
        {
            On.RoR2.RoR2Application.UnitySystemConsoleRedirector.Redirect += orig => { };
            ItemLib.Initialize();
        }

        [ItemAttribute("NEWONE")]
        public ItemDef test()
        {
            ItemDef newItemDef = new ItemDef
            {
                tier = ItemTier.NoTier,
                nameToken = "ITEM_AACANNON_NAME",
                pickupToken = "ITEM_AACANNON_PICKUP",
                descriptionToken = "ITEM_AACANNON_DESC",
                addressToken = ""
            };
            return newItemDef;
        }
    }
}
