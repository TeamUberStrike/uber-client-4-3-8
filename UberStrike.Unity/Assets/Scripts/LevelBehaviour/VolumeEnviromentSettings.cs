using UnityEngine;

[RequireComponent(typeof(Collider))]
public class VolumeEnviromentSettings : MonoBehaviour
{
    public EnviromentSettings Settings;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.tag == "Player")
        {
            //set to custom values
            GameState.LocalPlayer.MoveController.SetEnviroment(Settings, this.GetComponent<Collider>().bounds);

            if (Settings.Type == EnviromentSettings.TYPE.WATER)
            {
                float v = GameState.LocalPlayer.MoveController.Velocity.y;
                if (v < -20)
                    SfxManager.Play3dAudioClip(SoundEffectType.EnvBigSplash, collider.transform.position);
                else if (v < -10)
                    SfxManager.Play3dAudioClip(SoundEffectType.EnvMediumSplash, collider.transform.position);
            }
        }
        //else if (collider.tag == "MainCamera")
        //{
        //    if (Settings.Type == EnviromentSettings.TYPE.WATER)
        //    {
        //        if (LevelFXController.Instance) LevelFXController.Instance.SetUnderwater(true);
        //    }
        //}
    }

    private void OnTriggerExit(Collider c)
    {
        if (c.tag == "Player")
        {
            //reset to base values
            GameState.LocalPlayer.MoveController.ResetEnviroment();
        }
    }
}
