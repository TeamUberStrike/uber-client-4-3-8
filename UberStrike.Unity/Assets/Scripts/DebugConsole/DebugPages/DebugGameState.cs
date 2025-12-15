using UberStrike.Realtime.Common;
using UnityEngine;

public class DebugGameState : IDebugPage
{
    Vector2 v1;
    Vector2 v2;

    public string Title
    {
        get { return "Game"; }
    }

    // Use this for initialization
    public void Draw()
    {
        if (GameState.CurrentGame != null)
        {
            v1 = GUILayout.BeginScrollView(v1);
            {
                GUILayout.Label("Type:" + GameState.CurrentGame.GetType().ToString());
                GUILayout.Label("Room:" + GameState.CurrentGame.GameData.RoomID.ToString());
                GUILayout.Label("IsGameStarted:" + GameState.CurrentGame.IsGameStarted.ToString());
                GUILayout.Label("IsRoundRunning:" + GameState.CurrentGame.IsMatchRunning.ToString());
                GUILayout.Label("GameTime:" + GameState.CurrentGame.GameTime.ToString("N2"));
                //if (Application.isEditor)
                {
                    GUILayout.Label("CameraState:" + GameState.LocalPlayer.CurrentCameraControl.ToString());
                    GUILayout.Label("IsHudEnabled:" + HudController.Instance.enabled.ToString());
                    GUILayout.Label("HudDrawFlags:" + HudController.Instance.DrawFlagString);
                    GUILayout.Label("IsGamePaused:" + GameState.LocalPlayer.IsGamePaused.ToString());
                    GUILayout.Label("IsInputEnabled:" + InputManager.Instance.IsInputEnabled.ToString());
                }
                GUILayout.Label("PlayerSpectator:" + PlayerSpectatorControl.Instance.IsEnabled.ToString());
                GUILayout.Label("MyPlayerID:" + GameState.CurrentGame.MyActorId.ToString());
            }
            GUILayout.EndScrollView();

            v2 = GUILayout.BeginScrollView(v2, GUILayout.MinHeight(200));
            {
                GUILayout.Label("Players");
                GUILayout.BeginHorizontal();
                {
                    foreach (UberStrike.Realtime.Common.CharacterInfo p in GameState.CurrentGame.Players.Values)
                        GUILayout.Label(p.ToString());
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }
    }
}
