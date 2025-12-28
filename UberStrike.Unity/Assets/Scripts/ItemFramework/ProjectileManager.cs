
using System.Collections.Generic;
using UnityEngine;

public class ProjectileManager : Singleton<ProjectileManager>
{
    private Dictionary<int, IProjectile> _projectiles;
    private List<int> _limitedProjectiles;

    private ProjectileManager()
    {
        _projectiles = new Dictionary<int, IProjectile>();
        _limitedProjectiles = new List<int>();
    }

    public void AddProjectile(IProjectile p, int id)
    {
        if (p != null)
        {
            p.ID = id;
            _projectiles.Add(p.ID, p);
        }
    }

    public void AddLimitedProjectile(IProjectile p, int id, int count)
    {
        if (p != null)
        {
            p.ID = id;

            _projectiles.Add(p.ID, p);
            _limitedProjectiles.Add(p.ID);

            CheckLimitedProjectiles(count);
        }
    }

    private void CheckLimitedProjectiles(int count)
    {
        int[] projectiles = _limitedProjectiles.ToArray();

        for (int i = 0; i < (_limitedProjectiles.Count - count); i++)
        {
            RemoveProjectile(projectiles[i]);
            GameState.CurrentGame.RemoveProjectile(projectiles[i], true);
        }
    }

    public void RemoveAllLimitedProjectiles(bool explode = true)
    {
        int[] projectiles = _limitedProjectiles.ToArray();

        for (int i = 0; i < projectiles.Length; i++)
        {
            RemoveProjectile(projectiles[i], explode);
            GameState.CurrentGame.RemoveProjectile(projectiles[i], explode);
        }
    }

    public void RemoveProjectile(int id, bool explode = true)
    {
        try
        {
            IProjectile projectile;
            if (_projectiles.TryGetValue(id, out projectile))
            {
                if (explode)
                {
                    projectile.Explode();
                }
                else
                {
                    projectile.Destroy();
                }
            }
        }
        finally
        {
            _limitedProjectiles.RemoveAll(i => i == id);
            _projectiles.Remove(id);
        }
    }

    public void RemoveAllProjectilesFromPlayer(byte playerNumber)
    {
        foreach (int id in _projectiles.KeyArray())
        {
            if ((id & 0xFF) == playerNumber)
            {
                RemoveProjectile(id, false);
            }
        }
    }

    public void ClearAll()
    {
        try
        {
            foreach (var p in _projectiles)
            {
                p.Value.Destroy();
            }
        }
        finally
        {
            _projectiles.Clear();
            _projectiles.Clear();
        }
    }

    public static int CreateGlobalProjectileID(byte playerNumber, int localProjectileId)
    {
        return (localProjectileId << 8) + playerNumber;
    }

    public static string PrintID(int id)
    {
        return GetPlayerId(id) + "/" + (id >> 8);
    }

    private static int GetPlayerId(int projectileId)
    {
        return (projectileId & 0xFF);
    }

    public IEnumerable<KeyValuePair<int, IProjectile>> AllProjectiles { get { return _projectiles; } }
    public IEnumerable<int> LimitedProjectiles { get { return _limitedProjectiles; } }
}