using Reactor.Button;
using Reactor.Extensions;
using Reactor.Unstrip;
using UnityEngine;
using HarmonyLib;

namespace GamerMod
{
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Start))]
    public static class ShopButton
    {
        private static CooldownButton btn;

        public static void Postfix(HudManager __instance)
        {
            btn = new CooldownButton(
                () =>
                {
                    btn.Timer = btn.MaxTimer; 
                    // Area Where Button Stuff Goes
                    System.Console.WriteLine("Button Pressed!");
                },
                5f, 
                Resources.shopbutton,
                new Vector2(0.125f, 0.125f),
                () =>
                {
                    return PlayerControl.LocalPlayer.Data != null && !PlayerControl.LocalPlayer.Data.IsDead && !PlayerControl.LocalPlayer.Data.IsImpostor && AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Started;
                },
                __instance
            );
        }
    }
}