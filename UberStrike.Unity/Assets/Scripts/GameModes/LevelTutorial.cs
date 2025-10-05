using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LevelTutorial : MonoSingleton<LevelTutorial>
{
    #region Fields

    [SerializeField]
    private Animation _airlockBrigeAnim;
    public Animation AirlockBridgeAnim { get { return _airlockBrigeAnim; } }

    [SerializeField]
    private Animation _airlockDoorAnim;
    public Animation AirlockDoorAnim { get { return _airlockDoorAnim; } }

    [SerializeField]
    private DoorBehaviour _armoryDoor;
    public DoorBehaviour ArmoryDoor { get { return _armoryDoor; } }

    [SerializeField]
    private AudioSource _bridgeAudio;
    public AudioSource BridgeAudio { get { return _bridgeAudio; } }

    public AudioSource BackgroundMusic { get { return _backgroundMusic; } }
    [SerializeField]
    private AudioSource _backgroundMusic;

    [SerializeField]
    private BitmapFont _font;
    public BitmapFont Font { get { return _font; } }

    [SerializeField]
    private Transform _npcStartPos;
    public Transform NpcStartPos { get { return _npcStartPos; } }

    [SerializeField]
    private GameObject _gearBoots;
    public GameObject GearBoots { get { return _gearBoots; } }

    [SerializeField]
    private GameObject _gearFace;
    public GameObject GearFace { get { return _gearFace; } }

    [SerializeField]
    private GameObject _gearGloves;
    public GameObject GearGloves { get { return _gearGloves; } }

    [SerializeField]
    private GameObject _gearHead;
    public GameObject GearHead { get { return _gearHead; } }

    [SerializeField]
    private GameObject _gearLowerbody;
    public GameObject GearLowerbody { get { return _gearLowerbody; } }

    [SerializeField]
    private GameObject _gearUpperbody;
    public GameObject GearUpperbody { get { return _gearUpperbody; } }

    [SerializeField]
    private BaseWeaponDecorator _npcWeapon;
    public BaseWeaponDecorator Weapon { get { return _npcWeapon; } }

    [SerializeField]
    private AudioClip _bigDoorClose;
    public AudioClip BigDoorClose { get { return _bigDoorClose; } }

    [SerializeField]
    private AudioClip _waypoint;
    public AudioClip WaypointAppear { get { return _waypoint; } }

    [SerializeField]
    private AudioClip _bigObjComplete;
    public AudioClip BigObjComplete { get { return _bigObjComplete; } }

    [SerializeField]
    private AudioClip _voiceWelcome;
    public AudioClip VoiceWelcome { get { return _voiceWelcome; } }

    [SerializeField]
    private AudioClip _voiceToArmory;
    public AudioClip VoiceToArmory { get { return _voiceToArmory; } }

    [SerializeField]
    private AudioClip _voicePickupWeapon;
    public AudioClip VoicePickupWeapon { get { return _voicePickupWeapon; } }

    [SerializeField]
    private AudioClip _voiceShootingRange;
    public AudioClip VoiceShootingRange { get { return _voiceShootingRange; } }

    [SerializeField]
    private AudioClip _voiceShootMore;
    public AudioClip VoiceShootMore { get { return _voiceShootMore; } }

    [SerializeField]
    private AudioClip _voiceArena;
    public AudioClip VoiceArena { get { return _voiceArena; } }

    [SerializeField]
    private AudioClip _voiceTutorialComplete;
    public AudioClip TutorialComplete { get { return _voiceTutorialComplete; } }

    [SerializeField]
    private SplineController _airlockSplineController;
    public SplineController AirlockSplineController { get { return _airlockSplineController; } }

    [SerializeField]
    private TutorialAirlockFrontDoor _airlockFrontDoor;
    public TutorialAirlockFrontDoor AirlockFrontDoor { get { return _airlockFrontDoor; } }

    [SerializeField]
    private TutorialAirlockDoor _airlockBackDoor;
    public TutorialAirlockDoor AirlockBackDoor { get { return _airlockBackDoor; } }

    [SerializeField]
    private TutorialArmoryEnterTrigger _armoryTrigger;
    public TutorialArmoryEnterTrigger ArmoryTrigger { get { return _armoryTrigger; } }

    [SerializeField]
    private Texture _imgMouse;
    public Texture ImgMouse { get { return _imgMouse; } }

    [SerializeField]
    private Texture _imgObjTickBackground;
    public Texture ImgObjBk { get { return _imgObjTickBackground; } }

    [SerializeField]
    private Texture _imgObjTickForeground;
    public Texture ImgObjTick { get { return _imgObjTickForeground; } }

    [SerializeField]
    private Texture[] _imgWasdWalkBlack;
    public Texture[] ImgWasdWalkBlack { get { return _imgWasdWalkBlack; } }

    [SerializeField]
    private Texture[] _imgWasdWalkBlue;
    public Texture[] ImgWasdWalkBlue { get { return _imgWasdWalkBlue; } }

    [SerializeField]
    private TutorialWaypoint _armoryWaypoint;
    public TutorialWaypoint ArmoryWaypoint { get { return _armoryWaypoint; } }

    [SerializeField]
    private TutorialWaypoint _weaponWaypoint;
    public TutorialWaypoint WeaponWaypoint { get { return _weaponWaypoint; } }

    /* Armory */
    [SerializeField]
    private GameObject _shootingTarget;
    public GameObject ShootingTargetPrefab { get { return _shootingTarget; } }

    [SerializeField]
    private Transform[] _nearRangeTargetPos;
    public Transform[] NearRangeTargetPos { get { return _nearRangeTargetPos; } }

    [SerializeField]
    private Transform[] _farRangeTargetPos;
    public Transform[] FarRangeTargetPos { get { return _farRangeTargetPos; } }

    [SerializeField]
    private Transform _armoryCameraPathEnd;
    public Transform ArmoryCameraPathEnd { get { return _armoryCameraPathEnd; } }

    [SerializeField]
    private Transform _finalPlayerPos;
    public Transform FinalPlayerPos { get { return _finalPlayerPos; } }

    [SerializeField]
    private TutorialArmoryPickup _pickupWeapon;
    public TutorialArmoryPickup PickupWeapon { get { return _pickupWeapon; } }

    [SerializeField]
    private TutorialWaypoint _ammoWaypoint;
    public TutorialWaypoint AmmoWaypoint { get { return _ammoWaypoint; } }

    public Transform NPC { get; set; }

    public bool IsCinematic { get; set; }

    public bool ShowObjectives { get; set; }
    public bool ShowObjPickupMG { get; set; }
    public bool ShowObjShoot3 { get; set; }
    public bool ShowObjShoot6 { get; set; }
    public bool ShowObjComplete { get; set; }

    public HudDrawFlags HudFlags { get; set; }

    #endregion fields

    private void Awake()
    {
        HudFlags = HudDrawFlags.XpPoints;
    }

    private void Start()
    {
        if (LevelCamera.Exists)
        {
            AirlockSplineController.Target = LevelCamera.Instance.gameObject;
            ArmoryTrigger.ArmoryCameraPath.Target = LevelCamera.Instance.gameObject;
        }
    }

#if UNITY_EDITOR
    private void OnGUI()
    {
        if (GUI.Button(new Rect(0, Screen.height - 20, 60, 20), "Tutorial"))
        {
            GameStateController.Instance.LoadGameMode(GameMode.Tutorial);
        }

        //if (GUI.Button(new Rect(0, Screen.height - 40, 60, 20), "LevelUp"))
        //{
        //    EventFeedbackGUI.Instance.ShowLevelUpPopup(2, () => { });
        //}
    }
#endif
}
