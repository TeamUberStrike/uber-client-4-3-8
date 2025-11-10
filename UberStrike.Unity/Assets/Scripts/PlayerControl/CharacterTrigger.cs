using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CharacterTrigger : MonoBehaviour
{
    [SerializeField]
    private AvatarHudInformation _hud;

    public AvatarHudInformation HudInfo
    {
        get
        {
            if (_hud == null && _config != null && _config.Decorator != null) 
                return _config.Decorator.HudInformation;
            else 
                return _hud;
        }
    }

    [SerializeField]
    private CharacterConfig _config;

    public CharacterConfig Avatar
    {
        get { return _config; }
    }
}
