using UberStrike.Realtime.Common;
using UnityEngine;

public class AvatarHudInformation : MonoBehaviour
{
    private void Start()
    {
        _transform = transform;
        //_feedbackSize = new Vector2(64, 64);
        if (_mode == Mode.Robot)
            SetAvatarLabel(_text);
        _nameTextStyle = BlueStonez.label_interparkbold_11pt;
    }

    private void OnGUI()
    {
        if (BlueStonez.Skin == null) return;

        bool isGameFrozen = false;// GameState.HasCurrentGame && GameState.CurrentGame.IsFrozen;

        GUI.depth = (int)GuiDepth.Hud;

        Rect barPos = new Rect(_screenPosition.x - 50, (Screen.height - _screenPosition.y) + _barOffset.y, 100, 6);
        Rect namePos = new Rect(barPos.xMin, (Screen.height - _screenPosition.y) - _textSize.y - 6, _textSize.x, _textSize.y);
        Rect onlyNamePos = new Rect(_screenPosition.x - _textSize.x * 0.5f, (Screen.height - _screenPosition.y) - _textSize.y - 6, _textSize.x, _textSize.y);

        if (_isInViewport || isGameFrozen)
        {
            if (Application.isEditor && _isTeamDebug && _info != null)
            {
                GUI.Label(new Rect(_screenPosition.x, (Screen.height - _screenPosition.y), 50, 20), _info.TeamID.ToString());
            }

            //ENENMY
            if (IsEnemy && GameState.HasCurrentGame)
            {
                if (_isVisible || _forceShowInformation || isGameFrozen)
                {
                    DrawName(onlyNamePos);

                    if (isGameFrozen)
                        DrawBar(barPos, 100);

                    //if (_feedbackTimer > 0)
                    //    DrawInGameFeedback(new Rect(_screenPosition.x - _feedbackSize.x * 0.5f, (Screen.height - _screenPosition.y) - _textSize.y - _feedbackSize.y - 10, _feedbackSize.x, _feedbackSize.y));
                }
            }
            //FRIEND
            else if (((IsFriend || (_mode == Mode.Robot && _barValue > 0)) && GameState.HasCurrentGame) || _forceShowInformation)
            {

                DrawBar(barPos, 100);
                DrawName(namePos);

                //if (_feedbackTimer > 0)
                //    DrawInGameFeedback(new Rect(_screenPosition.x - _feedbackSize.x * 0.5f, (Screen.height - _screenPosition.y) - _textSize.y - _feedbackSize.y - 10, _feedbackSize.x, _feedbackSize.y));
            }
            //NOT IN GAME OR NOT ME
            else if (!GameState.HasCurrentGame && _mode != Mode.Robot)
            {
                DrawName(onlyNamePos);
            }
        }
    }

    private void LateUpdate()
    {
        if (Camera.main && _target)
        {
            Vector3 heading = (_target.position + _offset) - Camera.main.transform.position;
            _screenPosition = Camera.main.WorldToScreenPoint(_target.position + _offset);
            _screenPosition.x = Mathf.FloorToInt(_screenPosition.x);
            // get current level data
            //if (LevelFXController.Instance)
            //{
            //    _distanceCapMax = LevelFXController.Instance.HudInfo.DistanceCapMax;
            //    _distanceCapMin = LevelFXController.Instance.HudInfo.DistanceCapMin;
            //}
            //else
            //{
            //    _distanceCapMax = 50;
            //    _distanceCapMin = 3;
            //}
            // check if this player just become visible or was already
            bool tmp = _isInViewport;
            _isInViewport = Vector3.Dot(Camera.main.transform.forward, heading) > 0 && (_screenPosition.x >= 0 && _screenPosition.x <= /*UITools.ScreenWidth*/Screen.width && _screenPosition.y >= 0 && _screenPosition.y <= /*GUITools.ScreenHeight*/ Screen.height); // if Z > 0 then inforn, else behind camera (actually _screenPosition.z > 0  will not tell that, use vector3.Dot instead http://forum.unity3d.com/threads/51651-SOLVED-WorldToScreenPoint-behaviour)
            if (_isInViewport && !tmp)
            {
                _needFadeIn = true;
                //_fadeInAlpha = 0;
            }

            // if enemy go off screen forget about it's name
            if (!_isInViewport && _timeCap > 0)
            {
                _timeCap = 0;
                Visibility = 0;
                _isVisible = false;
                _currentFeedback = InGameEventFeedbackType.None;
            }
            
            if (GameState.HasCurrentGame || _forceShowInformation)
            {
                if (_mode == Mode.Robot)
                {
                    if (_isInViewport) // convert this to function later
                    {
                        Visibility = 1 - (((_screenPosition.z - _distanceCapMin) / (_distanceCapMax - _distanceCapMin)) * Camera.main.fieldOfView / 60.0f);
                    }
                    else
                    {
                        Visibility = 0;
                    }
                }
                else if (IsEnemy && _isVisible)
                {
                    if (_timeCap > 0)
                    {
                        _timeCap -= Time.deltaTime;
                        Visibility = Mathf.Clamp01(_timeCap / _maxTime);
                    }
                    else
                    {
                        Visibility = 0;
                        _isVisible = false;
                        _currentFeedback = InGameEventFeedbackType.None;
                    }
                }
                else if (IsFriend)
                {
                    //Visibility = 1;
                    if (_isInViewport)
                    {
                        if (_timeCap > 0)
                        {
                            _timeCap -= Time.deltaTime;
                            float focused = Mathf.Clamp01(_timeCap / _maxTime);
                            float unFocused = 1 - (((_screenPosition.z - _distanceCapMin) / (_distanceCapMax - _distanceCapMin)) * Camera.main.fieldOfView / 60.0f);
                            Visibility = focused > unFocused ? focused : unFocused;
                        }
                        else
                        {
                            Visibility = 1 - (((_screenPosition.z - _distanceCapMin) / (_distanceCapMax - _distanceCapMin)) * Camera.main.fieldOfView / 60.0f);
                        }

                    }
                    else
                    {
                        Visibility = 0;
                    }
                }
                if (_barValue == 0) Visibility = 0;
            }
            else
            {
                Visibility = 1;
            }

            if (_isInViewport) FadeInAlphaCorrection();//!IsEnemy && 

            _transform.position = _screenPosition;
        }

        if (_feedbackTimer >= 0)
            _feedbackTimer -= Time.deltaTime;
    }

    public float Visibility
    {
        get { return _color.a; }
        set
        {
            _color.a = value;
        }
    }

    public void Show(int seconds)
    {
        _needFadeIn = true;
        _fadeInAlpha = Mathf.Clamp01(Visibility);
        _isVisible = true;
        _timeCap = seconds + 0.3f;
        _maxTime = seconds;
    }

    public void Hide()
    {
        _isVisible = false;
        _timeCap = 0;
        _feedbackTimer = 0;
        _currentFeedback = InGameEventFeedbackType.None;
    }

    private void DrawName(Rect position)
    {
        if (_forceShowInformation)//|| (GameState.HasCurrentGame && GameState.CurrentGame.IsFrozen))
            Visibility = 1;

        if (Visibility > 0)
        {
            GUI.color = new Color(0, 0, 0, Visibility);
            GUI.Label(new Rect(position.x + 1, position.y + 1, position.width, position.height), _text, _nameTextStyle);
            GUI.color = _color;
            GUI.Label(position, _text, _nameTextStyle);
            GUI.color = Color.white;
        }
    }

    private void DrawBar(Rect position, int barWidth)
    {
        if (Visibility > 0)
        {
            GUI.color = new Color(1, 1, 1, Visibility);
            //GUI.Label(new Rect(position.x - barWidth * 0.5f, position.y, barWidth, 6), string.Empty, BlueStonez.box_white_2px);
            //GUI.DrawTexture(new Rect(position.x - barWidth * 0.5f, position.y, barWidth, 6), _healthBarImage);
            GUI.DrawTexture(position, _healthBarImage);
            Color c = ColorConverter.HsvToRgb(_barValue / 3f, 1, 0.9f);
            GUI.color = new Color(c.r, c.g, c.b, Visibility);
            //GUI.DrawTexture(new Rect(position.x - barWidth * 0.5f + 1, position.y + 1, (barWidth - 2) * _barValue, 4), _healthBarImage);
            GUI.DrawTexture(new Rect(position.xMin + 1, position.yMin + 1, (position.width - 2) * _barValue, position.height - 2), _healthBarImage);
            GUI.color = Color.white;
        }
    }

    //private void DrawInGameFeedback(Rect position)
    //{
    //    switch (_currentFeedback)
    //    {
    //        case InGameEventFeedbackType.NutShot:
    //            DrawInGameEvent(position, _nutshotImage);
    //            break;

    //        case InGameEventFeedbackType.HeadShot:
    //            DrawInGameEvent(position, _headshotImage);
    //            break;

    //        case InGameEventFeedbackType.Humiliation:
    //            DrawInGameEvent(position, _humiliationImage);
    //            break;
    //    }
    //}

    private void DrawInGameEvent(Rect position, Texture image)
    {
        if (image == null) return;

        GUI.DrawTexture(position, image);
    }

    public void SetAvatarLabel(string name)
    {
        _text = name;

        //TODO: get rid of this call - just use a wide rect and center the name
        if (BlueStonez.label_interparkbold_11pt != null)
            _textSize = BlueStonez.label_interparkbold_11pt.CalcSize(new GUIContent(_text));
        else
            _textSize = new Vector2(name.Length * 10, 20);
    }

    public void SetHealthBarValue(float value)
    {
        _barValue = Mathf.Clamp01(value);
    }

    public void SetInGameFeedback(InGameEventFeedbackType feedbackType)
    {
        _currentFeedback = feedbackType;
        _feedbackTimer = 3;
    }

    public void SetCharacterInfo(CharacterInfo info)
    {
        if (info != null)
        {
            SetAvatarLabel(info.PlayerName);
            _info = info;
        }
    }

    private void FadeInAlphaCorrection()
    {
        if (_needFadeIn)
        {
            _fadeInAlpha += Time.deltaTime;
            if (_fadeInAlpha >= Visibility)
            {
                _needFadeIn = false;
                _fadeInAlpha = 0;
            }
            else
            {
                Visibility = _fadeInAlpha;
            }
        }
    }

    #region FIELDS

    [SerializeField]
    private Mode _mode = Mode.None;

    [SerializeField]
    private bool _isTeamDebug = true;

    [SerializeField]
    private string _text = "text";

    [SerializeField]
    public Vector2 _barOffset = new Vector2(0, 0);//(4, 30);

    [SerializeField]
    private Color _color = Color.white;

    [SerializeField]
    private Vector3 _offset = new Vector3(0, 2, 0);//(0, 0.1f, 0.02f);

    [SerializeField]
    private Texture _healthBarImage;

    //[SerializeField]
    private float _distanceCapMax = 50;
    private float _distanceCapMin = 3;

    [SerializeField]
    private float _timeCap = 0;

    private float _maxTime = 0;

    private InGameEventFeedbackType _currentFeedback;

    //[SerializeField]
    //private Texture _nutshotImage;

    //[SerializeField]
    //private Texture _headshotImage;

    //[SerializeField]
    //private Texture _humiliationImage;

    private Transform _target;
    private Transform _transform;
    private Vector3 _screenPosition;

    [SerializeField]
    private float _barValue = 0;
    private Vector2 _textSize;

    private bool _isVisible = true;
    private bool _showBar = true;

    //private Vector2 _feedbackSize;

    private const int ImageSize = 64;
    private const int PixelOffset = 1;

    public float _feedbackTimer;

    private CharacterInfo _info;

    public CharacterInfo Info
    {
        get { return _info; }
        set { _info = value; }
    }

    private GUIStyle _nameTextStyle;

    private bool _isInViewport;

    private bool _needFadeIn = false;
    private float _fadeInAlpha = 0;

    [SerializeField]
    private bool _forceShowInformation = false;

    #endregion

    #region PROPERTIES
    public bool IsEnemy
    {
        get { return _info != null && GameState.HasCurrentPlayer && (_info.TeamID == TeamID.NONE || _info.TeamID != GameState.LocalCharacter.TeamID); }
    }
    public bool IsFriend
    {
        get { return !IsEnemy && !IsMe; }
    }
    public bool IsMe
    {
        get { return !GameState.HasCurrentGame || !GameState.HasCurrentPlayer || _info == null || GameState.CurrentPlayerID == _info.ActorId; }
    }
    public bool IsBarVisible
    {
        get { return _showBar; }
        set { _showBar = value; }
    }
    public Transform Target
    {
        get { return _target; }
        set { _target = value; }
    }
    public float DistanceCap
    {
        get { return _distanceCapMax; }
        set { _distanceCapMax = value; }
    }
    public InGameEventFeedbackType ActiveFeedbackType
    {
        get { return _currentFeedback; }
    }

    public bool ForceShowInformation
    {
        get { return _forceShowInformation; }
        set { _forceShowInformation = value; }
    }

    #endregion

    public enum Mode
    {
        None,
        Robot
    }
}
