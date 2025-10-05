using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(RobotAnimationController))]
public class RobotSimpleAI : BaseGameProp
{
    private AvatarHudInformation _hudInfo;
    private short _health;

    void Awake()
    {
        _animationController = GetComponent<RobotAnimationController>();

        _children = new List<Transform>();
        _parents = new List<Transform>();
        _position = new List<Vector3>();
        _rotation = new List<Quaternion>();
        _rBody = new List<Rigidbody>();

        GetChildrenData(Transform, ref _children, ref _parents, ref _position, ref _rotation, ref _rBody);

        _health = 100;

        if (transform.parent)
        {
            _hudInfo = transform.parent.GetComponentInChildren<AvatarHudInformation>();
            if (_hudInfo)
            {
                _hudInfo.Target = transform;
                _hudInfo.SetHealthBarValue(_health / 100f);
            }
        }


        CharacterHitArea[] allHitAreas = GetComponentsInChildren<CharacterHitArea>(true);
        foreach (CharacterHitArea hitBox in allHitAreas)
            hitBox.Shootable = this;
    }

    void Start()
    {
        _animationController.PlayAnimationHard("Dance");

        //for (int i = 0; i < _rBody.Count; i++)
        //{
        //    for (int j = i + 1; j < _rBody.Count; j++)
        //        Physics.IgnoreCollision(_rBody[i].collider, _rBody[j].collider);

        //    if (collider)
        //        Physics.IgnoreCollision(collider, _rBody[i].collider);
        //}

        //avoid self collision
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            for (int j = i + 1; j < colliders.Length; j++)
            {
                Physics.IgnoreCollision(colliders[i], colliders[j]);
            }
        }
    }

    void Update()
    {
        if (_myRobotStates == RobotStates.Show && !_animationController.CheckIfActive("BallToBot"))
            _myRobotStates = RobotStates.DoneShow;

        if (Time.time > _nextTimeToCheck)
        {
            switch (_myRobotStates)
            {
                case RobotStates.Dance:
                    _nextTimeToCheck = Time.time + _timeToHide;
                    _animationController.PlayAnimationHard("BotToBall");
                    _myRobotStates = RobotStates.Hide;
                    break;
                case RobotStates.Hide:
                    _animationController.PlayAnimationHard("BallToBot");
                    _myRobotStates = RobotStates.Show;
                    break;
                case RobotStates.DoneShow:
                    _nextTimeToCheck = Time.time + _timeToDance;
                    _animationController.PlayAnimationHard("Dance");
                    _myRobotStates = RobotStates.Dance;
                    break;
                case RobotStates.Explode:
                    _nextTimeToCheck = Time.time + _transparentTime * Time.deltaTime; // timeToReborn;
                    //HideRobot();
                    _myRobotStates = RobotStates.FadeOutParts;
                    break;
                case RobotStates.FadeOutParts:
                    _nextTimeToCheck = Time.time + _transparentTime * Time.deltaTime; // timeToReborn;
                    if (FadeOutRobot(0.1f))
                    {
                        _myRobotStates = RobotStates.Dead;
                        HideRobot();
                        _nextTimeToCheck = Time.time + _timeToReborn;
                    }
                    break;
                case RobotStates.Dead:
                    Reborn();
                    break;
            }
        }
    }

    public void Die(Vector3 force)
    {
        Explode(force);
        _myRobotStates = RobotStates.Explode;
        _nextTimeToCheck = Time.time + _timeToExplode;
    }

    private void Explode(Vector3 force)
    {
        _myRobotStates = RobotStates.Explode;
        _animationController.AnimationStop();
        _animationController.enabled = false;
        animation.enabled = false;
        if (collider) collider.isTrigger = true;
        gameObject.layer = (int)UberstrikeLayer.IgnoreRaycast;

        float f = force.magnitude;
        for (int i = 0; i < _rBody.Count; i++)
        {
            _rBody[i].isKinematic = false;
            _rBody[i].AddForce((Random.onUnitSphere * f + force) * _forceFactor * 0.1f, ForceMode.Impulse);
            _rBody[i].transform.parent = Transform;
        }
    }

    private void GetChildrenData(Transform root, ref List<Transform> go, ref List<Transform> parents, ref List<Vector3> pos, ref List<Quaternion> rot, ref List<Rigidbody> rb)
    {
        for (int i = 0; i < root.GetChildCount(); i++)
        {
            if (root.GetChild(i).GetComponent<Rigidbody>() != null)
            {
                go.Add(root.GetChild(i));
                pos.Add(root.GetChild(i).localPosition);
                rot.Add(root.GetChild(i).localRotation);
                rb.Add(root.GetChild(i).GetComponent<Rigidbody>());
                parents.Add(root);
            }
            if (root.GetChild(i).GetChildCount() > 0)
                GetChildrenData(root.GetChild(i), ref go, ref parents, ref pos, ref rot, ref rb);
        }
    }

    private bool FadeOutRobot(float alpha)
    {
        Vector4 color;
        bool timeToEnd = false;
        for (int i = 0; i < _rBody.Count; i++)
        {
            color = _children[i].renderer.material.color;
            if (color.w < alpha)
            {
                color.w = 0f;
                timeToEnd = true;
            }
            else
            {
                color.w = color.w - alpha;
            }
            _children[i].renderer.material.color = color;
        }
        return timeToEnd;
    }

    private void Reborn()
    {
        Vector4 color;

        ClearProjectileDecorators();

        for (int i = 0; i < _rBody.Count; i++)
        {
            _rBody[i].isKinematic = true;
            _children[i].localPosition = _position[i];
            _children[i].localRotation = _rotation[i];
            _children[i].GetComponent<MeshRenderer>().enabled = true;
            _children[i].collider.isTrigger = false;
            _children[i].parent = _parents[i];
            color = _children[i].renderer.material.color;
            color.w = 1f;
            _children[i].renderer.material.color = color;
            //_children[i].renderer.material.shader = Shader.Find("Diffuse");
        }

        _animationController.enabled = true;
        _myRobotStates = RobotStates.Show;
        animation.enabled = true;
        if (collider) collider.isTrigger = false;
        _animationController.PlayAnimationHard("BallToBot");
        gameObject.layer = (int)UberstrikeLayer.Props;
        _health = 100;

        if (_hudInfo)
        {
            _hudInfo.SetHealthBarValue(_health / 100f);
        }
    }

    private void HideRobot()
    {
        for (int i = 0; i < _children.Count; i++)
        {
            _children[i].GetComponent<MeshRenderer>().enabled = false;
            _children[i].collider.isTrigger = true;
        }
    }

    public override void ApplyDamage(DamageInfo d)
    {
        if (_health <= 0) return;

        _health -= d.Damage;

        //if (HudManager.IsInitialized)
        //    HudManager.Instance.AddHealthFeedback(transform.position + Vector3.up, -d.Damage);
        ShowDamageFeedback(d);
        
        if (_hudInfo)
        {
            _hudInfo.SetHealthBarValue(_health / 100f);
        }

        if (_health <= 0)
        {
            Die(d.Force);
        }
    }

    public override bool CanApplyDamage
    {
        get { return _health > 0; }
    }

    private void ClearProjectileDecorators()
    {
        foreach (ArrowProjectile item in GetComponentsInChildren<ArrowProjectile>(true))
        {
            item.Destroy();
        }
    }

    private void ShowDamageFeedback(DamageInfo shot)
    {
        GameObject obj = GameObject.Instantiate(_damageFeedback, shot.Hitpoint, Quaternion.LookRotation(shot.Force)) as GameObject;
        if (obj)
        {
            obj.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);

            PlayerDamageEffect effect = obj.GetComponent<PlayerDamageEffect>();
            if (effect)
            {
                effect.Show(shot);
            }
        }
    }

    #region PROPERTIES

    public int Health
    {
        get
        {
            return _health;
        }
    }

    #endregion

    #region FIELDS
    private List<Transform> _children;
    private List<Transform> _parents;
    private List<Vector3> _position;
    private List<Quaternion> _rotation;
    private List<Rigidbody> _rBody;

    private RobotStates _myRobotStates = RobotStates.Dance;
    private RobotAnimationController _animationController;

    private float _nextTimeToCheck = 0.0f;
    [SerializeField]
    private float _timeToDance = 2f;
    [SerializeField]
    private float _timeToHide = 3f;
    [SerializeField]
    private float _timeToExplode = 5f;
    [SerializeField]
    private float _timeToReborn = 5f;

    [SerializeField]
    private GameObject _damageFeedback;
    #endregion

    #region INSPECTOR
    [SerializeField]
    private float _forceFactor = 0.2f;
    [SerializeField]
    private float _transparentTime;
    #endregion

    private enum RobotStates
    {
        Dance,
        Hide,
        Show,
        DoneShow,
        Explode,
        FadeOutParts,
        Dead
    }
}
