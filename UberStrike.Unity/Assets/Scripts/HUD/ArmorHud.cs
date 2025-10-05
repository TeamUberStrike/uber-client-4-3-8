using UnityEngine;

public class ArmorHud : Singleton<ArmorHud>
{
    private MeshGUIText _defenseBonusText;
    private MeshGUIText _defenseBonusSymbol;
    private MeshGUIText _defenseBonusValue;
    private MeshGUIText _armorCarriedText;
    private MeshGUIText _armorCarriedSymbol;
    private MeshGUIText _armorCarriedValue;

    private Animatable2DGroup _meshGUITexts;

    public int ArmorCarried
    {
        set { _armorCarriedValue.Text = value.ToString(); }
    }

    public int DefenseBonus
    {
        set { _defenseBonusValue.Text = value.ToString(); }
    }

    public bool Enabled
    {
        set { _meshGUITexts.IsEnabled = value; }
    }

    private ArmorHud()
    {
        _armorCarriedValue = new MeshGUIText("", HudAssets.Instance.InterparkBitmapFont, TextAnchor.MiddleRight);
        _armorCarriedSymbol = new MeshGUIText("AP", HudAssets.Instance.HelveticaBitmapFont, TextAnchor.MiddleRight);
        _armorCarriedText = new MeshGUIText("Armor Carried", HudAssets.Instance.InterparkBitmapFont, TextAnchor.MiddleRight);

        _defenseBonusValue = new MeshGUIText("", HudAssets.Instance.InterparkBitmapFont, TextAnchor.MiddleRight);
        _defenseBonusSymbol = new MeshGUIText("%", HudAssets.Instance.HelveticaBitmapFont, TextAnchor.MiddleRight);
        _defenseBonusText = new MeshGUIText("Defense Bonus", HudAssets.Instance.InterparkBitmapFont, TextAnchor.MiddleRight);

        HudStyleUtility.Instance.SetBlueStyle(_defenseBonusText);
        HudStyleUtility.Instance.SetBlueStyle(_defenseBonusSymbol);
        HudStyleUtility.Instance.SetBlueStyle(_defenseBonusValue);
        HudStyleUtility.Instance.SetBlueStyle(_armorCarriedText);
        HudStyleUtility.Instance.SetBlueStyle(_armorCarriedSymbol);
        HudStyleUtility.Instance.SetBlueStyle(_armorCarriedValue);

        _meshGUITexts = new Animatable2DGroup();
        _meshGUITexts.Group.Add(_armorCarriedValue);
        _meshGUITexts.Group.Add(_armorCarriedSymbol);
        _meshGUITexts.Group.Add(_armorCarriedText);
        _meshGUITexts.Group.Add(_defenseBonusValue);
        _meshGUITexts.Group.Add(_defenseBonusSymbol);
        _meshGUITexts.Group.Add(_defenseBonusText);
    }

    public void Update()
    {
        float delta = (Screen.height * 20 / 660) - 20;

        _armorCarriedValue.Position = new Vector2(Screen.width - 625 - delta, 327);
        _armorCarriedValue.Scale = Vector2.one * 0.4f;
        _armorCarriedSymbol.Position = new Vector2(Screen.width - 605, 331.2f);
        _armorCarriedSymbol.Scale = Vector2.one * 0.25f;
        _armorCarriedText.Position = new Vector2(Screen.width - 602, 352);
        _armorCarriedText.Scale = Vector2.one * 0.25f;

        _defenseBonusValue.Position = new Vector2(Screen.width - 620 - delta, 417);
        _defenseBonusValue.Scale = Vector2.one * 0.4f;
        _defenseBonusSymbol.Position = new Vector2(Screen.width - 602, 421);
        _defenseBonusSymbol.Scale = Vector2.one * 0.3f;
        _defenseBonusText.Position = new Vector2(Screen.width - 602, 442);
        _defenseBonusText.Scale = Vector2.one * 0.25f;
    }
}