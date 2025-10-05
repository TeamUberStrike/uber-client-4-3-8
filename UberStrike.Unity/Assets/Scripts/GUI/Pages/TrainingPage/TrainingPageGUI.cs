using UberStrike.Core.Types;
using UberStrike.Core.ViewModel;
using UnityEngine;

public class TrainingPageGUI : MonoBehaviour
{
    #region Fields
    private UberstrikeMap _selectedMap = null;
    private Vector2 _mapScroll;
    #endregion

    private void OnGUI()
    {
        GUI.depth = (int)GuiDepth.Page;
        GUI.skin = BlueStonez.Skin;
        GUI.Box(new Rect(0, 56, Screen.width, Screen.height - 56), string.Empty, BlueStonez.box_grey31);

        //Stats GUI Panel
        GUI.BeginGroup(new Rect((Screen.width - 330) * 0.5f, ((Screen.height + 56) - 410) * 0.5f, 330, 410), string.Empty, BlueStonez.window);
        {
            GUI.Label(new Rect(10, 20, 300, 48), LocalizedStrings.TrainingCaps, BlueStonez.label_interparkbold_48pt);
            GUI.Label(new Rect(30, 50, 270, 120), LocalizedStrings.TrainingModeDesc, BlueStonez.label_interparkbold_13pt);

            // map list
            GUI.Box(new Rect(12, 160, 300, 20), string.Empty, BlueStonez.box_grey50);
            GUI.Label(new Rect(16, 160, 120, 20), LocalizedStrings.ChooseAMap, BlueStonez.label_interparkbold_18pt_left);

            int totalcount = LevelManager.Instance.Count;
            GUI.Box(new Rect(12, 179, 300, 176), string.Empty, BlueStonez.window);
            _mapScroll = GUI.BeginScrollView(new Rect(0, 179, 312, 176), _mapScroll, new Rect(0, 0, 100, 80 * Mathf.CeilToInt(totalcount * 0.5f)));
            {
                Vector2 s = new Vector2(283, 80);
                int i = 0;
                foreach (var map in LevelManager.Instance.AllMaps)
                {
                    if (!map.IsEnabled) continue;

                    int row = i / 2;
                    int col = i % 2;

                    Rect mapRect = new Rect(13, row * s.Height(), s.Width() * 0.5f, s.Height());
                    if (col == 1) mapRect.x += s.Width() * 0.5f;

                    if (_selectedMap == null)
                        _selectedMap = map;

                    bool isSelected = (_selectedMap == map);

                    //map icon
                    GUI.BeginGroup(mapRect);
                    {
                        if (isSelected) GUI.Box(mapRect.FullExtends(), string.Empty, BlueStonez.box_grey50);
                        //else GUI.color = new Color(1, 1, 1, 0.5f);

                        map.Icon.Draw(mapRect.CenterHorizontally(2, 100, 64));
                        //GUI.color = Color.white;

                        Vector2 size = BlueStonez.label_interparkbold_11pt.CalcSize(new GUIContent(map.Name));
                        GUI.Label(mapRect.CenterHorizontally(mapRect.height - size.y, size.x, size.y), map.Name, BlueStonez.label_interparkbold_11pt);
                    }
                    GUI.EndGroup();

                    //button overlay
                    if (GUI.Button(mapRect, string.Empty, BlueStonez.dropdown_list))
                    {
                        _selectedMap = map;

                        if (MouseInput.Instance.DoubleClick(mapRect))
                        {
                            GameLoader.Instance.CreateGame(_selectedMap, "Training");
                        }
                    }
                    GUI.enabled = true;
                    i++;
                }
            }
            GUI.EndScrollView();

            if (GUITools.Button(new Rect(190, 370, 120, 32), new GUIContent(LocalizedStrings.BackCaps), BlueStonez.button))
                MenuPageManager.Instance.LoadPage(PageType.Home);

            GUI.enabled = _selectedMap != null;

            if (GUITools.Button(new Rect(190 - 125, 370, 120, 32), new GUIContent(LocalizedStrings.OkCaps), BlueStonez.button_green))
            {
                GameLoader.Instance.CreateGame(_selectedMap, "Training");
            }
        }
        GUI.EndGroup();

        GUI.enabled = true;
    }
}