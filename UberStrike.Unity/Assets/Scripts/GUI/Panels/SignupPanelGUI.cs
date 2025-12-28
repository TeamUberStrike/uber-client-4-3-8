using System.Collections.Generic;
using Cmune.DataCenter.Common.Entities;
using UberStrike.WebService.Unity;
using UnityEngine;
using UberStrike.Helper;

public class SignupPanelGUI : PanelGuiBase
{
    private string _emailAddress = string.Empty;
    private string _password1 = string.Empty;
    private string _password2 = string.Empty;
    private string _errorMessage = string.Empty;

    private Color _errorMessageColor = Color.red;

    private Dictionary<MemberRegistrationResult, string> _errorMessages;

    private bool _enableGUI = true;

    private TouchScreenKeyboard _emailKeyboard;
    private TouchScreenKeyboard _password1Keyboard;
    private TouchScreenKeyboard _password2Keyboard;

    private const float NORMAL_HEIGHT = 300;
    private const float EXTENDED_HEIGHT = 340;

    private float _height = NORMAL_HEIGHT;
    private float _targetHeight = NORMAL_HEIGHT;

    private void Awake()
    {
        _errorMessages = new Dictionary<MemberRegistrationResult, string>();
    }

    private void Start()
    {
        _errorMessages.Add(MemberRegistrationResult.DuplicateEmail, LocalizedStrings.EmailAddressInUseMsg);
        _errorMessages.Add(MemberRegistrationResult.DuplicateEmailName, LocalizedStrings.EmailAddressAndNameInUseMsg);
        _errorMessages.Add(MemberRegistrationResult.DuplicateHandle, LocalizedStrings.NameInUseMsg);
        _errorMessages.Add(MemberRegistrationResult.DuplicateName, LocalizedStrings.NameInUseMsg);
        _errorMessages.Add(MemberRegistrationResult.InvalidData, LocalizedStrings.InvalidData);
        _errorMessages.Add(MemberRegistrationResult.InvalidEmail, LocalizedStrings.EmailAddressIsInvalid);
        _errorMessages.Add(MemberRegistrationResult.InvalidEsns, LocalizedStrings.InvalidData + " (Esns)");
        _errorMessages.Add(MemberRegistrationResult.InvalidHandle, LocalizedStrings.InvalidData + " (Handle)");
        _errorMessages.Add(MemberRegistrationResult.InvalidName, LocalizedStrings.NameInvalidCharsMsg);
        _errorMessages.Add(MemberRegistrationResult.InvalidPassword, LocalizedStrings.PasswordIsInvalid);
        _errorMessages.Add(MemberRegistrationResult.IsIpBanned, "IP is banned");
        _errorMessages.Add(MemberRegistrationResult.MemberNotFound, "I can't find that member. Maybe he's hiding. In any case, you'll have to try again.");
        _errorMessages.Add(MemberRegistrationResult.OffensiveName, LocalizedStrings.OffensiveNameMsg);
    }

    private void Update()
    {
        if (ApplicationDataManager.IsMobile)
        {
            if (_emailKeyboard != null)
            {
                if (_emailKeyboard.done)
                {
                    _emailAddress = _emailKeyboard.text;
                    _emailKeyboard = null;
                }
                else if (!_emailKeyboard.active)
                {
                    _emailKeyboard = null;
                }
            }

            if (_password1Keyboard != null)
            {
                if (_password1Keyboard.done)
                {
                    _password1 = _password1Keyboard.text;
                    _password1Keyboard = null;
                }
                else if (!_password1Keyboard.active)
                {
                    _password1Keyboard = null;
                }
            }

            if (_password2Keyboard != null)
            {
                if (_password2Keyboard.done)
                {
                    _password2 = _password2Keyboard.text;
                    _password2Keyboard = null;
                }
                else if (!_password2Keyboard.active)
                {
                    _password2Keyboard = null;
                }
            }
        }

        if (_height != _targetHeight)
        {
            _height = Mathf.Lerp(_height, _targetHeight, 10 * Time.deltaTime);

            if (Mathf.Approximately(_height, _targetHeight))
                _height = _targetHeight;
        }
    }

    private void OnGUI()
    {
        Rect position = new Rect((Screen.width - 500) * 0.5f, (Screen.height - _height) * 0.5f, 500, _height);

        GUI.BeginGroup(position, GUIContent.none, BlueStonez.window);
        {
            GUI.Label(new Rect(0, 0, position.width, 56), LocalizedStrings.Welcome, BlueStonez.tab_strip);

            Rect rect = new Rect(20, 55, position.width - 40, position.height - 78);
            GUI.Label(rect, GUIContent.none, BlueStonez.window_standard_grey38);
            GUI.BeginGroup(rect);
            {
                GUI.Label(new Rect(0, 0, rect.width, 60), LocalizedStrings.PleaseProvideValidEmailPasswordMsg, BlueStonez.label_interparkbold_18pt);

                GUI.Label(new Rect(0, 140 - 64, 170, 11), LocalizedStrings.Email, BlueStonez.label_interparkbold_11pt_right);
                GUI.Label(new Rect(0, 174 - 64, 170, 11), LocalizedStrings.Password, BlueStonez.label_interparkbold_11pt_right);
                GUI.Label(new Rect(0, 211 - 64, 170, 11), LocalizedStrings.VerifyPassword, BlueStonez.label_interparkbold_11pt_right);

                GUI.enabled = _enableGUI;

#if !UNITY_EDITOR
                if (ApplicationDataManager.IsMobile)
                {
                    if (GUI.Button(new Rect(180, 133 - 64, 180, 22), _emailAddress, BlueStonez.textField)) {
                        _emailKeyboard = TouchScreenKeyboard.Open(_emailAddress, TouchScreenKeyboardType.EmailAddress, false, false, false, false);
                    }

                    if (string.IsNullOrEmpty(_emailAddress))
                    {
                        GUI.color = new Color(1, 1, 1, 0.3f);
                        GUI.Label(new Rect(188, 137 - 62, 180, 22), LocalizedStrings.EnterYourEmailAddress, BlueStonez.label_interparkmed_11pt_left);
                        GUI.color = Color.white;
                    }

                    string maskedPassword1 = "".PadLeft(_password1.Length, '*');
                    if (GUI.Button(new Rect(180, 168 - 64, 180, 22), maskedPassword1, BlueStonez.textField)) {
                        _password1Keyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default, false, false, true, false);
                    }

                    if (string.IsNullOrEmpty(_password1))
                    {
                        GUI.color = new Color(1, 1, 1, 0.3f);
                        GUI.Label(new Rect(188, 172 - 62, 172, 18), LocalizedStrings.EnterYourPassword, BlueStonez.label_interparkmed_11pt_left);
                        GUI.color = Color.white;
                    }

                    string maskedPassword2 = "".PadLeft(_password2.Length, '*');
                    if (GUI.Button(new Rect(180, 204 - 64, 180, 22), maskedPassword2, BlueStonez.textField))
                    {
                        _password2Keyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default, false, false, true, false);
                    }

                    if (string.IsNullOrEmpty(_password2))
                    {
                        GUI.color = new Color(1, 1, 1, 0.3f);
                        GUI.Label(new Rect(188, 208 - 62, 180, 22), LocalizedStrings.RetypeYourPassword, BlueStonez.label_interparkmed_11pt_left);
                        GUI.color = Color.white;
                    }
                }
                else
                {
#endif
                    GUI.SetNextControlName("@Email");
                    _emailAddress = GUI.TextField(new Rect(180, 133 - 64, 180, 22), _emailAddress, BlueStonez.textField);
                    if (string.IsNullOrEmpty(_emailAddress) && GUI.GetNameOfFocusedControl() != "@Email")
                    {
                        GUI.color = new Color(1, 1, 1, 0.3f);
                        GUI.Label(new Rect(188, 137 - 62, 180, 22), LocalizedStrings.EnterYourEmailAddress, BlueStonez.label_interparkmed_11pt_left);
                        GUI.color = Color.white;
                    }

                    GUI.SetNextControlName("@Password1");
                    _password1 = GUI.PasswordField(new Rect(180, 168 - 64, 180, 22), _password1, '*', BlueStonez.textField);
                    if (string.IsNullOrEmpty(_password1) && GUI.GetNameOfFocusedControl() != "@Password1")
                    {
                        GUI.color = new Color(1, 1, 1, 0.3f);
                        GUI.Label(new Rect(188, 172 - 62, 172, 18), LocalizedStrings.EnterYourPassword, BlueStonez.label_interparkmed_11pt_left);
                        GUI.color = Color.white;
                    }

                    GUI.SetNextControlName("@Password2");
                    _password2 = GUI.PasswordField(new Rect(180, 204 - 64, 180, 22), _password2, '*', BlueStonez.textField);
                    if (string.IsNullOrEmpty(_password2) && GUI.GetNameOfFocusedControl() != "@Password2")
                    {
                        GUI.color = new Color(1, 1, 1, 0.3f);
                        GUI.Label(new Rect(188, 208 - 62, 180, 22), LocalizedStrings.RetypeYourPassword, BlueStonez.label_interparkmed_11pt_left);
                        GUI.color = Color.white;
                    }
#if !UNITY_EDITOR
                 }
#endif

                GUI.enabled = true;

                /* Error Message */
                GUI.contentColor = _errorMessageColor;
                GUI.Label(new Rect(0, 175, rect.width, 40), _errorMessage, BlueStonez.label_interparkbold_11pt);
                GUI.contentColor = Color.white;
            }
            GUI.EndGroup();

            /* Terms of Service */
            GUI.Label(new Rect(100, position.height - 42 - 22, 300, 16), "By clicking OK you agree to the", BlueStonez.label_interparkbold_11pt);
            if (GUI.Button(new Rect(205, position.height - 30 - 22, 90, 20), "Terms of Service", BlueStonez.label_interparkbold_11pt))
            {
                ApplicationDataManager.OpenUrl("Terms Of Service", "http://www.cmune.com/index.php/terms-of-service/");
            }
            GUI.Label(new Rect(207, position.height - 15 - 22, 90, 20), GUIContent.none, BlueStonez.horizontal_line_grey95);

            GUI.enabled = _enableGUI;

            if (GUITools.Button(new Rect((position.width - 150), position.height - 42 - 22, 120, 32), new GUIContent(LocalizedStrings.OkCaps), BlueStonez.button_green))
            {
                if (!ValidationUtilities.IsValidEmailAddress(_emailAddress))
                {
                    _targetHeight = EXTENDED_HEIGHT;
                    _errorMessageColor = Color.red;
                    _errorMessage = LocalizedStrings.EmailAddressIsInvalid;
                }
                else if (_password1 != _password2)
                {
                    _targetHeight = EXTENDED_HEIGHT;
                    _errorMessageColor = Color.red;
                    _errorMessage = LocalizedStrings.PasswordDoNotMatch;
                }
                else if (!ValidationUtilities.IsValidPassword(_password1))
                {
                    _targetHeight = EXTENDED_HEIGHT;
                    _errorMessageColor = Color.red;
                    _errorMessage = LocalizedStrings.PasswordInvalidCharsMsg;
                }
                else
                {
                    _enableGUI = false;
                    _targetHeight = EXTENDED_HEIGHT;

                    _errorMessageColor = Color.green;
                    _errorMessage = LocalizedStrings.PleaseWait;

                    AuthenticationWebServiceClient.CreateUser(_emailAddress, _password1, ApplicationDataManager.Channel, ApplicationDataManager.CurrentLocaleString, SystemInfo.deviceUniqueIdentifier,
                        (result) =>
                        {
                            if (result == MemberRegistrationResult.Ok)
                            {
                                Hide();
                                AuthenticationManager.Instance.AuthenticateMember(_emailAddress, _password1);
                            }
                            else
                            {
                                _enableGUI = true;
                                _targetHeight = EXTENDED_HEIGHT;
                                _errorMessageColor = Color.red;
                                _errorMessages.TryGetValue(result, out _errorMessage);
                            }
                        },
                        (ex) =>
                        {
                            _enableGUI = true;
                            _targetHeight = NORMAL_HEIGHT;
                            _errorMessage = string.Empty;
                            ShowSignUpErrorPopup(LocalizedStrings.Error, "Sign Up was unsuccessful. There was an error communicating with the server.");
                        });
                }
            }

            /* Back to Login */
            if (GUITools.Button(new Rect(30, position.height - 42 - 22, 120, 32), new GUIContent(LocalizedStrings.BackCaps), BlueStonez.button))
            {
                Hide();

                PanelManager.Instance.OpenPanel(PanelType.Login);
            }

            GUI.enabled = true;
        }
        GUI.EndGroup();
    }

    private void ShowSignUpErrorPopup(string title, string message)
    {
        Hide();
        PopupSystem.ShowMessage(title, message, PopupSystem.AlertType.OK,
            () =>
            {
                LoginPanelGUI.ErrorMessage = string.Empty;
                Show();
            });
    }
}