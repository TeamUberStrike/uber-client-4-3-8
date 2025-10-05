using UnityEngine;
using System.Collections;

public class CameraController : Singleton<CameraController>
{
    #region CameraConfiguration Handling

    private CameraComponents _currentConfiguration;

    private CameraController()
    { }

    public void SetCameraConfiguration(CameraComponents cameraConfiguration)
    {
        _currentConfiguration = cameraConfiguration;


        UpdateConfiguration();
    }

    internal void RemoveCameraConfiguration(CameraComponents cameraConfiguration)
    {
        if (_currentConfiguration == cameraConfiguration)
        {
            _currentConfiguration = null;
        }
    }

    private void UpdateConfiguration()
    {
        if (_currentConfiguration == null) return;

        EnableMouseOrbit = _isOrbitEnabled;
    }

    #endregion

    private bool _isOrbitEnabled;
    public bool EnableMouseOrbit
    {
        get { return _isOrbitEnabled; }
        set
        {
            _isOrbitEnabled = false;

            if (_currentConfiguration && _currentConfiguration.MouseOrbit)
                _currentConfiguration.MouseOrbit.enabled = _isOrbitEnabled;
        }
    }
}