using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LightFlicker : MonoBehaviour
{
    public Light2D light2D;
    public float flickerAmount = 0.5f;
    public float speed = 2f;
    float baseIntensity;
    //provides light a gentle flickering for effect
    private void Start()
    {
        baseIntensity = light2D.intensity;
    }
    private void Update()
    {
        light2D.intensity = baseIntensity + Mathf.Sin(Time.time * speed) * flickerAmount;
    }
}
