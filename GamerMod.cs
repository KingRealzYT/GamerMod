using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using HarmonyLib;
using Reactor;

namespace GamerMod
{
    [BepInPlugin(Id)]
    [BepInProcess("Among Us.exe")]
    [BepInDependency(ReactorPlugin.Id)]
    public class GamerPlugin : BasePlugin
    {
        private const string Id = "com.kingrealzyt.gamermod";
        public const string ModVersion = "1.0.0";
        private Harmony Harmony { get; } = new Harmony(Id);

        public ConfigEntry<string> Name { get; private set; }

        public override void Load()
        {
            System.Console.WriteLine("GamerMod Loaded!");
            Harmony.PatchAll();
        }
    }
}