using UnityEngine;
using System.Collections;

public class WaterController : MonoBehaviour
{
    public WaterBase waterBase;

    [SerializeField]
    private Color waterRefraction;

    [SerializeField]
    private Color waterReflection;

    private PlanarReflection _reflection;

#pragma warning disable 0169
    [SerializeField]
    private Color underwaterRefraction;

    [SerializeField]
    private Color underwaterReflection;
#pragma warning restore

    private bool isUnderwater = false;

    void Start()
    {
        if (waterBase != null)
        {
            if (GameState.LocalCharacter != null)
            {
                isUnderwater = SetUnderwater((GameState.LocalPlayer.MoveController.WaterLevel == 3));
            }

            _reflection = waterBase.gameObject.GetComponent<PlanarReflection>();
        }
    }

    private void Update()
    {
        if (waterBase != null)
        {
            waterBase.waterQuality = (WaterQuality)ApplicationDataManager.ApplicationOptions.VideoWaterMode;

            if (GameState.LocalCharacter != null)
            {
                if (isUnderwater != (GameState.LocalPlayer.MoveController.WaterLevel == 3))
                {
                    isUnderwater = SetUnderwater((GameState.LocalPlayer.MoveController.WaterLevel == 3));
                }
            }
        }
    }

    private bool SetUnderwater(bool enabled)
    {
        if (enabled)
        {
            //waterBase.gameObject.transform.rotation = Quaternion.Euler(new Vector3(0, 0, 180));
            if (_reflection) _reflection.EnableReflection = false;
            SetMaterialColor("_BaseColor", waterRefraction, waterBase.sharedMaterial);
            SetMaterialColor("_ReflectionColor", waterReflection, waterBase.sharedMaterial);
        }
        else
        {
            //waterBase.gameObject.transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));
            if (_reflection) _reflection.EnableReflection = true;
            SetMaterialColor("_BaseColor", waterRefraction, waterBase.sharedMaterial);
            SetMaterialColor("_ReflectionColor", waterReflection, waterBase.sharedMaterial);
        }
        return enabled;
    }

    private void SetMaterialColor(System.String name, Color color, Material mat)
    {
        mat.SetColor(name, color);
    }
}
