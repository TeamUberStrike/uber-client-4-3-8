using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UberStrike.DataCenter.Common.Entities;

public class TutorialShootingTargetController
{
    private TutorialGameMode _game;

    List<TutorialShootingTarget> _targets = new List<TutorialShootingTarget>(6);

    public TutorialShootingTargetController(TutorialGameMode mode)
    {
        _game = mode;

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
                    s.OnHitCallback = OnTargetHit;
                }
            }
        }
    }

    public IEnumerator StartShootingRange()
    {
        SfxManager.Play2dAudioClip(LevelTutorial.Instance.VoiceShootingRange);

        yield return new WaitForSeconds(3);

        _game.ShowShoot3();

        LevelTutorial.Instance.ShowObjShoot3 = true;
        SfxManager.Play2dAudioClip(SoundEffectType.UISubObjective);

        for (int i = 0; i < LevelTutorial.Instance.NearRangeTargetPos.Length; i++)
            _targets[i].Reset();

        SfxManager.Play2dAudioClip(SoundEffectType.PropsTargetPopup);

        float timeBeforeShoot = Time.time;
        bool allHit;

        do
        {
            allHit = true;

            for (int i = 0; i < LevelTutorial.Instance.NearRangeTargetPos.Length; i++)
                allHit &= _targets[i].IsHit;

            if (!AmmoDepot.HasAmmoOfType(AmmoType.Machinegun) && !LevelTutorial.Instance.AmmoWaypoint.CanShow)
                LevelTutorial.Instance.AmmoWaypoint.CanShow = true;
            else if (AmmoDepot.HasAmmoOfType(AmmoType.Machinegun) && LevelTutorial.Instance.AmmoWaypoint.CanShow)
                LevelTutorial.Instance.AmmoWaypoint.CanShow = false;

            yield return new WaitForSeconds(0.3f);
        } while (!allHit);

        float timeAfterShoot = Time.time - timeBeforeShoot;
        if (timeAfterShoot < (LevelTutorial.Instance.VoiceShootingRange.length - 3))
            yield return new WaitForSeconds(LevelTutorial.Instance.VoiceShootingRange.length - 3);

        XpPtsHud.Instance.GainXp(5);

        _game.ObjShootTarget3.Complete();
        SfxManager.Play2dAudioClip(SoundEffectType.UIObjectiveTick);

        _game.HideShoot3();

        yield return new WaitForSeconds(0.5f);

        LevelTutorial.Instance.ShowObjShoot3 = false;

        yield return new WaitForSeconds(4.5f);

        LevelTutorial.Instance.ShowObjShoot6 = true;
        SfxManager.Play2dAudioClip(SoundEffectType.UISubObjective);

        _game.ShowShoot6();

        foreach (var t in _targets)
            t.Reset();

        SfxManager.Play2dAudioClip(SoundEffectType.PropsTargetPopup);

        do
        {
            allHit = true;

            foreach (var t in _targets)
                allHit &= t.IsHit;

            if (!AmmoDepot.HasAmmoOfType(AmmoType.Machinegun) && !LevelTutorial.Instance.AmmoWaypoint.CanShow)
                LevelTutorial.Instance.AmmoWaypoint.CanShow = true;
            else if (AmmoDepot.HasAmmoOfType(AmmoType.Machinegun) && LevelTutorial.Instance.AmmoWaypoint.CanShow)
                LevelTutorial.Instance.AmmoWaypoint.CanShow = false;

            yield return new WaitForSeconds(0.5f);
        } while (!allHit);

        AmmoHud.Instance.Enabled = false;

        /* put gun down */
        WeaponController.Instance.ResetPickupSlot();

        GameState.LocalPlayer.SetPlayerControlState(LocalPlayer.PlayerState.None);
        InputManager.Instance.IsInputEnabled = false;

        GameState.LocalDecorator.SetPosition(LevelTutorial.Instance.FinalPlayerPos.position, LevelTutorial.Instance.FinalPlayerPos.rotation);

        AvatarAnimationManager.Instance.ResetAnimationState(PageType.Shop);

        XpPtsHud.Instance.GainXp(5);

        yield return new WaitForSeconds(0.5f);

        _game.ObjShootTarget6.Complete();
        SfxManager.Play2dAudioClip(SoundEffectType.UIObjectiveTick);

        yield return new WaitForSeconds(0.5f);

        _game.HideShoot6();
        _game.DestroyObjectives();

        SfxManager.Play2dAudioClip(LevelTutorial.Instance.VoiceArena);

        yield return new WaitForSeconds(0.5f);

        LevelTutorial.Instance.ShowObjShoot6 = false;
        LevelTutorial.Instance.ShowObjectives = false;

        yield return new WaitForSeconds(0.5f);

        XpPtsHud.Instance.GainXp(30);

        yield return new WaitForSeconds(1f);

        LevelTutorial.Instance.ShowObjComplete = true;

        //_game.ShowTutorialComplete();
        //SfxManager.Play2dAudioClip(LevelTutorial.Instance.TutorialComplete);

        yield return new WaitForSeconds(2);

        //yield return new WaitForSeconds(0.5f);

        LevelTutorial.Instance.ShowObjComplete = false;

        yield return new WaitForSeconds(3);

        _game.HideObjComplete();

        LevelTutorial.Instance.BackgroundMusic.Stop();

        /* Level up ! */
        Screen.lockCursor = false;
        HpApHud.Instance.Enabled = false;
        PopupSystem.Show(new LevelUpPopup(2, EndLevelup));
        HudController.Instance.enabled = false;

        foreach (var i in _targets)
        {
            Object.Destroy(i.gameObject);
        }
    }

    private void OnTargetHit()
    {
        XpPtsHud.Instance.GainXp(1);
    }

    private void EndLevelup()
    {
        _game.OnTutorialEnd();
    }
}
