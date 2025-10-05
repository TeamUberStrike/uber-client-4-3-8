
using System.Collections;
using Cmune.Realtime.Photon.Client;
using UnityEngine;

public class GameServerController : Singleton<GameServerController>
{
    private GameServerController() { }

    public void JoinFastestServer()
    {
        MonoRoutine.Start(StartJoiningBestGameServer());
    }

    private IEnumerator StartJoiningBestGameServer()
    {
        ProgressPopupDialog _autoJoinPopup = PopupSystem.ShowProgress(LocalizedStrings.LoadingGameList, LocalizedStrings.FindingAServerToJoin);

        yield return MonoRoutine.Start(GameServerManager.Instance.StartUpdatingLatency((progress) => _autoJoinPopup.ManualProgress = progress));

        MenuPageManager.Instance.LoadPage(PageType.Play);

        GameServerController.Instance.SelectedServer = GameServerManager.Instance.GetBestServer();
        if (PlayPageGUI.Exists)
            PlayPageGUI.Instance.SelectedServerUpdated();

        yield return new WaitForSeconds(0.5f);

        PopupSystem.HideMessage(_autoJoinPopup);
    }

    public GameServerView SelectedServer { get; set; }
}