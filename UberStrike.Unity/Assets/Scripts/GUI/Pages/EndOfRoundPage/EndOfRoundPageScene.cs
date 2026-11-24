using Cmune.Util;
using UnityEngine;

public class EndOfRoundPageScene : PageScene
{
    public override PageType PageType
    {
        get { return PageType.EndOfMatch; }
    }

    protected override void OnLoad()
    {
        CmuneEventHandler.AddListener<TeamGameEndEvent>(OnTeamGameEnd);
        CmuneEventHandler.AddListener<EndOfMatchCountdownEvent>(OnEndOfMatchCountdown);
    }

    protected override void OnUnload()
    {
        _scoreGroup.Hide();

        GameState.LocalDecorator.SetLayers(UberstrikeLayer.LocalPlayer);
        CmuneEventHandler.RemoveListener<TeamGameEndEvent>(OnTeamGameEnd);
        CmuneEventHandler.RemoveListener<EndOfMatchCountdownEvent>(OnEndOfMatchCountdown);
    }

    private void OnGUI()
    {
        // Not mobile-menu-scaled: this is an in-game results overlay whose MeshGUIText splats are
        // drawn in screen space and do not respect GUI.matrix, so a scale would desync them.
        GUI.depth = (int)GuiDepth.Hud;

        if (GameState.HasCurrentGame)
        {
            float x = Screen.height * 0.2f;
            Rect pixelRect = GameState.CurrentSpace.Camera.pixelRect;
            // Top of the score boxes in screen pixels. On mobile the ribbon is drawn at the menu scale, so they
            // start 30 units below its real bottom edge instead of under it.
            float top = MobileMenuScale.Active ? (GlobalUIRibbon.Instance.GetHeight() + 30) * MobileMenuScale.Scale : GlobalUIRibbon.HEIGHT + 30;
            if (GameState.CurrentGameMode == GameMode.TeamDeathMatch ||
                GameState.CurrentGameMode == GameMode.TeamElimination)
            {
                Rect rect = new Rect((pixelRect.width - x * 2 - 40) / 2, top, x * 2 + 40, x);

                GUI.BeginGroup(rect);
                {
                    GUI.Label(new Rect(0, 0, x, x), GUIContent.none, StormFront.BlueBox);
                    GUI.Label(new Rect((rect.width - x), 0, x, x), GUIContent.none, StormFront.RedBox);

                    if (_redTeamSplats != null)
                    {
                        _redTeamSplats.Position = new Vector2(Screen.width / 2 + (rect.width - x) / 2, rect.y + x / 2 - rect.height * 0.25f);
                        _redTeamSplats.Draw();
                    }

                    if (_blueTeamSplats != null)
                    {
                        _blueTeamSplats.Position = new Vector2(Screen.width / 2 - (rect.width - x) / 2, rect.y + x / 2 - rect.height * 0.25f);
                        _blueTeamSplats.Draw();
                    }

                    if (_redTeamText != null)
                    {
                        _redTeamText.Position = new Vector2(Screen.width / 2 + (rect.width - x) / 2, rect.y + x / 2 + rect.height * 0.3f);
                        _redTeamText.Draw();
                    }

                    if (_blueTeamText != null)
                    {
                        _blueTeamText.Position = new Vector2(Screen.width / 2 - (rect.width - x) / 2, rect.y + x / 2 + rect.height * 0.3f);
                        _blueTeamText.Draw();
                    }
                }
                GUI.EndGroup();
            }
            // Scale the READY toggle like the other menu buttons; the MeshGUI score splats stay
            // in raw screen space (they ignore GUI.matrix).
            Matrix4x4 readyScale = MobileMenuScale.Begin();
            Rect readyRect = new Rect(MobileMenuScale.ScreenToMenu(new Vector2((pixelRect.width - x * 2 - 60) / 2, top + x + 10)),
                new Vector2((x * 2 + 60) / MobileMenuScale.Scale, 50f));
            DrawReadyButton(readyRect);
            MobileMenuScale.End(readyScale);
        }
    }

    private void OnTeamGameEnd(TeamGameEndEvent ev)
    {
        if (_redTeamText == null)
        {
            _redTeamText = new MeshGUIText(LocalizedStrings.RedCaps, HudAssets.Instance.InterparkBitmapFont, TextAnchor.MiddleCenter);
            HudStyleUtility.Instance.SetRedStyle(_redTeamText);
            _redTeamText.Scale = new Vector2(0.5f, 0.5f);
            _scoreGroup.Group.Add(_redTeamText);
        }

        if (_blueTeamText == null)
        {
            _blueTeamText = new MeshGUIText(LocalizedStrings.BlueCaps, HudAssets.Instance.InterparkBitmapFont, TextAnchor.MiddleCenter);
            HudStyleUtility.Instance.SetBlueStyle(_blueTeamText);
            _blueTeamText.Scale = new Vector2(0.5f, 0.5f);
            _scoreGroup.Group.Add(_blueTeamText);
        }

        if (_redTeamSplats == null)
        {
            _redTeamSplats = new MeshGUIText(string.Empty, HudAssets.Instance.InterparkBitmapFont, TextAnchor.MiddleCenter);
            HudStyleUtility.Instance.SetRedStyle(_redTeamSplats);
            _redTeamSplats.Scale = new Vector2(0.8f, 0.8f);
            _scoreGroup.Group.Add(_redTeamSplats);
        }

        if (_blueTeamSplats == null)
        {
            _blueTeamSplats = new MeshGUIText(string.Empty, HudAssets.Instance.InterparkBitmapFont, TextAnchor.MiddleCenter);
            HudStyleUtility.Instance.SetBlueStyle(_blueTeamSplats);
            _blueTeamSplats.Scale = new Vector2(0.8f, 0.8f);
            _scoreGroup.Group.Add(_blueTeamSplats);
        }

        _redTeamSplats.Text = ev.RedTeamSplats.ToString();
        _blueTeamSplats.Text = ev.BlueTeamSplats.ToString();

        _scoreGroup.Show();
    }

    private void DrawReadyButton(Rect rect)
    {
        if (GameState.HasCurrentGame)
        {
            GUI.BeginGroup(rect);
            {
                string caption = string.Format("{0} {1}/{2}", LocalizedStrings.ReadyCaps, GameState.CurrentGame.PlayerReadyForNextRound, GameState.CurrentGame.Players.Count);

                GUITools.PushGUIState();
                GUI.enabled = !GameState.IsReadyForNextGame;

                if (_readyButtonStyle == null)
                {
                    _readyButtonStyle = new GUIStyle(StormFront.ButtonGray);
                    _readyButtonStyle.normal.background = StormFront.ButtonGray.normal.background;
                    _readyButtonStyle.hover.background = StormFront.ButtonGray.normal.background;
                    _readyButtonStyle.active.background = StormFront.ButtonGray.normal.background;
                    _readyButtonStyle.focused.background = StormFront.ButtonGray.normal.background;
                    _readyButtonStyle.onNormal.background = StormFront.ButtonGray.normal.background;
                    _readyButtonStyle.onHover.background = StormFront.ButtonGray.normal.background;
                    _readyButtonStyle.onActive.background = StormFront.ButtonGray.normal.background;
                    _readyButtonStyle.onFocused.background = StormFront.ButtonGray.normal.background;
                }

                bool b = GUI.Toggle(new Rect(rect.width / 2 - 70, rect.height / 2 - 23, 140, 45), GameState.IsReadyForNextGame, caption, _readyButtonStyle);

                GUITools.PopGUIState();
                
                GUI.Label(new Rect(rect.width / 2 + 80, 0, 60, rect.height), _endOfMatchCountdown.ToString(), BlueStonez.label_interparkbold_48pt_left);

                if (b && !GameState.IsReadyForNextGame)
                {
                    GameState.IsReadyForNextGame = true;

                    GUITools.Clicked();

                    GameState.CurrentGame.SetReadyForNextMatch(true);

                    SfxManager.Play2dAudioClip(SoundEffectType.UIClickReady);
                }
                else if (!b && GameState.IsReadyForNextGame)
                {
                    SfxManager.Play2dAudioClip(SoundEffectType.UIClickUnready);
                }
            }
            GUI.EndGroup();
        }
    }

    private void OnEndOfMatchCountdown(EndOfMatchCountdownEvent ev)
    {
        _endOfMatchCountdown = ev.Countdown;
    }

    private int _endOfMatchCountdown;
    private GUIStyle _readyButtonStyle;
    private MeshGUIText _redTeamText;
    private MeshGUIText _blueTeamText;
    private MeshGUIText _redTeamSplats;
    private MeshGUIText _blueTeamSplats;
    private Animatable2DGroup _scoreGroup = new Animatable2DGroup();
}