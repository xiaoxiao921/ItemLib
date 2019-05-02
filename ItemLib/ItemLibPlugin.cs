using BepInEx;

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
    }
}
