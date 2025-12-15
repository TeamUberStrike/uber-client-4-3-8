using System.Collections.Generic;
using UnityEngine;

public class PanelManager : MonoSingleton<PanelManager>
{
    private IDictionary<PanelType, IPanelGui> _allPanels;

    public LoginPanelGUI LoginPanel
    {
        get { return _allPanels[PanelType.Login] as LoginPanelGUI; }
    }

    private void Start()
    {
        _allPanels = new Dictionary<PanelType, IPanelGui>()
        {
            { PanelType.Login, GetComponent<LoginPanelGUI>() },
            { PanelType.Signup, GetComponent<SignupPanelGUI>() },
            { PanelType.CompleteAccount, GetComponent<CompleteAccountPanelGUI>() },
            { PanelType.Options, GetComponent<OptionsPanelGUI>() },
            { PanelType.Help, GetComponent<HelpPanelGUI>() },
            { PanelType.CreateGame, GetComponent<CreateGamePanelGUI>() },
            { PanelType.ReportPlayer, GetComponent<ReportPlayerPanelGUI>() },
            { PanelType.Moderation, GetComponent<ModerationPanelGUI>() },
            { PanelType.SendMessage, GetComponent<SendMessagePanelGUI>() },
            { PanelType.FriendRequest, GetComponent<FriendRequestPanelGUI>() },
            { PanelType.ClanRequest, GetComponent<InviteToClanPanelGUI>() },
            { PanelType.BuyItem, GetComponent<BuyPanelGUI>() },
            { PanelType.NameChange, GetComponent<NameChangePanelGUI>() },
        };

        foreach (var p in _allPanels.Values)
        {
            MonoBehaviour mono = p as MonoBehaviour;
            if (mono)
                mono.enabled = false;
        }
    }

    private void OnGUI()
    {
        IsAnyPanelOpen = false;
        foreach (var p in _allPanels.Values)
        {
            if (p.IsEnabled)
            {
                IsAnyPanelOpen = true;
                break;
            }
        }

        if (Event.current.type == EventType.Layout)
        {
            if (IsAnyPanelOpen)
            {
                GuiLockController.EnableLock(GuiDepth.Panel);
            }
            else
            {
                GuiLockController.ReleaseLock(GuiDepth.Panel);
                enabled = false;
            }

            if (_wasAnyPanelOpen != IsAnyPanelOpen)
            {
                if (_wasAnyPanelOpen)
                    SfxManager.Play2dAudioClip(SoundEffectType.UIClosePanel);
                else
                    SfxManager.Play2dAudioClip(SoundEffectType.UIOpenPanel);

                _wasAnyPanelOpen = !_wasAnyPanelOpen;
            }
        }
    }

    private static bool _wasAnyPanelOpen = false;

    public static bool IsAnyPanelOpen { get; private set; }

    public bool IsPanelOpen(PanelType panel)
    {
        return _allPanels[panel].IsEnabled;
    }

    public void CloseAllPanels(PanelType except = PanelType.None)
    {
        foreach (var p in _allPanels.Values)
        {
            if (p.IsEnabled)
                p.Hide();
        }
    }

    public IPanelGui OpenPanel(PanelType panel)
    {
        foreach (var p in _allPanels)
        {
            if (panel == p.Key)
            {
                if (!p.Value.IsEnabled)
                    p.Value.Show();
            }
            else
            {
                if (p.Value.IsEnabled)
                    p.Value.Hide();
            }
        }

        enabled = true;

        return _allPanels[panel];
    }

    public void ClosePanel(PanelType panel)
    {
        if (_allPanels.ContainsKey(panel) && _allPanels[panel].IsEnabled)
            _allPanels[panel].Hide();
    }
}