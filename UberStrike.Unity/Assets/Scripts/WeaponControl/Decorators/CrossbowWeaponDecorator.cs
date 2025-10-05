using UnityEngine;
using System.Collections;

public class CrossbowWeaponDecorator : BaseWeaponDecorator
{
    [SerializeField]
    private ArrowProjectile _arrowProjectile;

    protected override void ShowImpactEffects(RaycastHit hit, Vector3 direction, Vector3 muzzlePosition, float distance, bool playSound)
    {
        CreateArrow(hit, direction);

        base.ShowImpactEffects(hit, direction, muzzlePosition, distance, playSound);
    }

    private void CreateArrow(RaycastHit hit, Vector3 direction)
    {
        if (_arrowProjectile && hit.collider != null)
        {
            Quaternion rotation = new Quaternion();
            rotation = Quaternion.FromToRotation(Vector3.back, direction * (-1));

            ArrowProjectile go = Instantiate(_arrowProjectile, hit.point, rotation) as ArrowProjectile;

            if (hit.collider.gameObject.layer == (int)UberstrikeLayer.LocalPlayer)
            {
                if (GameState.LocalDecorator)
                {
                    // if that is local avatar, then parent to avatar decorator directly
                    go.gameObject.transform.parent = GameState.LocalDecorator.GetBone(BoneIndex.Hips);
                    // and hide them
                    foreach (Renderer item in go.GetComponentsInChildren<Renderer>(true))
                        item.enabled = false;
                }
            }
            else if (hit.collider.gameObject.layer == (int)UberstrikeLayer.RemotePlayer)
            {
                // attach to remote players
                go.SetParent(hit.collider.transform);
            }

            go.Destroy(15);
        }
    }
}