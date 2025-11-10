using System;
using System.Collections.Generic;
using Cmune.DataCenter.Common.Entities;
using Cmune.Util;
using UnityEngine;

class WeaponRecommendListGUI
{
    public WeaponRecommendListGUI(BuyingLocationType location)
    {
        _recommendedItemList = new List<KeyValuePair<RecommendType, BaseItemGUI>>();
        _location = location;
    }

    public bool Enabled
    {
        get { return _enabled; }
        set
        {
            if (value != _enabled)
            {
                _enabled = value;
                if (value)
                {
                    if (_selectedItem == null && _recommendedItemList.Count > 0)
                    {
                        SetSelection(_recommendedItemList[0].Value.Item);
                    }
                    if (_selectionStyle == null)
                    {
                        _selectionStyle = new GUIStyle(StormFront.GrayPanelBox);
                        _selectionStyle.overflow.left = 6;
                    }
                    if (_normalStyle == null)
                    {
                        _normalStyle = new GUIStyle(StormFront.GrayPanelBlankBox);
                        _normalStyle.overflow.left = 5;
                    }
                    CmuneEventHandler.AddListener<SelectShopItemEvent>(OnSelectItem);
                }
                else
                {
                    CmuneEventHandler.RemoveListener<SelectShopItemEvent>(OnSelectItem);
                    ClearSelection();
                }
            }
        }
    }

    public Action<IUnityItem, RecommendType> OnSelectionChange { get; set; }

    public IUnityItem SelectedItem
    {
        get { return _selectedItem; }
    }

    public void ClearSelection()
    {
        SetSelection(null);
    }

    public void Draw(Rect rect)
    {
        if (Enabled == false)
        {
            return;
        }

        DrawRecommendList(rect);
    }

    public void UpdateRecommendedList(IEnumerable<KeyValuePair<RecommendType, IUnityItem>> recomendations)
    {
        _recommendedItemList.Clear();

        foreach (var r in recomendations)
        {
            _recommendedItemList.Add(new KeyValuePair<RecommendType, BaseItemGUI>(r.Key, new InGameItemGUI(r.Value, ShopUtils.GetRecommendationString(r.Key), _location, r.Key == RecommendType.StaffPick ? BuyingRecommendationType.Manual : BuyingRecommendationType.Behavior)));
        }
    }

    #region Private

    private void OnSelectItem(SelectShopItemEvent ev)
    {
        if (_selectedItem != ev.Item)
        {
            SetSelection(ev.Item);
        }
    }

    private void SetSelection(IUnityItem item)
    {
        _selectedItem = item;

        foreach (var recommendation in _recommendedItemList)
        {
            if (recommendation.Value.Item == _selectedItem && OnSelectionChange != null)
            {
                OnSelectionChange(_selectedItem, recommendation.Key);
                break;
            }
        }
    }

    private void DrawRecommendList(Rect rect)
    {
        if (_recommendedItemList.Count <= 0)
        {
            GUI.Label(rect, "Nothing to recommend", BlueStonez.label_interparkbold_11pt);
            return;
        }

        GUI.BeginGroup(rect);
        {
            float itemHeight = rect.height / _recommendedItemList.Count;
            Rect itemRect = new Rect(5.0f, 0, rect.width - 10, itemHeight);

            for (int i = 0; i < _recommendedItemList.Count; i++)
            {
                IUnityItem item = _recommendedItemList[i].Value.Item;

                if (_selectedItem == item)
                {
                    GUI.Label(new Rect(itemRect.x, itemRect.y, rect.width - 5, itemHeight), GUIContent.none, _selectionStyle);
                }
                else
                {
                    GUI.Label(new Rect(itemRect.x, itemRect.y, rect.width - 5, itemHeight), GUIContent.none, _normalStyle);
                }
                _recommendedItemList[i].Value.Draw(itemRect, false);
                itemRect.y += (itemHeight - 1);
            }
        }
        GUI.EndGroup();
    }

    private bool _enabled;
    private GUIStyle _selectionStyle;
    private GUIStyle _normalStyle;
    private List<KeyValuePair<RecommendType, BaseItemGUI>> _recommendedItemList;
    private IUnityItem _selectedItem;
    private BuyingLocationType _location;
    #endregion
}
