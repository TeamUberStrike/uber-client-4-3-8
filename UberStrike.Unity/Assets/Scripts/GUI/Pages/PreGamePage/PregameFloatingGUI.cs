using System.Collections.Generic;
using UnityEngine;

public class PregameFloatingGUI : MonoBehaviour
{
    // Size of the join area on mobile relative to the menu scale: at the full menu scale the 130-unit
    // join buttons are larger than they need to be.
    private const float MobileJoinAreaScale = 0.8f;

    private BaseJoinGameGUI _joinGameGUI;
    private MeshGUIText _gameModeText = null;

    private void OnEnable()
    {
        InitializeJoinGameGUI();
    }

    private void OnDisable()
    {
        if (_gameModeText != null)
        {
            _gameModeText.Hide();
        }
    }

    private void OnGUI()
    {
        GUI.depth = (int)GuiDepth.Hud;

        if (GameState.HasCurrentSpace)
        {
            // The join buttons are laid out in menu units, at MobileJoinAreaScale of the menu's size.
            // The area is the whole camera view (the part of the screen left of the panel), not the
            // safe area, so the buttons stay centred over the avatar. On PC menu units are screen
            // pixels and the extra scale does not apply.
            Matrix4x4 scaleMatrix = MobileMenuScale.Begin(MobileJoinAreaScale);

            Rect pixelRect = GameState.CurrentSpace.Camera.pixelRect;
            float viewLeft = MobileMenuScale.ScreenToMenu(new Vector2(pixelRect.xMin, 0f)).x;
            float viewRight = MobileMenuScale.ScreenToMenu(new Vector2(pixelRect.xMax, 0f)).x;
            Rect rect = new Rect(viewLeft + 30, 80, viewRight - viewLeft - 30 * 2, MobileMenuScale.VirtualHeight - 80);

            GUI.BeginGroup(rect, string.Empty);
            {
                DrawJoinArea(rect);
            }
            GUI.EndGroup();

            MobileMenuScale.End(scaleMatrix);
        }
    }

    private void InitializeJoinGameGUI()
    {
        if (GameState.HasCurrentGame)
        {
            switch (GameState.CurrentGameMode)
            {
                case GameMode.Training:
                    _joinGameGUI = new JoinTrainingGameGUI(GameState.CurrentGame as TrainingFpsMode);
                    break;

                case GameMode.DeathMatch:
                    _joinGameGUI = new JoinNonTeamGameGUI(GameState.CurrentGame as DeathMatchGameMode);
                    break;

                case GameMode.TeamDeathMatch:
                case GameMode.TeamElimination:
                    _joinGameGUI = new JoinTeamGameGUI(GameState.CurrentGame as TeamDeathMatchGameMode);
                    break;
            }

            string mode = string.Empty;

            switch (GameState.CurrentGameMode)
            {
                case GameMode.DeathMatch:
                    mode = LocalizedStrings.DeathMatch.ToUpper();
                    break;
                case GameMode.TeamDeathMatch:
                    mode = LocalizedStrings.TeamDeathMatch.ToUpper();
                    break;
                case GameMode.TeamElimination:
                    mode = LocalizedStrings.TeamElimination.ToUpper();
                    break;
            }

            if (_gameModeText != null)
            {
                _gameModeText.Text = mode;
                _gameModeText.Show();
            }
            else
            {
                _gameModeText = new MeshGUIText(mode, HudAssets.Instance.InterparkBitmapFont, TextAnchor.MiddleCenter);
                HudStyleUtility.Instance.SetBlackStyle(_gameModeText);
                _gameModeText.Scale = new Vector2(0.4f, 0.4f);
            }
        }
    }

    private void DrawJoinArea(Rect rect)
    {
        _gameModeText.Position = new Vector2(Screen.width / 2, 120);
        _gameModeText.Draw();

        if (_joinGameGUI != null)
        {
            _joinGameGUI.Draw(rect);
        }

#if UNITY_EDITOR
        if (GUI.Button(new Rect(0, rect.height / 2, 100, 30), "Test SpawnPoint"))
        {
            int count = SpawnPointManager.Instance.GetSpawnPointCount(GameMode.DeathMatch, UberStrike.Realtime.Common.TeamID.NONE);
            GameState.CurrentSpace.DefaultSpawnPoint = (GameState.CurrentSpace.DefaultSpawnPoint + 1) % count;

            GamePageUtil.SpawnLocalAvatar();
        }
#endif
    }
}