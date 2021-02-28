using HarmonyLib;
using Reactor.Extensions;
using Reactor.Unstrip;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Reactor.Button
{
    public delegate bool UseTester();
    public class CooldownButton
    {
        public static List<CooldownButton> buttons = new List<CooldownButton>();
        public KillButtonManager killButtonManager;
        private Color startColorButton = new Color(255, 255, 255);
        private Color startColorText = new Color(255, 255, 255);
        public Vector2 PositionOffset = Vector2.zero;
        public float MaxTimer = 0f;
        public float Timer = 0f;
        public float EffectDuration = 0f;
        public bool isEffectActive;
        public bool hasEffectDuration;
        public bool enabled = true;
        public UseTester useTester;
        private byte[] image;
        private Action OnClick;
        private Action OnEffectEnd;
        private HudManager hudManager;
        private bool canUse;

        public CooldownButton(Action OnClick, float Cooldown, byte[] image, Vector2 PositionOffset, UseTester useTester, HudManager hudManager, float EffectDuration, Action OnEffectEnd)
        {
            this.hudManager = hudManager;
            this.OnClick = OnClick;
            this.OnEffectEnd = OnEffectEnd;
            this.PositionOffset = PositionOffset;
            this.EffectDuration = EffectDuration;
            this.useTester = useTester;
            MaxTimer = Cooldown;
            Timer = MaxTimer;
            this.image = image;
            hasEffectDuration = true;
            isEffectActive = false;
            buttons.Add(this);
            Start();
        }

        public CooldownButton(Action OnClick, float Cooldown, byte[] image, Vector2 PositionOffset, UseTester useTester, HudManager hudManager)
        {
            this.hudManager = hudManager;
            this.OnClick = OnClick;
            this.PositionOffset = PositionOffset;
            this.useTester = useTester;
            MaxTimer = Cooldown;
            Timer = MaxTimer;
            this.image = image;
            hasEffectDuration = false;
            buttons.Add(this);
            Start();
        }

        private void Start()
        {
            killButtonManager = UnityEngine.Object.Instantiate(hudManager.KillButton, hudManager.transform);
            startColorButton = killButtonManager.renderer.color;
            startColorText = killButtonManager.TimerText.Color;
            killButtonManager.gameObject.SetActive(true);
            killButtonManager.renderer.enabled = true;
            Texture2D tex = GUIExtensions.CreateEmptyTexture();
            ImageConversion.LoadImage(tex, this.image, false);
            killButtonManager.renderer.sprite = GUIExtensions.CreateSprite(tex);
            PassiveButton button = killButtonManager.GetComponent<PassiveButton>();
            button.OnClick.RemoveAllListeners();
            button.OnClick.AddListener((UnityEngine.Events.UnityAction)listener);
            void listener()
            {
                if (Timer < 0f && canUse)
                {
                    killButtonManager.renderer.color = new Color(1f, 1f, 1f, 0.3f);
                    if (hasEffectDuration)
                    {
                        isEffectActive = true;
                        Timer = EffectDuration;
                        killButtonManager.TimerText.Color = new Color(0, 255, 0);
                    }
                    OnClick();
                }
            }
        }
        public bool CanUse()
        {
            if (PlayerControl.LocalPlayer.Data == null) return false;
            canUse = useTester();
            return true;
        }
        public static void HudUpdate()
        {
            buttons.RemoveAll(item => item.killButtonManager == null);
            for (int i = 0; i < buttons.Count; i++)
            {
                try
                {
                    if (buttons[i].CanUse())
                        buttons[i].Update();
                }
                catch (NullReferenceException)
                {
                    System.Console.WriteLine("[WARNING] NullReferenceException from HudUpdate().CanUse(), if theres only one warning its fine");
                }
            }
        }
        private void Update()
        {
            if (killButtonManager.transform.localPosition.x > 0f)
                killButtonManager.transform.localPosition = new Vector3((killButtonManager.transform.localPosition.x + 1.3f) * -1, killButtonManager.transform.localPosition.y, killButtonManager.transform.localPosition.z) + new Vector3(PositionOffset.x, PositionOffset.y);
            if (Timer < 0f)
            {
                killButtonManager.renderer.color = new Color(1f, 1f, 1f, 1f);
                if (isEffectActive)
                {
                    killButtonManager.TimerText.Color = startColorText;
                    Timer = MaxTimer;
                    isEffectActive = false;
                    OnEffectEnd();
                }
            }
            else
            {
                if (canUse)
                    if (PlayerControl.LocalPlayer.CanMove)
                        Timer -= Time.deltaTime;
                killButtonManager.renderer.color = new Color(1f, 1f, 1f, 0.3f);
            }
            killButtonManager.gameObject.SetActive(canUse);
            killButtonManager.renderer.enabled = canUse;
            if (canUse)
            {
                killButtonManager.renderer.material.SetFloat("_Desat", 0f);
                killButtonManager.SetCoolDown(Timer, MaxTimer);
            }
        }
        internal delegate bool d_LoadImage(IntPtr tex, IntPtr data, bool markNonReadable);
        internal static d_LoadImage iCall_LoadImage;
        public static bool LoadImage(Texture2D tex, byte[] data, bool markNonReadable)
        {
            if (iCall_LoadImage == null)
                iCall_LoadImage = UnhollowerBaseLib.IL2CPP.ResolveICall<d_LoadImage>("UnityEngine.ImageConversion::LoadImage");

            var il2cppArray = (UnhollowerBaseLib.Il2CppStructArray<byte>)data;

            return iCall_LoadImage.Invoke(tex.Pointer, il2cppArray.Pointer, markNonReadable);
        }
    }
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    public static class ButtonUpdatePatch
    {
        public static void Postfix(HudManager __instance)
        {
            CooldownButton.HudUpdate();
        }
    }
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    public static class ButtonResetPatch
    {
        public static void Postfix(MeetingHud __instance)
        {
            CooldownButton.buttons.RemoveAll(item => item.killButtonManager == null);
            for (int i = 0; i < CooldownButton.buttons.Count; i++)
            {
                try
                {
                    if (CooldownButton.buttons[i].CanUse())
                        CooldownButton.buttons[i].Timer = CooldownButton.buttons[i].MaxTimer;
                }
                catch (NullReferenceException)
                {
                    System.Console.WriteLine("[WARNING] NullReferenceException from ButtonResetPatch().CanUse(), if theres only one warning its fine");
                }
            }
        }
    }
}