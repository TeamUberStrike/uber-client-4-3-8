using UnityEngine;
using Cmune.Util;

[RequireComponent(typeof(BoxCollider))]
public class SecretTrigger : BaseGameProp
{
    void Awake()
    {
        gameObject.layer = (int)UberstrikeLayer.Props;
    }

    void OnDisable()
    {
        foreach (Renderer r in _visuals)
        {
            r.material.SetColor("_Color", Color.black);
        }
    }

    void Update()
    {
        if (_showVisualsEndTime > Time.time)
        {
            foreach (Renderer r in _visuals)
            {
                r.material.SetColor("_Color", new Color((Mathf.Sin(Time.time * 4) + 1) * 0.3f, 0, 0));
            }
        }
        else
        {
            enabled = false;
        }
    }

    // Update is called once per frame
    public override void ApplyDamage(DamageInfo shot)
    {
        if (_reciever)
        {
            enabled = true;
            _showVisualsEndTime = Time.time + _activationTime;
            _reciever.SetTriggerActivated(this);
        }
        else
        {
            CmuneDebug.LogError("The SecretTrigger " + gameObject.name + " is not assigned to a SecretReciever!");
        }
    }

    public void SetSecretReciever(SecretBehaviour logic)
    {
        _reciever = logic;
    }

    public float ActivationTimeOut
    {
        get { return _showVisualsEndTime; }
    }

    #region Fields
    [SerializeField]
    private Renderer[] _visuals;
    [SerializeField]
    private float _activationTime = 15;

    private SecretBehaviour _reciever;
    private float _showVisualsEndTime = 0;
    #endregion
}
