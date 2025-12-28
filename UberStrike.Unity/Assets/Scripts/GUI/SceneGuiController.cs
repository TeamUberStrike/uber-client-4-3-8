using System.Collections;
using Cmune.Util;
using UnityEngine;

class SceneGuiController : MonoBehaviour
{
    private void Awake()
    {
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
        if (_guiPages[_currentGuiPageIndex] != null)
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
            GUI.Label(new Rect(0, 0, _rect.width, 56), _title, BlueStonez.tab_strip);

            GUI.changed = false;
            _currentGuiPageIndex = GUI.SelectionGrid(new Rect(0, 34, 140 * _guiPageTabs.Length, 22), _currentGuiPageIndex,
                _guiPageTabs, _guiPageTabs.Length, BlueStonez.tab_medium);

            if (GUI.changed)
            {
                SetCurrentPage(_currentGuiPageIndex);
                return;
            }
        }
        GUI.EndGroup();
        _guiPages[_currentGuiPageIndex].DrawGUI(new Rect(_rect.x, _rect.y + 57, _rect.width, _rect.height - 56));

        GuiManager.DrawTooltip();
    }

    private void SetCurrentPage(int index)
    {
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

    private GUIContent[] _guiPageTabs;
    private float _offset;
    private int _currentGuiPageIndex;
    private FloatAnim _guiPageAnim;
}