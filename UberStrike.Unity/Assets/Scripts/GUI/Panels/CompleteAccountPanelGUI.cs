using System.Collections.Generic;
using UberStrike.DataCenter.Common.Entities;
using UberStrike.WebService.Unity;
using UnityEngine;
using Cmune.Util;
using UberStrike.Helper;
using UberStrike.Core.Types;

public class CompleteAccountPanelGUI : PanelGuiBase
{
    #region Fields

    private string _characterName = string.Empty;

    private const int MAX_CHARACTER_NAME_LENGTH = 18;
    private string _leftCharacterNameLength = MAX_CHARACTER_NAME_LENGTH.ToString();

    private const float NORMAL_HEIGHT = 260;
    private const float EXTENDED_HEIGHT = 330;

    private float _height = NORMAL_HEIGHT;
    private float _targetHeight = NORMAL_HEIGHT;

    private bool _checkButtonClicked;
    private string _errorMessage = string.Empty;
    private Dictionary<int, string> _errorMessages;
    private List<string> _availableNames;
    private int _selectedIndex = -1;
    private bool _waitingForWsReturn = false;
    private Color _feedbackMessageColor = Color.white;

    #endregion

    private void Awake()
    {
        _availableNames = new List<string>();
        _errorMessages = new Dictionary<int, string>();

        _errorMessages.Add(UberStrikeAccountCompletionResult.AlreadyCompletedAccount, "");
        _errorMessages.Add(UberStrikeAccountCompletionResult.DuplicateName, "");
        _errorMessages.Add(UberStrikeAccountCompletionResult.InvalidData, "");
        _errorMessages.Add(UberStrikeAccountCompletionResult.InvalidName, "");
        _errorMessages.Add(UberStrikeAccountCompletionResult.IsIpBanned, LocalizedStrings.YourAccountHasBeenBanned);
    }

    private void Update()
    {
        if (_height != _targetHeight)
        {
            _height = Mathf.Lerp(_height, _targetHeight, Time.deltaTime * 5);

            if (Mathf.Approximately(_height, _targetHeight))
                _height = _targetHeight;
        }

        _leftCharacterNameLength = (MAX_CHARACTER_NAME_LENGTH - _characterName.Length).ToString();
    }

    private void OnGUI()
    {
        float width = 400;

        GUI.depth = (int)GuiDepth.Popup;

        Rect position = new Rect((Screen.width - width) * 0.5f, (Screen.height - _height) * 0.5f, width, _height);

        GUI.BeginGroup(position, GUIContent.none, BlueStonez.window);
        {
            GUI.Label(new Rect(0, 0, position.width, 56), LocalizedStrings.ChooseCharacterName, BlueStonez.tab_strip);

            Rect rect = new Rect(20, 55, position.width - 40, position.height - 76);
            GUI.Label(rect, GUIContent.none, BlueStonez.window_standard_grey38);
            GUI.BeginGroup(rect);
            {
                GUI.Label(new Rect(10, 8, rect.width - 20, 40),
                    "Please choose your character name.\nThis is the name that will be displayed to other players in game.",
                    BlueStonez.label_interparkbold_11pt);

                GUI.color = new Color(1, 1, 1, 0.3f);
                GUI.Label(new Rect(20, 66, 12, 11), _leftCharacterNameLength, BlueStonez.label_interparkmed_11pt_right);
                GUI.color = Color.white;

                GUI.enabled = !_waitingForWsReturn;

                GUI.changed = false;
                GUI.SetNextControlName("@Name");
                _characterName = GUI.TextField(new Rect(40, 60, 180, 22), _characterName, MAX_CHARACTER_NAME_LENGTH, BlueStonez.textField);
                _characterName = _characterName.Trim(new char[] { '\n', '\t' });

                if (GUI.changed)
                {
                    _selectedIndex = -1;
                    _checkButtonClicked = false;
                }

                if (string.IsNullOrEmpty(_characterName) && GUI.GetNameOfFocusedControl() != "@Name")
                {
                    GUI.color = new Color(1, 1, 1, 0.3f);
                    GUI.Label(new Rect(85, 67, 180, 22), LocalizedStrings.EnterYourName, BlueStonez.label_interparkmed_11pt_left);
                    GUI.color = Color.white;
                }

                GUI.enabled = true;

                DrawCheckAvailabilityButton(rect);

                if (_waitingForWsReturn)
                {
                    GUI.contentColor = Color.gray;
                    GUI.Label(new Rect(165, 100, 100, 20), LocalizedStrings.PleaseWait, BlueStonez.label_interparkbold_11pt_left);
                    GUI.contentColor = Color.white;

                    WaitingTexture.Draw(new Vector2(140, 110));
                }
                else
                {
                    GUI.contentColor = _feedbackMessageColor;
                    GUI.Label(new Rect(0, 100, rect.width, 20), _errorMessage, BlueStonez.label_interparkbold_11pt);
                    GUI.contentColor = Color.white;
                }

                DrawAvailableNames(new Rect(0, 120, rect.width, rect.height - 162));

                DrawOKButton(rect);
            }
            GUI.EndGroup();
        }
        GUI.EndGroup();
    }

    private void DrawCheckAvailabilityButton(Rect position)
    {
        GUI.enabled = !string.IsNullOrEmpty(_characterName) && !_checkButtonClicked && !_waitingForWsReturn;

        if (GUITools.Button(new Rect(225, 60, 110, 24), new GUIContent("Check Availability"), BlueStonez.buttondark_small))
        {
            _availableNames.Clear();
            _checkButtonClicked = true;
            _targetHeight = NORMAL_HEIGHT;

            if (!ValidationUtilities.IsValidMemberName(_characterName, ApplicationDataManager.CurrentLocaleString))
            {
                _feedbackMessageColor = Color.red;
                _errorMessage = "'" + _characterName + "' is not a valid name!";
            }
            else
            {
                _waitingForWsReturn = true;

                UserWebServiceClient.IsDuplicateMemberName(_characterName,
                    IsDuplicatedNameCallback,
                    (ex) =>
                    {
                        _waitingForWsReturn = false;
                        _feedbackMessageColor = Color.red;
                        _errorMessage = "Our server had an error, please try again.";
                        DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace);
                    });
            }
        }

        GUI.enabled = true;
    }

    private void DrawOKButton(Rect position)
    {
        GUI.enabled = !_waitingForWsReturn && !string.IsNullOrEmpty(_characterName);// && (!_nameIsDuplicated || _selectedIndex != -1);

        if (GUITools.Button(new Rect((position.width - 120) / 2, position.height - 42, 120, 32), new GUIContent(LocalizedStrings.OkCaps), BlueStonez.button_green))
        {
            string name = _characterName;

            if (_selectedIndex != -1)
                name = _availableNames[_selectedIndex];

            _waitingForWsReturn = true;
            AuthenticationWebServiceClient.CompleteAccount(PlayerDataManager.CmidSecure, name, ApplicationDataManager.Channel, ApplicationDataManager.CurrentLocaleString, SystemInfo.deviceUniqueIdentifier,
                (ev) => CompleteAccountCallback(ev, name),
                (ex) =>
                {
                    _waitingForWsReturn = false;
                    _feedbackMessageColor = Color.red;
                    _errorMessage = "Webservice error";

                    DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace, "There was a problem. Please try again later.");
                });
        }

        GUI.enabled = true;
    }

    private void DrawAvailableNames(Rect position)
    {
        if (_availableNames.Count == 0) return;

        GUI.BeginGroup(position);
        {
            GUI.Label(new Rect(0, 0, position.width, 20), "Here are some suggestions", BlueStonez.label_interparkbold_11pt);

            GUI.enabled = !_waitingForWsReturn;

            for (int i = 0; i < _availableNames.Count; i++)
            {
                if (GUI.Toggle(new Rect(94, 24 + i * 20, position.width, 18), (i == _selectedIndex), _availableNames[i], BlueStonez.radiobutton))
                    _selectedIndex = i;
            }

            GUI.enabled = true;
        }
        GUI.EndGroup();
    }

    private void IsDuplicatedNameCallback(bool isDuplicate)
    {
        if (isDuplicate)
        {
            UserWebServiceClient.GenerateNonDuplicatedMemberNames(_characterName,
            GetNonDuplicatedNamesCallback,
            (ex) =>
            {
                _waitingForWsReturn = false;
                DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace, "There was a problem. Please try again later.");
            });
        }
        else
        {
            _waitingForWsReturn = false;
            _feedbackMessageColor = Color.green;
            _errorMessage = "'" + _characterName + "' is available!";
        }
    }

    private void GetNonDuplicatedNamesCallback(List<string> names)
    {
        _selectedIndex = -1;
        _targetHeight = EXTENDED_HEIGHT;

        _waitingForWsReturn = false;
        _feedbackMessageColor = Color.red;
        _errorMessage = "'" + _characterName + "' is already taken!";

        _availableNames.Clear();
        _availableNames.AddRange(names);
    }

    private void CompleteAccountCallback(AccountCompletionResultView result, string name)
    {
        _selectedIndex = -1;
        _waitingForWsReturn = false;

        switch (result.Result)
        {
            case UberStrikeAccountCompletionResult.Ok:
                {
                    Hide();

                    if (GameState.LocalDecorator)
                        GameState.LocalDecorator.HudInformation.SetAvatarLabel(name);

                    //update player data
                    PlayerDataManager.NameSecure = name;
                    StartCoroutine(ItemManager.Instance.StartGetInventory(false));
                    CommConnectionManager.CommCenter.SendUpdatedActorInfo();

                    //start home screen
                    MenuPageManager.Instance.LoadPage(PageType.Home);
                    GlobalUIRibbon.IsVisible = true;
                    GlobalUIRibbon.Instance.Show();

                    //show presents
                    ApplicationDataManager.Instance.ShowAttributedItems(result.ItemsAttributed);

                    if (GameState.HasCurrentGame)
                    {
                        GameStateController.Instance.UnloadGameMode();
                        GameState.LocalPlayer.SetPlayerControlState(LocalPlayer.PlayerState.None);
                    }
                }
                break;

            case UberStrikeAccountCompletionResult.DuplicateName:
                {
                    GetNonDuplicatedNamesCallback(result.NonDuplicateNames);
                }
                break;

            case UberStrikeAccountCompletionResult.IsIpBanned:
                {
                    _feedbackMessageColor = Color.red;
                    _errorMessage = LocalizedStrings.YourAccountHasBeenBanned;
                }
                break;
            case UberStrikeAccountCompletionResult.InvalidName:
                {
                    _feedbackMessageColor = Color.red;
                    _errorMessage = LocalizedStrings.NameInvalidCharsMsg;
                }
                break;

            case UberStrikeAccountCompletionResult.AlreadyCompletedAccount:
                {
                    Hide();

                    MenuPageManager.Instance.LoadPage(PageType.Home);
                    GlobalUIRibbon.IsVisible = true;
                    GlobalUIRibbon.Instance.Show();
                }
                break;
        }
    }
}