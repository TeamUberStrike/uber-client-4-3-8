using UnityEngine;
using System.Collections;
using UberStrike.WebService.Unity;
using UberStrike.DataCenter.Common.Entities;
using UberStrike.Core.Types;

public class TutorialArmoryEnterTrigger : MonoBehaviour
{
    public Transform ArmoryDoor;
    public Transform ArmoryDesk;

    public SplineController ArmoryCameraPath;

    private bool _entered;

    private Vector3 _velocity;

    private void OnTriggerEnter()
    {
        if (!_entered)
        {
            _entered = true;

            /* Start camera path */
            GameState.LocalPlayer.IsWalkingEnabled = false;
            InputManager.Instance.IsInputEnabled = false;

            LevelCamera.SetBobMode(BobMode.None);
            LevelCamera.Instance.SetMode(LevelCamera.CameraMode.None);

            LevelTutorial.Instance.ArmoryWaypoint.CanShow = false;
            Destroy(LevelTutorial.Instance.ArmoryWaypoint);

            StartCoroutine(StartEnterArmory());
        }
    }

    private IEnumerator StartEnterArmory()
    {
        TutorialGameMode mode = null;

        if (GameState.HasCurrentGame && GameState.CurrentGame is TutorialGameMode)
        {
            mode = GameState.CurrentGame as TutorialGameMode;
        }

        if (mode != null)
            mode.ReachArmoryWaypoint();

        /* Smooth the camera movement */
        while (Vector3.SqrMagnitude(LevelCamera.Instance.MainCamera.velocity) > 0.1f)
        {
            Vector3 v = Vector3.Lerp(LevelCamera.Instance.MainCamera.velocity, Vector3.zero, Time.deltaTime * 5);

            LevelCamera.Instance.TransformCache.position += v * Time.deltaTime;

            yield return new WaitForEndOfFrame();
        }

        LevelTutorial.Instance.ShowObjComplete = true;

        if (mode != null)
        {
            mode.ShowObjComplete();

            SfxManager.Play2dAudioClip(LevelTutorial.Instance.BigObjComplete);
        }

        yield return new WaitForSeconds(2.5f);

        LevelTutorial.Instance.IsCinematic = true;

        if (mode != null)
        {
            mode.HideObjectives();
            mode.ResetBlackBar();

            mode.HideObjComplete(false);
        }

        yield return new WaitForSeconds(0.5f);

        LevelTutorial.Instance.PickupWeapon.Show(false);
        LevelTutorial.Instance.ShowObjComplete = false;

        _velocity = Vector3.Normalize(ArmoryDoor.position - LevelCamera.Instance.TransformCache.position);

        /* lerp to door */
        do
        {
            float speed = Vector3.Magnitude(LevelCamera.Instance.TransformCache.position - ArmoryDoor.position);

            LevelCamera.Instance.TransformCache.position += _velocity * Time.deltaTime * Mathf.Clamp(speed, 2, 12);
            LevelCamera.Instance.TransformCache.rotation = Quaternion.Lerp(LevelCamera.Instance.TransformCache.rotation, ArmoryDoor.rotation, Time.deltaTime * 2);

            if (Vector3.SqrMagnitude(LevelCamera.Instance.TransformCache.position - ArmoryDoor.position) < 0.3f)
                break;

            yield return new WaitForEndOfFrame();

        } while (true);

        LevelTutorial.Instance.ArmoryDoor.Open();

        /* spline to NPC in armory */

        OnEndCallback callback = null;

        if (mode != null)
        {
            mode.EnterArmory(ArmoryCameraPath);
        }
        else
        {
            Debug.LogError("Failed to get TutorialGameMode");
        }

        _velocity = Vector3.Normalize(ArmoryDesk.position - LevelCamera.Instance.TransformCache.position);

        // Show the MG Pickup
        LevelTutorial.Instance.ShowObjPickupMG = true;
        LevelTutorial.Instance.PickupWeapon.SetItemAvailable(true);
        LevelTutorial.Instance.WeaponWaypoint.CanShow = true;
        SfxManager.Play2dAudioClip(LevelTutorial.Instance.WaypointAppear);

        /* start camera path */
        ArmoryCameraPath.FollowSpline(callback);

        /* put npc in position */
        if (LevelTutorial.Instance.NPC)
        {
            LevelTutorial.Instance.NPC.position = new Vector3(-15.18789f, -2.342683f, -4.393633f);
            LevelTutorial.Instance.NPC.rotation = Quaternion.Euler(0, 55.204f, 0);
        }

        float time = 0;
        bool played = false;

        /* Wait for the camera to lerp to the correct position */
        while (Vector3.Dot(LevelCamera.Instance.TransformCache.position - LevelTutorial.Instance.ArmoryCameraPathEnd.position, Vector3.right) > 0 &&
            Vector3.Distance(LevelCamera.Instance.TransformCache.position, LevelTutorial.Instance.ArmoryCameraPathEnd.position) > 0.1f)
        {
            yield return new WaitForEndOfFrame();

            time += Time.deltaTime;

            if (time > 1 && !played)
            {
                played = true;

                LevelTutorial.Instance.NPC.GetComponent<Animation>().Blend(AnimationIndex.TutorialGuideIdle.ToString(), 0);
                LevelTutorial.Instance.NPC.GetComponent<Animation>().Blend(AnimationIndex.TutorialGuideArmory.ToString(), 1);
            }
        }

        ArmoryCameraPath.Stop();

        // Camera pos = (-13.1647, -0.8052588, -3.1917) rot = (230.8039)
        // Player pos = (-13.1647, -1.355259, -3.1917)
        Vector3 pos = LevelTutorial.Instance.ArmoryCameraPathEnd.position;
        pos.y = -1.355259f;

        Vector3 rotAngles = new Vector3(0, 230.1176f, 0); //LevelCamera.Instance.TransformCache.rotation.eulerAngles;

        GameState.LocalPlayer.SpawnPlayerAt(pos, Quaternion.Euler(rotAngles));
        GameState.LocalCharacter.Keys = UberStrike.Realtime.Common.KeyState.Still;

        LevelCamera.Instance.CanDip = false;

        // Only enabled user control after the voice over is finished

        if (mode != null)
            mode.OnArmoryEnterSubtitle();
        else
            Debug.LogError("No TutorialGameMode!");

        yield return new WaitForSeconds(2);

        LevelTutorial.Instance.NPC.GetComponent<Animation>().Blend(AnimationIndex.TutorialGuideArmory.ToString(), 0);
        LevelTutorial.Instance.NPC.GetComponent<Animation>().Blend(AnimationIndex.TutorialGuideTalk.ToString(), 1);

        ApplicationWebServiceClient.RecordTutorialStep(PlayerDataManager.CmidSecure, TutorialStepType.WalkToArmory,
            () => Debug.Log("WalkToArmory recorded"),
            (ex) =>
            {
                DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace);
            });

        yield return new WaitForSeconds(LevelTutorial.Instance.VoicePickupWeapon.length);

        LevelTutorial.Instance.NPC.GetComponent<Animation>().Blend(AnimationIndex.TutorialGuideTalk.ToString(), 0);
        LevelTutorial.Instance.NPC.GetComponent<Animation>().Blend(AnimationIndex.TutorialGuideIdle.ToString(), 1);

        InputManager.Instance.IsInputEnabled = true;

        GameState.LocalPlayer.IsWalkingEnabled = true;
        GameState.LocalPlayer.SetPlayerControlState(LocalPlayer.PlayerState.FirstPerson);

        yield return new WaitForSeconds(0.5f);

        LevelCamera.Instance.CanDip = true;
    }

    public void Reset()
    {
        _entered = false;
    }
}
