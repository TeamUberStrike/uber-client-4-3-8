using Cmune.Util;
using UberStrike.Realtime.Common;
using UnityEngine;

public class DebugPlayerManager : IDebugPage
{
    public string Title
    {
        get { return "Player"; }
    }

    // Use this for initialization
    public void Draw()
    {
        GUILayout.Label("IsInputEnabled: " + InputManager.Instance.IsInputEnabled.ToString());
        //GUILayout.Label("IsMouseLookEnabled:" + PlayerManager.IsMouseLookEnabled.ToString());
        GUILayout.Label("lockCursor:" + Screen.lockCursor.ToString());
        GUILayout.Label("IsMouseLockStateConsistent: " + GameState.LocalPlayer.IsMouseLockStateConsistent.ToString());
        GUILayout.Label("IsShootingEnabled: " + GameState.LocalPlayer.IsShootingEnabled.ToString());
        GUILayout.Label("IsWalkingEnabled: " + GameState.LocalPlayer.IsWalkingEnabled.ToString());
        //GUILayout.Label("MoveController:" + PlayerManager.MoveController.functionDebug);
        GUILayout.Label("IsWeaponControlEnabled: " + WeaponController.Instance.IsEnabled);
        GUILayout.Label("Players: " + (GameState.HasCurrentGame ? GameState.CurrentGame.Players.Count : 0).ToString());

        if (GameState.HasCurrentGame)
        {
            v1 = GUILayout.BeginScrollView(v1, GUILayout.MinHeight(200));
            {
                GUILayout.BeginHorizontal();
                foreach (CharacterInfo p in GameState.CurrentGame.Players.Values)
                {
                    GUILayout.BeginVertical();
                    GUILayout.Label(string.Format("{0} {1}/{2} {3} ", p.PlayerName, p.ActorId, p.Cmid, p.IsLoggedIn));
                    GUILayout.Label(string.Format("{0} {1} - {2}", p.CurrentWeaponSlot, p.CurrentWeaponID, CmunePrint.Values(p.Weapons.ItemIDs)));
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }
    }
    private Vector2 v1;
}
