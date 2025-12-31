using UnityEngine;

public class MobileDisableRenderer : MonoBehaviour
{

    void OnEnable()
    {
        if (ApplicationDataManager.IsMobile)
        {
            GetComponent<Renderer>().enabled = false;
        }
    }
}
