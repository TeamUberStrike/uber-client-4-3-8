using UnityEngine;
using System.Collections;

// Controls the window movement for MysteryBox/LuckyDraw
public class LotteryPopupTask2
{
    private enum State
    {
        None,
        Rolled,
    }

    private const float MinWaitingTime = 2;

    private State _state;
    private LotteryPopupDialog _popup;

    public LotteryPopupTask2(LotteryPopupDialog dialog)
    {
        _state = State.None;

        _popup = dialog;
        _popup.SetRollCallback(OnLotteryRolled);

        MonoRoutine.Start(StartTask());
    }

    private IEnumerator StartTask()
    {
        while (_state == State.None)
        {
            yield return new WaitForEndOfFrame();
        }

        if (_state == State.Rolled)
        {
            _popup.IsWaiting = true;
            _popup.IsUIDisabled = true;

            float time = 0;
            while (!_popup.IsLotteryReturned || time < MinWaitingTime)
            {
                time += Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }

            _popup.IsWaiting = false;

            //play expolosion effect
            PrefabManager.Instance.InstantiateLotteryEffect();
            SfxManager.Play2dAudioClip(SoundEffectType.UIMysteryBoxWin);

            MonoRoutine.Start(PlayerDataManager.Instance.StartGetMember());
            MonoRoutine.Start(ItemManager.Instance.StartGetInventory(false));
            yield return new WaitForSeconds(2);

            //fade in the winning popup
            LotteryWinningPopup winningPopup = _popup.ShowReward();
            PopupSystem.HideMessage(_popup);
            PopupSystem.Show(winningPopup);

            var _uiAnim = PrefabManager.Instance.GetLotteryUIAnimation();
            _uiAnim.Play("mainPopup");

            while (_uiAnim.isPlaying)
            {
                winningPopup.SetYOffset(_uiAnim.transform.position.y);
                yield return new WaitForEndOfFrame();
            }

            GameState.Destroy(_uiAnim.gameObject);
        }
    }

    private void OnLotteryRolled()
    {
        _state = State.Rolled;
    }
}