using UnityEngine;
using TrafficSimulation;

public class Deactivate : MonoBehaviour
{
    bool isActive = true;

    GameObject[] vehicles;
    TrafficSystem ts;

    private void Start()
    {
        vehicles = GameObject.FindGameObjectsWithTag("AutonomousVehicle");
        ts = GameObject.FindObjectOfType<TrafficSystem>();
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space)){
            if(isActive)
            {
                isActive = false;
                ts.SaveTrafficSystem();
                foreach(GameObject vehicle in vehicles){
                    vehicle.SetActive(false);
                }
            }
            else
            {
                isActive = true;

                foreach(GameObject vehicle in vehicles){
                    vehicle.SetActive(true);
                    
                }
                
                ts.ResumeTrafficSystem();
            }
        }
    }
}
