using UberStrike.DataCenter.Common.Entities;
using UberStrike.Realtime.Common;
using UnityEngine;
using UberStrike.Core.Types;

public class ReticleHud : Singleton<ReticleHud>
{
    private enum ReticleState
    {
        None,
        Enemy,
        Friend,
    }

    private class ReticleConfiguration
    {
        public Reticle Primary;
        public Reticle Secondary;
    }

    public bool Enabled { get; set; }

    public void ConfigureReticle(WeaponItemConfiguration weapon)
    {
        var reticle = ReticleRepository.Instance.GetReticle(weapon.ItemClass);

        _reticle = new ReticleConfiguration()
        {
            Primary = weapon.ShowReticleForPrimaryAction ? reticle : null,
            Secondary = (weapon.SecondaryActionReticle == ReticuleForSecondaryAction.Default) ? reticle : null,
        };
    }

    public void Draw()
    {
        if (_reticle == null) return;
        if (!WeaponController.Instance.HasAnyWeapon) return;

        //check if we are focusing on a friend
        if (_curState == ReticleState.Friend)
            GUI.color = Color.green;
        //if we hit an enemy in the last second
        else if (_isDisplayingEnemyReticle)
            GUI.color = Color.red;
        else
            GUI.color = Color.white;

        // draw sniper recticle
        // Fix me: Better move this part to WeaponDecorator to remove HudManager's dependcy on WeaponController
        if (WeaponController.Instance.IsSecondaryAction)
        {
            if (_reticle.Secondary != null)
            {
                _reticle.Secondary.Draw(new Rect((Screen.width - 64) * 0.5f, (Screen.height - 64) * 0.5f, 64, 64));
            }
            else if (!WeaponFeedbackManager.Instance.IsIronSighted)
            {
                float size = Mathf.Min(Screen.width, Screen.height);
                float w = (Screen.width - size) * 0.5f;
                float h = (Screen.height - size) * 0.5f;

                // draw the reticle
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(w, h, size, size), HudTextures.ReticleSRZoom);

                // draw two black quad to cover the rest of the screen
                if (Screen.width > Screen.height)
                {
                    GUI.DrawTexture(new Rect(0, 0, w, Screen.height), BlueStonez.box_black.normal.background);
                    GUI.DrawTexture(new Rect(size + w, 0, w, Screen.height), BlueStonez.box_black.normal.background);
                }
                else
                {
                    GUI.DrawTexture(new Rect(0, 0, Screen.width, h), BlueStonez.box_black.normal.background);
                    GUI.DrawTexture(new Rect(0, h + size, Screen.width, h), BlueStonez.box_black.normal.background);
                }
            }
        }
        else
        {
            if (_reticle.Primary != null)
            {
                _reticle.Primary.Draw(new Rect((Screen.width - 64) * 0.5f, (Screen.height - 64) * 0.5f, 64, 64));
            }
        }

        GUI.color = Color.white;
    }

    public void Update()
    {
        if (_isDisplayingEnemyReticle && Time.time > _enemyReticleHideTime)
        {
            _isDisplayingEnemyReticle = false;
        }

        ReticleRepository.Instance.UpdateAllReticles();
    }

    public void TriggerReticle(UberstrikeItemClass type)
    {
        Reticle reticle = ReticleRepository.Instance.GetReticle(type);

        if (reticle != null)
        {
            reticle.Trigger();
        }
        else
        {
            Debug.LogError("The weapon class: " + type.ToString() + " is not configured!");
        }
    }

    public void FocusCharacter(TeamID teamId)
    {
        if (GameState.CurrentGameMode == GameMode.TeamDeathMatch)
        {
            if (GameState.HasCurrentPlayer && teamId == GameState.LocalCharacter.TeamID)
                _curState = ReticleState.Friend;
            else if (_isDisplayingEnemyReticle)
                _curState = ReticleState.Enemy;
            else
                _curState = ReticleState.None;
        }
        else
        {
            if (_isDisplayingEnemyReticle)
                _curState = ReticleState.Enemy;
            else
                _curState = ReticleState.None;
        }
    }

    public void UnFocusCharacter()
    {
        _curState = ReticleState.None;
    }

    public void EnableEnemyReticle()
    {
        _isDisplayingEnemyReticle = true;
        _enemyReticleHideTime = Time.time + 1;
    }

    #region Private
    private ReticleHud()
    {
        Enabled = false;
    }

    private ReticleConfiguration _reticle;
    private ReticleState _curState;
    private bool _isDisplayingEnemyReticle;
    private float _enemyReticleHideTime;
    #endregion
}

