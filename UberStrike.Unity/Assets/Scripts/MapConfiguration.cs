using UnityEngine;

public class MapConfiguration : MonoBehaviour
{
    #region Fields

    [SerializeField]
    private bool _isEnabled = true;

    [SerializeField]
    private int _mapId = -1;

    [SerializeField]
    private int _defaultSpawnPoint = 0;

    [SerializeField]
    private FootStepSoundType _defaultFootStep = FootStepSoundType.Sand;

    [SerializeField]
    private Camera _camera;

    [SerializeField]
    private Transform _defaultViewPoint;

    [SerializeField]
    protected GameObject _staticContentParent;

    [SerializeField]
    private GameObject _spawnPoints;

    [SerializeField]
    private Transform _waterPlane;

    [SerializeField]
    private CombatRangeTier _combatRange;

    #endregion

    #region Properties

    public bool IsEnabled { get { return _isEnabled; } }

    public int DefaultSpawnPoint
    {
        get { return _defaultSpawnPoint; }
#if UNITY_EDITOR
        set { _defaultSpawnPoint = value; }
#endif
    }

    public int MapId { get { return _mapId; } }

    public Camera Camera { get { return _camera; } }

    public CombatRangeTier CombatRangeTiers { get { return _combatRange; } }

    public FootStepSoundType DefaultFootStep { get { return _defaultFootStep; } }

    public Transform DefaultViewPoint { get { return _defaultViewPoint; } }

    public GameObject SpawnPoints { get { return _spawnPoints; } }

    public bool HasWaterPlane { get { return _waterPlane != null; } }

    public float WaterPlaneHeight { get { return (_waterPlane) ? _waterPlane.position.y : float.MinValue; } }

    #endregion

    void Awake()
    {
        LevelManager.Instance.AddLoadedMap(this);

        //#if UNITY_EDITOR
        //        if (!ApplicationDataManager.Exists)
        //        {
        //            LevelTester.TestMap(this);
        //        }
        //#endif
    }

    public void SetEnabled(bool enabled)
    {
        if (enabled)
        {
            // This call is important for Pickup item sync
            PickupItem.ResetInstanceCounter();
        }

        gameObject.active = enabled;
        _staticContentParent.gameObject.SetActiveRecursively(enabled);
        _camera.gameObject.SetActiveRecursively(enabled);
    }
}