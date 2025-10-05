using UnityEngine;
using System.Collections;

public class TutorialWaypoint : MonoBehaviour
{
    [SerializeField]
    private Texture ImgWaypoint;

    private Transform _transform;
    private Sprite2D _imgWaypoint;
    private MeshGUIText _txtDistance;
    private MeshGUIText _txtTitle;

    private Vector2 _imgPos;
    private Vector2 _txtPos;
    private Vector2 _disPos;

    [SerializeField]
    private string _text = string.Empty;

    private bool _canShow = false;

    public bool CanShow
    {
        get { return _canShow; }
        set
        {
            _canShow = value;

            if (_canShow)
            {
                _imgWaypoint.Flicker(0.5f);
                _imgWaypoint.Scale = Vector2.one;
                _imgWaypoint.FadeAlphaTo(1, 0.5f);

                if (_txtDistance != null)
                {
                    _txtDistance.Show();
                    _txtDistance.Flicker(0.5f);
                    _txtDistance.FadeAlphaTo(1, 0.5f);
                }

                if (_txtTitle != null)
                {
                    _txtTitle.Show();
                    _txtTitle.Flicker(0.5f);
                    _txtTitle.FadeAlphaTo(1, 0.5f);
                }
            }
            else
            {
                _imgWaypoint.Flicker(0.5f);
                _imgWaypoint.FadeAlphaTo(0, 0.5f);

                if (_txtTitle != null)
                {
                    _txtTitle.Alpha = 0;
                    _txtTitle.Hide();
                }

                if (_txtDistance != null)
                {
                    _txtDistance.Alpha = 0;
                    _txtDistance.Hide();
                }
            }
        }
    }

    public string Text
    {
        get { return _text; }
        set { _text = value; }
    }

    private void Awake()
    {
        _transform = transform;

        _imgWaypoint = new Sprite2D(ImgWaypoint);
        _imgWaypoint.Alpha = 0;
    }

    private void OnGUI()
    {
        if ((CanShow || _imgWaypoint.Alpha > 0.1f) && LevelCamera.Instance.MainCamera && GameState.HasCurrentPlayer)
        {
            DrawWaypoint2();
        }
    }

    //private void DrawWaypoint()
    //{
    //    float xMax = (_txtDistance == null) ? 64 : (_txtDistance.Size.x + 50);

    //    Vector3 screenPos = LevelCamera.Instance.MainCamera.WorldToScreenPoint(_transform.position);

    //    screenPos.x = Mathf.Clamp(screenPos.x, 0, Screen.width - xMax);
    //    screenPos.y = Mathf.Clamp(Screen.height - screenPos.y, 0, Screen.height - 64);


    //    Vector2 icoPos = Vector2.zero;
    //    Vector2 disPos = Vector2.zero;
    //    Vector2 txtPos = Vector2.zero;


    //    CalcWaypointPos(screenPos, out txtPos, out icoPos, out disPos);


    //    if (screenPos.z > 0)
    //    {
    //        int x = Mathf.RoundToInt(screenPos.x);
    //        int y = Mathf.RoundToInt(screenPos.y);
    //        int distance = Mathf.RoundToInt(Vector3.Distance(_transform.position, GameState.CurrentPlayer.Position));

    //        _imgWaypoint.Draw(x, y);

    //        if (_txtDistance == null)
    //        {
    //            if (LevelTutorial.IsInitialized)
    //            {
    //                _txtDistance = new MeshGUIText(string.Empty, LevelTutorial.Instance.Font, TextAnchor.UpperLeft, gameObject);
    //                _txtDistance.Scale = new Vector2(0.2f, 0.2f);
    //                _txtDistance.Alpha = 0;

    //                HudUtility.Instance.SetDefaultStyle(_txtDistance);

    //                _txtTitle = new MeshGUIText(Text, LevelTutorial.Instance.Font, TextAnchor.MiddleCenter, gameObject);
    //                _txtTitle.Scale = new Vector2(0.2f, 0.2f);
    //                _txtTitle.Alpha = 0;
    //            }
    //        }
    //        else
    //        {
    //            _txtDistance.Show();
    //            _txtDistance.Position = new Vector2(x + 48, y);
    //            _txtDistance.Text = distance.ToString() + "M";
    //        }
    //    }
    //    else if (_txtDistance != null)
    //    {
    //        _txtDistance.Hide();
    //    }
    //}

    private void DrawWaypoint2()
    {
        Vector3 screenPos = LevelCamera.Instance.MainCamera.WorldToScreenPoint(_transform.position);

        screenPos.y = Screen.height - screenPos.y;

        bool toLeft = true;

        //if (GameState.HasCurrentPlayer)
        //{
        //    Vector3 dir = _transform.position - GameState.CurrentPlayer.Position;
        //    Matrix4x4 mat = Matrix4x4.TRS(Vector3.zero, Quaternion.Inverse(GameState.CurrentPlayer.HorizontalRotation), Vector3.one);

        //    float angle = Quaternion.LookRotation(mat * dir).eulerAngles.y;

        //    if (angle > 90 && angle < 180)
        //        toLeft = false;
        //}

        if (screenPos.z > 0)
            CalcWaypointPos(screenPos, toLeft, out _txtPos, out _imgPos, out _disPos);

        GUI.depth = (int)GuiDepth.Hud;

        //if (screenPos.z > 0)
        //{
        int distance = Mathf.RoundToInt(Vector3.Distance(_transform.position, GameState.LocalCharacter.Position));

        _imgWaypoint.Draw(_imgPos.x, _imgPos.y);

        if (_txtDistance == null)
        {
            if (LevelTutorial.Exists)
            {
                _txtDistance = new MeshGUIText(string.Empty, LevelTutorial.Instance.Font, TextAnchor.UpperLeft);
                _txtDistance.Scale = new Vector2(0.2f, 0.2f);
                _txtDistance.Color = Color.white;
                _txtDistance.Alpha = 0;

                HudStyleUtility.Instance.SetDefaultStyle(_txtDistance);

                _txtTitle = new MeshGUIText(Text, LevelTutorial.Instance.Font, TextAnchor.UpperLeft);
                _txtTitle.Scale = new Vector2(0.2f, 0.2f);
                _txtTitle.Color = Color.white;
                _txtTitle.Alpha = 0;

                HudStyleUtility.Instance.SetDefaultStyle(_txtTitle);
            }
        }
        else
        {
            _txtDistance.Show();
            _txtDistance.Position = _disPos;
            _txtDistance.Text = distance.ToString() + "M";

            _txtTitle.Show();
            _txtTitle.Position = _txtPos;
        }
        //}
        //else if (_txtDistance != null)
        //{
        //    _txtDistance.Hide();
        //}
    }

    private void CalcWaypointPos(Vector3 screenPos, bool toLeft, out Vector2 textPos, out Vector2 imgPos, out Vector2 distancePos)
    {
        float xMin, xMax, yMin, yMax;

        xMin = screenPos.x - _imgWaypoint.Size.x / 2f;
        xMax = screenPos.x + _imgWaypoint.Size.x / 2f;
        yMin = screenPos.y - _imgWaypoint.Size.y / 2f;
        yMax = screenPos.y + _imgWaypoint.Size.y / 2f;

        if (_txtTitle != null)
        {
            xMin = Mathf.Min(xMin, screenPos.x - _txtTitle.Size.x / 2f);
            xMax = screenPos.x + Mathf.Max(_txtTitle.Size.x / 2f, (_txtDistance != null) ? (_txtDistance.Size.x + 20) : (_imgWaypoint.Size.x / 2f));
            yMin -= _txtTitle.Size.y;
        }
        else
        {
            xMax = Mathf.Max(xMax, (_txtDistance != null) ? _txtDistance.Size.x + 50 : _imgWaypoint.Size.x / 2f);
        }

        textPos = new Vector2(xMin, yMin);
        imgPos = new Vector2(Mathf.RoundToInt(screenPos.x - _imgWaypoint.Size.x / 2), Mathf.RoundToInt(screenPos.y - _imgWaypoint.Size.y / 2));
        distancePos = imgPos + new Vector2(48, 0);

        if (screenPos.z > 0)
        {
            if (xMin < 0)
            {
                imgPos.x -= xMin;
                textPos.x -= xMin;
                distancePos.x -= xMin;
            }

            if (xMax > Screen.width)
            {
                imgPos.x -= (xMax - Screen.width);
                textPos.x -= (xMax - Screen.width);
                distancePos.x -= (xMax - Screen.width);
            }
        }
        //else
        //{
        //    if (toLeft)
        //    {
        //        imgPos.x -= xMin;
        //        textPos.x -= xMin;
        //        distancePos.x -= xMin;

        //        if (xMax > Screen.width)
        //        {
        //            imgPos.x -= (xMax - Screen.width);
        //            textPos.x -= (xMax - Screen.width);
        //            distancePos.x -= (xMax - Screen.width);
        //        }
        //    }
        //    else
        //    {
        //        if (xMin < 0)
        //        {
        //            imgPos.x -= xMin;
        //            textPos.x -= xMin;
        //            distancePos.x -= xMin;
        //        }

        //        imgPos.x -= (xMax - Screen.width);
        //        textPos.x -= (xMax - Screen.width);
        //        distancePos.x -= (xMax - Screen.width);
        //    }
        //}

        if (yMin < 0)
        {
            imgPos.y -= yMin;
            textPos.y -= yMin;
            distancePos.y -= yMin;
        }

        if (yMax > Screen.height)
        {
            imgPos.y -= (yMax - Screen.height);
            textPos.y -= (yMax - Screen.height);
            distancePos.y -= (yMax - Screen.height);
        }
    }

    private IEnumerator StartHideMe(float sec)
    {
        yield return new WaitForSeconds(sec);

        _canShow = false;

        if (_txtDistance != null)
            _txtDistance.Hide();

        if (_txtTitle != null)
            _txtTitle.Hide();
    }
}
