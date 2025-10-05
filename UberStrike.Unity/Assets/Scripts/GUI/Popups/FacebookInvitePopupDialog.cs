using System;
using UnityEngine;
using System.Collections;

class FacebookInvitePopupDialog : BaseEventPopup
{
    private const string _text = "UberStrike is more fun with friends! Assemble your troops & create a clan in game today!";
    private DynamicTexture _texture;

    public FacebookInvitePopupDialog()
        : this(_text, ApplicationDataManager.BaseImageURL + "pop_facebook.jpg")
    { }

    public FacebookInvitePopupDialog(string text, string imageUrl)
    {
        this.Text = text;
        this._texture = new DynamicTexture(imageUrl);
    }

    protected override void DrawGUI(UnityEngine.Rect rect)
    {
        //facebook image
        _texture.Draw(new Rect(20, 20, rect.width - 40, rect.height - 130));

        //text
        GUI.Label(new Rect(20, rect.height - 90, rect.width - 40, 50), Text, BlueStonez.label_interparkmed_11pt_left);

        if (GUI.Button(new Rect(rect.width - 170, rect.height - 50, 140, 30), "Invite!", BlueStonez.button_green))
        {
            PopupSystem.HideMessage(this);
            if (Screen.fullScreen) ScreenResolutionManager.IsFullScreen = false;
            ApplicationDataManager.Instance.ShowFBInviteFriendsLightbox();
        }
    }
}