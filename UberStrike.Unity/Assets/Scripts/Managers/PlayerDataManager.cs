using System.Collections;
using System.Collections.Generic;
using Cmune.DataCenter.Common.Entities;
using Cmune.Realtime.Common.Security;
using Cmune.Util;
using UberStrike.Core.ViewModel;
using UberStrike.DataCenter.Common.Entities;
using UnityEngine;
using UberStrike.Core.Types;

public class PlayerDataManager : Singleton<PlayerDataManager>
{
    #region Fields

    private PlayerStatisticsView _serverLocalPlayerPlayerStatisticsView;
    private MemberView _serverLocalPlayerMemberView;
    private LoadoutView _serverLocalPlayerLoadoutView;
    private Dictionary<int, PublicProfileView> _friends = new Dictionary<int, PublicProfileView>();
    private Dictionary<int, ClanMemberView> _clanMembers = new Dictionary<int, ClanMemberView>();

    private Color _localPlayerSkinColor = Color.white;

    private SecureMemory<int> _cmid;
    private SecureMemory<string> _name;
    private SecureMemory<string> _email;
    private SecureMemory<int> _accessLevel;
    private SecureMemory<int> _clanID;

    private SecureMemory<int> _experience;
    private SecureMemory<int> _level;
    private SecureMemory<int> _points;
    private SecureMemory<int> _credits;

    private ClanView _playerClanData;
    private GroupPosition _playerClanPosition = GroupPosition.Member;

    private bool _setLoadoutEventReturnDone = false;

    #endregion

    #region Properties

    /// <summary>
    /// Total weight of all gear, limited in the bounds of [0,1].
    /// Default value is 0
    /// </summary>
    public float GearWeight { get; private set; }

    public int FriendsCount { get { return _friends.Count; } }

    public MemberView ServerLocalPlayerMemberView
    {
        get { return _serverLocalPlayerMemberView; }
        set
        {
            _serverLocalPlayerMemberView = value;

            //here we update the secure data
            _cmid.WriteData(value.PublicProfile.Cmid);
            _accessLevel.WriteData((int)value.PublicProfile.AccessLevel);
            _name.WriteData(value.PublicProfile.Name);

            _points.WriteData(value.MemberWallet.Points);
            _credits.WriteData(value.MemberWallet.Credits);
        }
    }

    public void SetPlayerStatisticsView(PlayerStatisticsView value)
    {
        if (value != null)
        {
            _serverLocalPlayerPlayerStatisticsView = value;

            int level = PlayerXpUtil.GetLevelForXp(value.Xp);

            UpdateSecureLevelAndXp(level, value.Xp);
        }
    }

    public PlayerStatisticsView ServerLocalPlayerStatisticsView
    {
        get { return _serverLocalPlayerPlayerStatisticsView; }
    }

    public AvatarType LocalPlayerAvatarType
    {
        get
        {
            InventoryItem inventoryItem;
            if (LoadoutManager.Instance.TryGetItemInSlot(LoadoutSlotType.GearHolo, out inventoryItem) && inventoryItem.Item is HoloGearItem)
            {
                return ((HoloGearItem)inventoryItem.Item).Configuration.Holo;
            }
            else
            {
                return AvatarType.LutzRavinoff;
            }
        }
    }

    #endregion

    #region Static Properties

    public static Color SkinColor
    {
        get { return Instance._localPlayerSkinColor; }
    }

    public IEnumerable<PublicProfileView> FriendList
    {
        get { return _friends.Values; }
        set
        {
            _friends.Clear();
            if (value != null)
            {
                foreach (var f in value)
                    _friends.Add(f.Cmid, f);
            }
        }
    }

    public void AddFriend(PublicProfileView view)
    {
        _friends.Add(view.Cmid, view);
    }

    public void RemoveFriend(int friendCmid)
    {
        _friends.Remove(friendCmid);
    }

    public static bool IsPlayerLoggedIn
    {
        get { return Cmid > 0; }
    }

    public static MemberAccessLevel AccessLevel
    {
        get { return (MemberAccessLevel)Instance._accessLevel.ReadData(false); }
    }

    public static int Cmid
    {
        get { return Instance._cmid.ReadData(false); }
    }

    public static string GroupTag
    {
        get { return Instance.ServerLocalPlayerMemberView.PublicProfile.GroupTag; }
    }

    public static string Name
    {
        get { return Instance._name.ReadData(false); }
    }

    public static string Email
    {
        get { return Instance._email.ReadData(false); }
    }

    public static int Credits
    {
        get { return Instance._credits.ReadData(false); }
    }

    public static int Points
    {
        get { return Instance._points.ReadData(false); }
    }

    public static int PlayerExperience
    {
        get { return Instance._experience.ReadData(false); }
    }

    public static int PlayerLevel
    {
        get { return Instance._level.ReadData(false); }
    }

    public static ClanView ClanData
    {
        set
        {
            Instance._playerClanData = value;
            Instance._clanMembers.Clear();

            if (value != null)
            {
                Instance._clanID.WriteData(value.GroupId);

                int myCmid = CmidSecure;
                if (value.Members != null)
                {
                    foreach (var c in value.Members)
                    {
                        Instance._clanMembers[c.Cmid] = c;

                        if (c.Cmid == myCmid)
                            Instance._playerClanPosition = c.Position;
                    }
                }
            }
            else
            {
                Instance._clanID.WriteData(0);

                Instance._clanMembers.Clear();

                Instance._playerClanPosition = GroupPosition.Member;
            }
        }
    }

    public static bool IsPlayerInClan
    {
        get { return ClanID > 0; }
    }

    public static int ClanID
    {
        get { return Instance._clanID.ReadData(false); }
        set { Instance._clanID.WriteData(value); }
    }

    public GroupPosition RankInClan
    {
        get { return _playerClanPosition; }
        set { _playerClanPosition = value; }
    }

    public static string ClanName
    {
        get { return Instance._playerClanData != null ? Instance._playerClanData.Name : string.Empty; }
    }

    public static string ClanTag
    {
        get { return Instance._playerClanData != null ? Instance._playerClanData.Tag : string.Empty; }
    }

    public static string ClanMotto
    {
        get { return Instance._playerClanData != null ? Instance._playerClanData.Motto : string.Empty; }
    }

    public static System.DateTime ClanFoundingDate
    {
        get { return Instance._playerClanData != null ? Instance._playerClanData.FoundingDate : System.DateTime.Now; }
    }

    public static string ClanOwnerName
    {
        get { return Instance._playerClanData != null ? Instance._playerClanData.OwnerName : string.Empty; }
    }

    public static int ClanMembersLimit
    {
        get { return Instance._playerClanData != null ? Instance._playerClanData.MembersLimit : 0; }
    }

    public int ClanMembersCount
    {
        get { return _playerClanData != null ? _playerClanData.Members.Count : 0; }
    }

    public List<ClanMemberView> ClanMembers
    {
        get { return _playerClanData != null ? _playerClanData.Members : new List<ClanMemberView>(0); }
    }

    /// <summary>
    /// return if player have access right to invite to clan where he is now
    /// </summary>
    public static bool CanInviteToClan
    {
        get { return (Instance._playerClanPosition == GroupPosition.Leader || Instance._playerClanPosition == GroupPosition.Officer); }
    }

    #endregion

    #region Secure Properties

    public static MemberAccessLevel AccessLevelSecure
    {
        get { return (MemberAccessLevel)Instance._accessLevel.ReadData(true); }
    }

    public static bool IsMemberAccessSecure(params MemberAccessLevel[] levels)
    {
        foreach (var l in levels)
            if (AccessLevelSecure == l) return true;
        return false;
    }

    public static int CmidSecure
    {
        get { return Instance._cmid.ReadData(true); }
    }

    public static string NameSecure
    {
        get { return Instance._name.ReadData(true); }
        set { Instance._name.WriteData(value); }
    }

    public static string EmailSecure
    {
        get { return Instance._email.ReadData(true); }
        set { Instance._email.WriteData(value); }
    }

    public static int CreditsSecure
    {
        get { return Instance._credits.ReadData(true); }
    }

    public static int PointsSecure
    {
        get { return Instance._points.ReadData(true); }
    }

    public static void AddPointsSecure(int points)
    {
        Instance._points.WriteData(PointsSecure + points);
    }

    public static int PlayerExperienceSecure
    {
        get { return Instance._experience.ReadData(true); }
    }

    public static int PlayerLevelSecure
    {
        get { return Instance._level.ReadData(true); }
    }

    public static int ClanIDSecure
    {
        get { return Instance._clanID.ReadData(true); }
    }
    #endregion

    private PlayerDataManager()
    {
        // Initialize secure memory
        _cmid = new SecureMemory<int>(0);
        _name = new SecureMemory<string>(string.Empty);
        _email = new SecureMemory<string>(string.Empty);
        _accessLevel = new SecureMemory<int>(0);
        _points = new SecureMemory<int>(0);
        _credits = new SecureMemory<int>(0);
        _level = new SecureMemory<int>(0);
        _experience = new SecureMemory<int>(0);
        _clanID = new SecureMemory<int>(0);

        _serverLocalPlayerLoadoutView = new LoadoutView();
        _serverLocalPlayerMemberView = new MemberView();
        _serverLocalPlayerPlayerStatisticsView = new PlayerStatisticsView();
        _playerClanData = new ClanView();
    }

    private void HandleWebServiceError()
    {
        //Debug.Log("Error getting Member and Loadout data for local player.");
    }

    public void SetSkinColor(Color skinColor)
    {
        _localPlayerSkinColor = skinColor;
    }

    private LoadoutView CreateLocalPlayerLoadoutView()
    {
        LoadoutView loadoutView = new LoadoutView(
            _serverLocalPlayerLoadoutView.LoadoutId,
            _serverLocalPlayerLoadoutView.Backpack,
            LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.GearBoots),
            _serverLocalPlayerLoadoutView.Cmid,
          LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.GearFace),
          LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.FunctionalItem1),
          LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.FunctionalItem2),
          LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.FunctionalItem3),
          LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.GearGloves),
          LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.GearHead),
          LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.GearLowerBody),
          LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.WeaponMelee),
          LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.QuickUseItem1),
          LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.QuickUseItem2),
          LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.QuickUseItem3),
            AvatarType.LutzRavinoff,
           LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.GearUpperBody),
           LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.WeaponPrimary),
            _serverLocalPlayerLoadoutView.Weapon1Mod1,
            _serverLocalPlayerLoadoutView.Weapon1Mod2,
            _serverLocalPlayerLoadoutView.Weapon1Mod3,
            LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.WeaponSecondary),
            _serverLocalPlayerLoadoutView.Weapon2Mod1,
            _serverLocalPlayerLoadoutView.Weapon2Mod2,
            _serverLocalPlayerLoadoutView.Weapon2Mod3,
            LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.WeaponTertiary),
           _serverLocalPlayerLoadoutView.Weapon3Mod1,
            _serverLocalPlayerLoadoutView.Weapon3Mod2,
            _serverLocalPlayerLoadoutView.Weapon3Mod3,
            LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.GearHolo),
            ColorConverter.ColorToHex(_localPlayerSkinColor));

        return loadoutView;
    }

    #region Web Service Methods and Events

    public IEnumerator StartGetMemberWallet(bool showProgress)
    {
        if (PlayerDataManager.CmidSecure < 1)
        {
            Debug.LogError("Player CMID is invalid! Have you called AuthenticationManager.StartAuthenticateMember?");
            //PopupSystem.ShowMessage("Error Getting Player Wallet", "The player authentication data is invalid,\n perhaps there was an error logging in?", PopupSystem.AlertType.OK, HandleWebServiceError);
            ApplicationDataManager.Instance.LockApplication("The authentication process failed. Please sign in on www.uberstrike.com and restart UberStrike.");
        }
        else
        {
            if (showProgress)
            {
                IPopupDialog popupDialog = PopupSystem.ShowMessage("Updating", "Updating your points and credits balance...", PopupSystem.AlertType.None);
                yield return UberStrike.WebService.Unity.UserWebServiceClient.GetMemberWallet(CmidSecure,
                    OnGetMemberWalletEventReturn,
                    (ex) =>
                    {
                        DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace);
                    });
                yield return new WaitForSeconds(0.5f);
                PopupSystem.HideMessage(popupDialog);
            }
            else
            {
                yield return UberStrike.WebService.Unity.UserWebServiceClient.GetMemberWallet(CmidSecure,
                    OnGetMemberWalletEventReturn,
                    (ex) =>
                    {
                        DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace);
                    });
            }
        }
    }

    public IEnumerator StartSetLoadout()
    {
        if (!_setLoadoutEventReturnDone)
        {
            float timeToWait = 10;
            _setLoadoutEventReturnDone = true;
            while (timeToWait > 0)
            {
                yield return new WaitForSeconds(1);
                timeToWait -= 1;
            }

            yield return UberStrike.WebService.Unity.UserWebServiceClient.SetLoadout(CreateLocalPlayerLoadoutView(),
                (ev) =>
                {
                    _setLoadoutEventReturnDone = false;
                    if (ev != 0)
                    {
                        CmuneDebug.LogError("SetLoadout failed with error=" + ev);
                    }
                },
                (ex) =>
                {
                    _setLoadoutEventReturnDone = false;
                    DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace);
                });
        }
    }

    public IEnumerator StartGetLoadout()
    {
        //Catch any dependencies here
        if (!ItemManager.Instance.ValidateItemMall())
        {
            PopupSystem.ShowMessage("Error Getting Shop Data", "The shop is empty, perhaps there\nwas an error getting the Shop data?", PopupSystem.AlertType.OK, HandleWebServiceError);
            yield break;
        }
        else
        {
            yield return UberStrike.WebService.Unity.UserWebServiceClient.GetLoadout(PlayerDataManager.CmidSecure, (ev) =>
                {
                    if (ev != null)
                    {
                        //Let's see if we got back valid data from the web service call
                        _serverLocalPlayerLoadoutView = ev;

                        CheckLoadoutForExpiredItems();
                        LoadoutManager.Instance.RefreshLoadoutFromServerCache(ev);

                        // attributes
                        _localPlayerSkinColor = ColorConverter.HexToColor(ev.SkinColor);
                    }
                    else
                    {
                        ApplicationDataManager.Instance.LockApplication("It seems that you account is corrupted. Please contact support@cmune.com for advice.");
                    }
                },
                (ex) =>
                {
                    DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace);
                    ApplicationDataManager.Instance.LockApplication("There was an error getting your loadout.");
                });
        }
    }

    public IEnumerator StartGetMember()
    {
        if (PlayerDataManager.CmidSecure < 1)
        {
            Debug.LogError("Player CMID is invalid! Have you called AuthenticationManager.StartAuthenticateMember?");
            //PopupSystem.ShowMessage("Error Getting Player Stats Data", "The player authentication data is invalid,\n perhaps there was an error logging in?", PopupSystem.AlertType.OK, HandleWebServiceError);
            ApplicationDataManager.Instance.LockApplication("The authentication process failed. Please sign in on www.uberstrike.com and restart UberStrike.");
        }
        else
        {
            yield return UberStrike.WebService.Unity.UserWebServiceClient.GetMember(PlayerDataManager.CmidSecure,
                OnGetMemberEventReturn,
                (ex) =>
                {
                    DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace);
                    ApplicationDataManager.Instance.LockApplication("There was an error getting your player data.");
                });
        }
    }

    private void OnGetMemberWalletEventReturn(MemberWalletView ev)
    {
        //Let's see if we got back valid data from the web service call
        _serverLocalPlayerMemberView.MemberWallet = ev;

        UpdateSecurePointsAndCredits(ev.Points, ev.Credits);
    }

    private void OnGetMemberEventReturn(UberstrikeUserViewModel ev)
    {
        //Let's see if we got back valid data from the web service call
        int oldPoints = _points.ReadData(true);
        int oldCredits = _credits.ReadData(true);
        
        SetPlayerStatisticsView(ev.UberstrikeMemberView.PlayerStatisticsView);
        ServerLocalPlayerMemberView = ev.CmuneMemberView;

        if (ev.CmuneMemberView.MemberWallet.Points != oldPoints)
        {
            int newPoints = ev.CmuneMemberView.MemberWallet.Points - oldPoints;

            GlobalUIRibbon.Instance.AddPointsEvent(newPoints);
        }

        if (ev.CmuneMemberView.MemberWallet.Credits != oldCredits)
            GlobalUIRibbon.Instance.AddCreditsEvent(ev.CmuneMemberView.MemberWallet.Credits - oldCredits);
    }

    #endregion

    #region Public Methods

    public void AttributeXp(int xp)
    {
        int newXp = PlayerExperienceSecure + xp;
        int newLevel = PlayerXpUtil.GetLevelForXp(newXp);

        _serverLocalPlayerPlayerStatisticsView.Xp = newXp;
        _serverLocalPlayerPlayerStatisticsView.Level = newLevel;

        UpdateSecureLevelAndXp(newLevel, newXp);
    }

    private void UpdateSecureLevelAndXp(int level, int xp)
    {
        GlobalUIRibbon.Instance.UpdateLevelBounds(level);

        _experience.WriteData(xp);
        _level.WriteData(level);
    }

    public void UpdateSecurePointsAndCredits(int points, int credits)
    {
        _points.WriteData(points);
        _credits.WriteData(credits);
    }

    public void CheckLoadoutForExpiredItems()
    {
        LoadoutView view = _serverLocalPlayerLoadoutView;

        _serverLocalPlayerLoadoutView = new LoadoutView(view.LoadoutId,
            IsExpired(view.Backpack, "Backpack") ? 0 : view.Backpack,
            IsExpired(view.Boots, "Boots") ? 0 : view.Boots,
            view.Cmid,
            IsExpired(view.Face, "Face") ? 0 : view.Face,
            IsExpired(view.FunctionalItem1, "FunctionalItem1") ? 0 : view.FunctionalItem1,
            IsExpired(view.FunctionalItem2, "FunctionalItem2") ? 0 : view.FunctionalItem2,
            IsExpired(view.FunctionalItem3, "FunctionalItem3") ? 0 : view.FunctionalItem3,
            IsExpired(view.Gloves, "Gloves") ? 0 : view.Gloves,
            IsExpired(view.Head, "Head") ? 0 : view.Head,
            IsExpired(view.LowerBody, "LowerBody") ? 0 : view.LowerBody,
            IsExpired(view.MeleeWeapon, "MeleeWeapon") ? 0 : view.MeleeWeapon,
            IsExpired(view.QuickItem1, "QuickItem1") ? 0 : view.QuickItem1,
            IsExpired(view.QuickItem2, "QuickItem2") ? 0 : view.QuickItem2,
            IsExpired(view.QuickItem3, "QuickItem3") ? 0 : view.QuickItem3,
            view.Type,
            IsExpired(view.UpperBody, "UpperBody") ? 0 : view.UpperBody,
            IsExpired(view.Weapon1, "Weapon1") ? 0 : view.Weapon1,
            IsExpired(view.Weapon1Mod1, "Weapon1Mod1") ? 0 : view.Weapon1Mod1,
            IsExpired(view.Weapon1Mod2, "Weapon1Mod2") ? 0 : view.Weapon1Mod2,
            IsExpired(view.Weapon1Mod3, "Weapon1Mod3") ? 0 : view.Weapon1Mod3,
            IsExpired(view.Weapon2, "Weapon2") ? 0 : view.Weapon2,
            IsExpired(view.Weapon2Mod1, "Weapon2Mod1") ? 0 : view.Weapon2Mod1,
            IsExpired(view.Weapon2Mod2, "Weapon2Mod2") ? 0 : view.Weapon2Mod2,
            IsExpired(view.Weapon2Mod3, "Weapon2Mod3") ? 0 : view.Weapon2Mod3,
            IsExpired(view.Weapon3, "Weapon3") ? 0 : view.Weapon3,
            IsExpired(view.Weapon3Mod1, "Weapon3Mod1") ? 0 : view.Weapon3Mod1,
            IsExpired(view.Weapon3Mod2, "Weapon3Mod2") ? 0 : view.Weapon3Mod2,
            IsExpired(view.Weapon3Mod3, "Weapon3Mod3") ? 0 : view.Weapon3Mod3,
            IsExpired(view.Webbing, "Webbing") ? 0 : view.Webbing,
            view.SkinColor);
    }

    private bool IsExpired(int itemId, string debug)
    {
        return !InventoryManager.Instance.IsItemInInventory(itemId);
    }

    public bool ValidateMemberData()
    {
        return (_serverLocalPlayerMemberView.PublicProfile.Cmid > 0 && _serverLocalPlayerPlayerStatisticsView.Cmid > 0);
    }

    public static LoadoutSlotType GetSlotTypeForItemClass(UberstrikeItemClass itemClass)
    {
        LoadoutSlotType slot = LoadoutSlotType.None;

        switch (itemClass)
        {
            case UberstrikeItemClass.GearHead:
                slot = LoadoutSlotType.GearHead;
                break;

            case UberstrikeItemClass.GearFace:
                slot = LoadoutSlotType.GearFace;
                break;

            case UberstrikeItemClass.GearGloves:
                slot = LoadoutSlotType.GearGloves;
                break;

            case UberstrikeItemClass.GearUpperBody:
                slot = LoadoutSlotType.GearUpperBody;
                break;

            case UberstrikeItemClass.GearLowerBody:
                slot = LoadoutSlotType.GearLowerBody;
                break;

            case UberstrikeItemClass.GearBoots:
                slot = LoadoutSlotType.GearBoots;
                break;

            case UberstrikeItemClass.GearHolo:
                slot = LoadoutSlotType.GearHolo;
                break;
        }

        return slot;
    }

    public static bool IsClanMember(int cmid)
    {
        return Instance._clanMembers.ContainsKey(cmid);
    }

    public static bool IsFriend(int cmid)
    {
        return Instance._friends.ContainsKey(cmid);
    }

    public static bool TryGetFriend(int cmid, out PublicProfileView view)
    {
        return Instance._friends.TryGetValue(cmid, out view) && view != null;
    }

    public static bool TryGetClanMember(int cmid, out ClanMemberView view)
    {
        return Instance._clanMembers.TryGetValue(cmid, out view) && view != null;
    }

    #endregion
}