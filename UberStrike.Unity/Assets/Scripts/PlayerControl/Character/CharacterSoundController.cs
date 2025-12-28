using UberStrike.Realtime.Common;

public class PlayerSound
{
    private CharacterInfo _character;
    private CharacterConfig _config;

    public PlayerSound(CharacterInfo character)
    {
        _character = character;
    }

    public void SetCharacter(CharacterConfig config)
    {
        _config = config;
    }

    public void Update()
    {
        if (_config == null || _config.Decorator == null) return;

        bool playWalkingSound = (_character.PlayerState & (PlayerStates.WADING | PlayerStates.SWIMMING | PlayerStates.DIVING | PlayerStates.GROUNDED)) != 0;
        bool isMoving = (_character.Is(PlayerStates.DIVING) && _character.Keys != 0) || ((_character.Keys & KeyState.Walking) != 0);
        if (playWalkingSound && isMoving && _config.Decorator.CanPlayFootSound)
        {
            if (_character.Is(PlayerStates.WADING))
            {
                _config.Decorator.PlayFootSound(FootStepSoundType.Water, _config.WalkingSoundSpeed);
            }
            else if (_character.Is(PlayerStates.SWIMMING))
            {
                _config.Decorator.PlayFootSound(FootStepSoundType.Swim, _config.WalkingSoundSpeed);
            }
            else if (_character.Is(PlayerStates.DIVING))
            {
                _config.Decorator.PlayFootSound(FootStepSoundType.Dive, _config.WalkingSoundSpeed);
            }
            else
            {
                _config.Decorator.PlayFootSound(_config.WalkingSoundSpeed);
            }
        }
    }
}
