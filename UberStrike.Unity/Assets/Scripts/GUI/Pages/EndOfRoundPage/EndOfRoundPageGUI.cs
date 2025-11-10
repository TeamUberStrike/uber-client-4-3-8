using System.Collections.Generic;
using Cmune.DataCenter.Common.Entities;
using Cmune.Util;
using UberStrike.Realtime.Common;
using UnityEngine;

public class EndOfRoundPageGUI : PageGUI
{
    const float WeaponRecommendHeight = 265;

    public override void DrawGUI(Rect rect)
    {
        float playerListHeight = Mathf.Min(_playerListGui.Height, rect.height - WeaponRecommendHeight) - 2.0f;
        float recommendY = Mathf.Min(_playerListGui.Height, rect.height - WeaponRecommendHeight);

        GUI.BeginGroup(rect, GUIContent.none, BlueStonez.window_standard_grey38);
        {
            _playerListGui.Draw(new Rect(2, 2, rect.width - 4, playerListHeight));
            DrawWeaponRecommend(new Rect(2, 2 + recommendY, rect.width - 4, WeaponRecommendHeight));
        }
        GUI.EndGroup();
    }

    #region Private

    #region Mono Functions
    private void Awake()
    {
        _weaponRecomGui = new WeaponRecommendListGUI(BuyingLocationType.EndOfRound);
        _weaponDetailGui = new WeaponDetailGUI();
        _playerListGui = new ValuablePlayerListGUI();
        _playerDetailGui = new ValuablePlayerDetailGUI();
        _playerListGui.OnSelectionChange = OnValuablePlayerListSelectionChange;
        _weaponRecomGui.OnSelectionChange = OnRecomListSelectionChange;
    }

    private void OnEnable()
    {
        OnUpdateRecommendationEvent(null);

        CmuneEventHandler.AddListener<UpdateRecommendationEvent>(OnUpdateRecommendationEvent);

        if (EndOfMatchStats.Instance.Data.MostValuablePlayers.Count > 0)
        {
            _playerListGui.SetSelection(0);
        }
        else
        {
            _playerDetailGui.SetValuablePlayer(null);
        }

        _weaponRecomGui.Enabled = true;
        _playerListGui.Enabled = true;
    }

    private void OnDisabled()
    {
        _weaponRecomGui.Enabled = false;
        _playerListGui.Enabled = false;
        _playerListGui.ClearSelection();
        _playerDetailGui.StopBadgeShow();

        CmuneEventHandler.RemoveListener<UpdateRecommendationEvent>(OnUpdateRecommendationEvent);
    }

    private void OnUpdateRecommendationEvent(UpdateRecommendationEvent ev)
    {
        List<KeyValuePair<RecommendType, IUnityItem>> recommendations = new List<KeyValuePair<RecommendType, IUnityItem>>(3);
        recommendations.Add(new KeyValuePair<RecommendType, IUnityItem>(RecommendType.StaffPick, ItemManager.Instance.GetRecommendedItem(GameState.CurrentSpace.MapId)));
        recommendations.Add(new KeyValuePair<RecommendType, IUnityItem>(RecommendType.RecommendedArmor, ShopUtils.GetRecommendedArmor(PlayerDataManager.PlayerLevelSecure, LoadoutManager.Instance.GetItemOnSlot<HoloGearItem>(LoadoutSlotType.GearHolo), LoadoutManager.Instance.GetItemOnSlot<GearItem>(LoadoutSlotType.GearUpperBody), LoadoutManager.Instance.GetItemOnSlot<GearItem>(LoadoutSlotType.GearLowerBody))));

        //if there is no "most efficient weapon" we fallback to the map based recommendation
        IUnityItem mostEfficientWeapon = ItemManager.Instance.GetItemInShop(EndOfMatchStats.Instance.Data.MostEffecientWeaponId);
        if (mostEfficientWeapon == null)
        {
            var recommendation = RecommendationUtils.GetRecommendedWeapon(PlayerDataManager.PlayerLevelSecure, GameState.CurrentSpace.CombatRangeTiers);
            mostEfficientWeapon = recommendation.ItemWeapon ?? RecommendationUtils.FallBackWeapon;
            recommendations.Add(new KeyValuePair<RecommendType, IUnityItem>(RecommendType.RecommendedWeapon, mostEfficientWeapon));
        }
        else
        {
            recommendations.Add(new KeyValuePair<RecommendType, IUnityItem>(RecommendType.MostEfficient, mostEfficientWeapon));
        }

        _weaponRecomGui.UpdateRecommendedList(recommendations);
    }

    #endregion

    private void DrawWeaponRecommend(Rect rect)
    {
        GUI.BeginGroup(rect);
        {
            GUI.Label(new Rect(0, 2, rect.width, 20.0f), LocalizedStrings.RecommendedLoadoutCaps, BlueStonez.label_interparkbold_18pt);
            DrawRecommendContent(new Rect(0, 25, rect.width, rect.height - 25));
        }
        GUI.EndGroup();
    }

    private void DrawRecommendContent(Rect rect)
    {
        GUI.BeginGroup(rect);
        {
            if (_weaponRecomGui.SelectedItem != null)
            {
                _weaponDetailGui.Draw(new Rect(0, 0, 200, rect.height));
            }
            else
            {
                _playerDetailGui.Draw(new Rect(0, 0, 200, rect.height));
            }
            _weaponRecomGui.Draw(new Rect(200 - 1, 0, rect.width - 200 + 1, rect.height));
        }
        GUI.EndGroup();
    }

    private void OnRecomListSelectionChange(IUnityItem item, RecommendType type)
    {
        _playerListGui.ClearSelection();
        _playerDetailGui.StopBadgeShow();
        _weaponDetailGui.SetWeaponItem(item, type);
    }

    private void OnValuablePlayerListSelectionChange(StatsSummary playerStats)
    {
        _weaponRecomGui.ClearSelection();
        _playerDetailGui.SetValuablePlayer(playerStats);
    }

    private WeaponDetailGUI _weaponDetailGui;
    private ValuablePlayerDetailGUI _playerDetailGui;
    private ValuablePlayerListGUI _playerListGui;
    private WeaponRecommendListGUI _weaponRecomGui;
    #endregion
}