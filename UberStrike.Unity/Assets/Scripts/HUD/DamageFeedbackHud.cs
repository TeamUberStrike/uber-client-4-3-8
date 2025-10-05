using UnityEngine;
using System.Collections;

using System.Collections.Generic;

public class DamageFeedbackHud : Singleton<DamageFeedbackHud>
{
    public class DamageFeedbackMark
    {
        public float DamageAlpha;
        public float DamageAmount;
        public float DamageDirection;

        public DamageFeedbackMark(float normalizedDamage, float horizontalAngle)
        {
            DamageAlpha = normalizedDamage;
            DamageAmount = normalizedDamage;
            DamageDirection = horizontalAngle;
        }
    }

    public void Draw()
    {
        if (Enabled == false)
        {
            return;
        }

        for (int i = 0; i < _damageFeedbackMarkList.Count; i++)
        {
            float horizontalAngle = _damageFeedbackMarkList[i].DamageDirection;
            Vector3 dir = Quaternion.Euler(0, horizontalAngle, 0) * Vector3.back;
            dir = LevelCamera.Instance.TransformCache.InverseTransformDirection(dir);
            horizontalAngle = Quaternion.LookRotation(dir).eulerAngles.y;
            GUIUtility.RotateAroundPivot(horizontalAngle, new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
            {
                GUI.color = new Color(0.975f, 0.201f, 0.135f, _damageFeedbackMarkList[i].DamageAlpha);
                int damageMarkWidth = Mathf.RoundToInt(128 * _damageFeedbackMarkList[i].DamageAmount);
                GUI.DrawTexture(new Rect((Screen.width * 0.5f) - (damageMarkWidth * 0.5f), 
                                         (Screen.height * 0.5f) - 256, damageMarkWidth, 128), 
                                HudTextures.DamageFeedbackMark);
            }
            GUI.matrix = Matrix4x4.identity;
        }
        GUI.color = Color.white;
    }

    public void Update()
    {
        if (_damageFeedbackMarkList.Count > 0)
        {
            for (int i = 0; i < _damageFeedbackMarkList.Count; i++)
            {
                if (_damageFeedbackMarkList[i].DamageAlpha < 0) _damageFeedbackMarkList.RemoveAt(i);
            }

            for (int i = 0; i < _damageFeedbackMarkList.Count; i++)
            {
                _damageFeedbackMarkList[i].DamageAlpha -= Time.deltaTime * 0.5f;
            }
        }
    }

    public void AddDamageMark(float normalizedDamage, float horizontalAngle)
    {
        _damageFeedbackMarkList.Add(new DamageFeedbackMark(normalizedDamage, horizontalAngle));

        LevelCamera.Instance.DoFeedback(LevelCamera.FeedbackType.GetDamage, Vector3.back, 0.1f, normalizedDamage, PEAKTIME, ENDTIME, 10, Vector3.forward);
    }

    public void ClearAll()
    {
        _damageFeedbackMarkList.Clear();
    }

    private DamageFeedbackHud()
    {
        _damageFeedbackMarkList = new List<DamageFeedbackMark>();

        Enabled = true;
    }

    public bool Enabled { get; set; }

    private List<DamageFeedbackMark> _damageFeedbackMarkList;
    private const float PEAKTIME = 0.04f;
    private const float ENDTIME = 0.08f;
}

