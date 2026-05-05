using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopShootingTargetController
{
    private List<TutorialShootingTarget> _targets = new List<TutorialShootingTarget>(6);

    public ShopShootingTargetController()
    {
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
