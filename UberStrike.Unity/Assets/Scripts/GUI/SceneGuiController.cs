using System.Collections;
using Cmune.Util;
using UnityEngine;

class SceneGuiController : MonoBehaviour
{
    private void Awake()
    {
        _includeInGameChat = GetComponent<ShopPageScene>() && GetComponent<PregameLoadoutPageGUI>();
        if (_includeInGameChat)
        {
            _inGameController = this;
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
    }

    private void OnGUI()
    {
        GUI.depth = (int)GuiDepth.Page;

        _rect.y = GlobalUIRibbon.Instance.GetHeight();
        _rect.height = Screen.height - _rect.y;
        _rect.x = Screen.width - _rect.width + _offset;

        GUI.skin = BlueStonez.Skin;
        GUI.BeginGroup(_rect, GUIContent.none, BlueStonez.window_standard_grey38);
        {
            string title = _includeInGameChat && GameState.HasCurrentPlayer && GameState.LocalCharacter.IsAlive ? string.Empty : _title;
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

        GuiManager.DrawTooltip();
    }

    public static void OpenChat()
    {
        if (!GameState.HasCurrentGame) return;
        GameState.LocalPlayer.Pause();
        GamePageManager.Instance.LoadPage(PageType.Shop);
        if (_inGameController) _inGameController.SetCurrentPage(_inGameController._guiPages.Length - 1);
    }

    private void SetCurrentPage(int index)
    {
        if (_currentGuiPageIndex == index) return;
        for (int i = 0; i < _guiPages.Length; i++)
        {
            _guiPages[i].IsOnGUIEnabled = false;
            _guiPages[i].enabled = false;
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

    private static SceneGuiController _inGameController;
    private bool _includeInGameChat;
    private GUIContent[] _guiPageTabs;
    private float _offset;
    private int _currentGuiPageIndex = -1;
    private FloatAnim _guiPageAnim;
}