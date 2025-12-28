using Cmune.DataCenter.Common.Entities;
using UnityEngine;

public class TrainingPageGUI : MonoBehaviour
{
    #region Fields
    private UberstrikeMap _selectedMap = null;
    private Vector2 _mapScroll;
    #endregion

    public float PageWidth = 330;
    public float PageHeight = 410;
    public int MapsPerRow = 2;

    private void Start()
    {
        if (ApplicationDataManager.Channel == ChannelType.Android 
            || ApplicationDataManager.Channel == ChannelType.IPad 
            || ApplicationDataManager.Channel == ChannelType.IPhone)
        {
            PageWidth = 700;
            MapsPerRow = 5;
        }
    }

    private void OnGUI()
    {
        GUI.depth = (int)GuiDepth.Page;
        GUI.skin = BlueStonez.Skin;
        GUI.Box(new Rect(0, 56, Screen.width, Screen.height - 56), string.Empty, BlueStonez.box_grey31);

        //Stats GUI Panel
        GUI.BeginGroup(new Rect((Screen.width - PageWidth) * 0.5f, ((Screen.height + 56) - PageHeight) * 0.5f, PageWidth, PageHeight), string.Empty, BlueStonez.window);
        {
            GUI.Label(new Rect(10, 20, PageWidth - 30, 48), LocalizedStrings.TrainingCaps, BlueStonez.label_interparkbold_48pt);
            GUI.Label(new Rect(30, 50, PageWidth - 60, 120), LocalizedStrings.TrainingModeDesc, BlueStonez.label_interparkbold_13pt);

            // map list
            GUI.Box(new Rect(12, 160, PageWidth - 30, 20), string.Empty, BlueStonez.box_grey50);
            GUI.Label(new Rect(16, 160, 120, 20), LocalizedStrings.ChooseAMap, BlueStonez.label_interparkbold_18pt_left);

            int totalcount = LevelManager.Instance.Count;
            GUI.Box(new Rect(12, 179, PageWidth - 30, 176), string.Empty, BlueStonez.window);
            _mapScroll = GUITools.BeginScrollView(new Rect(0, 179, PageWidth - 18, 176), _mapScroll, new Rect(0, 0, 100, 80 * Mathf.CeilToInt(totalcount / MapsPerRow)));
            {
                Vector2 s = new Vector2((PageWidth - 47) / MapsPerRow, 80);
                int i = 0;
                foreach (var map in LevelManager.Instance.AllMaps)
                {
                    if (!map.IsEnabled) continue;

                    int row = i / MapsPerRow;
                    int col = i % MapsPerRow;

                    Rect mapRect = new Rect(13 + col * s.Width(), row * s.Height(), s.Width(), s.Height());

                    if (ApplicationDataManager.Channel != ChannelType.Android
                        && ApplicationDataManager.Channel != ChannelType.IPad
                        && ApplicationDataManager.Channel != ChannelType.IPhone)
                    {
                        if (_selectedMap == null)
                            _selectedMap = map;
                    }

                    bool isSelected = (_selectedMap == map);

                    //map icon
                    GUI.BeginGroup(mapRect);
                    {
                        if (isSelected) GUI.Box(mapRect.FullExtends(), string.Empty, BlueStonez.box_grey50);

                        map.Icon.Draw(mapRect.CenterHorizontally(2, 100, 64));

                        Vector2 size = BlueStonez.label_interparkbold_11pt.CalcSize(new GUIContent(map.Name));
                        GUI.Label(mapRect.CenterHorizontally(mapRect.height - size.y, size.x, size.y), map.Name, BlueStonez.label_interparkbold_11pt);
                    }
                    GUI.EndGroup();

                    //button overlay
                    if (GUI.Button(mapRect, string.Empty, BlueStonez.dropdown_list))
                    {
                        if (ApplicationDataManager.Channel == ChannelType.Android
                            || ApplicationDataManager.Channel == ChannelType.IPad
                            || ApplicationDataManager.Channel == ChannelType.IPhone)
                        {
                            GameLoader.Instance.CreateGame(map, "Training");
                        }
                        else
                        {
                            _selectedMap = map;

                            if (MouseInput.Instance.DoubleClick(mapRect))
                            {
                                GameLoader.Instance.CreateGame(_selectedMap, "Training");
                            }
                        }
                    }
                    GUI.enabled = true;
                    i++;
                }
            }
            GUITools.EndScrollView();

            if (GUITools.Button(new Rect(PageWidth - 140, 370, 120, 32), new GUIContent(LocalizedStrings.BackCaps), BlueStonez.button))
                MenuPageManager.Instance.LoadPage(PageType.Home);

            GUI.enabled = _selectedMap != null;

            if (ApplicationDataManager.Channel != ChannelType.Android
                && ApplicationDataManager.Channel != ChannelType.IPad
                && ApplicationDataManager.Channel != ChannelType.IPhone)
            {
                if (GUITools.Button(new Rect(PageWidth - 140 - 125, 370, 120, 32), new GUIContent(LocalizedStrings.OkCaps), BlueStonez.button_green))
                {
                    GameLoader.Instance.CreateGame(_selectedMap, "Training");
                }
            }
        }
        GUI.EndGroup();

        GUI.enabled = true;
    }
}