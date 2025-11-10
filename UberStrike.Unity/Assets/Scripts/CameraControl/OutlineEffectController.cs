using UnityEngine;
using System.Collections.Generic;
using System;

public class OutlineEffectController : MonoSingleton<OutlineEffectController>
{
    private void Update()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        foreach (KeyValuePair<GameObject, OutlineProperty> outlinePair in _outlinedGroup)
        {
            GameObject gameObject = outlinePair.Key;
            if (gameObject == null)
            {
                _outlinedGroup.Remove(gameObject);
                return;
            }
            OutlineProperty outlineProp = outlinePair.Value;

            Vector3 screenPosition = mainCamera.WorldToScreenPoint(gameObject.transform.position);
            float t = 1 - Mathf.Clamp((screenPosition.z - _distanceToAttenuateOutline) /
                (_distanceToHideOutline - _distanceToAttenuateOutline), 0.0f, 1.0f);
            t = Mathf.Pow(t, 3);
            t *= mainCamera.fieldOfView / 60.0f;

            foreach (var m in outlineProp.MaterialGroup)
            {
                m.SetFloat("_Outline_Size", outlineProp.OutlineSize * t);
            }
        }
    }

    public void AddOutlineObject(GameObject gameObject, List<Material> materialGroup, Color outlineColor, float outlineSize = 0.010f)
    {
        if (_outlinedGroup.ContainsKey(gameObject) || _outlineMaterial == null)
        {
            return;
        }
        OutlineProperty outlineProp = new OutlineProperty(outlineColor, outlineSize, materialGroup);
        _outlinedGroup.Add(gameObject, outlineProp);
        SetOutlineMaterial(outlineProp);
    }

    public void RemoveOutlineObject(GameObject gameObject)
    {
        OutlineProperty outlineProp = null;
        _outlinedGroup.TryGetValue(gameObject, out outlineProp);
        if (outlineProp != null)
        {
            SetDefaultMaterial(outlineProp.MaterialGroup);
            _outlinedGroup.Remove(gameObject);
        }
    }

    #region Private
    private OutlineEffectController()
    {
        _outlinedGroup = new Dictionary<GameObject, OutlineProperty>();
    }

    private void SetOutlineMaterial(OutlineProperty outlineProp)
    {
        if (_outlineMaterial == null)
        {
            return;
        }
        foreach (var m in outlineProp.MaterialGroup)
        {
            m.shader = _outlineMaterial.shader;
            m.SetFloat("_Outline_Size", outlineProp.OutlineSize);
            m.SetColor("_Outline_Color", outlineProp.OutlineColor);
        }
    }

    private void SetOutlineSize(OutlineProperty outlineProp, float size)
    {
        foreach (var m in outlineProp.MaterialGroup)
        {
            m.shader = _outlineMaterial.shader;
            m.SetFloat("_Outline_Size", outlineProp.OutlineSize);
        }
    }

    private void SetDefaultMaterial(List<Material> materialGroup)
    {
        foreach (var m in materialGroup)
        {
            m.shader = Shader.Find("Diffuse");
        }
    }

    private class OutlineProperty
    {
        public Color OutlineColor { get; set; }
        public float OutlineSize { get; set; }
        public List<Material> MaterialGroup { get { return _materialGroup; } }

        public OutlineProperty(Color outlineColor, float outlineSize, List<Material> materialGroup)
        {
            OutlineColor = outlineColor;
            OutlineSize = outlineSize;
            _materialGroup = materialGroup;
        }

        private List<Material> _materialGroup;
    }

    private Dictionary<GameObject, OutlineProperty> _outlinedGroup;
    private int _distanceToAttenuateOutline = 3;
    private int _distanceToHideOutline = 60;

    [SerializeField]
    private Material _outlineMaterial;
    #endregion
}
