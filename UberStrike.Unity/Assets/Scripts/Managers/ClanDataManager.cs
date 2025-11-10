using Cmune.DataCenter.Common.Entities;
using Cmune.Util;
using UberStrike.Core.Types;
using UberStrike.DataCenter.Common.Entities;
using UnityEngine;

public class ClanDataManager : Singleton<ClanDataManager>
{
    public bool IsGetMyClanDone { get; set; }
    public bool HaveFriends { get { return PlayerDataManager.Instance.FriendsCount >= UberStrikeCommonConfig.ClanLeaderMinContactsCount; } }
    public bool HaveLevel { get { return PlayerDataManager.PlayerLevelSecure >= UberStrikeCommonConfig.ClanLeaderMinLevel; } }
    public bool HaveLicense { get { return InventoryManager.Instance.HasClanLicense(); } }

    public float NextClanRefresh { get; private set; }

    private ClanDataManager() { }

    private void HandleWebServiceError()
    {
        Debug.LogError("Error getting Clan data for local player.");
    }

    public void CheckCompleteClanData()
    {
        UberStrike.WebService.Unity.ClanWebServiceClient.GetMyClanId(PlayerDataManager.ClanIDSecure, UberStrikeCommonConfig.ApplicationId,
                (ev) =>
                {
                    PlayerDataManager.ClanID = ev;
                    RefreshClanData(true);
                },
                (ex) =>
                {
                    DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace);
                });
    }

    /// <summary>
    /// Refresh your clan data
    /// </summary>
    public void RefreshClanData(bool force = false)
    {
        if (PlayerDataManager.IsPlayerInClan && (force || NextClanRefresh < Time.time))
        {
            NextClanRefresh = Time.time + 30;

            UberStrike.WebService.Unity.ClanWebServiceClient.GetClan(PlayerDataManager.ClanIDSecure,
                (ev) =>
                {
                    SetClanData(ev);
                },
                (ex) =>
                {
                    DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace);
                });
        }
    }


    public void SetClanData(ClanView view)
    {
        PlayerDataManager.ClanData = view;

        CommConnectionManager.CommCenter.SendUpdatedActorInfo();
        CommConnectionManager.CommCenter.SendContactList();

        ChatManager.Instance.UpdateClanSection();
    }

    public bool IsProcessingWebservice { get; private set; }

    public void LeaveClan()
    {
        IsProcessingWebservice = true;
        UberStrike.WebService.Unity.ClanWebServiceClient.LeaveAClan(PlayerDataManager.ClanIDSecure, PlayerDataManager.CmidSecure,
            (ev) =>
            {
                IsProcessingWebservice = false;
                if (ev == 0)
                {
                    CommConnectionManager.CommCenter.SendUpdateClanMembers(PlayerDataManager.Instance.ClanMembers);
                    SetClanData(null);
                }
                else
                {
                    PopupSystem.ShowMessage("Leave Clan", "There was an error removing you from this clan.\nErrorCode = " + ev, PopupSystem.AlertType.OK);
                }
            },
            (ex) =>
            {
                IsProcessingWebservice = false;
                DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace);
            });
    }

    public void DisbanClan()
    {
        IsProcessingWebservice = true;
        UberStrike.WebService.Unity.ClanWebServiceClient.DisbandGroup(PlayerDataManager.ClanIDSecure, PlayerDataManager.CmidSecure,
            (ev) =>
            {
                IsProcessingWebservice = false;
                if (ev == 0)
                {
                    CommConnectionManager.CommCenter.SendUpdateClanMembers(PlayerDataManager.Instance.ClanMembers);
                    SetClanData(null);
                }
            },
            (ex) =>
            {
                IsProcessingWebservice = false;
                DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace, "There was a problem. Please try again later.");
            });
    }

    public void CreateNewClan(string name, string motto, string tag)
    {
        IsProcessingWebservice = true;
        GroupCreationView clanCreation = new GroupCreationView()
        {
            Name = name,
            Motto = motto,
            ApplicationId = CommonConfig.ApplicationIdUberstrike,
            Cmid = PlayerDataManager.CmidSecure,
            Tag = tag,
            Locale = ApplicationDataManager.CurrentLocaleString
        };

        UberStrike.WebService.Unity.ClanWebServiceClient.CreateClan(clanCreation,
            (ev) =>
            {
                IsProcessingWebservice = false;

                if (ev.ResultCode == 0)
                {
                    CmuneEventHandler.Route(new ClanPageGUI.ClanCreationEvent());
                    SetClanData(ev.ClanView);
                }
                else
                {
                    switch (ev.ResultCode)
                    {
                        case UberStrikeGroupOperationResult.ClanLicenceNotFound:
                        case UberStrikeGroupOperationResult.InvalidLevel:
                        case UberStrikeGroupOperationResult.InvalidContactsCount:
                            {
                                PopupSystem.ShowMessage("Sorry", "You don't fulfill the minimal requirements to create your own clan.");
                            } break;
                        case UberStrikeGroupOperationResult.AlreadyMemberOfAGroup:
                            {
                                PopupSystem.ShowMessage("Clan Collision", "You are already member of another clan, please leave first before creating your own.");
                            } break;
                        case UberStrikeGroupOperationResult.InvalidName:
                            {
                                PopupSystem.ShowMessage("Invalid Clan Name", "The name '" + name + "' is not valid, please modify it.");
                            } break;
                        case UberStrikeGroupOperationResult.InvalidTag:
                            {
                                PopupSystem.ShowMessage("Invalid Clan Tag", "The tag '" + tag + "' is not valid, please modify it.");
                            } break;
                        case UberStrikeGroupOperationResult.InvalidMotto:
                            {
                                PopupSystem.ShowMessage("Invalid Clan Motto", "The motto '" + motto + "' is not valid, please modify it.");
                            } break;
                        case UberStrikeGroupOperationResult.DuplicateName:
                            {
                                PopupSystem.ShowMessage("Clan Name", "The name '" + name + "' is already taken, try another one.");
                            } break;
                        case UberStrikeGroupOperationResult.DuplicateTag:
                            {
                                PopupSystem.ShowMessage("Clan Tag", "The tag '" + tag + "' is already taken, try another one.");
                            } break;

                        default:
                            {
                                PopupSystem.ShowMessage("Sorry", "There was an error (code " + ev.ResultCode + "), please contact support@cmune.com for help.");
                            } break;
                    }
                }
            },
            (ex) =>
            {
                IsProcessingWebservice = false;
                SetClanData(null);

                DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace, "There was a problem. Please try again later.");
            }
            );
    }

    public void UpdateMemberTo(int cmid, GroupPosition position)
    {
        IsProcessingWebservice = true;
        UberStrike.WebService.Unity.ClanWebServiceClient.UpdateMemberPosition(new MemberPositionUpdateView(PlayerDataManager.ClanIDSecure, PlayerDataManager.CmidSecure, cmid, position),
            (ev) =>
            {
                IsProcessingWebservice = false;
                if (ev == 0)
                {
                    ClanMemberView view;
                    if (PlayerDataManager.TryGetClanMember(cmid, out view))
                    {
                        view.Position = position;
                    }

                    //TODO: inform the member of his new position
                }
            },
            (ex) =>
            {
                IsProcessingWebservice = false;
                DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace, "There was a problem. Please try again later.");
            });
    }

    public void TransferOwnershipTo(int cmid)
    {
        IsProcessingWebservice = true;
        UberStrike.WebService.Unity.ClanWebServiceClient.TransferOwnership(PlayerDataManager.ClanIDSecure, PlayerDataManager.CmidSecure, cmid,
            (ev) =>
            {
                IsProcessingWebservice = false;
                if (ev == 0)
                {
                    ClanMemberView member;
                    if (PlayerDataManager.TryGetClanMember(cmid, out member))
                    {
                        member.Position = GroupPosition.Leader;
                    }
                    if (PlayerDataManager.TryGetClanMember(PlayerDataManager.CmidSecure, out member))
                    {
                        member.Position = GroupPosition.Member;
                    }
                    PlayerDataManager.Instance.RankInClan = GroupPosition.Member;
                }
                else
                {
                    switch (ev)
                    {
                        case UberStrikeGroupOperationResult.InvalidLevel:
                            {
                                PopupSystem.ShowMessage("Sorry", "The player you selected can't be a clan leader, because he is not level 4 yet!");
                            } break;
                        case UberStrikeGroupOperationResult.InvalidContactsCount:
                            {
                                PopupSystem.ShowMessage("Sorry", "The player you selected can't be a clan leader, because has no friends!");
                            } break;
                        case UberStrikeGroupOperationResult.ClanLicenceNotFound:
                            {
                                PopupSystem.ShowMessage("Sorry", "The player you selected can't be a clan leader, because he doesn't own a clan license.");
                            } break;
                        default:
                            {
                                PopupSystem.ShowMessage("Sorry", "There was an error (code " + ev + "), please contact support@cmune.com for help.");
                            } break;
                    }
                }
            },
            (ex) =>
            {
                IsProcessingWebservice = false;
                DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace, "There was a problem. Please try again later.");
            });
    }

    public void RemoveMemberFromClan(int cmid)
    {
        IsProcessingWebservice = true;

        UberStrike.WebService.Unity.ClanWebServiceClient.KickMemberFromClan(PlayerDataManager.ClanIDSecure, PlayerDataManager.CmidSecure, cmid,
            (ev) =>
            {
                IsProcessingWebservice = false;
                if (ev == 0)
                {
                    PlayerDataManager.Instance.ClanMembers.RemoveAll(m => m.Cmid == cmid);
                    CommConnectionManager.CommCenter.SendUpdateClanMembers(PlayerDataManager.Instance.ClanMembers);
                    CommConnectionManager.CommCenter.SendRefreshClanData(cmid);
                    ChatManager.Instance.UpdateClanSection();
                }
            },
            (ex) =>
            {
                IsProcessingWebservice = false;
                DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace, "There was a problem. Please try again later.");
            }
           );
    }
}