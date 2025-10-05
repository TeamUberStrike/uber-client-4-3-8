#if UNITY_EDITOR

using System.Collections;
using UnityEngine;

public class LevelTester : MonoBehaviour
{
    static LevelTester()
    {
    }

    // Use this for initialization
    public static void TestMap(MapConfiguration map)
    {
        GameObject go = new GameObject("Tester");
        LevelTester tester = go.AddComponent<LevelTester>();
        tester.StartCoroutine(tester.StartTesting(map));
    }

    IEnumerator StartTesting(MapConfiguration map)
    {
        yield return new WaitForEndOfFrame();

        GameState.SetCurrentSpace(map);

        LevelCamera.Instance.SetLevelCamera(GameState.CurrentSpace.Camera, GameState.CurrentSpace.DefaultViewPoint.position, GameState.CurrentSpace.DefaultViewPoint.rotation);

        GameState.LocalPlayer.InitializePlayer();

        GameState.LocalPlayer.SetPlayerControlState(LocalPlayer.PlayerState.FirstPerson);
    }

    IEnumerator StartOfflineTraining(int mapId, string levelName)
    {
        yield return new WaitForSeconds(1);

        LevelManager.Instance.AddMapView(mapId, levelName);

        //// Get Encryption vectors
        //yield return ApplicationWebServiceClient.AuthenticateApplication(ApplicationDataManager.VersionShort, ChannelType.WebPortal, string.Empty,
        //    (ev) =>
        //    {
        //        Configuration.EncryptionInitVector = ev.EncryptionInitVector;
        //        Configuration.EncryptionPassPhrase = ev.EncryptionPassPhrase;
        //    },
        //    (ex) =>
        //    {
        //        ApplicationDebug.SendExceptionReport(ex.Message, ex.StackTrace);
        //    });

        //// Get all shop itmes
        //yield return StartCoroutine(ItemManager.Instance.StartGetShop());

        //// Enable all items in inventory
        //ItemManager.Instance.EnableAllItems();

        //// load level
        //GameStateController.Instance.LoadLevel(mapId);

        //// start game;
        //GameStateController.Instance.LoadGameMode(GameMode.Training);

        //yield return new WaitForSeconds(1);

        //// remove popup for offline mode
        //PopupSystem.ClearAll();
    }
}
#endif