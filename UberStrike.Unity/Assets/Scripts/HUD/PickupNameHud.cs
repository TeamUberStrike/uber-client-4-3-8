using UnityEngine;
using System.Collections;

public class PickupNameHud : Singleton<PickupNameHud>
{
    public bool Enabled
    {
        get
        {
            return _pickUpText.IsVisible;
        }
        set
        {
            if (_pickUpText.IsVisible != value)
            {
                if (value) _pickUpText.Show();
                else _pickUpText.Hide();
                _displayAnim.Stop();
            }
        }
    }

    public void Draw()
    {
    }

    public void Update()
    {
        _displayAnim.Update();
        _pickUpText.Draw();
        _pickUpText.ShadowColorAnim.Alpha = 0.0f;
    }

    public void DisplayPickupName(string itemName, PickUpMessageType pickupItem)
    {
        if (IsSupportedPickupType(pickupItem))
        {
            OnPickupItem(itemName, pickupItem);
        }
    }

    #region Private
    private PickupNameHud()
    {
        _pickUpText = new MeshGUIText("", HudAssets.Instance.InterparkBitmapFont, TextAnchor.MiddleCenter);
        _pickUpText.NamePrefix = "Pickup";
        _displayAnim = new TemporaryDisplayAnim(_pickUpText, 2.0f, 1.0f);

        ResetHud();
        Enabled = true;
    }

    private void ResetHud()
    {
        ResetStyle();
        ResetTransform();
    }

    private void ResetStyle()
    {
        HudStyleUtility.Instance.SetNoShadowStyle(_pickUpText);
        _pickUpText.Color = ColorConverter.RgbToColor(255, 248, 192);
        _pickUpText.ShadowColorAnim.Alpha = 0.0f;
    }

    private void ResetTransform()
    {
        _curScaleFactor = 0.45f;
        _pickUpText.Scale = new Vector2(_curScaleFactor, _curScaleFactor);
        _pickUpText.Position = new Vector2(Screen.width / 2, Screen.height * 0.60f);
    }

    private bool IsSupportedPickupType(PickUpMessageType selectedItem)
    {
        return selectedItem != PickUpMessageType.None;
    }

    private void OnPickupItem(string itemName, PickUpMessageType pickupItem)
    {
        if (IsComboPickup(pickupItem))
        {
            _samePickUpCount++;
            _pickUpText.Text = GetComboPickupString(itemName);
        }
        else
        {
            _pickUpText.Text = itemName;
            _samePickUpCount = 1;
        }
        _lastPickUpType = pickupItem;
        if (_displayAnim.IsAnimating)
        {
            _displayAnim.Stop();
        }
        ResetStyle();
        _displayAnim.Start();
    }

    private string GetComboPickupString(string itemName)
    {
        return string.Format("{0} x {1}", itemName, _samePickUpCount.ToString());
    }

    private bool IsComboPickup(PickUpMessageType selectedItem)
    {
        return _lastPickUpType == selectedItem && _displayAnim.IsAnimating && _samePickUpCount > 0;
    }

    //private bool _isEnabled;
    private float _curScaleFactor;
    private MeshGUIText _pickUpText;

    private PickUpMessageType _lastPickUpType;
    private int _samePickUpCount;

    private TemporaryDisplayAnim _displayAnim;
    #endregion
}
