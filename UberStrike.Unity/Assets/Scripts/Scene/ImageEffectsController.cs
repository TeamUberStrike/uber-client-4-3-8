using UnityEngine;
using System.Collections;
using Cmune.Util;

#if UNITY_3_5
[RequireComponent(typeof(BloomAndLensFlares))]
[RequireComponent(typeof(Vignetting))]
#endif
public class ImageEffectsController : MonoBehaviour
{

#if UNITY_3_5
    private BloomAndLensFlares bloomEffect = null;
    private Vignetting vignettingEffect = null;
#endif

#pragma warning disable 0169
    [SerializeField]
    private bool useVignetting;

    [SerializeField]
    private bool useBloomAndFlares;
#pragma warning restore

    void Awake()
    {
        //TODO: we turn off image effects until the item mateerials are tweaked
        //#if UNITY_3_5
        //        bloomEffect = GetComponent<BloomAndLensFlares>();
        //        vignettingEffect = GetComponent<Vignetting>();
        //#endif
    }

    void OnEnable()
    {
        CmuneEventHandler.AddListener<ImageEffectsUpdate>(OnEffectsUpdated);
        OnEffectsUpdated(null);
    }

    void OnDisable()
    {
        CmuneEventHandler.RemoveListener<ImageEffectsUpdate>(OnEffectsUpdated);
    }

    private void OnEffectsUpdated(ImageEffectsUpdate ev)
    {
#if UNITY_3_5
        if(bloomEffect) bloomEffect.enabled = (ApplicationDataManager.ApplicationOptions.VideoBloomAndFlares && useBloomAndFlares);
        if(vignettingEffect)vignettingEffect.enabled = (ApplicationDataManager.ApplicationOptions.VideoVignetting && useVignetting);
#endif
    }
}

public class ImageEffectsUpdate { }