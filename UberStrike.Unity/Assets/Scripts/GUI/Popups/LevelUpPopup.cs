using System;
using Cmune.DataCenter.Common.Entities;
using UnityEngine;

public class LevelUpPopup : BaseEventPopup
{
    private const float _alpha = 1.0f;
    private int _level;
    private Action _callback = null;

    public LevelUpPopup(int level, Action callback)
    {
        _level = level;
        _callback = callback;

        ClickAnywhereToExit = true;

        SfxManager.Play2dAudioClip(SoundEffectType.GameLevelUp);
    }

    public override void OnHide()
    {
        if (_callback != null) _callback();
    }

    protected override void DrawGUI(Rect rect)
    {
        // Draw the level up image
        Rect levelUpTextureRect = new Rect((rect.width - UberstrikeIcons.LevelUp.width) * 0.5f, (rect.height - UberstrikeIcons.LevelUp.height) * 0.5f, UberstrikeIcons.LevelUp.width, UberstrikeIcons.LevelUp.height);
        GUI.DrawTexture(levelUpTextureRect, UberstrikeIcons.LevelUp);

        GUI.BeginGroup(new Rect(0, rect.height - (64 * Scale), rect.width, 64));
        {
            int leftOffset = Mathf.RoundToInt((rect.width - 300) * 0.5f);
            GUITools.LabelShadow(new Rect(leftOffset - 25, 16, 300, 32), LocalizedStrings.BangYouJustReachedLevelN, BlueStonez.label_interparkbold_18pt_left, Color.white.SetAlpha(_alpha));
            GUITools.LabelShadow(new Rect(leftOffset + 172, 16, 300, 32), LocalizedStrings.Level + " " + _level + ".", BlueStonez.label_interparkbold_18pt_left, Color.yellow.SetAlpha(_alpha));

            GUI.color = new Color(1, 1, 1, _alpha);
            if (ApplicationDataManager.Channel == ChannelType.WebFacebook)
            {
                GUI.DrawTexture(new Rect(leftOffset + 246, 16, 32, 32), UberstrikeIcons.Facebook);
                if (GUITools.Button(new Rect(leftOffset + 280, 16, 80, 32), new GUIContent(LocalizedStrings.Share), BlueStonez.button_green))
                {
                    ApplicationDataManager.Instance.PublishFBLevelUpStreamPost(_level.ToString());
                    PopupSystem.HideMessage(this);
                }
                if (GUITools.Button(new Rect(leftOffset + 370, 16, 80, 32), new GUIContent(LocalizedStrings.Cancel), BlueStonez.button))
                {
                    PopupSystem.HideMessage(this);
                }
            }
            else
            {
                if (GUITools.Button(new Rect(leftOffset + 246, 16, 100, 32), new GUIContent(LocalizedStrings.OkCaps), BlueStonez.button_green))
                {
                    PopupSystem.HideMessage(this);
                }
            }
            GUI.color = Color.white;
        }
        GUI.EndGroup();
    }
}