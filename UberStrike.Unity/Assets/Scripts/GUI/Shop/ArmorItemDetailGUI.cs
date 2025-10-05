using Cmune.Util;
using UnityEngine;

public class ArmorItemDetailGUI : IBaseItemDetailGUI
{
    private GearItem _item;
    private Texture2D _armorPointsIcon;

    public ArmorItemDetailGUI(GearItem item, Texture2D armorPointsIcon)
    {
        _item = item;
        _armorPointsIcon = armorPointsIcon;
    }

    public void Draw()
    {
        float armorAbsorption = (float)_item.Configuration.ArmorAbsorptionPercent / 100f;

        GUI.DrawTexture(new Rect(48, 89, 32, 32), _armorPointsIcon);
        GUI.contentColor = Color.black;
        GUI.Label(new Rect(48, 89, 32, 32), _item.Configuration.ArmorPoints.ToString(), BlueStonez.label_interparkbold_16pt);
        GUI.contentColor = Color.white;
        GUI.Label(new Rect(80, 89, 32, 32), "AP", BlueStonez.label_interparkbold_18pt_left);

        GUITools.ProgressBar(new Rect(120, 95, 160, 12), LocalizedStrings.Absorption, armorAbsorption, ColorScheme.ProgressBar, 64, CmunePrint.Percent(armorAbsorption));
    }
}