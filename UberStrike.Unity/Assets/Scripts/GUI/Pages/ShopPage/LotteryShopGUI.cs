using System;
using Cmune.DataCenter.Common.Entities;
using UnityEngine;
using System.Collections.Generic;

public class LotteryShopGUI
{
    private int scrollHeight = 0;
    private Vector2 scroll = Vector2.zero;
    private Dictionary<int, float> _alpha = new Dictionary<int, float>();

    public void Draw(Rect position)
    {
        float contentHeight = Mathf.Max(position.height, scrollHeight);
        scroll = GUITools.BeginScrollView(position, scroll, new Rect(0, 0, position.width - 17, contentHeight), false, true);
        {
            int yOffset = 4;
            foreach (BundleCategoryType bundle in Enum.GetValues(typeof(BundleCategoryType)))
            {
                if (bundle == BundleCategoryType.None) continue;
                int counter = 0;

                List<LotteryShopItem> items;
                if (LotteryManager.Instance.TryGetBundle(bundle, out items))
                {
                    GUI.Label(new Rect(4, yOffset + 4, position.width - 20, 20), bundle.ToString(), BlueStonez.label_interparkbold_18pt_left);
                    yOffset += 30;

                    foreach (var box in items)
                    {
                        int xOffset = (counter % 2 == 1) ? 187 : 0; // If it's an odd number, draw in second column
                        DrawLotterySlot(new Rect(xOffset, yOffset, 188, 95), box);
                        yOffset += (counter % 2 == 1) ? 94 : 0; // If it's the second column, add a new row
                        counter++;
                    }
                }

                if (counter > 0)
                {
                    if (counter % 2 == 1) yOffset += 94; // If we end on an odd (left) column, add a new row
                    GUI.Label(new Rect(4, yOffset, position.width - 8, 1), GUIContent.none, BlueStonez.horizontal_line_grey95);
                    yOffset += 4;
                }
            }
            scrollHeight = yOffset;
        }
        GUITools.EndScrollView();
    }

    private void DrawLotterySlot(Rect position, LotteryShopItem item)
    {
        bool selected = position.Contains(Event.current.mousePosition);
        if (!_alpha.ContainsKey(item.Id)) _alpha[item.Id] = 0;
        _alpha[item.Id] = Mathf.Lerp(_alpha[item.Id], selected ? 1 : 0, Time.deltaTime * (selected ? 3 : 10));

        GUI.BeginGroup(position);
        {
            GUI.color = new Color(1, 1, 1, _alpha[item.Id]);
            if (GUI.Button(new Rect(2, 2, position.width - 4, 79), GUIContent.none, BlueStonez.gray_background))
            {
                UseLotteryItem(item);
            }
            GUI.color = Color.white;

            item.Icon.Draw(new Rect(4, 4, 75, 75));
            GUI.Label(new Rect(81, 0, position.width - 80, 44), item.Name, BlueStonez.label_interparkbold_13pt_left);

            // Buy button
            if (GUI.Button(new Rect(81, 51, position.width - 110, 20), item.Price.PriceTag(false, item.Description), BlueStonez.buttongold_medium))
            {
                UseLotteryItem(item);
            }
        }
        GUI.EndGroup();
    }

    private void UseLotteryItem(LotteryShopItem item)
    {
        item.Use();
        //BackgroundMusicPlayer.Instance.Stop();
        //LotteryAudioPlayer.Instance.Play();
    }
}