using UnityEngine;
using System.Collections;
using Cmune.Util;

public class MeshGUIManager : MonoSingleton<MeshGUIManager>
{
    #region Inspector Variables

    [SerializeField]
    private GameObject _meshTextObject;

    [SerializeField]
    private GameObject _quadMeshObject;

    [SerializeField]
    private GameObject _circleMeshObject;

    [SerializeField]
    private Camera _guiCamera;

    #endregion

    #region Fields

    private ObjectRecycler _meshTextRecycler;
    private ObjectRecycler _quadMeshRecycler;
    private ObjectRecycler _circleMeshRecycler;
    private GameObject _meshTextContainer;
    private GameObject _quadMeshContainer;
    private GameObject _circleMeshContainer;

    #endregion

    #region Private Methods

    private void Awake()
    {
        CreateMeshGUIContainers();
        CreateMeshGUIRecyclers();

        CmuneEventHandler.AddListener<CameraWidthChangeEvent>(OnCameraRectChange);
    }

    private void OnCameraRectChange(CameraWidthChangeEvent ev)
    {
        if (_guiCamera != null)
            _guiCamera.rect = new Rect(0, 0, ev.Width, 1);
    }

    private void CreateMeshGUIContainers()
    {
        _meshTextContainer = new GameObject("MeshTextContainer");
        _meshTextContainer.transform.parent = gameObject.transform;
        _quadMeshContainer = new GameObject("QuadMeshContainer");
        _quadMeshContainer.transform.parent = gameObject.transform;
        _circleMeshContainer = new GameObject("CircleContainer");
        _circleMeshContainer.transform.parent = gameObject.transform;
    }

    private void CreateMeshGUIRecyclers()
    {
        _meshTextRecycler = new ObjectRecycler(_meshTextObject, 5, _meshTextContainer);
        _quadMeshRecycler = new ObjectRecycler(_quadMeshObject, 5, _quadMeshContainer);
        _circleMeshRecycler = new ObjectRecycler(_circleMeshObject, 5, _circleMeshContainer);
    }

    #endregion

    #region Public Methods

    public GameObject CreateMeshText(GameObject parentObject = null)
    {
        GameObject meshTextObj = _meshTextRecycler.GetNextFree();
        if (parentObject != null)
        {
            meshTextObj.transform.parent = parentObject.transform;
        }
        return meshTextObj;
    }

    public void FreeMeshText(GameObject meshTextObject)
    {
        meshTextObject.transform.parent = _meshTextContainer.transform;
        _meshTextRecycler.FreeObject(meshTextObject);
    }

    public GameObject CreateQuadMesh(GameObject parentObject = null)
    {
        GameObject quadMeshObject = _quadMeshRecycler.GetNextFree();
        if (parentObject != null)
        {
            quadMeshObject.transform.parent = parentObject.transform;
        }
        return quadMeshObject;
    }

    public void FreeQuadMesh(GameObject quadMeshObject)
    {
        quadMeshObject.transform.parent = _quadMeshContainer.transform;
        _quadMeshRecycler.FreeObject(quadMeshObject);
    }

    public GameObject CreateCircleMesh(GameObject parentObject = null)
    {
        GameObject circleMeshObject = _circleMeshRecycler.GetNextFree();
        if (parentObject != null)
        {
            circleMeshObject.transform.parent = parentObject.transform;
        }
        return circleMeshObject;
    }

    public void FreeCircleMesh(GameObject circleMeshObject)
    {
        circleMeshObject.transform.parent = _circleMeshContainer.transform;
        _circleMeshRecycler.FreeObject(circleMeshObject);
    }

    public Vector3 TransformPosFromScreenToWorld(Vector2 screenPos)
    {
        float cameraSizeY = _guiCamera.orthographicSize;
        float cameraSizeX = cameraSizeY / Screen.height * Screen.width;

        Vector3 worldPos = Vector3.zero;
        worldPos.x = screenPos.x / Screen.width * cameraSizeX * 2 - cameraSizeX;
        worldPos.y = screenPos.y / Screen.height * cameraSizeY * 2 - cameraSizeY;
        worldPos.y = -worldPos.y;
        return worldPos;
    }

    public Vector2 TransformPosFromWorldToScreen(Vector3 worldPos)
    {
        float cameraSizeY = _guiCamera.orthographicSize;
        float cameraSizeX = cameraSizeY / Screen.height * Screen.width;

        Vector2 screenPos = Vector2.zero;
        screenPos.x = (worldPos.x + cameraSizeX) * Screen.width / cameraSizeX / 2;
        screenPos.y = (-worldPos.y + cameraSizeY) * Screen.height / cameraSizeY / 2;
        return screenPos;
    }

    public Vector3 TransformSizeFromScreenToWorld(Vector2 screenSize)
    {
        float cameraSizeY = _guiCamera.orthographicSize;
        float cameraSizeX = cameraSizeY / Screen.height * Screen.width;

        Vector3 worldSize = Vector3.zero;
        worldSize.x = screenSize.x / Screen.width * cameraSizeX * 2;
        worldSize.y = screenSize.y / Screen.height * cameraSizeY * 2;
        return worldSize;
    }

    public Vector2 TransformSizeFromWorldToScreen(Vector3 worldSize)
    {
        float cameraSizeY = _guiCamera.orthographicSize;
        float cameraSizeX = cameraSizeY / Screen.height * Screen.width;

        Vector2 screenSize = Vector2.zero;
        screenSize.x = worldSize.x / cameraSizeX / 2 * Screen.width;
        screenSize.y = worldSize.y / cameraSizeY / 2 * Screen.height;
        return screenSize;
    }

    #endregion
}