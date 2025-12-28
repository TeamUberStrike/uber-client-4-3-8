//using UnityEngine;
//using Cmune.Realtime.Common.Utils;
//using UberStrike.Realtime.Common;
//using System.Collections.Generic;

//public class PlayerDataModifier : MonoBehaviour
//{
//#if UNITY_EDITOR
//    private void OnGUI()
//    {
//        if (InputManager.AltPressed && InputManager.GetKeyDown(KeyCode.P))
//        {
//            _showPanel = !_showPanel;
//        }

//        if (!_showPanel) return;

//        GUI.BeginGroup(new Rect(5, 100, 300, 300), GUIContent.none, "window");
//        {
//            GUI.Label(new Rect(5, 25, 100, 20), "XP");
//            playerXp = GUI.TextField(new Rect(105, 25, 100, 20), playerXp);
//            GUI.Label(new Rect(210, 25, 100, 20), PlayerDataManager.PlayerExperience.ToString());

//            GUI.Label(new Rect(5, 50, 100, 20), "Level");
//            playerLevel = GUI.TextField(new Rect(105, 50, 100, 20), playerLevel);
//            GUI.Label(new Rect(210, 50, 100, 20), PlayerDataManager.PlayerLevel.ToString());

//            GUI.Label(new Rect(5, 75, 100, 20), "Earned Xp");
//            earnedXp = GUI.TextField(new Rect(105, 75, 100, 20), earnedXp);

//            if (GUI.Button(new Rect(0, 265, 120, 30), "Add Xp Event"))
//            {
//                PlayerDataManager.Instance.SetLevelAndXp(Level, Xp + EarnedXp);
//                GlobalUIRibbon.Instance.AddXPEvent(EarnedXp);
//            }
//            if (GUI.Button(new Rect(0, 235, 120, 30), "Add Points Event"))
//            {
//                PlayerDataManager.Instance.SetPointsAndCredits(PlayerDataManager.Points + EarnedXp, PlayerDataManager.Credits);
//                GlobalUIRibbon.Instance.AddPointsEvent(EarnedXp);
//            }
//            if (GUI.Button(new Rect(0, 205, 120, 30), "Add Credits Event"))
//            {
//                PlayerDataManager.Instance.SetPointsAndCredits(PlayerDataManager.Points, PlayerDataManager.Credits + EarnedXp);
//                GlobalUIRibbon.Instance.AddCreditsEvent(EarnedXp);
//            }

//            if (GUI.Button(new Rect(130, 265, 120, 30), "Deathmatch EOM"))
//            {
//                PlayerDataManager.Instance.SetLevelAndXp(Level, Xp + EarnedXp);

//                EndOfMatchGUI.Instance.Show();

//                EndOfMatchGUI.Instance.ShowEndOfDeathmatch(
//               new CmunePairList<string, int>(),
//               new CmunePairList<int, int>(),
//               new StatsCollection(),
//               new List<int>() { EarnedXp },
//               Level,
//               Xp);
//            }

//            if (GUI.Button(new Rect(250, 265, 45, 30), "Apply"))
//            {
//                PlayerDataManager.Instance.SetLevelAndXp(Level, Xp);
//            }
//        }
//        GUI.EndGroup();
//    }

//    public int EarnedXp
//    {
//        get
//        {
//            int xp;
//            if (!int.TryParse(earnedXp, out xp))
//            {
//                xp = 0;
//            }
//            return xp;
//        }
//    }

//    public int Xp
//    {
//        get
//        {
//            int xp;
//            if (!int.TryParse(playerXp, out xp))
//            {
//                xp = PlayerDataManager.PlayerExperience;
//            }
//            return xp;
//        }
//    }

//    public int Level
//    {
//        get
//        {
//            int level;
//            if (!int.TryParse(playerLevel, out level))
//            {
//                level = PlayerXpUtil.GetLevelForXp(Xp);
//            }
//            return level;
//        }
//    }

//    private string playerLevel = string.Empty;
//    private string playerXp = string.Empty;
//    private string earnedXp = string.Empty;
//    //private string playerPoints = string.Empty;
//    //private string playerCredits = string.Empty;

//    private bool _showPanel = false;
//#endif
//}
