using System;
using System.Collections.Generic;
using UberStrike.Realtime.Common;
using UnityEngine;
using Cmune.Util;

class HudStyleUtility : Singleton<HudStyleUtility>
{
    public Color TeamTextColor { get; private set; }
    public Color TeamGlowColor { get; private set; }

    public void SetTeamStyle(MeshGUIText meshText3D)
    {
        SetDefaultStyle(meshText3D);
        meshText3D.BitmapMeshText.ShadowColor = TeamTextColor;
    }

    public void SetDefaultStyle(MeshGUIText meshText3D)
    {
        meshText3D.Color = Color.white;
        meshText3D.BitmapMeshText.ShadowColor = DEFAULT_BLUE_COLOR;
        meshText3D.BitmapMeshText.AlphaMin = 0.45f;
        meshText3D.BitmapMeshText.AlphaMax = 0.62f;
        meshText3D.BitmapMeshText.ShadowAlphaMin = 0.20f;
        meshText3D.BitmapMeshText.ShadowAlphaMax = 0.45f;
        meshText3D.BitmapMeshText.ShadowOffsetU = 0.0f;
        meshText3D.BitmapMeshText.ShadowOffsetV = 0.0f;
    }

    public void SetSamllTextStyle(MeshGUIText meshText3D)
    {
        meshText3D.Color = Color.white;
        meshText3D.BitmapMeshText.ShadowColor = DEFAULT_BLUE_COLOR;
        meshText3D.BitmapMeshText.AlphaMin = 0.40f;
        meshText3D.BitmapMeshText.AlphaMax = 0.62f;
        meshText3D.BitmapMeshText.ShadowAlphaMin = 0.20f;
        meshText3D.BitmapMeshText.ShadowAlphaMax = 0.45f;
        meshText3D.BitmapMeshText.ShadowOffsetU = 0.0f;
        meshText3D.BitmapMeshText.ShadowOffsetV = 0.0f;
    }

    public void SetBlueStyle(MeshGUIText meshText3D)
    {
        SetDefaultStyle(meshText3D);
    }

    public void SetRedStyle(MeshGUIText meshText3D)
    {
        SetDefaultStyle(meshText3D);
        meshText3D.BitmapMeshText.ShadowColor = DEFAULT_RED_COLOR;
    }

    public void SetNoShadowStyle(MeshGUIText meshText3D)
    {
        meshText3D.Color = Color.white;
        meshText3D.BitmapMeshText.ShadowColor = new Color(1.0f, 1.0f, 1.0f, 0.0f);
        meshText3D.BitmapMeshText.AlphaMin = 0.18f;
        meshText3D.BitmapMeshText.AlphaMax = 0.62f;
        meshText3D.BitmapMeshText.ShadowAlphaMin = 0.18f;
        meshText3D.BitmapMeshText.ShadowAlphaMax = 0.39f;
        meshText3D.BitmapMeshText.ShadowOffsetU = 0.0f;
        meshText3D.BitmapMeshText.ShadowOffsetV = 0.0f;
    }

    public void SetBlackStyle(MeshGUIText meshText3D)
    {
        meshText3D.Color = Color.white;
        meshText3D.BitmapMeshText.ShadowColor = Color.black;
        meshText3D.BitmapMeshText.AlphaMin = 0.45f;
        meshText3D.BitmapMeshText.AlphaMax = 0.62f;
        meshText3D.BitmapMeshText.ShadowAlphaMin = 0.20f;
        meshText3D.BitmapMeshText.ShadowAlphaMax = 0.45f;
        meshText3D.BitmapMeshText.ShadowOffsetU = 0.0f;
        meshText3D.BitmapMeshText.ShadowOffsetV = 0.0f;
    }

    public void OnTeamChange(OnSetPlayerTeamEvent ev)
    {
        TeamTextColor = Color.white;
        switch (ev.TeamId)
        {
            case TeamID.BLUE:
            case TeamID.NONE:
                TeamTextColor = DEFAULT_BLUE_COLOR;
                TeamGlowColor = GLOW_BLUR_BLUE_COLOR;
                break;
            case TeamID.RED:
                TeamTextColor = DEFAULT_RED_COLOR;
                TeamGlowColor = GLOW_BLUR_RED_COLOR;
                break;
        }
    }

    public void ResetOverlayBoxTransform(Sprite2DGUI boxOverlay, Rect rect, Vector2 scaleFactor)
    {
        float overlaySizeX = rect.width * scaleFactor.x;
        float overlaySizeY = rect.height * scaleFactor.y;
        Vector2 overlayScale2 = new Vector2(overlaySizeX / boxOverlay.GUIBounds.x,
            overlaySizeY / boxOverlay.GUIBounds.y);

        boxOverlay.Scale = overlayScale2;
        boxOverlay.Position = new Vector2(rect.x - overlaySizeX / 2, rect.y - overlaySizeY / 2);
    }

    private HudStyleUtility()
    {
        CmuneEventHandler.AddListener<OnSetPlayerTeamEvent>(OnTeamChange);
    }

    public static Color DEFAULT_BLUE_COLOR = new Color(48.0f / 255.0f, 147.0f / 255.0f, 185.0f / 255.0f);
    public static Color DEFAULT_RED_COLOR = new Color(171.0f / 255.0f, 46.0f / 255.0f, 35.0f / 255.0f);
    public static float BLUR_WIDTH_SCALE_FACTOR = 3.0f;
    public static float BLUR_HEIGHT_SCALE_FACTOR = 128.0f / 40.0f;
    public static float XP_BAR_WIDTH_PROPORTION_IN_SCREEN = 0.15f;
    public static float ACRONYM_TEXT_SCALE = 0.25f;
    public static float BIGGER_DIGITS_SCALE = 1.0f;
    public static float SMALLER_DIGITS_SCALE = 0.7f;
    public static float GAP_BETWEEN_TEXT = 3.0f;
    public static Color GLOW_BLUR_BLUE_COLOR = new Color(0.0f, 159.0f / 255, 1.0f);
    public static Color GLOW_BLUR_RED_COLOR = new Color((float)0xb8 / 0xff, (float)0x32 / 0xff, (float)0x2b / 0xff);
}
