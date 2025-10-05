using System.Collections;
using System.Collections.Generic;
using System.Text;
using Cmune.Realtime.Common.Utils;
using UberStrike.DataCenter.Common.Entities;
using UnityEngine;
using Cmune.Util;

public class StatsPageGUI : MonoBehaviour
{
    #region FIELDS

    #region Inspector
    [SerializeField]
    private Texture2D _mostSplatsIcon;
    [SerializeField]
    private Texture2D _mostXPEarnedIcon;

    [SerializeField]
    private Texture2D _mostHealthPickedUpIcon;
    [SerializeField]
    private Texture2D _mostArmorPickedUpIcon;

    [SerializeField]
    private Texture2D _mostDamageDealtIcon;
    [SerializeField]
    private Texture2D _mostDamageReceivedIcon;

    [SerializeField]
    private Texture2D _mostHeadshotsIcon;
    [SerializeField]
    private Texture2D _mostNutshotsIcon;

    [SerializeField]
    private Texture2D _mostConsecutiveSnipesIcon;

    [SerializeField]
    private Texture2D _mostMeleeSplatsIcon;
    [SerializeField]
    private Texture2D _mostHandgunSplatsIcon;

    [SerializeField]
    private Texture2D _mostMachinegunSplatsIcon;
    [SerializeField]
    private Texture2D _mostCannonSplatsIcon;

    [SerializeField]
    private Texture2D _mostShotgunSplatsIcon;
    [SerializeField]
    private Texture2D _mostSniperSplatsIcon;

    [SerializeField]
    private Texture2D _mostSplattergunSplatsIcon;
    [SerializeField]
    private Texture2D _mostLauncherSplatsIcon;
    #endregion

    private Vector2 _scrollGeneral;
    private Vector2 _filterScroll;

    private Rect statsPage;
    private int _selectedStatsTab = 0;
    private int _playerCurrentLevelXpReq;
    private int _playerNextLevelXpReq;

    private Dictionary<string, Texture2D> _weaponIcons;
    private CmunePairList<float, string> _weaponStatList;

    private bool _isFilterDropDownOpen = false;
    private int _selectedFilterIndex = 0;
    private float _maxWeaponStat = 0;
    private float _xpSliderPos;

    private GUIContent[] _statsTabs;
    private string[] _selectionsToShow;

    private StatisticValueType _currentStatsType = StatisticValueType.Counter;
    #endregion

    private float statsPositionX;

    private void Awake()
    {
        _weaponStatList = new CmunePairList<float, string>();

        CmuneEventHandler.AddListener<LogoutEvent>((ev) =>
        {
            _selectedStatsTab = 0;
            _selectedFilterIndex = 0;
        });
    }

    private IEnumerator ScrollStatsFromRight(float time)
    {
        float t = 0;

        while (t < time)
        {
            t += Time.deltaTime;
            statsPositionX = Mathf.Lerp(0, 490, (t / time) * (t / time));
            yield return new WaitForEndOfFrame();
        }
    }

    private void Start()
    {
        _statsTabs = new GUIContent[] 
        { 
            new GUIContent(LocalizedStrings.Personal), 
            new GUIContent(LocalizedStrings.Weapons), 
            new GUIContent("Account") 
        };

        _selectionsToShow = new string[]
        { 
            LocalizedStrings.Damage,
            LocalizedStrings.Kills, 
            LocalizedStrings.Accuracy, 
            LocalizedStrings.Hits 
        };

        _weaponIcons = new Dictionary<string, Texture2D>()
        {
            { LocalizedStrings.MeleeWeapons, _mostMeleeSplatsIcon },
            { LocalizedStrings.Handguns, _mostHandgunSplatsIcon },
            { LocalizedStrings.Machineguns, _mostMachinegunSplatsIcon },
            { LocalizedStrings.Cannons, _mostCannonSplatsIcon },
            { LocalizedStrings.Shotguns, _mostShotgunSplatsIcon },
            { LocalizedStrings.Splatterguns, _mostSplattergunSplatsIcon },
            { LocalizedStrings.Launchers, _mostLauncherSplatsIcon },
            { LocalizedStrings.SniperRifles, _mostSniperSplatsIcon },
        };
    }

    private void OnGUI()
    {
        GUI.depth = (int)GuiDepth.Page;
        GUI.skin = BlueStonez.Skin;

        statsPage = new Rect(Screen.width - statsPositionX, GlobalUIRibbon.Instance.GetHeight(), statsPositionX, Screen.height - GlobalUIRibbon.Instance.GetHeight());

        //Stats GUI Panel
        GUI.BeginGroup(statsPage, GUIContent.none, BlueStonez.window_standard_grey38);
        {
            GUI.Label(new Rect(0, 0, statsPage.width, 56), LocalizedStrings.YourProfileCaps, BlueStonez.tab_strip);

            //Draw Stats Tabs
            GUI.changed = false;
            _selectedStatsTab = GUI.SelectionGrid(new Rect(0, 34, 260, 22), _selectedStatsTab, _statsTabs, 3, BlueStonez.tab_medium);
            if (GUI.changed)
            {
                if (_selectedStatsTab == 2)
                    TransactionHistory.Instance.GetCurrentTransactions();

                SfxManager.Play2dAudioClip(SoundEffectType.UIButtonClick);
            }

            //Draw Tab Contents
            GUI.BeginGroup(new Rect(0, 55, statsPage.width, statsPage.height - 55), string.Empty, BlueStonez.window_standard_grey38);
            {
                switch (_selectedStatsTab)
                {
                    case 0:
                        DrawPersonalStatsTab(new Rect(0, 0, statsPage.width, statsPage.height - 56));
                        break;
                    case 1:
                        DrawWeaponsStatsTab(new Rect(0, 0, statsPage.width, statsPage.height - 56));
                        break;
                    case 2:
                        DrawAccountStatsTab(new Rect(0, 0, statsPage.width, statsPage.height - 55));
                        break;
                }
            }
            GUI.EndGroup();
        }
        GUI.EndGroup();
    }

    private void OnEnable()
    {
        PlayerXpUtil.GetXpRangeForLevel(PlayerDataManager.PlayerLevelSecure, out _playerCurrentLevelXpReq, out _playerNextLevelXpReq);
        StartCoroutine(ScrollStatsFromRight(0.25f));

        UpdateWeaponStatList();
    }

    private void DrawWeaponsStatsTab(Rect rect)
    {
        bool guiEnabled = GUI.enabled;
        GUI.enabled = !_isFilterDropDownOpen;

        //Draw Stats Tabs
        GUI.changed = false;
        int index = GUI.SelectionGrid(new Rect(2, 5, rect.width - 4, 22), _selectedFilterIndex, _selectionsToShow, 4, BlueStonez.tab_medium);
        if (GUI.changed)
            SfxManager.Play2dAudioClip(SoundEffectType.UIButtonClick);

        if (index != _selectedFilterIndex)
        {
            _selectedFilterIndex = index;
            UpdateWeaponStatList();
        }

        //select title
        string title = LocalizedStrings.WeaponPerformaceTotal;
        switch (index)
        {
            case 0: title = LocalizedStrings.BestWeaponByDamageDealt; break;
            case 1: title = LocalizedStrings.BestWeaponByKills; break;
            case 2: title = LocalizedStrings.BestWeaponByAccuracy; break;
            case 3: title = LocalizedStrings.BestWeaponByHits; break;
        }

        //draw Weapon Stats
        _scrollGeneral = GUI.BeginScrollView(new Rect(0, 26, rect.width - 2, rect.height - 26), _scrollGeneral, new Rect(0, 0, 340, 680));
        {
            DrawGroupControl(new Rect(14, 16, rect.width - 40, 646), title, BlueStonez.label_group_interparkbold_18pt);

            int columnWidth = Mathf.RoundToInt((statsPage.width - 80) * 0.5f);

            int i = 0;
            foreach (KeyValuePair<float, string> kvp in _weaponStatList)
            {
                float bar = index == 2 ? kvp.Key / 100f : (_maxWeaponStat > 0 ? kvp.Key / _maxWeaponStat : 0);
                DrawWeaponStat(new Rect(36, 32 + (i * 76), columnWidth, 60), kvp.Value, kvp.Key, bar, _weaponIcons[kvp.Value]);
                i++;
            }
        }
        GUI.EndScrollView();
        GUI.enabled = guiEnabled;

        //DoDropDownList(new Rect(0, 0, statsPage.width, 22));
    }

    private void UpdateWeaponStatList()
    {
        _weaponStatList.Clear();

        var stats = PlayerDataManager.Instance.ServerLocalPlayerStatisticsView.WeaponStatistics;

        switch (_selectedFilterIndex)
        {
            case 0: // Best Weapon by Damage Dealt
                _weaponStatList.Add(stats.MeleeTotalDamageDone, LocalizedStrings.MeleeWeapons);
                _weaponStatList.Add(stats.HandgunTotalDamageDone, LocalizedStrings.Handguns);
                _weaponStatList.Add(stats.MachineGunTotalDamageDone, LocalizedStrings.Machineguns);
                _weaponStatList.Add(stats.CannonTotalDamageDone, LocalizedStrings.Cannons);
                _weaponStatList.Add(stats.ShotgunTotalDamageDone, LocalizedStrings.Shotguns);
                _weaponStatList.Add(stats.SplattergunTotalDamageDone, LocalizedStrings.Splatterguns);
                _weaponStatList.Add(stats.LauncherTotalDamageDone, LocalizedStrings.Launchers);
                _weaponStatList.Add(stats.SniperTotalDamageDone, LocalizedStrings.SniperRifles);
                _currentStatsType = StatisticValueType.Counter;
                break;

            case 1: // Best Weapon by Splats
                _weaponStatList.Add(stats.MeleeTotalSplats, LocalizedStrings.MeleeWeapons);
                _weaponStatList.Add(stats.HandgunTotalSplats, LocalizedStrings.Handguns);
                _weaponStatList.Add(stats.MachineGunTotalSplats, LocalizedStrings.Machineguns);
                _weaponStatList.Add(stats.CannonTotalSplats, LocalizedStrings.Cannons);
                _weaponStatList.Add(stats.ShotgunTotalSplats, LocalizedStrings.Shotguns);
                _weaponStatList.Add(stats.SplattergunTotalSplats, LocalizedStrings.Splatterguns);
                _weaponStatList.Add(stats.LauncherTotalSplats, LocalizedStrings.Launchers);
                _weaponStatList.Add(stats.SniperTotalSplats, LocalizedStrings.SniperRifles);
                _currentStatsType = StatisticValueType.Counter;
                break;

            case 2: // Best Weapon by Accuracy
                _weaponStatList.Add((stats.MeleeTotalShotsHit == 0) ? 0 : Mathf.Clamp01(stats.MeleeTotalShotsHit / (float)stats.MeleeTotalShotsFired) * 100, LocalizedStrings.MeleeWeapons);
                _weaponStatList.Add((stats.HandgunTotalShotsHit == 0) ? 0 : Mathf.Clamp01(stats.HandgunTotalShotsHit / (float)stats.HandgunTotalShotsFired) * 100, LocalizedStrings.Handguns);
                _weaponStatList.Add((stats.MachineGunTotalShotsHit == 0) ? 0 : Mathf.Clamp01(stats.MachineGunTotalShotsHit / (float)stats.MachineGunTotalShotsFired) * 100, LocalizedStrings.Machineguns);
                _weaponStatList.Add((stats.CannonTotalShotsHit == 0) ? 0 : Mathf.Clamp01(stats.CannonTotalShotsHit / (float)stats.CannonTotalShotsFired) * 100, LocalizedStrings.Cannons);
                _weaponStatList.Add((stats.ShotgunTotalShotsHit == 0) ? 0 : Mathf.Clamp01(stats.ShotgunTotalShotsHit / (float)stats.ShotgunTotalShotsFired) * 100, LocalizedStrings.Shotguns);
                _weaponStatList.Add((stats.SplattergunTotalShotsHit == 0) ? 0 : Mathf.Clamp01(stats.SplattergunTotalShotsHit / (float)stats.SplattergunTotalShotsFired) * 100, LocalizedStrings.Splatterguns);
                _weaponStatList.Add((stats.LauncherTotalShotsHit == 0) ? 0 : Mathf.Clamp01(stats.LauncherTotalShotsHit / (float)stats.LauncherTotalShotsFired) * 100, LocalizedStrings.Launchers);
                _weaponStatList.Add((stats.SniperTotalShotsHit == 0) ? 0 : Mathf.Clamp01(stats.SniperTotalShotsHit / (float)stats.SniperTotalShotsFired) * 100, LocalizedStrings.SniperRifles);
                _currentStatsType = StatisticValueType.Percent;
                break;

            case 3: // Best Weapon by Hits
                _weaponStatList.Add(stats.MeleeTotalShotsHit, LocalizedStrings.MeleeWeapons);
                _weaponStatList.Add(stats.HandgunTotalShotsHit, LocalizedStrings.Handguns);
                _weaponStatList.Add(stats.MachineGunTotalShotsHit, LocalizedStrings.Machineguns);
                _weaponStatList.Add(stats.CannonTotalShotsHit, LocalizedStrings.Cannons);
                _weaponStatList.Add(stats.ShotgunTotalShotsHit, LocalizedStrings.Shotguns);
                _weaponStatList.Add(stats.SplattergunTotalShotsHit, LocalizedStrings.Splatterguns);
                _weaponStatList.Add(stats.LauncherTotalShotsHit, LocalizedStrings.Launchers);
                _weaponStatList.Add(stats.SniperTotalShotsHit, LocalizedStrings.SniperRifles);
                _currentStatsType = StatisticValueType.Counter;
                break;
        }

        _weaponStatList.Sort(delegate(KeyValuePair<float, string> a, KeyValuePair<float, string> b)
            {
                // sort the weapon from top to down
                return -a.Key.CompareTo(b.Key);
            });

        _maxWeaponStat = 0;
        foreach (KeyValuePair<float, string> kvp in _weaponStatList)
        {
            if (kvp.Key > _maxWeaponStat) _maxWeaponStat = kvp.Key;
        }
    }

    private void DebugWeaponStatistics()
    {
        int i = 0;
        StringBuilder strBuilder = new StringBuilder();

        foreach (KeyValuePair<float, string> pair in _weaponStatList)
        {
            strBuilder.AppendLine(i++ + ": " + pair.Key + " " + pair.Value);
        }
    }

    private void DrawPersonalStatsTab(Rect rect)
    {
        _scrollGeneral = GUI.BeginScrollView(rect, _scrollGeneral, new Rect(0, 0, 340, 915));
        {
            int columnWidth = Mathf.RoundToInt((rect.width - 80) * 0.5f);

            var stats = PlayerDataManager.Instance.ServerLocalPlayerStatisticsView.PersonalRecord;

            // Level & Experience
            DrawGroupControl(new Rect(14, 16, rect.width - 40, 100), LocalizedStrings.LevelAndXP, BlueStonez.label_group_interparkbold_18pt);
            DrawXPMeter(new Rect(24, 32, rect.width - 60, 64));

            // Personal Records
            DrawGroupControl(new Rect(14, 142, rect.width - 40, 405), LocalizedStrings.PersonalRecordsPerLife, BlueStonez.label_group_interparkbold_18pt);

            // Left Column
            DrawPersonalStat(36, 158, columnWidth, LocalizedStrings.MostKills, stats.MostSplats.ToString(), _mostSplatsIcon);
            DrawPersonalStat(36, 234, columnWidth, LocalizedStrings.MostDamageDealt, stats.MostDamageDealt.ToString(), _mostDamageDealtIcon);
            DrawPersonalStat(36, 310, columnWidth, LocalizedStrings.MostHealthPickedUp, stats.MostHealthPickedUp.ToString(), _mostHealthPickedUpIcon);
            DrawPersonalStat(36, 386, columnWidth, LocalizedStrings.MostHeadshots, stats.MostHeadshots.ToString(), _mostHeadshotsIcon);
            DrawPersonalStat(36, 462, columnWidth, LocalizedStrings.MostConsecutiveSnipes, stats.MostConsecutiveSnipes.ToString(), _mostConsecutiveSnipesIcon);

            // Right Column
            DrawPersonalStat(36 + columnWidth, 158, columnWidth, LocalizedStrings.MostXPEarned, stats.MostXPEarned.ToString(), _mostXPEarnedIcon);
            DrawPersonalStat(36 + columnWidth, 234, columnWidth, LocalizedStrings.MostDamageReceived, stats.MostDamageReceived.ToString(), _mostDamageReceivedIcon);
            DrawPersonalStat(36 + columnWidth, 310, columnWidth, LocalizedStrings.MostArmorPickedUp, stats.MostArmorPickedUp.ToString(), _mostArmorPickedUpIcon);
            DrawPersonalStat(36 + columnWidth, 386, columnWidth, LocalizedStrings.MostNutshots, stats.MostNutshots.ToString(), _mostNutshotsIcon);

            DrawGroupControl(new Rect(14, 142 + 433, statsPage.width - 40, 328), "Weapon Records (per Life)", BlueStonez.label_group_interparkbold_18pt);

            // Left Column
            DrawPersonalStat(36, 593, columnWidth, LocalizedStrings.MostMeleeKills, stats.MostMeleeSplats.ToString(), _mostMeleeSplatsIcon);
            DrawPersonalStat(36, 669, columnWidth, LocalizedStrings.MostMachinegunKills, stats.MostMachinegunSplats.ToString(), _mostMachinegunSplatsIcon);
            DrawPersonalStat(36, 745, columnWidth, LocalizedStrings.MostShotgunKills, stats.MostShotgunSplats.ToString(), _mostShotgunSplatsIcon);
            DrawPersonalStat(36, 821, columnWidth, LocalizedStrings.MostSplattergunKills, stats.MostSplattergunSplats.ToString(), _mostSplattergunSplatsIcon);

            // Right Column
            DrawPersonalStat(36 + columnWidth, 593, columnWidth, LocalizedStrings.MostHandgunKills, stats.MostHandgunSplats.ToString(), _mostHandgunSplatsIcon);
            DrawPersonalStat(36 + columnWidth, 669, columnWidth, LocalizedStrings.MostCannonKills, stats.MostCannonSplats.ToString(), _mostCannonSplatsIcon);
            DrawPersonalStat(36 + columnWidth, 745, columnWidth, LocalizedStrings.MostSniperRifleKills, stats.MostSniperSplats.ToString(), _mostSniperSplatsIcon);
            DrawPersonalStat(36 + columnWidth, 821, columnWidth, LocalizedStrings.MostLauncherKills, stats.MostLauncherSplats.ToString(), _mostLauncherSplatsIcon);
        }
        GUI.EndScrollView();
    }

    private void DrawAccountStatsTab(Rect rect)
    {
        TransactionHistory.Instance.DrawPanel(rect);
    }

    #region GUI Helpers

    private void DoDropDownList(Rect position)
    {
        Rect rectLabel = new Rect(position.x, position.y, position.width - position.height, position.height);
        Rect rectButton = new Rect(position.x + position.width - position.height - 2, position.y - 1, position.height, position.height);

        if (GUI.Button(rectButton, GUIContent.none, BlueStonez.dropdown_button))
        {
            _isFilterDropDownOpen = !_isFilterDropDownOpen;
        }

        if (_isFilterDropDownOpen)
        {
            Rect rect = new Rect(position.x, position.y + position.height, position.width - position.height, ((_selectionsToShow.Length) * 20) + 1);
            GUI.Box(rect, string.Empty, BlueStonez.window_standard_grey38);
            _filterScroll = GUI.BeginScrollView(rect, _filterScroll, new Rect(0, 0, rect.width - 20, (_selectionsToShow.Length) * 20));

            //Other Filters
            for (int i = 0; i < _selectionsToShow.Length; i++)
            {
                GUI.Label(new Rect(4, i * 20, rect.width, 20), _selectionsToShow[i], BlueStonez.label_interparkbold_11pt_left);
                if (GUI.Button(new Rect(2, i * 20, rect.width, 20), string.Empty, BlueStonez.dropdown_list))
                {
                    _isFilterDropDownOpen = false;
                    _selectedFilterIndex = i;
                    UpdateWeaponStatList();
                }
            }
            GUI.EndScrollView();
        }
        else
        {
            if (GUITools.Button(rectLabel, new GUIContent(_selectionsToShow[_selectedFilterIndex]), BlueStonez.label_dropdown))
            {
                _isFilterDropDownOpen = !_isFilterDropDownOpen;
            }
        }
    }

    private void DrawPersonalStat(int x, int y, int width, string statName, string statValue, Texture2D icon)
    {
        GUI.Label(new Rect(x, y, width, 20), statName, BlueStonez.label_interparkbold_13pt_left);
        GUI.DrawTexture(new Rect(x, y + 22, 48, 48), icon);
        GUI.Label(new Rect(x + 54, y + 36, width - 54, 20), statValue, BlueStonez.label_interparkbold_18pt_left);
    }

    private void DrawWeaponStat(Rect position, string statName, float statValue, float barValue, Texture2D icon)
    {
        GUI.Label(new Rect(position.x, position.y, position.width, 20), statName, BlueStonez.label_interparkmed_18pt_left);
        GUI.DrawTexture(new Rect(position.x, position.y + 22, 48, 48), icon);
        DrawLevelBar(new Rect(position.x + 54, position.y + 32, position.width - 54, 12), barValue, ColorScheme.ProgressBar);
        string value = _currentStatsType == StatisticValueType.Percent ? statValue.ToString("f1") + "%" : statValue.ToString("f0");
        GUI.Label(new Rect(position.x + 54, position.y + 48, position.width - 54, 20), value, BlueStonez.label_interparkbold_11pt_left);
    }

    private void DrawLevelBar(Rect position, float amount, Color barColor)
    {
        GUI.BeginGroup(position);
        GUI.Label(new Rect(0, 0, position.width, 12), GUIContent.none, BlueStonez.progressbar_background);
        GUI.color = barColor;
        GUI.Label(new Rect(2, 2, (position.width - 4) * Mathf.Clamp01(amount), 8), GUIContent.none, BlueStonez.progressbar_thumb);
        GUI.color = Color.white;
        GUI.EndGroup();
    }

    private void DrawXPMeter(Rect position)
    {
        float range = _playerNextLevelXpReq - _playerCurrentLevelXpReq;
        _xpSliderPos = range > 0 ? Mathf.Clamp01((PlayerDataManager.PlayerExperience - _playerCurrentLevelXpReq) / range) : 0;
        GUI.BeginGroup(position);
        {
            if (PlayerDataManager.PlayerLevel < PlayerXpUtil.MaxPlayerLevel)
            {
                GUI.Label(new Rect(0, 0, 200, 16), LocalizedStrings.CurrentXP + " " + PlayerDataManager.PlayerExperience, BlueStonez.label_interparkbold_11pt_left);
                GUI.Label(new Rect(position.width - 200, 0, 200, 16), LocalizedStrings.RemainingXP + " " + Mathf.Max(0, _playerNextLevelXpReq - PlayerDataManager.PlayerExperience), BlueStonez.label_interparkbold_11pt_right);

                GUI.Box(new Rect(0, 25, position.width, 23), GUIContent.none, BlueStonez.progressbar_large_background);

                GUI.color = ColorScheme.ProgressBar;
                GUI.Box(new Rect(2, 27, Mathf.RoundToInt((position.width - 4) * _xpSliderPos), 19), GUIContent.none, BlueStonez.progressbar_large_thumb);
                GUI.color = Color.white;

                GUI.Label(new Rect(0, 50, position.width, 16), PlayerXpUtil.GetLevelDescription(PlayerDataManager.PlayerLevel), BlueStonez.label_interparkbold_11pt_left);
                GUI.Label(new Rect(0, 50, position.width, 16), PlayerXpUtil.GetLevelDescription(PlayerDataManager.PlayerLevel + 1), BlueStonez.label_interparkbold_11pt_right);
            }
            else
            {
                GUI.Label(new Rect(0, 0, 200, 16), LocalizedStrings.CurrentXP + " " + PlayerDataManager.PlayerExperience, BlueStonez.label_interparkbold_11pt_left);
                GUI.Label(new Rect(position.width - 200, 0, 200, 16), "(Lvl " + PlayerDataManager.PlayerLevel + ")", BlueStonez.label_interparkbold_11pt_right);

                GUI.color = new Color(0, 0.607f, 0.662f);
                GUI.Box(new Rect(0, 30, position.width, 23), "YOU ARE FLOATING IN UBER SPACE", BlueStonez.label_interparkbold_18pt);
                GUI.color = Color.white;
            }

        }
        GUI.EndGroup();
    }

    private void DrawStatLabel(Rect position, string label, string text)
    {
        GUI.Label(position, label + ": " + text, BlueStonez.label_interparkbold_16pt);
    }

    private Rect CenteredAspectRect(float aspectRatio, int screenWidth, int screenHeight, int offsetTop, int minpWidth, int minHeight)
    {
        float currentAspect = (float)screenWidth / (float)(screenHeight);
        Rect windowrect;
        if (currentAspect > aspectRatio)
        {
            //Window is wider than wider than desired aspectRation
            int windowWidth = Mathf.Clamp(Mathf.RoundToInt(screenHeight * aspectRatio), minpWidth, 2048);
            windowrect = new Rect((screenWidth - windowWidth) * 0.5f, offsetTop, windowWidth, Mathf.Clamp(screenHeight, minHeight, 2048));
        }
        else
        {
            //Window is taller than desired aspectRatio 
            windowrect = new Rect(0, offsetTop, Mathf.Clamp(screenWidth, minpWidth, 2048), Mathf.Clamp(Mathf.RoundToInt(screenWidth / aspectRatio), minHeight, 2048));
        }

        return windowrect;
    }

    private void DrawGroupControl(Rect rect, string title, GUIStyle style)
    {
        GUI.BeginGroup(rect, string.Empty, BlueStonez.group_grey81);
        GUI.EndGroup();
        GUI.Label(new Rect(rect.x + 18, rect.y - 8, style.CalcSize(new GUIContent(title)).x + 10, 16), title, style);
    }

    #endregion

    private enum StatisticValueType
    {
        Counter,
        Percent,
    }
}