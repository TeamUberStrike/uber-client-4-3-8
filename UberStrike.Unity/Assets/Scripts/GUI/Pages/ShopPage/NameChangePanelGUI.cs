using Cmune.DataCenter.Common.Entities;
using UnityEngine;
using UberStrike.Helper;

public class NameChangePanelGUI : PanelGuiBase
{
    private Rect _groupRect = new Rect(1, 1, 1, 1);
    private string newName = string.Empty;
    private string oldName = string.Empty;
    private IUnityItem nameChangeItem;
    private bool isChangingName = false;

    private void OnGUI()
    {
        _groupRect = new Rect((Screen.width - 340) * 0.5f, (Screen.height - 200) * 0.5f, 340, 200);

        GUI.depth = (int)GuiDepth.Panel;
        GUI.skin = BlueStonez.Skin;
        Rect groupRect = _groupRect;

        GUI.BeginGroup(groupRect, string.Empty, BlueStonez.window_standard_grey38);
        {
            if (nameChangeItem != null)
            {
                // Icon
                GUI.Label(new Rect(8, 8, 48, 48), nameChangeItem.Icon, BlueStonez.item_slot_large);

                // Title
                if (BlueStonez.label_interparkbold_32pt_left.CalcSize(new GUIContent(nameChangeItem.ItemView.Name)).x > (groupRect.width - 72))
                    GUI.Label(new Rect(64, 8, groupRect.width - 72, 30), nameChangeItem.ItemView.Name, BlueStonez.label_interparkbold_18pt_left);
                else
                    GUI.Label(new Rect(64, 8, groupRect.width - 72, 30), nameChangeItem.ItemView.Name, BlueStonez.label_interparkbold_32pt_left);
            }

            // Item class
            GUI.Label(new Rect(64, 30, groupRect.width - 72, 30), LocalizedStrings.FunctionalItem, BlueStonez.label_interparkbold_16pt_left);

            // Draw frame
            Rect myGroupRect = new Rect(8, 70 + 46, _groupRect.width - 16, _groupRect.height - 120 - 46);
            GUI.BeginGroup(new Rect(myGroupRect.xMin, 74, myGroupRect.width, myGroupRect.height + 42), string.Empty, BlueStonez.group_grey81);
            GUI.EndGroup();

            GUI.Label(new Rect(6 + 50, 2 + 70, 177 + 50, 20), LocalizedStrings.ChooseCharacterName, BlueStonez.label_interparkbold_11pt);
            GUI.SetNextControlName("@ChooseName");
            Rect rectName = new Rect(6 + 50, 22 + 80, 177 + 50, 24);
            GUI.changed = false;
            newName = GUI.TextField(rectName, newName, 18, BlueStonez.textField);
            newName = TextUtilities.Trim(newName);

            if (string.IsNullOrEmpty(newName) && GUI.GetNameOfFocusedControl() != "@ChooseName")
            {
                GUI.color = new Color(1, 1, 1, 0.3f);
                GUI.Label(rectName, LocalizedStrings.EnterYourName, BlueStonez.label_interparkmed_11pt);
                GUI.color = Color.white;
            }

            if (GUITools.Button(new Rect(groupRect.width - 118, 240 - 80, 110, 32), new GUIContent(LocalizedStrings.CancelCaps), BlueStonez.button))
            {
                Hide();
            }

            GUI.enabled = !isChangingName;
            if (GUITools.Button(new Rect(groupRect.width - 230, 240 - 80, 110, 32), new GUIContent(LocalizedStrings.OkCaps), BlueStonez.button_green))
            {
                if (!newName.Equals(oldName) && !string.IsNullOrEmpty(newName))
                {
                    isChangingName = true;
                    UberStrike.WebService.Unity.UserWebServiceClient.ChangeMemberName(PlayerDataManager.Instance.ServerLocalPlayerMemberView.PublicProfile.Cmid, newName, ApplicationDataManager.CurrentLocaleString, SystemInfo.deviceUniqueIdentifier,
                        t =>
                        {
                            switch (t)
                            {
                                case MemberOperationResult.InvalidName:
                                    PopupSystem.ShowMessage(LocalizedStrings.Error, LocalizedStrings.NameInvalidCharsMsg);
                                    break;

                                case MemberOperationResult.DuplicateName:
                                    PopupSystem.ShowMessage(LocalizedStrings.Error, LocalizedStrings.NameInUseMsg);
                                    break;

                                case MemberOperationResult.OffensiveName:
                                    PopupSystem.ShowMessage(LocalizedStrings.Error, LocalizedStrings.OffensiveNameMsg);
                                    break;

                                case MemberOperationResult.Ok:
                                    // Change name in game
                                    PlayerDataManager.NameSecure = newName;
                                    StartCoroutine(ItemManager.Instance.StartGetInventory(false));
                                    CommConnectionManager.CommCenter.SendUpdatedActorInfo();
                                    Hide();
                                    break;

                                default:
                                    PopupSystem.ShowMessage(LocalizedStrings.Error, LocalizedStrings.Unknown);
                                    break;
                            }
                            isChangingName = false;
                        },
                        (ex) =>
                        {
                            isChangingName = false;
                            Hide();

                            DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace, "There was a problem. Please try again later.");
                        }
                    );
                }
            }
        }
        GUI.EndGroup();
        GUI.enabled = true;

        if (isChangingName == true)
        {
            WaitingTexture.Draw(new Vector2(groupRect.x + 305, groupRect.y + 114));
        }

        GuiManager.DrawTooltip();
    }

    public override void Show()
    {
        base.Show();

        nameChangeItem = ItemManager.Instance.GetItemInShop(CommonConfig.NameChangeItem);

        oldName = PlayerDataManager.Instance.ServerLocalPlayerMemberView.PublicProfile.Name;
        newName = oldName;
    }
}
