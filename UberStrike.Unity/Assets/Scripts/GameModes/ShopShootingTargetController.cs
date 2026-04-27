using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopShootingTargetController
{
    private List<TutorialShootingTarget> _targets = new List<TutorialShootingTarget>(6);

    public ShopShootingTargetController()
    {
        // Unity 2022 additive-scene artifact (2026-04-24): Lobby scene
        // can get reloaded additively without its prior copy unloading
        // after a match→lobby round-trip, producing duplicate
        // LevelTutorial MonoBehaviours. MonoSingleton<T>.Instance
        // throws on >1 match, which crashes Try-Weapon entry and
        // corrupts the state machine so the user can't return to
        // maps. Prune duplicates before reading the singleton.
        // Keep the first instance (stable ordering from
        // FindObjectsOfType matches creation order in practice);
        // log the count so the root cause stays visible for a later
        // scene-unloading pass.
        var allTutorials = Object.FindObjectsOfType<LevelTutorial>();
        if (allTutorials.Length > 1)
        {
            Debug.LogWarning("[ShopShootingTargetController] Found " + allTutorials.Length +
                             " LevelTutorial instances; pruning " + (allTutorials.Length - 1) +
                             " duplicate(s). Likely a duplicate LevelSpaceship scene load.");
            // DestroyImmediate (not Destroy) so the very next
            // LevelTutorial.Instance access below sees only the
            // surviving instance — Destroy would defer removal to
            // end-of-frame and MonoSingleton would still throw.
            for (int i = 1; i < allTutorials.Length; i++)
            {
                GameObject.DestroyImmediate(allTutorials[i].gameObject);
            }
        }

        List<Transform> trans = new List<Transform>();

        trans.AddRange(LevelTutorial.Instance.NearRangeTargetPos);
        trans.AddRange(LevelTutorial.Instance.FarRangeTargetPos);

        foreach (var t in trans)
        {
            GameObject obj = GameObject.Instantiate(LevelTutorial.Instance.ShootingTargetPrefab, t.position, t.rotation) as GameObject;
            if (obj)
            {
                TutorialShootingTarget s = obj.GetComponent<TutorialShootingTarget>();
                if (s)
                {
                    _targets.Add(s);
                }
            }
        }
    }

    public void Enable()
    {
        MonoRoutine.Start(StartShootingRange());
    }

    public void Disable()
    {
        foreach (var t in _targets)
            GameObject.Destroy(t.gameObject);

        _targets.Clear();
    }

    private IEnumerator StartShootingRange()
    {
        while (_targets.Count > 0)
        {
            bool allHit;

            foreach (var i in _targets)
                i.Reset();

            do
            {
                allHit = true;

                foreach (var t in _targets)
                    allHit &= t.IsHit;

                yield return new WaitForSeconds(1f);
            } while (!allHit);

            yield return new WaitForEndOfFrame();
        }
    }
}
