using Cmune.DataCenter.Common.Entities;
using UnityEngine;
using UberStrike.Helper;

public class LoginPanelGUI : PanelGuiBase
{
    [SerializeField]
    private Texture panelQuadFullscreenOn;
    [SerializeField]
    private Texture panelQuadFullscreenOff;
    [SerializeField]
    private Texture panelQuadSoundOn;
    [SerializeField]
    private Texture panelQuadSoundOff;

    private Rect _rect;
    private string _emailAddress = string.Empty;
    private string _password = string.Empty;
    private bool _rememberPassword = false;
    private float _errorAlpha;

    private TouchScreenKeyboard _loginKeyboard;
    private TouchScreenKeyboard _passwordKeyboard;

    public static string ErrorMessage { get; set; }
    public static bool IsBanned { get; set; }

    private void Start()
    {
        _rememberPassword = CmunePrefs.ReadKey<bool>(CmunePrefs.Key.Player_AutoLogin);
        if (_rememberPassword)
        {
            _password = CmunePrefs.ReadKey<string>(CmunePrefs.Key.Player_Password);
            _emailAddress = CmunePrefs.ReadKey<string>(CmunePrefs.Key.Player_Email);
        }
    }

    public override void Hide()
    {
        base.Hide();
        _errorAlpha = 0.0f;
        ErrorMessage = string.Empty;
    }

    public override void Show()
    {
        base.Show();
        if (IsBanned) ErrorMessage = LocalizedStrings.YourAccountHasBeenBanned;
        if (!string.IsNullOrEmpty(ErrorMessage)) _errorAlpha = 1.0f;
    }

    private void Update()
    {
        if (ApplicationDataManager.IsMobile)
        {
            if (_loginKeyboard != null)
            {
                if (_loginKeyboard.done)
                {
                    _emailAddress = _loginKeyboard.text;
                    _loginKeyboard = null;
                }
                else if (!_loginKeyboard.active)
                {
                    _loginKeyboard = null;
                }
            }

            if (_passwordKeyboard != null)
            {
                if (_passwordKeyboard.done)
                {
                    _password = _passwordKeyboard.text;
                    _passwordKeyboard = null;
                }
                else if (!_passwordKeyboard.active)
                {
                    _passwordKeyboard = null;
                }
            }
        }

        // Remove CRLF from email address
        if (!string.IsNullOrEmpty(_emailAddress))
        {
            _emailAddress = _emailAddress.Replace("\n", "").Replace("\t", "");
        }

        // Remove CRLF from password
        if (!string.IsNullOrEmpty(_password))
        {
            _password = _password.Replace("\n", "").Replace("\t", "");
        }

        if (_errorAlpha > 0.0f)
            _errorAlpha -= Time.deltaTime * 0.1f;
    }

    private void OnGUI()
    {
        _rect = new Rect((Screen.width - 334) / 2, (Screen.height - 200) / 2, 334, 200);

        DrawLoginPanel();

        if (!string.IsNullOrEmpty(GUI.tooltip))
        {
            Matrix4x4 currentMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.identity;
            Vector2 tipSize = BlueStonez.tooltip.CalcSize(new GUIContent(GUI.tooltip));
            Rect position = new Rect(Mathf.Clamp(Event.current.mousePosition.x, 14, Screen.width - (tipSize.x + 14)), Event.current.mousePosition.y + 24, tipSize.x, tipSize.y + 16);
            GUI.Label(position, GUI.tooltip, BlueStonez.tooltip);
            GUI.matrix = currentMatrix;
        }
    }


    private void DrawLoginPanel()
    {
        GUI.BeginGroup(_rect, GUIContent.none, BlueStonez.window);

        // Player pressed enter, try to login
        if (!(string.IsNullOrEmpty(_emailAddress) || string.IsNullOrEmpty(_password)))
        {
            if (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.KeypadEnter || Event.current.keyCode == KeyCode.Return))
            {
                Login(_emailAddress, _password, _rememberPassword);
            }
        }

        GUI.depth = (int)GuiDepth.Panel;

        GUI.Label(new Rect(0, 0, _rect.width, 23), "WELCOME TO UBERSTRIKE", BlueStonez.tab_strip);

        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            GUI.contentColor = ColorScheme.UberStrikeYellow.SetAlpha(_errorAlpha);
            GUI.Label(new Rect(8, 30, _rect.width - 16, 23), ErrorMessage, BlueStonez.label_interparkmed_11pt);
            GUI.contentColor = Color.white;
        }
#if !UNITY_EDITOR
        if (ApplicationDataManager.IsMobile)
        {
            if (GUI.Button(new Rect(8, 64, 220, 24), new GUIContent(_emailAddress), BlueStonez.textField))
            {
                _loginKeyboard = TouchScreenKeyboard.Open(_emailAddress, TouchScreenKeyboardType.EmailAddress, false, false, false, false);
            }
            string maskedPassword = "".PadLeft(_password.Length, '*');
            if (GUI.Button(new Rect(8, 92, 220, 24), new GUIContent(maskedPassword), BlueStonez.textField))
            {
                _passwordKeyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default, false, false, true, false);
            }
        }
        else
        {
#endif
            // Email Address
            _emailAddress = GUI.TextField(new Rect(8, 64, 220, 24), _emailAddress, CommonConfig.MemberEmailMaxLength, BlueStonez.textField);
            if (string.IsNullOrEmpty(_emailAddress))
            {
                GUI.color = Color.white.SetAlpha(0.3f);
                GUI.Label(new Rect(8, 64, 200, 24), "  " + LocalizedStrings.Email, BlueStonez.label_interparkbold_13pt_left);
                GUI.color = Color.white;
            }

            // Password
            _password = GUI.PasswordField(new Rect(8, 92, 220, 24), _password, '*', CommonConfig.MemberPasswordMaxLength, BlueStonez.textField);
            if (string.IsNullOrEmpty(_password))
            {
                GUI.color = Color.white.SetAlpha(0.3f);
                GUI.Label(new Rect(8, 92, 200, 24), "  " + LocalizedStrings.Password, BlueStonez.label_interparkbold_13pt_left);
                GUI.color = Color.white;
            }
#if !UNITY_EDITOR
        }
#endif

        GUI.color = Color.white.SetAlpha(0.7f);

        // Remember password
        GUI.Label(new Rect(8, 120, 108, 24), GUIContent.none, BlueStonez.buttondark_small);
        _rememberPassword = GUI.Toggle(new Rect(12, 124, 100, 24), _rememberPassword, LocalizedStrings.RememberMe, BlueStonez.toggle);

        // Forgot Password
        if (GUI.Button(new Rect(120, 120, 108, 24), new GUIContent("Forgot Password?", "Did you forget your password?\nFear not, we can resend it via email!"), BlueStonez.buttondark_small))
        {
            switch (ApplicationDataManager.BuildType)
            {
                case BuildType.Dev:
                    ApplicationDataManager.OpenUrl(string.Empty, "http://dev.uberstrike.com/Account/RecoverPassword");
                    break;
                case BuildType.Staging:
                    ApplicationDataManager.OpenUrl(string.Empty, "http://qa.uberstrike.com/Account/RecoverPassword");
                    break;
                case BuildType.Prod:
                    ApplicationDataManager.OpenUrl(string.Empty, "http://uberstrike.cmune.com/Account/RecoverPassword");
                    break;
            }
        }

        GUI.color = Color.white;

        // Login 
        GUI.enabled = !(string.IsNullOrEmpty(_emailAddress) || string.IsNullOrEmpty(_password));
        if (GUITools.Button(new Rect(236, 64, 90, 52), new GUIContent("SIGN IN"), BlueStonez.button_green))
        {
            Login(_emailAddress, _password, _rememberPassword);
        }
        GUI.enabled = true;

        // Horizontal rule
        GUI.Label(new Rect(8, 150, _rect.width - 16, 8), GUIContent.none, BlueStonez.horizontal_line_grey95);

        // I'm new to UberStrike
        if (GUITools.Button(new Rect(8, 160, 152, 30), new GUIContent("Create Account", "Create a brand new account."), BlueStonez.buttondark_medium))
        {
            Hide();
            PanelManager.Instance.OpenPanel(PanelType.Signup);
        }

#if !UNITY_ANDROID && !UNITY_IPHONE
        // I play UberStrike on Facebook
        if (GUITools.Button(new Rect(178, 160, 152, 30), new GUIContent("", "If you already play UberStrike on Facebook,\nget your email and password set up."), BlueStonez.button_fbconnect))
        {
            ApplicationDataManager.Instance.OpenLinkFacebookUrl();
        }
#endif

        DrawMiniButtons();

        GUI.EndGroup();
    }

    private void DrawMiniButtons()
    {
#if !UNITY_ANDROID && !UNITY_IPHONE
        if (!Application.isWebPlayer)
        {
            // Mute
            if (GUITools.Button(new Rect(_rect.width - 57, 9, 16, 16), ApplicationDataManager.ApplicationOptions.AudioEnabled ? new GUIContent(panelQuadSoundOn, LocalizedStrings.Mute) : new GUIContent(panelQuadSoundOff, LocalizedStrings.Unmute), BlueStonez.panelquad_button))
            {
                ApplicationDataManager.ApplicationOptions.AudioEnabled = !ApplicationDataManager.ApplicationOptions.AudioEnabled;
                SfxManager.EnableAudio(ApplicationDataManager.ApplicationOptions.AudioEnabled);
                ApplicationDataManager.ApplicationOptions.SaveApplicationOptions();
            }

            // Fullscreen
            if (GUITools.Button(new Rect(_rect.width - 41, 9, 16, 16), Screen.fullScreen ? new GUIContent(panelQuadFullscreenOff, LocalizedStrings.ExitFullscreen) : new GUIContent(panelQuadFullscreenOn, LocalizedStrings.GoFullscreen), BlueStonez.panelquad_button))
            {
                if (Screen.fullScreen)
                    ScreenResolutionManager.SetTwoMinusMaxResolution();
                else
                    ScreenResolutionManager.SetFullScreenMaxResolution();
            }

            // Quit
            if (GUITools.Button(new Rect(_rect.width - 25, 9, 16, 16), new GUIContent("x"), BlueStonez.panelquad_button))
            {
                Application.Quit();
            }
        }
#endif
    }

    private void Login(string emailAddress, string password, bool remember)
    {
        _errorAlpha = 1.0f;
        if (string.IsNullOrEmpty(emailAddress))
        {
            ErrorMessage = LocalizedStrings.EnterYourEmailAddress;
        }
        else if (string.IsNullOrEmpty(password))
        {
            ErrorMessage = LocalizedStrings.EnterYourPassword;
        }
        else if (!ValidationUtilities.IsValidEmailAddress(emailAddress))
        {
            ErrorMessage = LocalizedStrings.EmailAddressIsInvalid;
        }
        else if (!ValidationUtilities.IsValidPassword(password))
        {
            ErrorMessage = LocalizedStrings.PasswordIsInvalid;
        }
        else
        {
            Hide();
            AuthenticationManager.Instance.AuthenticateMember(emailAddress, password);
            CmunePrefs.WriteKey<bool>(CmunePrefs.Key.Player_AutoLogin, remember);
        }
    }
}