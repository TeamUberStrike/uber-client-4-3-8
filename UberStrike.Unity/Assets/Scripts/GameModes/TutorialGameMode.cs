using System.Collections;
using UberStrike.DataCenter.Common.Entities;
using Cmune.Realtime.Common;
using Cmune.Realtime.Common.Utils;
using Cmune.Realtime.Photon.Client;
using UberStrike.Realtime.Common;
using UberStrike.WebService.Unity;
using UnityEngine;
using Cmune.Realtime.Common.Synchronization;
using UberStrike.Core.Types;

[NetworkClass(-1)]
public class TutorialGameMode : FpsGameMode, ITutorialCinematicSequenceListener
{
    private AvatarDecorator _airlockNPC;
    private TutorialCinematicSequence _sequence;
    private TutorialShootingTargetController _shootingRangeController;

    private float _fadeInAlpha = 1;
    private string _subtitle = string.Empty;

    /* GUI */
    private Vector2 _scale = Vector2.one;

    private MeshGUIText _txtObjectives;
    private MeshGUIText _txtObjUnderscore;
    private MeshGUIText _txtMouseLook;
    private MeshGUIText _txtWasdWalk;
    private MeshGUIText _txtToArmory;
    private MeshGUIText _txtPickupMG;
    private MeshGUIText _txtShoot3;
    private MeshGUIText _txtShoot6;
    private MeshGUIText _txtComplete;

    private ObjectiveTick _objMouseMove;
    private ObjectiveTick _objWasdWalk;
    private ObjectiveTick _objGotoArmory;
    private ObjectiveTick _objPickupWeapon;
    private ObjectiveTick _objShootTarget3;
    private ObjectiveTick _objShootTarget6;

    private bool _showObjective;
    private bool _showObjMouse;
    private bool _showObjWasdWalk;
    private bool _showGotoArmory;

    private float _blackBarHeight;

    private Rect _mousePos;
    private float _mouseXOffset;

    //private string _characterName = string.Empty;

    private KeyState[] _keys = new KeyState[] { KeyState.Forward, KeyState.Left, KeyState.Backward, KeyState.Right };

    public TutorialCinematicSequence Sequence { get { return _sequence; } }

    public ObjectiveTick ObjShootTarget3 { get { return _objShootTarget3; } }
    public ObjectiveTick ObjShootTarget6 { get { return _objShootTarget6; } }

    public TutorialGameMode(RemoteMethodInterface rmi)
        : base(rmi, new GameMetaData(0, string.Empty, 120, 0, (short)GameMode.Tutorial))
    {
        _sequence = new TutorialCinematicSequence(this);

        LoadoutManager.Instance.SetLoadoutWeapons(new int[] { 0, 0, 0, 0 });

        MonoRoutine.Start(StartTutorialMode());
    }

    public void DrawGui()
    {
        GUI.depth = (int)GuiDepth.Hud;

        DrawFadeInRect();
        DrawSubtitle();
        //DrawContinueToLockCursor();

        if (LevelTutorial.Instance.ShowObjectives)
            DrawObjectives();

        if (LevelTutorial.Instance.ShowObjComplete)
            _txtComplete.Draw((Screen.width - _txtComplete.Size.x) / 2, (Screen.height - _txtComplete.Size.y) / 2);

        PlayAvatarAnimation();

        if (LevelTutorial.Instance.IsCinematic)
            DrawBlackBars();
    }

    private void DrawFadeInRect()
    {
        if (_fadeInAlpha > 0)
        {
            _fadeInAlpha = Mathf.Lerp(_fadeInAlpha, 0, Time.deltaTime);

            if (Mathf.Approximately(0, _fadeInAlpha)) _fadeInAlpha = 0;

            GUI.color = new Color(1, 1, 1, _fadeInAlpha);
            GUI.Label(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none, BlueStonez.box_black);
            GUI.color = Color.white;
        }
    }

    private void DrawSubtitle()
    {
        GUI.color = Color.black;
        GUI.Label(new Rect(1, Screen.height - 149, Screen.width, 80), _subtitle, BlueStonez.label_interparkbold_18pt);
        GUI.color = Color.white;
        GUI.Label(new Rect(0, Screen.height - 150, Screen.width, 80), _subtitle, BlueStonez.label_interparkbold_18pt);
    }

    private void DrawContinueToLockCursor()
    {
        if (PopupSystem.IsAnyPopupOpen) return;

        if (!Screen.lockCursor &&
            GUITools.Button(new Rect(Screen.width * 0.5f - 100, Screen.height * 0.5f, 200, 50), new GUIContent(LocalizedStrings.Continue), StormFront.ButtonBlue))
        {
            if (_sequence.State < TutorialCinematicSequence.TutorialState.AirlockMouseLookSubtitle)
            {
                Screen.lockCursor = true;
            }
            else
            {
                GameState.LocalPlayer.UnPausePlayer();
            }
        }
    }

    private void PlayAvatarAnimation()
    {
        switch (_sequence.State)
        {
            case TutorialCinematicSequence.TutorialState.AirlockWelcome:
            case TutorialCinematicSequence.TutorialState.AirlockCameraReady:
            case TutorialCinematicSequence.TutorialState.AirlockCameraZoomIn:
            case TutorialCinematicSequence.TutorialState.AirlockMouseLook:
            case TutorialCinematicSequence.TutorialState.AirlockMouseLookSubtitle:
            case TutorialCinematicSequence.TutorialState.AirlockWasdWalk:
            case TutorialCinematicSequence.TutorialState.AirlockWasdSubtitle:
                GameState.LocalDecorator.AnimationController.PlayAnimation(AnimationIndex.HomeNoWeaponIdle);
                break;

            case TutorialCinematicSequence.TutorialState.ArmoryPickupMG:
            case TutorialCinematicSequence.TutorialState.TutorialEnd:
                GameState.LocalDecorator.AnimationController.PlayAnimation(AnimationIndex.HomeNoWeaponnLookAround);
                break;
        }
    }

    private void DrawObjectives()
    {
        float factor = Mathf.Clamp(Screen.height * 0.5f / 1000f, 0.35f, 0.4f);
        float guiScale = Screen.height / 1000f;

        float width = Screen.width / 4f;
        float height = Screen.height / 4f;
        float buttonSize = Mathf.Clamp(Mathf.Min(width, height) / 2f, 20f, 68f);
        float space = buttonSize / 5f;

        Rect wasdRect = new Rect(Mathf.Clamp(Screen.width / 6 - 70, 0, Mathf.Infinity), (Screen.height - height) / 2 - 40, buttonSize * 3 + space * 2, buttonSize * 2 + space);
        Rect mouseRect = wasdRect;
        mouseRect.x = Screen.width - mouseRect.x - mouseRect.width - 140;
        mouseRect.height = mouseRect.width * 0.8f;

        _scale.x = factor;
        _scale.y = factor;

        GUI.BeginGroup(new Rect(70, 40, Screen.width - 70, Screen.height - 40));
        {
            if (_showObjective)
            {
                _txtObjectives.Scale = _scale * 1.5f;
                _txtObjectives.Position = new Vector2(70, 36);

                _txtObjUnderscore.Scale = _scale * 1.5f;
                _txtObjUnderscore.Position = new Vector2(73 + _txtObjectives.Size.x, 36);

                _txtObjUnderscore.Alpha = (Mathf.Sin(Time.time * 4) > 0) ? 1 : 0;

                _txtObjectives.Draw();
            }

            if (_showObjMouse)
            {
                _txtMouseLook.Scale = _scale;
                _txtMouseLook.Position = new Vector2(70, 40);

                _txtMouseLook.Draw(0, 58);
                _objMouseMove.Draw(new Vector2(_txtMouseLook.Size.x + 5, 38), guiScale);

                GUI.BeginGroup(mouseRect);
                {
                    float imgWidth = mouseRect.height * LevelTutorial.Instance.ImgMouse.width / LevelTutorial.Instance.ImgMouse.height;

                    Rect imgRect = new Rect((mouseRect.width - imgWidth) / 2, imgWidth / 4, imgWidth, mouseRect.height);

                    GUIUtility.RotateAroundPivot(-17f, new Vector2(imgRect.x, imgRect.y));
                    GUI.Label(new Rect(imgRect.x + _mouseXOffset, imgRect.y, imgRect.width, imgRect.height), LevelTutorial.Instance.ImgMouse);
                    GUI.matrix = Matrix4x4.identity;
                }
                GUI.EndGroup();
            }

            if (_showObjWasdWalk && GameState.HasCurrentPlayer)
            {
                _txtWasdWalk.Scale = _scale;
                _txtWasdWalk.Position = new Vector2(70, 40);
                _txtWasdWalk.Draw(0, 64);

                _objWasdWalk.Draw(new Vector2(_txtWasdWalk.Size.x + 5, 38), guiScale);

                GUI.BeginGroup(wasdRect);
                {
                    GUI.Label(new Rect(buttonSize + space, 0, buttonSize, buttonSize), UserInput.IsPressed(KeyState.Forward) ? LevelTutorial.Instance.ImgWasdWalkBlue[0] : LevelTutorial.Instance.ImgWasdWalkBlack[0]);

                    for (int i = 1; i < 4; i++)
                    {
                        GUI.Label(new Rect((i - 1) * (buttonSize + space), buttonSize + space, buttonSize, buttonSize), UserInput.IsPressed(_keys[i]) ? LevelTutorial.Instance.ImgWasdWalkBlue[i] : LevelTutorial.Instance.ImgWasdWalkBlack[i]);
                    }
                }
                GUI.EndGroup();
            }

            if (_showGotoArmory)
            {
                _txtToArmory.Scale = _scale;
                _txtToArmory.Position = new Vector2(70, 40);
                _txtToArmory.Draw(0, 58);

                _objGotoArmory.Draw(new Vector2(_txtToArmory.Size.x + 5, 38), guiScale);
            }

            if (LevelTutorial.Instance.ShowObjPickupMG)
            {
                _txtPickupMG.Scale = _scale;
                _txtPickupMG.Position = new Vector2(70, 40);
                _txtPickupMG.Draw(0, 57);

                _objPickupWeapon.Draw(new Vector2(_txtPickupMG.Size.x + 5, 38), guiScale);
            }

            if (LevelTutorial.Instance.ShowObjShoot3)
            {
                _txtShoot3.Scale = _scale;
                _txtShoot3.Position = new Vector2(70, 40);
                _txtShoot3.Draw(0, 57);

                _objShootTarget3.Draw(new Vector2(_txtShoot3.Size.x + 5, 38), guiScale);
            }

            if (LevelTutorial.Instance.ShowObjShoot6)
            {
                _txtShoot6.Scale = _scale;
                _txtShoot6.Position = new Vector2(70, 40);
                _txtShoot6.Draw(0, 57);

                _objShootTarget6.Draw(new Vector2(_txtShoot6.Size.x + 5, 38), guiScale);
            }
        }
        GUI.EndGroup();
    }

    private void DrawWaypoints()
    {
        /* way point to armory */
        if (_sequence.State > TutorialCinematicSequence.TutorialState.AirlockWasdWalk)
        {

        }

        /* way point to machine gun */

    }

    private void DrawBlackBars()
    {
        if (Event.current.type == UnityEngine.EventType.Repaint)
        {
            _blackBarHeight = Mathf.Lerp(_blackBarHeight, Screen.height * 1 / 8f, Time.deltaTime * 3);
        }

        GUI.DrawTexture(new Rect(0, 0, Screen.width, _blackBarHeight), BlueStonez.box_black.normal.background);
        GUI.DrawTexture(new Rect(0, Screen.height - _blackBarHeight, Screen.width, _blackBarHeight), BlueStonez.box_black.normal.background);
    }

    private void DrawFreeItems()
    {
        Rect position = new Rect((Screen.width - 460) / 2, (Screen.height - 320) / 2, 460, 320);

        GUI.BeginGroup(position, GUIContent.none, BlueStonez.window);
        {
            GUI.Label(new Rect(0, 0, position.width, 56), LocalizedStrings.PrivateersLicenseGranted, BlueStonez.tab_strip);

            Rect rect = new Rect(16, 55, position.width - 32, 320 - 120);
            GUI.BeginGroup(rect, GUIContent.none, BlueStonez.window_standard_grey38);
            {
                GUI.Label(new Rect((rect.width - 64) / 2, 16, 64, 64), GUIContent.none, BlueStonez.item_slot_64);
                //GUI.DrawTexture(new Rect((rect.width - 64) / 2 + 8, 16 + 8, 48, 48), _items[0].Icon);
                GUI.Label(new Rect((rect.width - 128) / 2, 88, 128, 25), LocalizedStrings.PrivateersLicense, BlueStonez.label_interparkmed_11pt);

                string text = LocalizedStrings.CongratulationsGrantedLicenseMsg;

                GUI.Label(new Rect(8, 120, rect.width - 16, 70), text, BlueStonez.label_interparkbold_11pt);
            }
            GUI.EndGroup();

            if (GUITools.Button(new Rect((position.width - 140) / 2, position.height - 32 - 16, 140, 32), new GUIContent(LocalizedStrings.OkCaps), BlueStonez.button))
            {
            }
        }
        GUI.EndGroup();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        MonoRoutine.Run(StartDecreasingHealthAndArmor);
        MonoRoutine.Run(SimulateGameFrameUpdate);
    }

    protected override void OnCharacterLoaded()
    {
        CreateAirlockNPC();
        PlaceAvatarInAirlock();

        GameState.LocalDecorator.HideWeapons();
        MonoRoutine.Start(_sequence.StartSequences());
    }

    protected override void OnModeInitialized()
    {
        // Turn off the ribbon when we test in the editor
        if (Application.isEditor)
            GlobalUIRibbon.Instance.Hide();

        GameState.LocalPlayer.SetWeaponControlState(PlayerHudState.None);

        GameState.LocalPlayer.SetEnabled(true);

        OnPlayerJoined(SyncObjectBuilder.GetSyncData(GameState.LocalCharacter, true), Vector3.zero);

        switch (_sequence.State)
        {
            case TutorialCinematicSequence.TutorialState.None:
            case TutorialCinematicSequence.TutorialState.AirlockCameraZoomIn:
                InputManager.Instance.IsInputEnabled = false;
                break;
        }

        IsMatchRunning = true;

        Screen.lockCursor = true;

        HudDrawFlagGroup.Instance.BaseDrawFlag = HudDrawFlags.XpPoints;

        ApplicationWebServiceClient.RecordTutorialStep(PlayerDataManager.CmidSecure, TutorialStepType.TutorialStart,
            () => Debug.Log("TutorialStart recorded"),
            (ex) =>
            {
                DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace);
            });
    }

    public override void PlayerHit(int targetPlayer, short damage, BodyPart part, Vector3 force, int shotCount, int weaponID,
        UberstrikeItemClass weaponClass, DamageEffectType damageEffectFlag, float damageEffectValue)
    {
        if (MyCharacterState.Info.IsAlive)
        {
            byte angle = Conversion.Angle2Byte(Vector3.Angle(Vector3.forward, force));

            MyCharacterState.Info.Health -= MyCharacterState.Info.Armor.AbsorbDamage(damage, part);

            DamageFeedbackHud.Instance.AddDamageMark(Mathf.Clamp01(damage / 50f), Conversion.Byte2Angle(angle));
            HpApHud.Instance.HP = GameState.LocalCharacter.Health;
            HpApHud.Instance.AP = GameState.LocalCharacter.Armor.ArmorPoints;

            GameState.LocalPlayer.MoveController.ApplyForce(force, CharacterMoveController.ForceType.Additive);
        }
    }

    protected override void ApplyCurrentGameFrameUpdates(SyncObject delta)
    {
        base.ApplyCurrentGameFrameUpdates(delta);

        if (delta.Contains(UberStrike.Realtime.Common.CharacterInfo.FieldTag.Health) && !GameState.LocalCharacter.IsAlive)
        {
            OnSetNextSpawnPoint(Random.Range(0, SpawnPointManager.Instance.GetSpawnPointCount(GameMode.Tutorial, TeamID.NONE)), 3);
        }
    }

    public override void RequestRespawn()
    {
        OnSetNextSpawnPoint(Random.Range(0, SpawnPointManager.Instance.GetSpawnPointCount(GameMode.Tutorial, TeamID.NONE)), 3);
    }

    public override void IncreaseHealthAndArmor(int health, int armor)
    {
        UberStrike.Realtime.Common.CharacterInfo info = GameState.LocalCharacter;
        if (health > 0 && info.Health < 200)
        {
            info.Health = (short)Mathf.Clamp(info.Health + health, 0, 200);
        }

        if (armor > 0 && info.Armor.ArmorPoints < 200)
        {
            info.Armor.ArmorPoints = Mathf.Clamp(info.Armor.ArmorPoints + armor, 0, 200);
        }
    }

    public override void PickupPowerup(int pickupID, PickupItemType type, int value)
    {
        switch ((PickupItemType)type)
        {
            case PickupItemType.Armor:
                {
                    GameState.LocalCharacter.Armor.ArmorPoints += value;
                    break;
                }
            case PickupItemType.Health:
                {
                    switch (value)
                    {
                        case 5:
                        case 100:
                            {
                                if (GameState.LocalCharacter.Health < 200)
                                {
                                    GameState.LocalCharacter.Health = (short)Mathf.Clamp(GameState.LocalCharacter.Health + value, 0, 200);
                                }
                            } break;
                        case 25:
                        case 50:
                            {
                                if (GameState.LocalCharacter.Health < 100)
                                {
                                    GameState.LocalCharacter.Health = (short)Mathf.Clamp(GameState.LocalCharacter.Health + value, 0, 100);
                                }
                            } break;
                    }
                    break;
                }
        }
    }

    public void ResetBlackBar()
    {
        _blackBarHeight = 0;
    }

    private void PlaceAvatarInAirlock()
    {
        Vector3 pos = new Vector3(58, -6.437f, 64.4f);
        Quaternion rot = Quaternion.Euler(0, 224f, 0);

        GameState.LocalCharacter.Position = pos;
        GameState.LocalCharacter.HorizontalRotation = rot;

        GameState.LocalDecorator.HideWeapons();

        GameState.LocalPlayer.transform.position = pos;

        if (GameState.LocalPlayer.Character && GameState.LocalPlayer.Decorator && GameState.LocalPlayer.Decorator.AnimationController != null)
        {
            GameState.LocalPlayer.Character.StateController.IsCinematic = true;
            GameState.LocalPlayer.Decorator.AnimationController.ResetAllAnimations();
            GameState.LocalPlayer.Decorator.AnimationController.PlayAnimation(AnimationIndex.HomeNoWeaponIdle);
        }
    }

    private void CreateAirlockNPC()
    {
        Vector3 finalPos = new Vector3(56.97f, -7.4f, 63.18f);

        if (GameState.HasCurrentSpace)
        {
            if (LevelTutorial.Exists)
            {
                LevelTutorial level = LevelTutorial.Instance;

                GameObject npc = SkinnedMeshCombiner.CreateCharacter(false,
                    PrefabManager.Instance.GetAvatarPrefab(AvatarType.LutzRavinoff).gameObject,
                    level.GearHead, level.GearFace, level.GearGloves, level.GearUpperbody, level.GearLowerbody, level.GearBoots);

                if (npc)
                {
                    npc.layer = (int)UberstrikeLayer.Props;

                    npc.transform.position = LevelTutorial.Instance.NpcStartPos.position;
                    npc.transform.rotation = LevelTutorial.Instance.NpcStartPos.rotation;

                    _airlockNPC = npc.GetComponent<AvatarDecorator>();

                    if (_airlockNPC)
                    {
                        if (_airlockNPC.Animation)
                        {
                            _airlockNPC.Animation.enabled = false;
                            _airlockNPC.Animation.cullingType = AnimationCullingType.BasedOnRenderers;
                        }

                        BaseWeaponDecorator weapon = LevelTutorial.Instance.Weapon.Clone();
                        if (weapon)
                        {
                            weapon.transform.parent = _airlockNPC.WeaponAttachPoint;
                            weapon.transform.localPosition = Vector3.zero;
                            weapon.transform.localRotation = Quaternion.identity;

                            LayerUtil.SetLayerRecursively(weapon.transform, UberstrikeLayer.Props);
                        }

                        Object.Destroy(_airlockNPC);
                    }

                    npc.GetComponent<Animation>().enabled = true;
                    npc.GetComponent<Animation>().Play(AnimationIndex.HomeSmallGunIdle.ToString());
                    npc.GetComponent<Animation>().Stop();

                    CapsuleCollider cc = npc.AddComponent<CapsuleCollider>();
                    if (cc)
                    {
                        cc.radius = 0.4f;
                    }

                    TutorialAirlockNPC airlockNPC = npc.AddComponent<TutorialAirlockNPC>();
                    if (airlockNPC)
                    {
                        MonoRoutine.Start(StartNPCAirlockVoiceOver());
                        airlockNPC.SetFinalPosition(finalPos);
                    }

                    LevelTutorial.Instance.NPC = npc.transform;
                }
            }
            else
            {
                throw new System.Exception("LevelTutorial is not initialized!");
            }
        }
        else
        {
            throw new System.Exception("GameState doesn't have current space!");
        }
    }

    private IEnumerator StartNPCAirlockVoiceOver()
    {
        yield return new WaitForSeconds(3.0f);
        _subtitle = "Come on private, let's have a look at you.";
        SfxManager.Play2dAudioClip(LevelTutorial.Instance.VoiceWelcome);
    }

    private IEnumerator StartTutorialMode()
    {
        GlobalUIRibbon.Instance.Hide();

        while (!LevelTutorial.Exists)
            yield return new WaitForEndOfFrame();

        LevelTutorial.Instance.gameObject.SetActiveRecursively(true);

        _objWasdWalk = new ObjectiveTick(LevelTutorial.Instance.ImgObjBk, LevelTutorial.Instance.ImgObjTick);
        _objMouseMove = new ObjectiveTick(LevelTutorial.Instance.ImgObjBk, LevelTutorial.Instance.ImgObjTick);
        _objGotoArmory = new ObjectiveTick(LevelTutorial.Instance.ImgObjBk, LevelTutorial.Instance.ImgObjTick);

        _objPickupWeapon = new ObjectiveTick(LevelTutorial.Instance.ImgObjBk, LevelTutorial.Instance.ImgObjTick);
        _objShootTarget3 = new ObjectiveTick(LevelTutorial.Instance.ImgObjBk, LevelTutorial.Instance.ImgObjTick);
        _objShootTarget6 = new ObjectiveTick(LevelTutorial.Instance.ImgObjBk, LevelTutorial.Instance.ImgObjTick);

        _txtObjectives = new MeshGUIText("OBJECTIVES", LevelTutorial.Instance.Font, TextAnchor.UpperLeft);
        _txtObjUnderscore = new MeshGUIText("_", LevelTutorial.Instance.Font, TextAnchor.UpperLeft);
        _txtMouseLook = new MeshGUIText("> USE THE MOUSE TO LOOK AROUND", LevelTutorial.Instance.Font, TextAnchor.UpperLeft);
        _txtWasdWalk = new MeshGUIText("> USE THE W A S D KEYS TO MOVE TO THE DOOR", LevelTutorial.Instance.Font, TextAnchor.UpperLeft);
        _txtToArmory = new MeshGUIText("> NAVIGATE TO THE ARMORY", LevelTutorial.Instance.Font, TextAnchor.UpperLeft);
        _txtPickupMG = new MeshGUIText("> PICK UP THE MACHINE GUN", LevelTutorial.Instance.Font, TextAnchor.UpperLeft);
        _txtShoot3 = new MeshGUIText("> SHOOT THE THREE TARGETS", LevelTutorial.Instance.Font, TextAnchor.UpperLeft);
        _txtShoot6 = new MeshGUIText("> SHOOT THE SIX TARGETS", LevelTutorial.Instance.Font, TextAnchor.UpperLeft);
        _txtComplete = new MeshGUIText("OBJECTIVES COMPLETE", LevelTutorial.Instance.Font, TextAnchor.UpperLeft);

        _txtObjectives.Hide();
        _txtObjUnderscore.Hide();
        _txtMouseLook.Hide();
        _txtWasdWalk.Hide();
        _txtToArmory.Hide();
        _txtPickupMG.Hide();
        _txtShoot3.Hide();
        _txtShoot6.Hide();
        _txtComplete.Hide();

        HudStyleUtility.Instance.SetDefaultStyle(_txtObjectives);
        HudStyleUtility.Instance.SetDefaultStyle(_txtObjUnderscore);
        HudStyleUtility.Instance.SetDefaultStyle(_txtMouseLook);
        HudStyleUtility.Instance.SetDefaultStyle(_txtWasdWalk);
        HudStyleUtility.Instance.SetDefaultStyle(_txtToArmory);
        HudStyleUtility.Instance.SetDefaultStyle(_txtPickupMG);
        HudStyleUtility.Instance.SetDefaultStyle(_txtShoot3);
        HudStyleUtility.Instance.SetDefaultStyle(_txtShoot6);
        HudStyleUtility.Instance.SetDefaultStyle(_txtComplete);

        LevelTutorial.Instance.ShowObjectives = true;
        LevelTutorial.Instance.BackgroundMusic.Play();
        LevelTutorial.Instance.BridgeAudio.Play();
        LevelTutorial.Instance.AirlockBridgeAnim.Play();

        LevelTutorial.Instance.AirlockSplineController.FollowSpline();

        LevelTutorial.Instance.AirlockBackDoor.Reset();
        //LevelTutorial.Instance.AirlockFrontDoor.Reset();
        LevelTutorial.Instance.ArmoryTrigger.Reset();

        SpawnPointManager.Instance.ConfigureSpawnPoints(GameState.CurrentSpace.SpawnPoints.GetComponentsInChildren<SpawnPoint>(true));

        InitializeMode();
    }

    private IEnumerator StartDecreasingHealthAndArmor()
    {
        while (IsInitialized)
        {
            yield return new WaitForSeconds(1);
            if (GameState.LocalCharacter.Health > 100) GameState.LocalCharacter.Health--;
            if (GameState.LocalCharacter.Armor.ArmorPoints > GameState.LocalCharacter.Armor.ArmorPointCapacity) GameState.LocalCharacter.Armor.ArmorPoints--;
        }
    }

    private IEnumerator SimulateGameFrameUpdate()
    {
        while (IsInitialized)
        {
            yield return new WaitForEndOfFrame();

            if (_sequence.State < TutorialCinematicSequence.TutorialState.AirlockMouseLookSubtitle)
            {
                if (InputManager.Instance.IsInputEnabled)
                {
                    if (GameState.HasCurrentPlayer)
                    {
                        GameState.LocalCharacter.Position = new Vector3(58, -6.437f, 64.4f);
                        GameState.LocalCharacter.HorizontalRotation = Quaternion.Euler(0, 224f, 0);
                    }
                    InputManager.Instance.IsInputEnabled = false;
                }
            }

            if (GameState.LocalPlayer.Character != null)
            {
                SyncObject delta = SyncObjectBuilder.GetSyncData(GameState.LocalCharacter, false);
                if (!delta.IsEmpty)
                {
                    ApplyCurrentGameFrameUpdates(delta);
                    GameState.LocalPlayer.Character.OnCharacterStateUpdated(delta);
                }
            }
        }
    }

    private IEnumerator StartAirlockCameraSmoothIn()
    {
        Vector3 splineEnd = new Vector3(57.659f, -5.366f, 68.957f);
        Vector3 camVelocity = Vector3.zero;

        while (Vector3.SqrMagnitude(LevelCamera.Instance.TransformCache.position - splineEnd) > 0.2f)
        {
            yield return new WaitForEndOfFrame();
        }

        /* stop the spline */
        LevelTutorial.Instance.AirlockSplineController.Stop();

        camVelocity = LevelCamera.Instance.MainCamera.velocity;

        while (camVelocity.magnitude > 0.05f)
        {
            camVelocity = Vector3.Lerp(camVelocity, Vector3.zero, Time.deltaTime * 3.5f);
            LevelCamera.Instance.TransformCache.position += (camVelocity * Time.deltaTime);
            yield return new WaitForEndOfFrame();
        }

        _sequence.OnAirlockCameraReady();
    }

    private IEnumerator StartShowObjective()
    {
        _subtitle = string.Empty;

        yield return new WaitForSeconds(1);

        _showObjective = true;

        ShowDirective(_txtObjectives);
        ShowDirective(_txtObjUnderscore);

        SfxManager.Play2dAudioClip(SoundEffectType.UIObjective);
    }

    private IEnumerator StartAirlockMouseLook()
    {
        /* Disable input for now */
        InputManager.Instance.IsInputEnabled = false;
        InputManager.Instance.Reset();

        UserInput.SetRotation(180);

        /* Respawn local player */
        GameState.LocalPlayer.SetWeaponControlState(PlayerHudState.Playing);

        IsWaitingForSpawn = false;

        Vector3 pos = new Vector3(58, -6.437f, 64.4f);
        Quaternion rot = Quaternion.Euler(0, 224f, 0);

        SpawnPlayerAt(pos, rot);
        LevelCamera.Instance.CanDip = false;

        SfxManager.Play2dAudioClip(SoundEffectType.UISubObjective);

        _showObjMouse = true;

        _mousePos.width = LevelTutorial.Instance.ImgMouse.width;
        _mousePos.height = LevelTutorial.Instance.ImgMouse.height;

        ShowDirective(_txtMouseLook);

        InputManager.Instance.IsInputEnabled = true;

        /* Wait for mouse look */
        float mouseValue = 0;

        bool mouseIconMoving = false;
        float time = 0;
        float frequency = 10;
        float magnitude = -20;

        while (mouseValue < 90)
        {
            if (mouseIconMoving)
            {
                if (time < 2 * Mathf.PI / frequency)
                {
                    _mouseXOffset = Mathf.Sin(frequency * time) * Mathf.Cos(frequency * time / 2.5f) * magnitude;

                    time += Time.deltaTime;
                }
                else
                {
                    time = 0;
                    mouseIconMoving = false;
                }
            }
            else
            {
                if (time < 1)
                {
                    time += Time.deltaTime;
                }
                else
                {
                    time = 0;
                    mouseIconMoving = true;
                }
            }

            mouseValue += Mathf.Abs(InputManager.Instance.GetValue(GameInputKey.HorizontalLook)) + Mathf.Abs(InputManager.Instance.GetValue(GameInputKey.VerticalLook));
            yield return new WaitForEndOfFrame();
        }

        LevelCamera.Instance.CanDip = true;

        _objMouseMove.Complete();
        _sequence.OnAirlockMouseLook();

        ApplicationWebServiceClient.RecordTutorialStep(PlayerDataManager.CmidSecure, TutorialStepType.MouseLook,
            () => Debug.Log("MouseLook recorded"),
            (ex) =>
            {
                DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace);
            });

        SfxManager.Play2dAudioClip(SoundEffectType.UIObjectiveTick);

        yield return new WaitForSeconds(0.5f);

        XpPtsHud.Instance.GainXp(5);

        MonoRoutine.Start(StartHideDirective(_txtMouseLook));

        yield return new WaitForSeconds(0.5f);

        _showObjMouse = false;

        _txtMouseLook.Hide();
        _txtMouseLook.FreeObject();

        EnableObjectives(false);

        yield return new WaitForSeconds(0.5f);

        LevelTutorial.Instance.AirlockDoorAnim.Play();
    }

    private IEnumerator StartAirlockWasdWalk()
    {
        _showObjWasdWalk = true;

        ShowDirective(_txtWasdWalk);
        LevelTutorial.Instance.AirlockFrontDoor.Waypoint.CanShow = true;

        GameState.LocalPlayer.IsWalkingEnabled = true;
        SfxManager.Play2dAudioClip(SoundEffectType.UISubObjective);

        yield return new WaitForSeconds(0.5f);

        SfxManager.Play2dAudioClip(LevelTutorial.Instance.VoiceToArmory);
        _subtitle = "Through the door, up on the right and we'll get you started.";

        while (!LevelTutorial.Instance.AirlockFrontDoor.PlayerEntered)
            yield return new WaitForEndOfFrame();

        _subtitle = string.Empty;

        _objWasdWalk.Complete();
        SfxManager.Play2dAudioClip(SoundEffectType.UIObjectiveTick);
        ApplicationWebServiceClient.RecordTutorialStep(PlayerDataManager.CmidSecure, TutorialStepType.KeyboardMove,
            () => Debug.Log("KeyboardMove recorded"),
            (ex) =>
            {
                DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace);
            });

        yield return new WaitForSeconds(0.5f);

        XpPtsHud.Instance.GainXp(5);

        MonoRoutine.Start(StartHideDirective(_txtWasdWalk));

        yield return new WaitForSeconds(0.5f);

        _showObjWasdWalk = false;

        yield return new WaitForSeconds(1);

        _sequence.OnAirlockWasdWalk();

        yield return new WaitForSeconds(1);

        if (LevelTutorial.Instance.ArmoryWaypoint)
            LevelTutorial.Instance.ArmoryWaypoint.CanShow = true;

        SfxManager.Play2dAudioClip(LevelTutorial.Instance.WaypointAppear);
    }

    private IEnumerator StartNavigateToArmory()
    {
        yield return new WaitForSeconds(1);

        SfxManager.Play2dAudioClip(SoundEffectType.UISubObjective);

        _showGotoArmory = true;

        _txtToArmory.Alpha = 0;
        _txtToArmory.Flicker(0.5f);
        _txtToArmory.FadeAlphaTo(1, 0.5f);

        _txtToArmory.Show();

        EnableObjectives(true);
    }

    private IEnumerator StartHideObjectives()
    {
        _txtObjectives.Flicker(0.5f);
        _txtObjectives.FadeAlphaTo(0, 0.5f);

        _txtToArmory.Flicker(0.5f);
        _txtToArmory.FadeAlphaTo(0, 0.5f);

        yield return new WaitForSeconds(0.5f);

        _txtObjectives.Hide();
        _txtObjUnderscore.Hide();
        _txtToArmory.Hide();
        _txtToArmory.FreeObject();

        _showGotoArmory = false;

        EnableObjectives(false);

        LevelTutorial.Instance.ShowObjectives = false;
    }

    private IEnumerator StartArmoryObjective()
    {
        _txtObjectives.Flicker(0.5f);
        _txtObjectives.FadeAlphaTo(1, 0.5f);
        _txtObjectives.Show();
        _txtObjUnderscore.Show();

        _txtPickupMG.Alpha = 0;
        _txtPickupMG.Flicker(0.5f);
        _txtPickupMG.FadeAlphaTo(1, 0.5f);
        _txtPickupMG.Show();

        EnableObjectives(true);

        yield return new WaitForSeconds(7);

        _subtitle = string.Empty;
    }

    private IEnumerator StartPickupWeapon()
    {
        _txtPickupMG.Flicker(0.5f);
        _txtPickupMG.FadeAlphaTo(0, 0.5f);

        yield return new WaitForSeconds(0.5f);

        _txtPickupMG.Hide();
        _txtPickupMG.FreeObject();

        EnableObjectives(false);

        LevelTutorial.Instance.ShowObjPickupMG = false;

        XpPtsHud.Instance.GainXp(5);

        yield return new WaitForSeconds(8);

        _subtitle = string.Empty;
    }

    private void ShowDirective(MeshGUIText txt, bool showObjective = true)
    {
        EnableObjectives(showObjective);

        txt.Alpha = 0;
        txt.Flicker(0.5f);
        txt.FadeAlphaTo(1, 0.5f);
        txt.Show();
    }

    private IEnumerator StartHideDirective(MeshGUIText txt, bool delete = true)
    {
        txt.Flicker(0.5f);
        txt.FadeAlphaTo(0, 0.5f);

        yield return new WaitForSeconds(0.5f);

        txt.Hide();

        if (delete)
            txt.FreeObject();

        EnableObjectives(false);
    }

    private IEnumerator StartTerminateTutorial()
    {
        yield return new WaitForEndOfFrame();

        PlayerDataManager.Instance.AttributeXp(UberStrikeCommonConfig.XpAttributedOnTutorialCompletion);

        ApplicationWebServiceClient.RecordTutorialStep(PlayerDataManager.CmidSecure, TutorialStepType.TutorialComplete,
            () => Debug.Log("TutorialComplete recorded"),
            (ex) =>
            {
                DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace);
            });

        GameState.LocalPlayer.SetWeaponControlState(PlayerHudState.None);

        InventoryManager.Instance.EquipItemOnSlot((int)DefaultWeaponId.WeaponMelee, LoadoutSlotType.WeaponMelee);
        InventoryManager.Instance.EquipItemOnSlot((int)DefaultWeaponId.WeaponMachinegun, LoadoutSlotType.WeaponPrimary);
        InventoryManager.Instance.UnequipWeaponSlot(LoadoutSlotType.WeaponSecondary);
        InventoryManager.Instance.UnequipWeaponSlot(LoadoutSlotType.WeaponTertiary);

        ResetBlackBar();

        LevelTutorial.Instance.NPC.gameObject.SetActiveRecursively(false);

        GlobalUIRibbon.Instance.Hide();
        PanelManager.Instance.OpenPanel(PanelType.CompleteAccount);

        LevelTutorial.Instance.gameObject.SetActiveRecursively(false);
        LevelTutorial.Instance.StopAllCoroutines();
    }

    public void ShowShoot3()
    {
        ShowDirective(_txtShoot3);
    }

    public void ShowShoot6()
    {
        _subtitle = string.Empty;

        ShowDirective(_txtShoot6);
    }

    public void HideShoot3()
    {
        ApplicationWebServiceClient.RecordTutorialStep(PlayerDataManager.CmidSecure, TutorialStepType.ShootFirstGroup,
            () => Debug.Log("ShootFirstGroup recorded"),
            (ex) =>
            {
                DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace);
            });

        SfxManager.Play2dAudioClip(LevelTutorial.Instance.VoiceShootMore);

        _subtitle = "Hah, not bad at all! Try some more!";

        MonoRoutine.Start(StartHideDirective(_txtShoot3));
    }

    public void HideShoot6()
    {
        ApplicationWebServiceClient.RecordTutorialStep(PlayerDataManager.CmidSecure, TutorialStepType.ShootSecondGroup,
            () => Debug.Log("ShootSecondGroup recorded"),
            (ex) =>
            {
                DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace);
            });

        _subtitle = "Who would have thought you had it in you.\nNow let's see how you do in some real combat.";

        MonoRoutine.Start(StartHideDirective(_txtShoot6));
    }

    public void DestroyObjectives()
    {
        MonoRoutine.Start(StartHideDirective(_txtObjectives, true));
        MonoRoutine.Start(StartHideDirective(_txtObjUnderscore, true));
    }

    public void ShowObjComplete()
    {
        ShowDirective(_txtComplete, false);
    }

    public void HideObjComplete(bool destroyAfter = true)
    {
        _subtitle = string.Empty;

        MonoRoutine.Start(StartHideDirective(_txtComplete, destroyAfter));
    }

    public void ShowTutorialComplete()
    {
        _txtComplete.Text = "TUTORIAL COMPLETE";
        _txtComplete.BitmapMeshText.ShadowColor = new Color(0xC0 / 255f, 0x8C / 255f, 0);

        ShowDirective(_txtComplete, false);
    }

    /* Cinematic Sequences */

    public void OnAirlockCameraZoomIn()
    {
        GameState.LocalPlayer.Character.Decorator.MeshRenderer.enabled = true;
        GameState.LocalPlayer.Character.Decorator.HudInformation.enabled = true;

        MonoRoutine.Start(StartAirlockCameraSmoothIn());
    }

    public void OnAirlockWelcome()
    {
        MonoRoutine.Start(StartShowObjective());
    }

    public void OnAirlockMouseLookSubtitle()
    {
        _subtitle = string.Empty;

        MonoRoutine.Start(StartAirlockMouseLook());
    }

    public void OnAirlockWasdSubtitle()
    {
        MonoRoutine.Start(StartAirlockWasdWalk());
    }

    public void OnAirlockDoorOpen()
    {
        _subtitle = string.Empty;

        MonoRoutine.Start(StartNavigateToArmory());
    }

    /* Armory */

    public void ReachArmoryWaypoint()
    {
        _objGotoArmory.Complete();
        SfxManager.Play2dAudioClip(SoundEffectType.UIObjectiveTick);

        GameState.LocalCharacter.Keys = KeyState.Still;

        XpPtsHud.Instance.GainXp(5);
    }

    public void HideObjectives()
    {
        MonoRoutine.Start(StartHideObjectives());
    }

    public void EnterArmory(SplineController splineController)
    {
        _sequence.OnArmoryEnter();
        _shootingRangeController = new TutorialShootingTargetController(this);
    }

    public void OnArmoryEnterSubtitle()
    {
        _subtitle = "Right, go to the counter, pick up your weapon and lets see what you're made of.";

        SfxManager.Play2dAudioClip(LevelTutorial.Instance.VoicePickupWeapon);

        LevelTutorial.Instance.ArmoryDoor.Close();
        LevelTutorial.Instance.IsCinematic = false;
        LevelTutorial.Instance.ShowObjectives = true;

        SfxManager.Play2dAudioClip(SoundEffectType.UISubObjective);

        MonoRoutine.Start(StartArmoryObjective());
    }

    public void OnArmoryPickupMG()
    {
        ApplicationWebServiceClient.RecordTutorialStep(PlayerDataManager.CmidSecure, TutorialStepType.PickUpWeapon,
            () => Debug.Log("PickUpWeapon recorded"),
            (ex) =>
            {
                DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace);
            });

        _subtitle = "Alright my soldier. Let's see if you can shoot straight.\nFeed these targets some lead.";

        _sequence.OnArmoryPickupMG();

        GameState.LocalPlayer.SetWeaponControlState(PlayerHudState.Playing);

        GameState.LocalPlayer.UnPausePlayer();
        HudDrawFlagGroup.Instance.BaseDrawFlag = HudDrawFlags.XpPoints | HudDrawFlags.HealthArmor |
            HudDrawFlags.Ammo | HudDrawFlags.Reticle;

        _objPickupWeapon.Complete();
        SfxManager.Play2dAudioClip(SoundEffectType.UIObjectiveTick);

        MonoRoutine.Start(StartPickupWeapon());

        LevelTutorial.Instance.WeaponWaypoint.CanShow = false;
        MonoRoutine.Start(_shootingRangeController.StartShootingRange());
    }

    public void OnTutorialEnd()
    {
        if (LevelTutorial.Exists)
            LevelTutorial.Instance.StartCoroutine(StartTerminateTutorial());
    }

    private void EnableObjectives(bool enabled)
    {
        if (enabled)
        {
            _txtObjectives.Show();
            _txtObjUnderscore.Show();
        }
        else
        {
            _txtObjectives.Hide();
            _txtObjUnderscore.Hide();
        }
    }
}