
using Cmune.Realtime.Common;
using Cmune.Util;
using UberStrike.Realtime.Common;
using UnityEngine;
using UberStrike.Helper;

public class LocalCharacterState : ICharacterState
{
    int posSyncFrame;
    int bagSyncFrame;
    Vector3 lastPosition;
    int sendCounter = 0;

    public LocalCharacterState(CharacterInfo info, FpsGameMode game)
    {
        _myInfo = info;

        _game = game;

        posSyncFrame = SystemTime.Running;
        bagSyncFrame = SystemTime.Running;
    }

    /// <summary>
    /// only these values will synced from server to client
    /// </summary>
    private const int PlayerSyncMask = CharacterInfo.FieldTag.Armor | CharacterInfo.FieldTag.Health | CharacterInfo.FieldTag.Stats | CharacterInfo.FieldTag.TeamID | CharacterInfo.FieldTag.ReadyForGame;

    public void RecieveDeltaUpdate(SyncObject data)
    {
        data.DeltaCode &= PlayerSyncMask;

        if (data.DeltaCode != 0)
        {
            _myInfo.ReadSyncData(data, PlayerSyncMask);

            if (_updateRecievedEvent != null) _updateRecievedEvent(data);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ev"></param>
    public void SubscribeToEvents(CharacterConfig config)
    {
        _updateRecievedEvent = null;
        _updateRecievedEvent += config.OnCharacterStateUpdated;
    }

    public void UnSubscribeAll()
    {
        _updateRecievedEvent = null;
    }

    public void SendUpdates()
    {
        if (SystemTime.Running >= posSyncFrame)
        {
            posSyncFrame = SystemTime.Running + 50;

            if (lastPosition != GameState.LocalCharacter.Position)
            {
                sendCounter = 0;
            }
            else
            {
                sendCounter++;
            }

            //SEND POSITIONAL UPDATES
            if (sendCounter < 10)
            {
                lastPosition = GameState.LocalCharacter.Position;
                _game.SendPositionUpdate();
            }
        }

        if (SystemTime.Running >= bagSyncFrame)
        {
            bagSyncFrame = SystemTime.Running + 100;

            //SEND PLAYER DATA HERE
            _game.SendCharacterInfoUpdate();
        }
    }

    #region Properties

    public CharacterInfo Info { get { return _myInfo; } }

    public Vector3 LastPosition { get { return _myInfo.Position; } }

    #endregion

    #region Fields

    private CharacterInfo _myInfo;
    private FpsGameMode _game;

    private event System.Action<SyncObject> _updateRecievedEvent;

    #endregion
}
