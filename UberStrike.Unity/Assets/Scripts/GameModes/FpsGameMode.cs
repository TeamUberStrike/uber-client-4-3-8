using System;
using System.Collections.Generic;
using System.Text;
using Cmune.Realtime.Common;
using Cmune.Realtime.Common.IO;
using Cmune.Realtime.Common.Synchronization;
using Cmune.Realtime.Common.Utils;
using Cmune.Realtime.Photon.Client;
using Cmune.Util;
using UberStrike.Core.Types;
using UberStrike.DataCenter.Common.Entities;
using UberStrike.Helper;
using UberStrike.Realtime.Common;
using UnityEngine;

public abstract class FpsGameMode : ClientGameMode
{
    protected FpsGameMode(RemoteMethodInterface rmi, GameMetaData gameData)
        : base(rmi, gameData)
    {
        _singleWeaponSettings = new Dictionary<GameFlags.GAME_FLAGS, int[]>(4);

        _singleWeaponSettings.Add(GameFlags.GAME_FLAGS.CannonArena, new int[] { 0, 1020, 0, 0 });
        _singleWeaponSettings.Add(GameFlags.GAME_FLAGS.Instakill, new int[] { 0, 1147, 0, 0 });
        _singleWeaponSettings.Add(GameFlags.GAME_FLAGS.NinjaArena, new int[] { 1136, 0, 0, 0 });
        _singleWeaponSettings.Add(GameFlags.GAME_FLAGS.SniperArena, new int[] { 0, 1018, 0, 0 });

        ApplyGameFlags();

        _characterByActorId = new Dictionary<int, CharacterConfig>(16);

        _stateInterpolator = new GameStateInterpolator();
        _localStateSender = new LocalCharacterState(GameState.LocalCharacter, this);
    }

    public void FixedUpdate()
    {
        if (IsGameStarted && HasJoinedGame)
        {
            _localStateSender.SendUpdates();
        }
    }

    public void Update()
    {
        if (IsGameStarted)
        {
            _stateInterpolator.Interpolate();
        }
    }

    public static bool IsSingleWeapon(GameMetaData data)
    {
        return GameFlags.IsFlagSet(GameFlags.GAME_FLAGS.NinjaArena, data.GameModifierFlags) ||
                    GameFlags.IsFlagSet(GameFlags.GAME_FLAGS.CannonArena, data.GameModifierFlags) ||
                    GameFlags.IsFlagSet(GameFlags.GAME_FLAGS.SniperArena, data.GameModifierFlags) ||
                    GameFlags.IsFlagSet(GameFlags.GAME_FLAGS.Instakill, data.GameModifierFlags);
    }

    public static string GetGameFlagText(GameMetaData data)
    {
        string str = string.Empty;

        if (GameFlags.IsFlagSet(GameFlags.GAME_FLAGS.CannonArena, data.GameModifierFlags))
            str = LocalizedStrings.CannonArena;
        else if (GameFlags.IsFlagSet(GameFlags.GAME_FLAGS.Instakill, data.GameModifierFlags))
            str = LocalizedStrings.Instakill;
        else if (GameFlags.IsFlagSet(GameFlags.GAME_FLAGS.LowGravity, data.GameModifierFlags))
            str = LocalizedStrings.LowGravity;
        else if (GameFlags.IsFlagSet(GameFlags.GAME_FLAGS.NinjaArena, data.GameModifierFlags))
            str = LocalizedStrings.NinjaArena;
        else if (GameFlags.IsFlagSet(GameFlags.GAME_FLAGS.SniperArena, data.GameModifierFlags))
            str = LocalizedStrings.SniperArena;

        return str;
    }

    public virtual void RespawnPlayer()
    {
        GameState.LocalPlayer.SetWeaponControlState(PlayerHudState.Playing);

        IsWaitingForSpawn = false;
        Vector3 pos = Vector3.zero;
        Quaternion rot = Quaternion.identity;

        SpawnPointManager.Instance.GetSpawnPointAt(_nextSpawnPoint, (GameMode)GameData.GameMode, TeamID.NONE, out pos, out rot);
        SpawnPlayerAt(pos, rot);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (GameData != null)
        {
            SendMethodToServer(FpsGameRPC.SetSpawnPoints,
                (byte)SpawnPointManager.Instance.GetSpawnPointCount((GameMode)GameData.GameMode, TeamID.NONE),
                (byte)SpawnPointManager.Instance.GetSpawnPointCount((GameMode)GameData.GameMode, TeamID.RED),
                (byte)SpawnPointManager.Instance.GetSpawnPointCount((GameMode)GameData.GameMode, TeamID.BLUE));
        }

        if (_hasGameStarted)
        {
            InitializeMode(GameState.LocalCharacter.TeamID, GameState.LocalCharacter.IsSpectator);
        }
    }

    public void InitializeMode(TeamID team = TeamID.NONE, bool isSpectator = false)
    {
        _hasGameStarted = true;

        GameState.LocalCharacter.ResetState();

        //here we write the ActorId and CmuneRoomID into the CharacterInfo!
        GameState.LocalCharacter.ActorId = _rmi.Messenger.PeerListener.ActorIdSecure;
        GameState.LocalCharacter.CurrentRoom = _rmi.Messenger.PeerListener.CurrentRoom;
        GameState.LocalCharacter.Channel = ApplicationDataManager.Channel;
        GameState.LocalCharacter.TeamID = team;

        //get all account data
        GameState.LocalCharacter.Cmid = PlayerDataManager.CmidSecure;
        GameState.LocalCharacter.PlayerName = PlayerDataManager.IsPlayerInClan ? string.Format("[{0}] {1}", PlayerDataManager.ClanTag, PlayerDataManager.NameSecure) : PlayerDataManager.NameSecure;
        GameState.LocalCharacter.ClanTag = PlayerDataManager.GroupTag;
        GameState.LocalCharacter.Level = PlayerDataManager.PlayerLevelSecure;

        if (isSpectator)
        {
            GameState.LocalPlayer.SetWeaponControlState(PlayerHudState.Spectating);

            GameState.LocalCharacter.Set(PlayerStates.SPECTATOR);
        }
        else
        {
            if (PlayerSpectatorControl.Instance.IsEnabled)
            {
                GameState.LocalPlayer.SetWeaponControlState(PlayerHudState.Spectating);
            }
            else
            {
                GameState.LocalPlayer.SetWeaponControlState(PlayerHudState.AfterRound);
            }

            GameState.LocalPlayer.UpdateLocalCharacterLoadout();
        }

        SendMethodToServer(GameRPC.Join, GameState.LocalCharacter);
#if !UNITY_ANDROID && !UNITY_IPHONE
        if (LevelCamera.HasCamera)
            LevelCamera.Instance.EnableLowPassFilter(false);
#endif
        OnModeInitialized();

        CmuneEventHandler.Route(new OnModeInitializedEvent());
    }

    protected virtual void OnModeInitialized() { }

    public void DebugAll()
    {
        StringBuilder b = new StringBuilder();
        b.AppendFormat("_avatars: {0}", this._characterByActorId.Count);
        b.AppendFormat("_coolDownTime: {0}", this._nextSpawnCountdown);
        b.AppendFormat("_instanceID: {0}", this._instanceID);
        b.AppendFormat("_lookupIndexMethod: {0}", this._lookupIndexMethod.Count);
        b.AppendFormat("_lookupNameIndex: {0}", this._lookupNameIndex.Count);
        b.AppendFormat("_nextSpawnPoint: {0}", this._nextSpawnPoint);
        b.AppendFormat("IsGameStarted: {0}", this.IsGameStarted);
        b.AppendFormat("IsGlobal: {0}", this.IsGlobal);
        b.AppendFormat("IsInitialized: {0}", this.IsInitialized);
        b.AppendFormat("IsRoundRunning: {0}", this.IsMatchRunning);
        b.AppendFormat("NetworkID: {0}", this.NetworkID);
        b.AppendFormat("Players: {0}", this.Players.Count);
        Debug.Log(b.ToString());
    }

    protected override void OnUninitialized()
    {
        ChatManager.Instance.UpdateLastgamePlayers();

        SendMethodToServer(GameRPC.Leave, MyActorId);

        int[] player = Conversion.ToArray<int>(Players.Keys);
        foreach (int i in player)
            OnPlayerLeft(i);

        base.OnUninitialized();
    }

    protected override void Dispose(bool dispose)
    {
        PlayerSpectatorControl.Instance.IsEnabled = false;

        if (_isDisposed) return;

        if (_previousLoadoutWeaponIds != null)
            LoadoutManager.Instance.SetLoadoutWeapons(_previousLoadoutWeaponIds);

        if (dispose)
        {
            IsMatchRunning = false;

            InGameChatHud.Instance.ClearHistory();
            PopupSystem.ClearAll();

            if (GameState.LocalDecorator)
                GameState.LocalDecorator.MeshRenderer.enabled = true;

            //remove all instances of final word grenades
            foreach (var stickyObject in GameObject.FindGameObjectsWithTag("Sticky"))
            {
                GameObject.Destroy(stickyObject);
            }

            //destroy all avatars
            UnloadAllPlayers();
        }

        base.Dispose(dispose);
    }

    private void ApplyGameFlags()
    {
        GameFlags.GAME_FLAGS flag = (GameFlags.GAME_FLAGS)GameData.GameModifierFlags;

        if (_singleWeaponSettings.ContainsKey(flag))
        {
            int[] setting = _singleWeaponSettings[flag];

            _previousLoadoutWeaponIds = LoadoutManager.Instance.SetLoadoutWeapons(setting);
        }
    }

    private void ConfigureCharacter(UberStrike.Realtime.Common.CharacterInfo info, CharacterConfig character, bool isLocal)
    {
        if (character != null && info != null)
        {
            // if that's our own avatar we set the reference to the LocalCharacterState
            if (isLocal)
            {
                GameState.LocalPlayer.SetCurrentCharacterConfig(character);
                // QuickItemController.Instance.Restriction.RenewGameUses();

                _localStateSender.Info.Position = GameState.LocalDecorator.transform.position + Vector3.up;
                _localStateSender.Info.HorizontalRotation = GameState.LocalDecorator.transform.rotation;

                //setup decorator
                character.Initialize(_localStateSender, GameState.LocalDecorator);

                GameState.LocalPlayer.MoveController.IsLowGravity = GameFlags.IsFlagSet(GameFlags.GAME_FLAGS.LowGravity, GameData.GameModifierFlags);
            }
            // otherwise we set the reference to a RemoteCharacterState
            else
            {
                AvatarDecorator decorator = AvatarBuilder.Instance.CreateRemoteAvatar(info.Gear.ToArray(), info.SkinColor);

                // here we assign the state interpolation to the character
                character.Initialize(_stateInterpolator.GetState(info.ActorId), decorator);

                // only show the message of joined players for the ones joining after me
                if (info.ActorId > MyActorId)
                    HudUtil.Instance.AddInGameEvent(info.PlayerName, LocalizedStrings.JoinedTheGame, UberstrikeItemClass.FunctionalGeneral, InGameEventFeedbackType.CustomMessage, info.TeamID, TeamID.NONE);
            }

            OnCharacterLoaded();
        }
        else
        {
            CmuneDebug.LogError("OnAvatarLoaded failed because loaded Avatar is {0} and Info is {1}", character != null, info != null);
        }
    }

    protected virtual void OnCharacterLoaded() { }

    #region NETWORK EVENTS

    protected override sealed void OnPlayerJoined(SyncObject data, Vector3 position)
    {
        base.OnPlayerJoined(data, position);

        ChatManager.Instance.SetGameSection(GameData.RoomID, Players.Values);

        UberStrike.Realtime.Common.CharacterInfo player;
        if (Players.TryGetValue(data.Id, out player))
        {
            OnNormalJoin(player);
        }
        else
        {
            GameState.LocalPlayer.UnPausePlayer();
        }
    }

    protected virtual void OnNormalJoin(UberStrike.Realtime.Common.CharacterInfo info)
    {
        if (info.ActorId != MyActorId)
        {
            // Insert the CharacterInfo in the synchronization / interpolation system
            _stateInterpolator.AddCharacterInfo(info);
        }
        else
        {
            //Debug.LogWarning("SetPowerUpCount: " + PickupItem.GetRespawnDurations().Count);
            SendMethodToServer(FpsGameRPC.SetPowerUpCount, MyActorId, PickupItem.GetRespawnDurations());
        }

        InstantiateCharacter(info);
    }

    protected override void OnPlayerLeft(int actorId)
    {
        try
        {
            //Remove avatar instance
            UberStrike.Realtime.Common.CharacterInfo info = GetPlayerWithID(actorId);

            if (info != null)
                EventStreamHud.Instance.AddEventText(info.PlayerName, info.TeamID, LocalizedStrings.LeftTheGame);

            //Delete Asset
            CharacterConfig character;
            if (_characterByActorId.TryGetValue(actorId, out character))
            {
                if (character)
                {
                    character.Destroy();
                }

                _characterByActorId.Remove(actorId);
            }

            //Remove reference to Input layer / simulator
            if (actorId == GameState.LocalCharacter.ActorId)
            {
                GameState.LocalCharacter.ResetState();
                GameState.LocalPlayer.SetCurrentCharacterConfig(null);
            }
            else
            {
                //stop and remove CharacterInfo Interpolation
                _stateInterpolator.RemoveCharacterInfo(actorId);
            }
        }
        catch (Exception e)
        {
            throw CmuneDebug.Exception(e.InnerException, "OnPlayerLeft with actorId={0}", actorId);
        }
        finally
        {
            base.OnPlayerLeft(actorId);

            ChatManager.Instance.SetGameSection(GameData.RoomID, Players.Values);
        }
    }

    public void SendCharacterInfoUpdate()
    {
        if (IsInitialized)
        {
            if (GameState.LocalCharacter.ActorId > 0 && !GameState.LocalCharacter.IsSpectator)
            {
                SyncObject s = SyncObjectBuilder.GetSyncData(GameState.LocalCharacter, false);
                if (!s.IsEmpty)
                {
                    SendMethodToServer(GameRPC.PlayerUpdate, s);
                }
            }
        }
    }

    public void SendPositionUpdate()
    {
        if (IsInitialized && GameState.LocalCharacter.PlayerNumber > 0)
        {
            List<byte> bytes = new List<byte>(14);
            DefaultByteConverter.FromInt(GameState.LocalCharacter.ActorId, ref bytes);
            ShortVector3.Bytes(bytes, GameState.LocalCharacter.Position);
            DefaultByteConverter.FromInt(GameConnectionManager.Client.PeerListener.ServerTimeTicks, ref bytes);
            SendUnreliableMethodToServer(FpsGameRPC.PositionUpdate, (object)bytes);
        }
    }

    [NetworkMethod(FpsGameRPC.PositionUpdate)]
    protected void OnPositionsUpdate(List<byte> positions)
    {
        int idx = 0;
        int count = positions[idx++];
        byte[] bytes = positions.ToArray();

        List<PlayerPosition> all = new List<PlayerPosition>(count);
        for (int i = 0; i < count && idx + 11 <= bytes.Length; ++i)
        {
            //adding local latency to timestamp on deserialization
            byte id = bytes[idx++];
            int timeStamp = DefaultByteConverter.ToInt(bytes, ref idx);
            ShortVector3 pos = new ShortVector3(bytes, ref idx);

            all.Add(new PlayerPosition(id, pos, timeStamp));
        }

        _stateInterpolator.UpdatePositionSmooth(all);
    }

    protected override void OnGameFrameUpdate(List<SyncObject> deltas)
    {
        try
        {
            bool teamsChanged = false;

            foreach (var data in deltas)
            {
                if (!data.IsEmpty)
                {
                    if (data.Id == MyActorId)
                    {
                        ApplyCurrentGameFrameUpdates(data);

                        //update current player state
                        _localStateSender.RecieveDeltaUpdate(data);
                    }
                    else
                    {
                        _stateInterpolator.UpdateCharacterInfo(data);
                    }

                    if (data.Contains(UberStrike.Realtime.Common.CharacterInfo.FieldTag.TeamID))
                    {
                        teamsChanged = true;
                    }
                }
            }

            if (teamsChanged)
            {
                UpdatePlayerCounters();
            }
        }
        catch (Exception e)
        {
            e.Data.Add("OnGameFrameUpdate", deltas.Count);
            throw;// CmuneDebug.Exception("OnGameFrameUpdate failed with: {0}\n{1}", e.Message, e.StackTrace);
        }
    }

    protected virtual void UpdatePlayerCounters() { }

    protected virtual void ApplyCurrentGameFrameUpdates(SyncObject delta)
    {
        try
        {
            if (delta.Contains(UberStrike.Realtime.Common.CharacterInfo.FieldTag.Health))
            {
                int newHealth = (short)delta.Data[UberStrike.Realtime.Common.CharacterInfo.FieldTag.Health];
                HpApHud.Instance.HP = newHealth;

                if (newHealth <= 0)
                {
                    GameState.LocalPlayer.SetPlayerDead();
                }
            }

            if (delta.Contains(UberStrike.Realtime.Common.CharacterInfo.FieldTag.Armor))
            {
                ArmorInfo newArmor = (ArmorInfo)delta.Data[UberStrike.Realtime.Common.CharacterInfo.FieldTag.Armor];
                HpApHud.Instance.AP = newArmor.ArmorPoints;
            }

            if (delta.Contains(UberStrike.Realtime.Common.CharacterInfo.FieldTag.Stats))
            {
                StatsInfo stats = (StatsInfo)delta.Data[UberStrike.Realtime.Common.CharacterInfo.FieldTag.Stats];
                //Debug.LogError("%%%%%%%%%% XP AND POINTS: " + CmunePrint.Properties(stats) + " " + CmunePrint.Properties(GameState.LocalCharacter.Stats));

                if (stats.XP == 0 && stats.Points == 0 && stats.Kills == 0 && stats.Deaths == 0)
                {
                    XpPtsHud.Instance.OnGameStart(PlayerDataManager.PlayerLevelSecure);
                }
                else
                {
                    XpPtsHud.Instance.GainXp(stats.XP - GameState.LocalCharacter.XP);
                    XpPtsHud.Instance.GainPoints(stats.Points - GameState.LocalCharacter.Points);
                }
            }
        }
        catch (Exception e)
        {
            throw CmuneDebug.Exception(e.InnerException, "ApplyCurrentGameFrameUpdates with delta.Id={0} and DeltaCode={1}", delta.Id, delta.DeltaCode);
        }
    }

    protected override void OnStartGame()
    {
        base.OnStartGame();

        _stateInterpolator.Run();
    }

    [NetworkMethod(FpsGameRPC.MatchStart)]
    protected virtual void OnMatchStart(int matchCount, int matchEndServerTicks)
    {
        GameConnectionManager.Client.PeerListener.UpdateServerTime();
#if !UNITY_IPHONE
        CheatDetection.SyncSystemTime();
#endif

        IsMatchRunning = true;
        _roundStartTime = matchEndServerTicks - (GameData.RoundTime * 1000);
        _stateInterpolator.Run();

        if (LevelCamera.Exists) LevelCamera.Instance.ResetFeedback();

        GameState.LocalPlayer.UpdateWeaponController();

        foreach (CharacterConfig cfg in _characterByActorId.Values)
        {
            cfg.IsAnimationEnabled = true;
        }

        CmuneEventHandler.Route(new OnMatchStartEvent()
        {
            MatchCount = matchCount,
            MatchEndServerTicks = matchEndServerTicks
        });
    }

    /// <summary>
    /// Notice: This function should be called by the function listening to the FpsGameRPC.MatchEnd in each derivitive of FpsGameMode.
    /// We break the usual inheritance model here because the method contract for the MatchEnd event does not carry the same parameters for all different game modes.
    /// </summary>
    [NetworkMethod(FpsGameRPC.MatchEnd)]
    protected void OnMatchEnd(EndOfMatchData matchData)
    {
        //make sure that dead people are respawn properly
        if (GameState.LocalPlayer.IsDead)
        {
            if (GameState.LocalDecorator != null)
            {
                GameState.LocalDecorator.DisableRagdoll();
            }

            Vector3 pos = Vector3.zero;
            Quaternion rot = Quaternion.identity;
            SpawnPointManager.Instance.GetSpawnPointAt(_nextSpawnPoint, (GameMode)GameData.GameMode, GameState.LocalCharacter.TeamID, out pos, out rot);
            GameState.LocalPlayer.SpawnPlayerAt(pos, rot);
        }

        if (GameState.LocalPlayer.Character != null)
        {
            switch (WeaponController.Instance.CurrentSlot)
            {
                case LoadoutSlotType.WeaponMelee:
                    GameState.LocalPlayer.Character.WeaponSimulator.UpdateWeaponSlot(0, false); break;
                case LoadoutSlotType.WeaponPrimary:
                    GameState.LocalPlayer.Character.WeaponSimulator.UpdateWeaponSlot(1, false); break;
                case LoadoutSlotType.WeaponSecondary:
                    GameState.LocalPlayer.Character.WeaponSimulator.UpdateWeaponSlot(2, false); break;
                case LoadoutSlotType.WeaponTertiary:
                    GameState.LocalPlayer.Character.WeaponSimulator.UpdateWeaponSlot(3, false); break;
            }
        }

        //stop bobbing around
        LevelCamera.SetBobMode(BobMode.Idle);

        //TODO: no, thats not the right way to fix a bug!
        if (WeaponController.Instance.CurrentDecorator)
            WeaponController.Instance.CurrentDecorator.StopSound();

        //make sure to disable all animations
        foreach (CharacterConfig character in _characterByActorId.Values)
        {
            character.State.Set(PlayerStates.PAUSED, true);
        }

        //disable the weapon control
        GameState.LocalPlayer.SetWeaponControlState(PlayerHudState.AfterRound);

        UpdatePlayerStatistics(matchData.PlayerStatsTotal, matchData.PlayerStatsBestPerLife);

        EndOfMatchStats.Instance.Data = matchData;

        GameState.LocalPlayer.SetPlayerControlState(LocalPlayer.PlayerState.Overview);

        CmuneEventHandler.Route(new OnMatchEndEvent());
        OnEndOfMatch();
    }

    protected virtual void OnEndOfMatch() { }

    private void UpdatePlayerStatistics(StatsCollection totalStats, StatsCollection bestPerLife)
    {
        if (PlayerDataManager.PlayerLevelSecure > 0 && PlayerDataManager.PlayerLevelSecure < PlayerXpUtil.MaxPlayerLevel)
        {
            int oldXp = PlayerDataManager.PlayerExperienceSecure;

            PlayerStatisticsView v = ConvertStatistics.UpdatePlayerStatisticsView(PlayerDataManager.Instance.ServerLocalPlayerStatisticsView,
                PlayerDataManager.PlayerLevelSecure, totalStats, bestPerLife);
            //update Player statistics
            PlayerDataManager.Instance.SetPlayerStatisticsView(v);

            if (PlayerDataManager.PlayerLevelSecure != PlayerXpUtil.GetLevelForXp(oldXp))
            {
                PopupSystem.Show(new LevelUpPopup(PlayerDataManager.PlayerLevelSecure, null));
            }

            //show XP increase in GlobalUIRibbon
            GlobalUIRibbon.Instance.AddXPEvent(totalStats.Xp);

            //if player earned points we update his wallet
            if (totalStats.Points > 0)
            {
                PlayerDataManager.AddPointsSecure(totalStats.Points);
                GlobalUIRibbon.Instance.AddPointsEvent(totalStats.Points);
            }

            //update the players level
            GameState.LocalCharacter.Level = PlayerDataManager.PlayerLevelSecure;
        }
    }

    [NetworkMethod(FpsGameRPC.SetPowerupState)]
    protected void OnSetPowerupState(List<int> pickedPowerupIds)
    {
        //Debug.LogWarning("OnSetPowerupState " + pickedPowerupIds.Count);

        for (int i = 0; pickedPowerupIds != null && i < pickedPowerupIds.Count; i++)
        {
            CmuneEventHandler.Route(new PickupItemEvent(pickedPowerupIds[i], false));
        }
    }

    [NetworkMethod(FpsGameRPC.PowerUpPicked)]
    protected void OnPowerUpPicked(int powerupID, byte state)
    {
        CmuneEventHandler.Route(new PickupItemEvent(powerupID, state == 0 ? true : false));
    }

    [NetworkMethod(FpsGameRPC.DoorOpen)]
    protected void OnDoorOpened(int doorID)
    {
        CmuneEventHandler.Route(new DoorOpenedEvent(doorID));
    }

    [NetworkMethod(FpsGameRPC.SetNextSpawnPointForPlayer)]
    protected virtual void OnSetNextSpawnPoint(int index, int coolDownTime)
    {
        if (!GameState.LocalCharacter.IsSpectator)
            RespawnPlayerInSeconds(index, coolDownTime);
    }

    public void RespawnPlayerInSeconds(int index, int seconds)
    {
        _nextSpawnPoint = index;

        if (seconds > 0)
        {
            _nextSpawnCountdown = seconds;
            NextSpawnTime = Time.time + seconds;
            IsWaitingForSpawn = true;
        }
        else
        {
            RespawnPlayer();
        }
    }

    public bool HasAvatarLoaded(int actorId)
    {
        return _characterByActorId.ContainsKey(actorId);
    }

    /// <summary>
    /// Starts End of Match Screen and showing the time to next Round
    /// </summary>
    /// <param name="secondsUntilNextRound"></param>
    [NetworkMethod(FpsGameRPC.SetEndOfRoundCountdown)]
    protected void OnSetEndOfRoundCountdown(int secondsUntilNextRound)
    {
        CmuneEventHandler.Route(new OnSetEndOfMatchCountdownEvent()
        {
            SecondsUntilNextMatch = secondsUntilNextRound
        });
    }

    [NetworkMethod(FpsGameRPC.PlayerEvent)]
    protected virtual void OnDamageEvent(DamageEvent ev)
    {
        if (GameState.HasCurrentPlayer)
        {
            foreach (var v in ev.Damage)
            {
                CmuneEventHandler.Route(new OnPlayerDamageEvent()
                {
                    Angle = Conversion.Byte2Angle(v.Key),
                    DamageValue = v.Value
                });
                if (((DamageEffectType)ev.DamageEffectFlag &
                      DamageEffectType.SlowDown)
                     != DamageEffectType.None)
                {
                    GameState.LocalPlayer.DamageFactor = ev.DamgeEffectValue;
                }
            }
        }
    }

    [NetworkMethod(FpsGameRPC.SplatGameEvent)]
    protected virtual void OnSplatGameEvent(int shooter, int target, byte weaponClass, byte bodyPart)
    {
        UberStrike.Realtime.Common.CharacterInfo shooterInfo, targetInfo;
        if (Players.TryGetValue(shooter, out shooterInfo) && Players.TryGetValue(target, out targetInfo))
        {
            UberstrikeItemClass category = (UberstrikeItemClass)weaponClass;
            BodyPart bodyHitPart = (BodyPart)bodyPart;

            if (shooter != target)
            {
                if (shooter == MyActorId)
                {
                    CmuneEventHandler.Route(new OnPlayerKillEnemyEvent()
                    {
                        EmemyInfo = targetInfo,
                        WeaponCategory = category,
                        BodyHitPart = bodyHitPart
                    });
                }
                else if (target == MyActorId)
                {
                    CmuneEventHandler.Route(new OnPlayerKilledEvent()
                    {
                        ShooterInfo = shooterInfo,
                        WeaponCategory = category,
                        BodyHitPart = bodyHitPart
                    });
                }
                else
                {
                    InGameEventFeedbackType type = InGameEventFeedbackType.None;
                    if (category == UberstrikeItemClass.WeaponMelee)
                    {
                        type = InGameEventFeedbackType.Humiliation;
                    }
                    else if (bodyPart == (byte)BodyPart.Head)
                    {
                        type = InGameEventFeedbackType.HeadShot;
                    }
                    else if (bodyPart == (byte)BodyPart.Nuts)
                    {
                        type = InGameEventFeedbackType.NutShot;
                    }

                    HudUtil.Instance.AddInGameEvent(shooterInfo.PlayerName, targetInfo.PlayerName, category, type, shooterInfo.TeamID, targetInfo.TeamID);
                }
            }
            else
            {
                CmuneEventHandler.Route(new OnPlayerSuicideEvent() { PlayerInfo = shooterInfo });
            }
        }
    }

    [NetworkMethod(FpsGameRPC.SetPlayerSpawnPosition)]
    protected void OnSetPlayerSpawnPosition(byte playerNumber, Vector3 pos)
    {
        _stateInterpolator.UpdatePositionHard(playerNumber, pos);
    }

    #endregion

    public void SendPlayerTeamChange()
    {
        SendMethodToServer(FpsGameRPC.PlayerTeamChange, GameState.CurrentPlayerID);
    }

    public void SetReadyForNextMatch(bool isReady)
    {
        if (isReady)
        {
            SendMethodToServer(FpsGameRPC.SetPlayerReadyForNextRound, MyActorId);
        }
    }

    public void SendPlayerSpawnPosition(Vector3 position)
    {
        SendMethodToServer(FpsGameRPC.SetPlayerSpawnPosition, MyActorId, position);
    }

    public void UpdatePlayerReadyForNextRound()
    {
        _playerReadyForNextRound = 0;
        foreach (UberStrike.Realtime.Common.CharacterInfo c in Players.Values)
        {
            if (c.IsReadyForGame) _playerReadyForNextRound++;
        }
    }

    protected void SpawnPlayerAt(Vector3 position, Quaternion rotation)
    {
        try
        {
            GameState.LocalPlayer.SpawnPlayerAt(position, rotation);
            GameState.LocalPlayer.InitializePlayer();

            CmuneEventHandler.Route(new OnPlayerRespawnEvent());
            GameState.LocalPlayer.UpdateWeaponController();

            SendMethodToServer(GameRPC.ResetPlayer, MyActorId);
        }
        catch (Exception e)
        {
            throw CmuneDebug.Exception(e.InnerException, "SpawnPlayerAt with game {0}", CmunePrint.Properties(this));
        }
    }

    public virtual void RequestRespawn()
    {
        SendMethodToServer(FpsGameRPC.RequestRespawnForPlayer, MyActorId);
    }

    public virtual void IncreaseHealthAndArmor(int health, int armor)
    {
        SendMethodToServer(FpsGameRPC.IncreaseHealthAndArmor, MyActorId, (byte)health, (byte)armor);
    }

    public virtual void PickupPowerup(int pickupID, PickupItemType type, int value)
    {
        SendMethodToServer(FpsGameRPC.PowerUpPicked, MyActorId, pickupID, (byte)type, (byte)value);
    }

    public void OpenDoor(int doorID)
    {
        SendMethodToServer(FpsGameRPC.DoorOpen, doorID);
    }

    public void EmitQuickItem(Vector3 origin, Vector3 direction, int itemId, byte playerNumber, int projectileID)
    {
        SendMethodToAll(FpsGameRPC.EmitQuickItem, origin, direction, itemId, playerNumber, projectileID);
    }

    [NetworkMethod(FpsGameRPC.EmitQuickItem)]
    protected void OnEmitQuickItem(Vector3 origin, Vector3 direction, int itemId, byte playerNumber, int projectileID)
    {
        if (GameState.CurrentGame.IsGameStarted)
        {
            //Debug.LogError("OnEmitQuickItem " + projectileID);
            var item = ItemManager.Instance.GetItemInShop(itemId);
            var prefab = item.Prefab as IGrenadeProjectile;
            if (prefab != null)
            {
                var instance = prefab.Throw(origin, direction);

                if (playerNumber == GameState.CurrentPlayerID)
                {
                    instance.SetLayer(UberstrikeLayer.LocalProjectile);
                }
                else
                {
                    instance.SetLayer(UberstrikeLayer.RemoteProjectile);
                }

                ProjectileManager.Instance.AddProjectile(instance, projectileID);
            }
            else
            {
                //Debug.LogError("OnEmitQuickItem " + itemId + " " + playerNumber + " " + projectileID);
            }
        }
    }

    public void EmitProjectile(Vector3 origin, Vector3 direction, LoadoutSlotType slot, int projectileID, bool explode)
    {
        //Debug.LogError("EmitProjectile " + projectileID);
        SendMethodToAll(FpsGameRPC.EmitProjectile, MyActorId, origin, direction, (byte)slot, projectileID, explode);
    }

    [NetworkMethod(FpsGameRPC.EmitProjectile)]
    protected void OnEmitProjectile(int actorId, Vector3 origin, Vector3 direction, byte slot, int projectileID, bool explode)
    {
        //Debug.LogError("OnEmitProjectile " + projectileID);
        CharacterConfig character;
        if (TryGetCharacter(actorId, out character))
        {
            var p = character.WeaponSimulator.EmitProjectile(actorId, character.State.PlayerNumber, origin, direction, (LoadoutSlotType)slot, projectileID, explode);
            ProjectileManager.Instance.AddProjectile(p, projectileID);
        }
    }

    public void RemoveProjectile(int projectileId, bool explode)
    {
        //Debug.LogError("ExplodeProjectileAt " + projectileId);
        SendMethodToAll(FpsGameRPC.ExplodeProjectile, projectileId, explode);
    }

    [NetworkMethod(FpsGameRPC.ExplodeProjectile)]
    protected virtual void OnRemoveProjectile(int projectileId, bool explode)
    {
        ProjectileManager.Instance.RemoveProjectile(projectileId, explode);
    }

    public void SingleBulletFire()
    {
        SendMethodToAll(FpsGameRPC.SingleBulletFire, MyActorId);
    }

    [NetworkMethod(FpsGameRPC.SingleBulletFire)]
    protected virtual void OnSingleBulletFire(int actorId)
    {
        CharacterConfig character;
        if (TryGetCharacter(actorId, out character))
        {
            if (character.State.IsAlive && !character.IsLocal)
                character.WeaponSimulator.Shoot(character.State);
        }
    }

    public virtual void PlayerHit(int targetPlayer, short damage, BodyPart part, Vector3 force, int projectileId, int weaponID,
        UberstrikeItemClass weaponClass, DamageEffectType damageEffectFlag, float damageEffectValue)
    {
        Vector3 dir = force.normalized;
        dir.y = 0;
        Quaternion lookRot = Quaternion.LookRotation(dir);
        byte angle = Conversion.Angle2Byte(lookRot.eulerAngles.y);

        SendMethodToServer(FpsGameRPC.PlayerHit, MyActorId, targetPlayer, damage, (byte)part, projectileId, angle, weaponID, (byte)weaponClass, (int)damageEffectFlag, damageEffectValue);

        //for self hits - simulate results to hide network lag
        if (MyActorId == targetPlayer)
        {
            short finalDamage;
            byte finalArmorPoints;
            GameState.LocalCharacter.Armor.SimulateAbsorbDamage(damage, part, out finalDamage, out finalArmorPoints);

            HpApHud.Instance.HP = GameState.LocalCharacter.Health - finalDamage;
            HpApHud.Instance.AP = finalArmorPoints;

            GameState.LocalPlayer.MoveController.ApplyForce(force, CharacterMoveController.ForceType.Additive);
        }
    }

    public void SendPlayerHitFeedback(int targetPlayer, Vector3 force)
    {
        //Debug.LogWarning("SendPlayerHitFeedback " + targetPlayer + " " + force);
        SendMethodToPlayer(targetPlayer, FpsGameRPC.PlayerHit, force);
    }

    [NetworkMethod(FpsGameRPC.PlayerHit)]
    public void OnPlayerHit(Vector3 force)
    {
        GameState.LocalPlayer.MoveController.ApplyForce(force, CharacterMoveController.ForceType.Additive);
    }

    public void ActivateQuickItem(QuickItemLogic logic, int robotLifeTime, int scrapsLifeTime, bool isInstant = false)
    {
        SendMethodToAll(FpsGameRPC.QuickItemEvent, GameState.LocalCharacter.ActorId,
            (byte)logic, robotLifeTime, scrapsLifeTime, isInstant);
    }

    [NetworkMethod(FpsGameRPC.QuickItemEvent)]
    public void OnQuickItemEvent(int actorId, byte eventType, int robotLifeTime, int scrapsLifeTime, bool isInstant)
    {
        CharacterConfig character;
        if (TryGetCharacter(actorId, out character))
        {
            QuickItemSfxController.Instance.ShowThirdPersonEffect(character,
                (QuickItemLogic)(int)eventType, robotLifeTime, scrapsLifeTime, isInstant);
        }
    }

    [NetworkMethod(FpsGameRPC.KickFromGame)]
    protected void KickPlayer(string message)
    {
        GameStateController.Instance.UnloadGameMode();
        MenuPageManager.Instance.LoadPage(PageType.Home);

        PopupSystem.ShowMessage("Cheat Detection", message, PopupSystem.AlertType.OK, delegate() { });
    }

    [NetworkMethod(CommRPC.ModerationCustomMessage)]
    protected void OnModCustomMessage(string message)
    {
        CommConnectionManager.CommCenter.OnModerationCustomMessage(message);
    }

    [NetworkMethod(CommRPC.ModerationMutePlayer)]
    protected void OnMutePlayer(bool mute)
    {
        CommConnectionManager.CommCenter.OnModerationMutePlayer(mute);
    }

    public bool TryGetCharacter(int actorId, out  CharacterConfig character)
    {
        return _characterByActorId.TryGetValue(actorId, out character) && character != null;
    }

    public bool TryGetDecorator(int actorId, out  AvatarDecorator decorator)
    {
        CharacterConfig character;
        if (_characterByActorId.TryGetValue(actorId, out character) && character != null)
            decorator = character.Decorator;
        else
            decorator = null;
        return decorator != null;
    }

    protected void HideRemotePlayerHudFeedback()
    {
        foreach (CharacterConfig character in _characterByActorId.Values)
        {
            if (character != null && character.Decorator != null)
                character.Decorator.HudInformation.Hide();
        }
    }

    public bool TryGetPlayerWithCmid(int cmid, out UberStrike.Realtime.Common.CharacterInfo config)
    {
        config = null;
        foreach (var i in Players.Values)
            if (i.Cmid == cmid)
            {
                config = i;
                break;
            }

        return config != null;
    }

    protected void InstantiateCharacter(UberStrike.Realtime.Common.CharacterInfo info)
    {
        //only load a character, if tyhere is no existing entry for this ActorId already
        if (!_characterByActorId.ContainsKey(info.ActorId))
        {
            //if that's our own avatar we set the reference to the LocalCharacterState
            if (info.ActorId == MyActorId)
            {
                _isLocalAvatarLoaded = true;
                CharacterConfig character = PrefabManager.Instance.InstantiateLocalCharacter();
                _characterByActorId.Add(info.ActorId, character);
                ConfigureCharacter(info, character, true);
            }
            else
            {
                CharacterConfig character = PrefabManager.Instance.InstantiateRemoteCharacter();
                _characterByActorId.Add(info.ActorId, character);
                ConfigureCharacter(info, character, false);
            }
        }
        else
        {
            CmuneDebug.LogError("Failed call of LoadAvatarAsset {0} because already loaded!", info.ActorId);
        }
    }

    protected void LeaveClientGameMode(int playerId)
    {
        base.OnPlayerLeft(playerId);
    }

    /// <summary>
    /// Changes all game players outline by setting it on for frends and removing from enemies
    /// </summary>
    /// <param name="myTeam">Team of my friends and me</param>
    public void ChangeAllPlayerOutline(TeamID myTeam)
    {
        if (myTeam != TeamID.NONE)
        {
            foreach (var kvp in _characterByActorId)
            {
                if (kvp.Key != MyActorId) UpdatePlayerOutlineByTeamID(kvp.Value, myTeam);
            }
        }
    }

    public void ChangePlayerOutlineById(int playerID)
    {
        CharacterConfig cfg;
        if (TryGetCharacter(playerID, out cfg))
        {
            ChangePlayerOutline(cfg);
        }
    }

    /// <summary>
    /// Change player outline by setting it on for friend and off for enemy
    /// </summary>
    /// <param name="player">Player CharacterConfig</param>
    /// <param name="myteam">Team of my friends and me</param>
    public void ChangePlayerOutline(CharacterConfig player)
    {
        UpdatePlayerOutlineByTeamID(player, GameState.LocalCharacter.TeamID);
    }

    public void UpdatePlayerOutlineByTeamID(CharacterConfig player, TeamID id)
    {
        // uncomment this to enable back outline on friends
        if (player != null)
        {
            if (id != TeamID.NONE && player.Team == id)
            {
                player.Decorator.EnableOutline(true);
            }
            else
            {
                player.Decorator.EnableOutline(false);
            }
            //Debug.LogError("ChangePlayerOutline: " + myteam + " " + player.Name + " " + player.Team);
        }
        else
        {
            Debug.LogError("Failed to Change player outline");
        }
    }

    public void UnloadAllPlayers()
    {
        foreach (var character in _characterByActorId.Values)
        {
            character.Destroy();
        }

        _characterByActorId.Clear();
    }

    #region Properties

    public virtual bool IsWaitingForPlayers
    {
        get
        {
            return IsGameStarted && Players.Count <= 1;
        }
    }

    public bool IsGameAboutToEnd
    {
        get { return GameTime >= GameData.RoundTime - 1; }
    }

    public virtual bool CanShowTabscreen
    {
        get { return IsGameStarted || GameState.LocalCharacter.IsSpectator; }
    }

    public virtual float GameTime
    {
        get { return (float)(GameConnectionManager.Client.PeerListener.ServerTimeTicks - _roundStartTime) / 1000f; }
    }

    public bool IsWaitingForSpawn { get; protected set; }

    public float NextSpawnTime { get; private set; }

    public bool IsGameClosed
    {
        get { return _isGameClosed; }
        set { _isGameClosed = value; }
    }

    public ICharacterState MyCharacterState
    {
        get { return _localStateSender; }
    }

    public int PlayerReadyForNextRound
    {
        get { return _playerReadyForNextRound; }
    }

    public GameMode GameMode
    {
        get { return (GameMode)GameData.GameMode; }
    }

    public bool IsLocalAvatarLoaded
    {
        get { return _isLocalAvatarLoaded; }
    }

    public int CurrentSpawnPoint
    {
        get { return _nextSpawnPoint; }
    }

    public int PlayerCount
    {
        get { return _characterByActorId.Count; }
    }

    #endregion

    #region Fields

    protected bool _hasGameStarted = false;
    protected bool _isLocalAvatarLoaded = false;
    protected bool _isGameClosed = false;

    protected int _nextSpawnPoint = 0;
    protected int _nextSpawnCountdown = 0;

    private int _playerReadyForNextRound = 0;

    protected LocalCharacterState _localStateSender;
    protected GameStateInterpolator _stateInterpolator;

    protected Dictionary<int, CharacterConfig> _characterByActorId;

    public IEnumerable<CharacterConfig> AllCharacters { get { return _characterByActorId.Values; } }

    protected int _roundStartTime;

    private int[] _previousLoadoutWeaponIds;
    protected Dictionary<GameFlags.GAME_FLAGS, int[]> _singleWeaponSettings;

    #endregion
}
