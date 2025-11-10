using UnityEngine;

public static class WeaponDataManager
{
    public static Vector3 ApplyDispersion(Vector3 shootingRay, WeaponItemConfiguration config, bool ironSight)
    {
        float dispersion = WeaponConfigurationHelper.GetAccuracySpread(config);

        if (WeaponFeedbackManager.Exists &&
            WeaponFeedbackManager.Instance.IsIronSighted && ironSight)
        {
            //half the dispersion when in iron sight
            dispersion *= 0.5f;
        }

        Vector2 r = UnityEngine.Random.insideUnitCircle * dispersion * 0.5f;

        return Quaternion.AngleAxis(r.x, GameState.WeaponCameraTransform.right) * Quaternion.AngleAxis(r.y, GameState.WeaponCameraTransform.up) * shootingRay;
    }
}