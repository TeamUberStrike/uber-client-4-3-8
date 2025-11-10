using UnityEngine;

class PlayerLeadStatus : Singleton<PlayerLeadStatus>
{
    private PlayerLeadStatus() { }

    public void ResetPlayerLead()
    {
        _lastLead = LeadState.None;
    }

    public void PlayLeadAudio(int myKills, int otherKills, bool isLeading, bool playAudio = true)
    {
        if (myKills == 0 && otherKills == 0)
        {
            _lastLead = LeadState.None;
        }
        else
        {
            if (isLeading)
            {
                if (playAudio && _lastLead != LeadState.Me && myKills > 0)
                    SfxManager.Play2dAudioClip(SoundEffectType.GameTakenLead, 0.5f);
                _lastLead = LeadState.Me;
            }
            else
            {
                if (_lastLead == LeadState.Me)
                {
                    _lastLead = LeadState.Tied;
                    if (playAudio && myKills > 0)
                        SfxManager.Play2dAudioClip(SoundEffectType.GameTiedLead, 0.5f);
                }
                else if (_lastLead == LeadState.Tied)
                {
                    _lastLead = LeadState.Others;
                    if (playAudio)
                        SfxManager.Play2dAudioClip(SoundEffectType.GameLostLead, 0.5f);
                }
                else if (myKills == otherKills && myKills > 0)
                {
                    _lastLead = LeadState.Tied;
                    if (playAudio)
                        SfxManager.Play2dAudioClip(SoundEffectType.GameTiedLead, 0.5f);
                }
                else
                {
                    _lastLead = LeadState.Others;
                }
            }
        }
    }

    public bool IsLeading
    {
        get { return _lastLead == LeadState.Me; }
    }

    public void OnDeathMatchOver()
    {
        if (_lastLead == LeadState.Me)
        {
            SfxManager.StopAll2dAudio();
            SfxManager.Play2dAudioClip(SoundEffectType.GameYouWin, 1);
        }
        else if (_lastLead == LeadState.Others)
        {
            SfxManager.StopAll2dAudio();
            SfxManager.Play2dAudioClip(SoundEffectType.GameGameOver, 1);
        }
        else// if (_lastLead == LeadState.Tied)
        {
            SfxManager.StopAll2dAudio();
            SfxManager.Play2dAudioClip(SoundEffectType.GameDraw, 1);
        }
    }

    private enum LeadState
    {
        None,
        Me,
        Tied,
        Others
    }

    private LeadState _lastLead = LeadState.None;
}