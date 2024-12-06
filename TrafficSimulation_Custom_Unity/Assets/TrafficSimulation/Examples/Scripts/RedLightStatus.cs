// Traffic Simulation
// https://github.com/mchrbn/unity-traffic-simulation

using TrafficSimulation.Scripts;
using UnityEngine;

public class RedLightStatus : MonoBehaviour
{
    public int lightGroupId; // Belong to traffic light 1 or 2?
    public Intersection intersection;

    private Light pointLight;

    private void Start()
    {
        pointLight = transform.GetChild(0).GetComponent<Light>();
        SetTrafficLightColor();
    }

    // Update is called once per frame
    private void Update()
    {
        SetTrafficLightColor();
    }

    private void SetTrafficLightColor()
    {
        if (lightGroupId == intersection._currentRedLightsGroup)
            pointLight.color = new Color(1, 0, 0);
        else
            pointLight.color = new Color(0, 1, 0);
    }
}