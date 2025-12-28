using UberStrike.Realtime.Common;
using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    #region Inspector Variables

    [SerializeField]
    private bool DrawGizmos = true;

    [SerializeField]
    private float Radius = 1;

    [SerializeField]
    public TeamID TeamPoint;

    [SerializeField]
    public GameMode GameMode;

    #endregion

    #region Properties

    public Vector3 Position { get { return transform.position; } }

    public Vector2 Rotation { get { return new Vector2(transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.x); } }

    public TeamID TeamId { get { return TeamPoint; } }

    public float SpawnRadius { get { return Radius; } }

    #endregion

    #region Private Methods

    private void OnDrawGizmos()
    {
        if (!DrawGizmos) return;

        switch (TeamPoint)
        {
            case TeamID.NONE:
                Gizmos.color = Color.green;
                break;
            case TeamID.RED:
                Gizmos.color = Color.red;
                break;
            case TeamID.BLUE:
                Gizmos.color = Color.blue;
                break;
        }

        Gizmos.matrix = Matrix4x4.TRS(transform.position, Quaternion.identity, new Vector3(1, 0.1f, 1));
        Gizmos.DrawSphere(Vector3.zero, Radius);
        switch (GameMode)
        {
            case global::GameMode.DeathMatch:
                Gizmos.color = Color.yellow;
                break;
            case global::GameMode.TeamDeathMatch:
                Gizmos.color = Color.white;
                break;
            case global::GameMode.TeamElimination:
                Gizmos.color = Color.magenta;
                break;
        }
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.DrawLine(transform.position + transform.forward * Radius, transform.position + transform.forward * 2 * Radius);
    }

    #endregion
}