using BepInEx.Configuration;
using UnityEngine;

namespace ItemLib
{
    public static class ItemLibConfig
    {
        internal static void Init(ConfigFile config)
        {
            EnableEliteSpawningOverhaul = config.Wrap(
                "Features",
                "EnableEliteSpawningOverhaul",
                "Whether to enable overhauled version of elite spawning; some mods may depend on this being enabled",
                true);
        }

        public static ConfigWrapper<bool> EnableEliteSpawningOverhaul;
    }
}
