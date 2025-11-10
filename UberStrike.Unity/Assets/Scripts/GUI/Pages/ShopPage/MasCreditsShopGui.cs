using System.Collections.Generic;
using Cmune.DataCenter.Common.Entities;
using UnityEngine;

public class MasCreditsShopGui
{
    private Vector2 packScroll = Vector2.zero;
    private int scrollHeight = 0;
    private Dictionary<int, float> _alpha = new Dictionary<int, float>();

    public void Draw(Rect position)
    {
        float contentHeight = Mathf.Max(position.height, scrollHeight);
        packScroll = GUI.BeginScrollView(position, packScroll, new Rect(0, 0, position.width - 17, contentHeight), false, true);
        {
            var creditPacks = MasBundleManager.Instance.GetBundlesInCategory(BundleCategoryType.None);

            if (creditPacks.Count == 0)
            {
                GUI.Label(new Rect(4, 4, position.width - 20, 24), "No credit packs are currently on sale.", BlueStonez.label_interparkbold_16pt);
            }
            else
            {
                int yOffset = 4;
                int counter = 0;

                GUI.Label(new Rect(4, yOffset + 4, position.width - 20, 20), "Credit Packs", BlueStonez.label_interparkbold_18pt_left);
                yOffset += 30;
                foreach (var pack in creditPacks)
                {
                    int xOffset = (counter % 2 == 1) ? 187 : 0; // If it's an odd number, draw in second column
                    DrawPackSlot(new Rect(xOffset, yOffset, 188, 95), pack);
                    yOffset += (counter % 2 == 1) ? 94 : 0; // If it's the second column, add a new row
                    counter++;
                }
                if (counter % 2 == 1) yOffset += 94; // If we end on an odd (left) column, add a new row
                GUI.Label(new Rect(4, yOffset, position.width - 8, 1), GUIContent.none, BlueStonez.horizontal_line_grey95);

                scrollHeight = yOffset;
            }
        }
        GUI.EndScrollView();
    }

    private void DrawItem(Rect position, IUnityItem baseItem)
    {
        GUI.BeginGroup(position);

        // Item Icon
        GUI.Label(new Rect(4, 4, 48, 48), baseItem.Icon, BlueStonez.item_slot_small);

        // Name
        string itemName = baseItem.ItemView.Name != null ? baseItem.ItemView.Name : "#####";
        GUI.Label(new Rect(54, 0, position.width - 60, 24), itemName, BlueStonez.label_interparkbold_13pt_left);

        // Description
        GUI.Label(new Rect(6, 60, position.width - 10, 53), string.IsNullOrEmpty(baseItem.ItemView.Description) ? "No description available." : baseItem.ItemView.Description, BlueStonez.label_itemdescription);

        // Grey Line
        GUI.Label(new Rect(4, position.height - 1, position.width - 4, 1), string.Empty, BlueStonez.horizontal_line_grey95);

        GUI.EndGroup();
    }

    private void DrawPackSlot(Rect position, MasBundleUnityView masBundleView)
    {
        int itemId = masBundleView.BundleView.Id;

        bool selected = position.Contains(Event.current.mousePosition);
        if (!_alpha.ContainsKey(itemId)) _alpha[itemId] = 0;
        _alpha[itemId] = Mathf.Lerp(_alpha[itemId], selected ? 1 : 0, Time.deltaTime * (selected ? 3 : 10));

        GUI.BeginGroup(position);
        {
            GUI.color = new Color(1, 1, 1, _alpha[itemId]);
            if (GUI.Button(new Rect(2, 2, position.width - 4, 79), GUIContent.none, BlueStonez.gray_background))
            {
                BuyBundle(masBundleView);
            }
            GUI.color = Color.white;

            masBundleView.Icon.Draw(new Rect(4, 4, 75, 75));
            GUI.Label(new Rect(81, 0, position.width - 80, 44), masBundleView.BundleView.Name, BlueStonez.label_interparkbold_13pt_left);

            // Buy button
            GUI.enabled = GUITools.SaveClickIn(1);

            if (GUI.Button(new Rect(81, 51, position.width - 110, 20), new GUIContent(masBundleView.CurrencySymbol + masBundleView.Price, "Buy the " + masBundleView.BundleView.Name + " pack."), BlueStonez.buttongold_medium))
            {
                BuyBundle(masBundleView);
            }
            GUI.enabled = true;
        }
        GUI.EndGroup();
    }

    private void BuyBundle(MasBundleUnityView masBundleView)
    {
        if (MasBundleManager.Instance.CanMakeMasPayments)
        {
            GUITools.Clicked();
            if (ScreenResolutionManager.IsFullScreen) ScreenResolutionManager.IsFullScreen = false;
            MasBundleManager.Instance.BuyStoreKitItem(masBundleView.BundleView.MacAppStoreUniqueId, masBundleView.BundleView.Id);
        }
        else
        {
            PopupSystem.ShowMessage(LocalizedStrings.Error, "Sorry, it appears you are unable to make Mac App Store payments at this time.", PopupSystem.AlertType.OK);
        }
    }
}