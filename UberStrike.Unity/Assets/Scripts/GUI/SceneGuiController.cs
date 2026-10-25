using System.Collections;
using Cmune.Util;
using UnityEngine;

class SceneGuiController : MonoBehaviour
{
    private void Awake()
    {
        // chat tab on every in-game panel (pregame, death/esc, end of match)
        _includeInGameChat = GetComponent<ShopPageGUI>();
        _hideTitleWhenAlive = GetComponent<ShopPageScene>();
        if (_hideTitleWhenAlive)
        {
            _deathShop = this;
            // esc/death panel like steam: Loadout & Shop | Chat
            var keep = new System.Collections.Generic.List<PageGUI>();
            foreach (PageGUI p in _guiPages)
            {
                if (p is PregameLoadoutPageGUI) p.enabled = false;
                else keep.Add(p);
            }
            _guiPages = keep.ToArray();
        }
        if (_includeInGameChat)
        {
            GameObject chatObject = new GameObject("InGameChat");
            chatObject.SetActive(false);
            chatObject.transform.parent = transform;
            InGameChatPageGUI chat = chatObject.AddComponent<InGameChatPageGUI>();
            chat.enabled = false;
            chatObject.SetActive(true);
            PageGUI[] pages = new PageGUI[_guiPages.Length + 1];
            _guiPages.CopyTo(pages, 0);
            pages[_guiPages.Length] = chat;
            _guiPages = pages;
        }
        _guiPageAnim = new FloatAnim((float oldValue, float newValue) =>
        {
            _offset = newValue;
            CameraRectController.Instance.Width = (Screen.width - _rect.width + _offset) / Screen.width;
        });

        for (int i = 0; i < _guiPages.Length; i++)
        {
            _guiPages[i].IsOnGUIEnabled = false;
        }

        _guiPageTabs = new GUIContent[_guiPages.Length];
        for (int i = 0; i < _guiPages.Length; i++)
        {
            _guiPageTabs[i] = new GUIContent(_guiPages[i].Title);
        }
    }

    private void OnEnable()
    {
        if (_guiPages.Length > 0)
        {
            SetCurrentPage(0);
            _closing = false;
            _returnIndex = 0;
            if (_includeInGameChat) _open = this;
            _guiPageAnim.Value = _rect.width;
            _guiPageAnim.AnimTo(0.0f, 0.5f, EaseType.In);
        }
        GameState.IsReadyForNextGame = false;
        CmuneEventHandler.AddListener<ScreenResolutionEvent>(OnScreenResolutionChange);
    }

    private void OnDisable()
    {
        if (_currentGuiPageIndex >= 0 && _currentGuiPageIndex < _guiPages.Length && _guiPages[_currentGuiPageIndex] != null)
        {
            _guiPages[_currentGuiPageIndex].enabled = false;
        }
        MonoRoutine.Start(resetGuiPage());
        _currentGuiPageIndex = -1;
        if (_open == this) _open = null;
        CmuneEventHandler.RemoveListener<ScreenResolutionEvent>(OnScreenResolutionChange);
    }

    private IEnumerator resetGuiPage()
    {
        // there is a bug with Unity's Screen.width, when called in OnDisable, it's not correct.
        yield return new WaitForEndOfFrame();
        _guiPageAnim.Value = _rect.width;
    }

    public void Update()
    {
        if (_guiPageAnim.IsAnimating)
        {
            _guiPageAnim.Update();
        }
        else if (_closing)
        {
            _closing = false;
            GamePageManager.Instance.UnloadCurrentPage();
        }
    }

    private void OnGUI()
    {
        Matrix4x4 scaleMatrix = MobileMenuScale.Begin();

        GUI.depth = (int)GuiDepth.Page;

        _rect.y = GlobalUIRibbon.Instance.GetHeight();
        //anchor against the virtual (scaled) screen
        _rect.height = MobileMenuScale.VirtualHeight - _rect.y;
        _rect.x = MobileMenuScale.VirtualWidth - _rect.width + _offset;

        GUI.skin = BlueStonez.Skin;

        for (int i = 0; i < _guiPages.Length; i++)
        {
            _guiPages[i].IsOnGUIEnabled = false;
        }

        if (GUITools.Button(new Rect(_rect.x - 44, _rect.y + 4, 40, 40), new GUIContent("X"), BlueStonez.buttondark_medium))
        {
            SlideOut();
        }

        GUI.BeginGroup(_rect, GUIContent.none, BlueStonez.window_standard_grey38);
        {
            string title = _hideTitleWhenAlive && GameState.HasCurrentPlayer && GameState.LocalCharacter.IsAlive ? string.Empty : _title;
            GUI.Label(new Rect(0, 0, _rect.width, 56), title, BlueStonez.tab_strip);

            GUI.changed = false;
            int selected = GUI.SelectionGrid(new Rect(0, 34, 140 * _guiPageTabs.Length, 22), _currentGuiPageIndex,
                _guiPageTabs, _guiPageTabs.Length, BlueStonez.tab_medium);

            if (GUI.changed)
            {
                SetCurrentPage(selected);
            }
        }
        GUI.EndGroup();
        _guiPages[_currentGuiPageIndex].DrawGUI(new Rect(_rect.x, _rect.y + 57, _rect.width, _rect.height - 56));

        MobileMenuScale.End(scaleMatrix);

        GuiManager.DrawTooltip();
    }

    // chat button = toggle. panel open -> its chat tab; chat already shown -> esc/death slide out, pregame back to last tab
    public static void OpenChat()
    {
        if (!GameState.HasCurrentGame) return;
        if (_open)
        {
            int chat = _open._guiPages.Length - 1;
            if (_open._currentGuiPageIndex != chat)
            {
                _open._returnIndex = _open._currentGuiPageIndex;
                _open.SetCurrentPage(chat);
            }
            else if (_open._hideTitleWhenAlive)
                _open.SlideOut();
            else
                _open.SetCurrentPage(_open._returnIndex);
            return;
        }
        GameState.LocalPlayer.Pause();
        GamePageManager.Instance.LoadPage(PageType.Shop);
        SceneGuiController target = _open ? _open : _deathShop;
        if (target) target.SetCurrentPage(target._guiPages.Length - 1);
    }

    private void SlideOut()
    {
        if (_closing) return;
        _closing = true;
        _guiPageAnim.AnimTo(_rect.width, 0.3f, EaseType.Out);
    }

    private void SetCurrentPage(int index)
    {
        if (_currentGuiPageIndex == index) return;
        for (int i = 0; i < _guiPages.Length; i++)
        {
            _guiPages[i].IsOnGUIEnabled = false;
            if (i != index)
            {
                _guiPages[i].enabled = false;
            }
        }
        if (index >= 0 && index < _guiPages.Length)
        {
            _currentGuiPageIndex = index;
            _guiPages[_currentGuiPageIndex].enabled = true;
        }
    }

    private void OnScreenResolutionChange(ScreenResolutionEvent ev)
    {
        CameraRectController.Instance.Width = (Screen.width - _rect.width + _offset) / Screen.width;
    }

    [SerializeField]
    private string _title;
    [SerializeField]
    private PageGUI[] _guiPages;
    [SerializeField]
    private Rect _rect;

    private static SceneGuiController _open;
    private static SceneGuiController _deathShop;
    private bool _includeInGameChat;
    private bool _hideTitleWhenAlive;
    private bool _closing;
    private int _returnIndex;
    private GUIContent[] _guiPageTabs;
    private float _offset;
    private int _currentGuiPageIndex = -1;
    private FloatAnim _guiPageAnim;
}