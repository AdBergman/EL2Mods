using BepInEx;
using HarmonyLib;

namespace EL2MapGenMod
{
    [BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
    public sealed class EL2MapGenModPlugin : BaseUnityPlugin
    {
        private void Awake()
        {
            var harmony = new Harmony(ModInfo.Guid);
            harmony.PatchAll();
        }
    }

    internal static class ModInfo
    {
        public const string Guid = "com.yourname.el2.mapgen.bigger.vertical.rivers";
        public const string Name = "EL2 MapGen: Bigger + More Vertical + More Rivers + Persistent Seas";
        public const string Version = "1.0.0";
    }
}