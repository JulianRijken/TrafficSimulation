using UnityEditor;
using UnityEngine;

namespace TrafficSimulation.Scripts.Editor
{
    public class VehicleEditor : UnityEditor.Editor
    {
        [MenuItem("GameObject/Traffic Simulation/Setup Vehicle")]
        private static void SetupVehicle(){
            EditorHelper.SetUndoGroup("Setup Vehicle");

            GameObject selected = Selection.activeGameObject;

            //Create raycast anchor
            GameObject anchor = EditorHelper.CreateGameObject("Raycast Anchor", selected.transform);

            //Add AI scripts
            VehicleAI veAi = EditorHelper.AddComponent<VehicleAI>(selected);
            WheelDrive wheelDrive = EditorHelper.AddComponent<WheelDrive>(selected);

            var trafficSystem = GameObject.FindFirstObjectByType<TrafficSystem>();

            //Configure the vehicle AI script with created objects
            anchor.transform.localPosition = Vector3.zero;
            anchor.transform.localRotation = Quaternion.Euler(Vector3.zero);
            veAi._raycastAnchor = anchor.transform;

            if(trafficSystem != null) 
                veAi._trafficSystem = trafficSystem;

            //Create layer AutonomousVehicle if it doesn't exist
            EditorHelper.CreateLayer("AutonomousVehicle");
            
            //Set the tag and layer name
            selected.tag = "AutonomousVehicle";
            EditorHelper.SetLayer(selected, LayerMask.NameToLayer("AutonomousVehicle"), true);
        }
    }
}