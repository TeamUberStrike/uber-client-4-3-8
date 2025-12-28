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
        GUI.depth = (int)GuiDepth.Hud;

        if (GameState.HasCurrentGame)
        {
            float x = Screen.height * 0.2f;
            Rect pixelRect = GameState.CurrentSpace.Camera.pixelRect;
            if (GameState.CurrentGameMode == GameMode.TeamDeathMatch ||
                GameState.CurrentGameMode == GameMode.TeamElimination)
            {
                Rect rect = new Rect((pixelRect.width - x * 2 - 40) / 2, GlobalUIRibbon.HEIGHT + 30, x * 2 + 40, x);

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
            DrawReadyButton(new Rect((pixelRect.width - x * 2 - 60) / 2, GlobalUIRibbon.HEIGHT + x + 40, x * 2 + 60, 50));
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

                bool b = GUI.Toggle(new Rect(rect.width / 2 - 70, rect.height / 2 - 23, 140, 45), GameState.IsReadyForNextGame, caption, StormFront.ButtonGray);

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
    private MeshGUIText _redTeamText;
    private MeshGUIText _blueTeamText;
    private MeshGUIText _redTeamSplats;
    private MeshGUIText _blueTeamSplats;
    private Animatable2DGroup _scoreGroup = new Animatable2DGroup();
}