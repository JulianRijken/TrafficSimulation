using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TrafficSimulation.Scripts.Editor
{
    [CustomEditor(typeof(TrafficSystem))]
    public class TrafficEditor : UnityEditor.Editor
    {
        private Vector3 _lastPoint;
        private Waypoint _lastWaypoint;

        //References for moving a waypoint
        private Vector3 _startPosition;
        private TrafficSystem _trafficSystem;

        private void OnEnable()
        {
            _trafficSystem = target as TrafficSystem;
        }

        private void OnSceneGUI()
        {
            var e = Event.current;
            if (e == null)
                return;

            var ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

            if (Physics.Raycast(ray, out var hit) && e.type == EventType.MouseDown && e.button == 0)
            {
                //Add a new waypoint on mouseclick + shift
                if (e.shift)
                {
                    if (_trafficSystem.CurSegment == null)
                        return;

                    EditorHelper.BeginUndoGroup("Add Waypoint", _trafficSystem);
                    AddWaypoint(hit.point);

                    //Close Undo Group
                    Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
                }

                //Create a segment + add a new waypoint on mouseclick + ctrl
                else if (e.control)
                {
                    EditorHelper.BeginUndoGroup("Add Segment", _trafficSystem);
                    AddSegment(hit.point);
                    AddWaypoint(hit.point);

                    //Close Undo Group
                    Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
                }

                //Create an intersection type
                else if (e.alt)
                {
                    EditorHelper.BeginUndoGroup("Add Intersection", _trafficSystem);
                    AddIntersection(hit.point);

                    //Close Undo Group
                    Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
                }
            }

            //Set waypoint system as the selected gameobject in hierarchy
            Selection.activeGameObject = _trafficSystem.gameObject;

            //Handle the selected waypoint
            if (_lastWaypoint != null)
            {
                //Uses a endless plain for the ray to hit
                var plane = new Plane(Vector3.up.normalized, _lastWaypoint.GetVisualPos());
                plane.Raycast(ray, out var dst);
                var hitPoint = ray.GetPoint(dst);

                //Reset lastPoint if the mouse button is pressed down the first time
                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    _lastPoint = hitPoint;
                    _startPosition = _lastWaypoint.transform.position;
                }

                //Move the selected waypoint
                if (e.type == EventType.MouseDrag && e.button == 0)
                {
                    var realDPos = new Vector3(hitPoint.x - _lastPoint.x, 0, hitPoint.z - _lastPoint.z);

                    _lastWaypoint.transform.position += realDPos;
                    _lastPoint = hitPoint;
                }

                //Release the selected waypoint
                if (e.type == EventType.MouseUp && e.button == 0)
                {
                    var curPos = _lastWaypoint.transform.position;
                    _lastWaypoint.transform.position = _startPosition;
                    Undo.RegisterFullObjectHierarchyUndo(_lastWaypoint, "Move Waypoint");
                    _lastWaypoint.transform.position = curPos;
                }

                //Draw a Sphere
                Handles.SphereHandleCap(0, _lastWaypoint.GetVisualPos(), Quaternion.identity,
                    _trafficSystem.WaypointSize * 2f,
                    EventType.Repaint);
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                SceneView.RepaintAll();
            }

            //Set the current hovering waypoint
            if (_lastWaypoint == null)
                _lastWaypoint = _trafficSystem.GetAllWaypoints().FirstOrDefault(i =>
                    EditorHelper.SphereHit(i.GetVisualPos(), _trafficSystem.WaypointSize, ray));

            //Update the current segment to the currently interacting one
            if (_lastWaypoint != null && e.type == EventType.MouseDown)
                _trafficSystem.CurSegment = _lastWaypoint.segment;

            //Reset current waypoint
            else if (_lastWaypoint != null && e.type == EventType.MouseMove) _lastWaypoint = null;
        }

        [MenuItem("GameObject/Traffic Simulation/Create Traffic Objects")]
        private static void CreateTraffic()
        {
            EditorHelper.SetUndoGroup("Create Traffic Objects");

            var systemGameObject = EditorHelper.CreateGameObject("Traffic System");
            systemGameObject.transform.position = Vector3.zero;
            EditorHelper.AddComponent<TrafficSystem>(systemGameObject);

            var segmentsGameObject = EditorHelper.CreateGameObject("Segments", systemGameObject.transform);
            segmentsGameObject.transform.position = Vector3.zero;

            var intersectionsGameObject = EditorHelper.CreateGameObject("Intersections", systemGameObject.transform);
            intersectionsGameObject.transform.position = Vector3.zero;

            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            //Register an Undo if changes are made after this call
            Undo.RecordObject(_trafficSystem, "Traffic Inspector Edit");

            //Draw the Inspector
            TrafficEditorInspector.DrawInspector(_trafficSystem, serializedObject, out var restructureSystem);

            //Rename waypoints if some have been deleted
            if (restructureSystem)
                RestructureSystem();


            //Repaint the scene if values have been edited
            if (EditorGUI.EndChangeCheck())
                SceneView.RepaintAll();

            serializedObject.ApplyModifiedProperties();
        }

        private void AddWaypoint(Vector3 position)
        {
            var go = EditorHelper.CreateGameObject("Waypoint-" + _trafficSystem.CurSegment.Waypoints.Count,
                _trafficSystem.CurSegment.transform);
            go.transform.position = position;

            var wp = EditorHelper.AddComponent<Waypoint>(go);
            wp.Refresh(_trafficSystem.CurSegment.Waypoints.Count, _trafficSystem.CurSegment);

            //Record changes to the TrafficSystem (string not relevant here)
            Undo.RecordObject(_trafficSystem.CurSegment, "");
            _trafficSystem.CurSegment.Waypoints.Add(wp);
        }

        private void AddSegment(Vector3 position)
        {
            var segId = _trafficSystem.Segments.Count;
            var segGo = EditorHelper.CreateGameObject("Segment-" + segId,
                _trafficSystem.transform.GetChild(0).transform);
            segGo.transform.position = position;

            _trafficSystem.CurSegment = EditorHelper.AddComponent<Segment>(segGo);
            _trafficSystem.CurSegment.Id = segId;
            _trafficSystem.CurSegment.Waypoints = new List<Waypoint>();
            _trafficSystem.CurSegment.ConnectedSegments = new List<Segment>();

            //Record changes to the TrafficSystem (string not relevant here)
            Undo.RecordObject(_trafficSystem, "");
            _trafficSystem.Segments.Add(_trafficSystem.CurSegment);
        }

        private void AddIntersection(Vector3 position)
        {
            var intId = _trafficSystem.Intersections.Count;
            var intGo =
                EditorHelper.CreateGameObject("Intersection-" + intId, _trafficSystem.transform.GetChild(1).transform);
            intGo.transform.position = position;

            var bc = EditorHelper.AddComponent<BoxCollider>(intGo);
            bc.isTrigger = true;
            var intersection = EditorHelper.AddComponent<Intersection>(intGo);
            intersection._id = intId;

            //Record changes to the TrafficSystem (string not relevant here)
            Undo.RecordObject(_trafficSystem, "");
            _trafficSystem.Intersections.Add(intersection);
        }

        private void RestructureSystem()
        {
            //Rename and restructure segments and waypoints
            var nSegments = new List<Segment>();
            var itSeg = 0;
            foreach (Transform tS in _trafficSystem.transform.GetChild(0).transform)
            {
                var segment = tS.GetComponent<Segment>();
                if (segment != null)
                {
                    var nWaypoints = new List<Waypoint>();
                    segment.Id = itSeg;
                    segment.gameObject.name = "Segment-" + itSeg;

                    var itWp = 0;
                    foreach (Transform tW in segment.gameObject.transform)
                    {
                        var waypoint = tW.GetComponent<Waypoint>();
                        if (waypoint != null)
                        {
                            waypoint.Refresh(itWp, segment);
                            nWaypoints.Add(waypoint);
                            itWp++;
                        }
                    }

                    segment.Waypoints = nWaypoints;
                    nSegments.Add(segment);
                    itSeg++;
                }
            }

            //Check if next segments still exist
            foreach (var segment in nSegments)
            {
                var nNextSegments = new List<Segment>();
                foreach (var nextSeg in segment.ConnectedSegments)
                {
                    if (nextSeg != null)
                        nNextSegments.Add(nextSeg);
                }

                segment.ConnectedSegments = nNextSegments;
            }

            _trafficSystem.Segments = nSegments;

            //Check intersections
            var nIntersections = new List<Intersection>();
            var itInter = 0;
            foreach (Transform tI in _trafficSystem.transform.GetChild(1).transform)
            {
                var intersection = tI.GetComponent<Intersection>();
                if (intersection != null)
                {
                    intersection._id = itInter;
                    intersection.gameObject.name = "Intersection-" + itInter;
                    nIntersections.Add(intersection);
                    itInter++;
                }
            }

            _trafficSystem.Intersections = nIntersections;

            //Tell Unity that something changed and the scene has to be saved
            if (!EditorUtility.IsDirty(target)) EditorUtility.SetDirty(target);

            Debug.Log("[Traffic Simulation] Successfully rebuilt the traffic system.");
        }
    }
}