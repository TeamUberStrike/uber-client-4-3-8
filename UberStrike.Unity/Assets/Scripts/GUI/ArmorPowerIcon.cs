
using System;
using UnityEngine;
using System.Text;

public class ArmorPowerIcon : MonoSingleton<ArmorPowerIcon>
{
    [SerializeField]
    private Texture2D[] _armorGauge;
    [SerializeField]
    private Texture2D[] _armorShield;

    [SerializeField]
    private Texture2D _armorIndicatorBackground;
    [SerializeField]
    private Texture2D _armorIndicatorForeground;

    //public int armorPoints;
    //public int absortionPoints;

    //public GUISkin skin;

    //void OnGUI()
    //{
    //    DrawArmorPower(new Rect(100, 100, 150, 150), armorPoints, absortionPoints);
    //}

    void OnEnable()
    {
        if (_armorGauge.Length != 4) throw new Exception("ArmorGauge icons in ArmorPowerIcon class not set!");
        if (_armorShield.Length != 4) throw new Exception("ArmorShield icons in ArmorPowerIcon class not set!");
    }

    public void DrawArmorPower(Rect rect, int armorPoints, int absortionPoints)
    {
        if (rect.Contains(Event.current.mousePosition))
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(armorPoints + " Armor Points");
            builder.AppendLine((50 + absortionPoints).ToString("N0") + "%" + " Defense");
            GUI.tooltip = builder.ToString();
        }

        float armorPercent = armorPoints / MaxArmorPoints;
        float absortionPercent = (50 + absortionPoints) / 100f;

        GUI.DrawTexture(rect, _armorIndicatorBackground);
        GUI.color = Color.white;
        GUI.BeginGroup(new Rect(rect.x, rect.y + rect.height * (1 - armorPercent), rect.width / 2, rect.height * armorPercent));
        {
            GUI.DrawTexture(new Rect(0, -rect.height * (1 - armorPercent), rect.width, rect.height), _armorIndicatorForeground);
        }
        GUI.EndGroup();
        GUI.BeginGroup(new Rect(rect.x + rect.width / 2, rect.y + rect.height * (1 - absortionPercent),
            rect.width / 2, rect.height * absortionPercent));
        {
            GUI.DrawTexture(new Rect(-rect.width / 2, -rect.height * (1 - absortionPercent), rect.width, rect.height),
                _armorIndicatorForeground);
        }
        GUI.EndGroup();
    }

    private int GetIconIndex(int value)
    {
        if (value <= 0) return 0;
        else if (value < 40) return 1;
        else if (value < 90) return 2;
        else return 3;
    }

    private static float MaxArmorPoints = 100;
}