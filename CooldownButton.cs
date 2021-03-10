using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Reactor.Extensions;
using Reactor.Unstrip;
using UnityEngine;
using HarmonyLib;



/*HOW TO USE: (reactor is recommended)
1. Copy the code in this file (not including this comment) to CooldownButton.cs
2. Get an image for your button (150 x 150 pixels is recommended) and add it to the VS solution. Make sure the 'Build Action' of the image in VS is 'Embedded resource'
3. Make a button patch. This one will make a button in the bottom left of the screen that prints 'PRESS' on press, everyone can press it and has a cooldown of 5 seconds.
```*/
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
                
                System.Console.WriteLine("Button Pressed!");
            },
            5f,
            "GamerMod.Resources.BatmanButton.png",
            0.25f,
            new Vector2(0.125f, 0.125f),
            CooldownButton.Category.Everyone,
            __instance
        );
    }
}
/*
4. Then, if you haven't already, add this patch to your plugin file: (only add this once, or else the button may bug out)
```*/
[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class CooldownButtonUpdatePatch
{
    public static void Postfix(HudManager __instance)
    {
        CooldownButton.HudUpdate();
    }
}

/*
If you've done everything correctly, a button should appear when going into a game/freeplay!
Credits to https://gist.github.com/gabriel-nsiqueira/827dea0a1cdc2210db6f9a045ec4ce0a for the actual code!! I only added some minor stuff.
```*/



// In HudManager.Start, Initialize the class
// HudManager.Update, call CooldownButton.HudUpdate
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
    public Category category;
    private string ResourceName;
    private Action OnClick;
    private Action OnEffectEnd;
    private HudManager hudManager;
    private float pixelsPerUnit;
    private bool canUse;

    public CooldownButton(Action OnClick, float Cooldown, string ImageEmbededResourcePath, float PixelsPerUnit, Vector2 PositionOffset, Category category, HudManager hudManager, float EffectDuration, Action OnEffectEnd)
    {
        this.hudManager = hudManager;
        this.OnClick = OnClick;
        this.OnEffectEnd = OnEffectEnd;
        this.PositionOffset = PositionOffset;
        this.EffectDuration = EffectDuration;
        this.category = category;
        pixelsPerUnit = PixelsPerUnit;
        MaxTimer = Cooldown;
        Timer = MaxTimer;
        ResourceName = ImageEmbededResourcePath;
        hasEffectDuration = true;
        isEffectActive = false;
        buttons.Add(this);
        Start();
    }

    public CooldownButton(Action OnClick, float Cooldown, string ImageEmbededResourcePath, float pixelsPerUnit, Vector2 PositionOffset, Category category, HudManager hudManager)
    {
        this.hudManager = hudManager;
        this.OnClick = OnClick;
        this.pixelsPerUnit = pixelsPerUnit;
        this.PositionOffset = PositionOffset;
        this.category = category;
        MaxTimer = Cooldown;
        Timer = MaxTimer;
        ResourceName = ImageEmbededResourcePath;
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
        Assembly assembly = Assembly.GetExecutingAssembly();
        Stream myStream = assembly.GetManifestResourceStream(ResourceName);
        byte[] buttonTexture = Reactor.Extensions.Extensions.ReadFully(myStream);
        ImageConversion.LoadImage(tex, buttonTexture, false);
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
        switch (category)
        {
            case Category.Everyone:
                {
                    canUse = true;
                    break;
                }
            case Category.OnlyCrewmate:
                {
                    canUse = !PlayerControl.LocalPlayer.Data.IsImpostor;
                    break;
                }
            case Category.OnlyImpostor:
                {
                    canUse = PlayerControl.LocalPlayer.Data.IsImpostor;
                    break;
                }
        }
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
    public enum Category
    {
        Everyone,
        OnlyCrewmate,
        OnlyImpostor
    }
}