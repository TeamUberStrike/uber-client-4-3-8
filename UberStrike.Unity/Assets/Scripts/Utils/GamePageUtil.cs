using UnityEngine;

public class GamePageUtil
{
    private static GameObject _cameraTarget;
    private static Vector3 _offset = Vector3.up * 0.4f;

    public static Vector3 CameraTargetPosition
    {
        get { return _cameraTarget ? _cameraTarget.transform.position : Vector3.zero; }
    }

    public static void GetAvatarSpawnPoint(out Vector3 pos, out Quaternion rot)
    {
        SpawnPointManager.Instance.GetSpawnPointAt(GameState.CurrentSpace.DefaultSpawnPoint, GameMode.DeathMatch, UberStrike.Realtime.Common.TeamID.NONE, out pos, out rot);

        RaycastHit hit;
        if (Physics.Raycast(pos, Vector3.down, out hit, 1000, UberstrikeLayerMasks.ShootMask))
        {
            pos = hit.point;
        }

        rot = GetCollisionFreeRotation(pos);
    }

    public static void SpawnLocalAvatar()
    {
        if (GameState.LocalDecorator)
        {
            Vector3 pos;
            Quaternion rot;

            GetAvatarSpawnPoint(out pos, out rot);

            AvatarAnimationManager.Instance.ResetAnimationState(PageType.Shop);

            GameState.LocalDecorator.HideWeapons();
            GameState.LocalDecorator.SetPosition(pos, rot);
            GameState.LocalPlayer.transform.position = pos + Vector3.up * 2;
            GameState.LocalDecorator.MeshRenderer.enabled = true;
            GameState.LocalDecorator.SetLayers(UberstrikeLayer.RemotePlayer);
        }
    }

    public static void EnableOverviewCamera(Transform parent)
    {
        if (GameState.LocalDecorator)
        {
            Vector3 dir = -GameState.LocalDecorator.transform.forward;
            Transform spine = GameState.LocalDecorator.GetBone(BoneIndex.Spine);

            _cameraTarget = new GameObject("Camera Target");
            _cameraTarget.transform.parent = parent;
            _cameraTarget.transform.position = spine.position + _offset;
            _cameraTarget.transform.rotation = Quaternion.LookRotation(dir);

            LevelCamera.Instance.SetTarget(_cameraTarget.transform);
            LevelCamera.Instance.SetMode(LevelCamera.CameraMode.Overview);
        }
    }

    public static void DisableOverviewCamera()
    {
        if (LevelCamera.Instance.CurrentMode == LevelCamera.CameraMode.Overview)
        {
            LevelCamera.Instance.SetMode(LevelCamera.CameraMode.None);
        }

        if (_cameraTarget)
            GameObject.Destroy(_cameraTarget);
    }

    public static void SetCameraTargetPosition(Vector3 position)
    {
        if (_cameraTarget)
        {
            _cameraTarget.transform.position = position;
        }
    }

    private static Quaternion GetCollisionFreeRotation(Vector3 pos)
    {
        float randAngle = Random.Range(0, 45f);
        float farthestCollisionDistance = 0;
        Quaternion farthestCollisionRotation = Quaternion.identity;

        for (int i = 0; i < 8; i++)
        {
            Quaternion rot = Quaternion.Euler(0, i * 45 + randAngle, 0);
            Vector3 origin = pos + Vector3.up * 1.5f;
            Vector3 direction = rot * LevelCamera.OverviewState.ViewDirection;

            RaycastHit hit;
            if (Physics.Raycast(origin, direction, out hit, LevelCamera.OverviewState.InitialDistance, UberstrikeLayerMasks.ShootMask))
            {
                if (hit.distance > farthestCollisionDistance)
                {
                    farthestCollisionDistance = hit.distance;
                    farthestCollisionRotation = rot;
                }
            }
            else
            {
                farthestCollisionRotation = rot;
                break;
            }
        }

        return farthestCollisionRotation;
    }
}