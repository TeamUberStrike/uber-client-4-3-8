using System.Collections;
using UberStrike.DataCenter.Common.Entities;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LevelBoundary : MonoBehaviour
{
    private static float _checkTime;
    private static LevelBoundary _currentLevelBoundary;

    private void Awake()
    {
        if (GetComponent<Renderer>()) GetComponent<Renderer>().enabled = false;

        StartCoroutine(StartCheckingPlayerInBounds(GetComponent<Collider>()));
    }

    private void OnDisable()
    {
        _checkTime = 0;
        _currentLevelBoundary = null;
    }

    private void OnTriggerExit(Collider c)
    {
        if (c.tag == "Player" && GameState.HasCurrentGame)
        {
            if (_currentLevelBoundary == this)
                _currentLevelBoundary = null;

            StartCoroutine(StartCheckingPlayer());
        }
    }

    private IEnumerator StartCheckingPlayer()
    {
        if (_checkTime == 0)
        {
            _checkTime = Time.time + 0.5f;

            while (_checkTime > Time.time)
            {
                yield return new WaitForEndOfFrame();
            }

            //in case the player is still outside of the death collider after 1 second we have to kill him
            if (_currentLevelBoundary == null)
            {
                if (GameState.LocalCharacter.IsAlive)
                {
                    KillPlayer();
                }
            }
            else
            {
                Debug.LogError("Stop killing the player!");
            }

            _checkTime = 0;
        }
        else
        {
            _checkTime = Time.time + 1;
        }
    }

    private IEnumerator StartCheckingPlayerInBounds(Collider c)
    {
        while (true)
        {
            if (GameState.HasCurrentPlayer)
            {
                if (!c.bounds.Contains(GameState.LocalCharacter.Position))
                {
                    KillPlayer();
                }
            }

            yield return new WaitForSeconds(1);
        }
    }

    public static void KillPlayer()
    {
        //in waiting mode - just respawn
        if (GameState.HasCurrentGame && GameState.CurrentGame.IsWaitingForPlayers)
        {
            GameState.CurrentGame.RespawnPlayer();
        }
        else if (!GameState.LocalPlayer.IsDead)
        {
            if (GameState.LocalPlayer.Character != null && !GameState.LocalPlayer.IsDead)
            {
                GameState.LocalPlayer.Character.ApplyDamage(new DamageInfo(999));
            }
        }
    }

    private void OnTriggerEnter(Collider c)
    {
        if (c.tag == "Player" && GameState.HasCurrentGame)
        {
            _currentLevelBoundary = this;
        }
    }

    private string PrintHierarchy(Transform t)
    {
        System.Text.StringBuilder b = new System.Text.StringBuilder();
        b.Append(t.name);
        Transform p = t.parent;
        while (p)
        {
            b.Insert(0, p.name + "/");
            p = p.parent;
        }
        return b.ToString();
    }

    private string PrintVector(Vector3 v)
    {
        return string.Format("({0:N6},{1:N6},{2:N6})", v.x, v.y, v.z);
    }
}