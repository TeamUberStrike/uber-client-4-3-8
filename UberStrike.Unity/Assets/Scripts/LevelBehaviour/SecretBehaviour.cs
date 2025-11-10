using UnityEngine;

public class SecretBehaviour : MonoBehaviour
{
    void Awake()
    {
        foreach (Door d in _doors)
        {
            foreach (SecretTrigger t in d.Trigger)
                t.SetSecretReciever(this);
        }
    }

    /// <summary>
    /// This method is called by SecretTriggers on activation.
    /// We set the timeout based on the individual trigger activation time.
    /// If all triggers are activated simultaneously the secret area will be revealed.
    /// </summary>
    /// <param name="t"></param>
    public void SetTriggerActivated(SecretTrigger trigger)
    {
        foreach (Door d in _doors)
        {
            d.CheckAllTriggers();
        }
    }

    [SerializeField]
    private Door[] _doors;

    [System.Serializable]
    public class Door
    {
        public string _description;

        [SerializeField]
        private SecretDoor _door;

        [SerializeField]
        private SecretTrigger[] _trigger;

        public SecretTrigger[] Trigger
        {
            get { return _trigger; }
        }

        public void CheckAllTriggers()
        {
            bool activate = true;
            foreach (SecretTrigger t in _trigger)
            {
                activate &= t.ActivationTimeOut > Time.time;
            }

            if (activate)
                _door.Open();
        }
    }
}
