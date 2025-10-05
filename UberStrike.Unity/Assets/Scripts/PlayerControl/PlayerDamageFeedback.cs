using UnityEngine;
using System.Collections;

public class PlayerDamageFeedback : MonoBehaviour
{
    private Material damageSplat;
    public Color[] DamageColors;
    public float Factor;
    private int colorIndex;

    // Use this for initialization
    void Awake()
    {
        damageSplat = renderer.material;
    }

    public void RandomizeDamageFeedbackcolor()
    {
        colorIndex = Random.Range(0, 5);
    }

    public void ShowDamageFeedback(float damage)
    {
        DamageColors[colorIndex].a = damage * Factor;
        damageSplat.color = DamageColors[colorIndex];
    }
}
