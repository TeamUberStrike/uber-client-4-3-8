
using UberStrike.Realtime.Common;
using Cmune.Realtime.Common;
using UnityEngine;

public interface ICharacterState
{
    CharacterInfo Info { get; }
    Vector3 LastPosition { get; }

    void RecieveDeltaUpdate(SyncObject delta);
    void SubscribeToEvents(CharacterConfig config);
    void UnSubscribeAll();
}
