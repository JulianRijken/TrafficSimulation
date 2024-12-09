using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using math = Unity.Mathematics.math;


public class MathExtensions : MonoBehaviour
{
    public static float LerpSmooth(float a, float b, float deltaTime, float duration)
    {
        // half life (2)
        float h = -duration / math.log2(1.0f / 1000.0f) ;
        return b + (a - b) *  math.exp2(-deltaTime / h);
    }
    
    public static Vector3 LerpSmooth(Vector3 a, Vector3 b, float deltaTime, float duration)
    {
        // half life (2)
        float h = -duration / math.log2(1.0f / 1000.0f) ;
        return b + (a - b) *  math.exp2(-deltaTime / h);
    }

    public static Quaternion LerpSmooth(Quaternion a, Quaternion b, float deltaTime, float duration)
    {
        // half life (2)
        float h = -duration / math.log2(1.0f / 1000.0f) ;
        return math.slerp(a, b, math.exp2(-deltaTime / h));
    }

    public static void DrawArrow(Vector3 from, Vector3 forward, float size)
    {
        forward = forward.normalized * size;
        var left = Quaternion.Euler(0, 45, 0) * forward;
        var right = Quaternion.Euler(0, -45, 0) * forward;

        Gizmos.DrawLine(from, from + forward);
        Gizmos.DrawLine(from + forward, from + left);
        Gizmos.DrawLine(from + forward, from + right);
    }
    
    public static void DrawArrowTip(Vector3 point, Vector3 forward)
    {
        forward = forward.normalized * -0.5f;
        var left = Quaternion.Euler(0, 45, 0) * forward;
        var right = Quaternion.Euler(0, -45, 0) * forward;

        Gizmos.DrawLine(point, point + left);
        Gizmos.DrawLine(point, point + right);
    }
    
    public static void DrawCircle(Vector3 center, float radius)
    {
        int segments = 32;
        float angle = 0;
        Vector3 lastPos = center + new Vector3(radius, 0, 0);
        for (int i = 0; i < segments + 1; i++)
        {
            float x = center.x + radius * Mathf.Cos(angle);
            float z = center.z + radius * Mathf.Sin(angle);
            Vector3 newPos = new Vector3(x, center.y, z);
            Gizmos.DrawLine(lastPos, newPos);
            lastPos = newPos;
            angle += 2 * Mathf.PI / segments;
        }
    }

    public static void DrawCircle(Vector3 center, Vector3 up, float radius)
    {
        int segments = 32;
        float angle = 0;
        Vector3 perpendicular = Vector3.Cross(up, Vector3.right).magnitude > 0.001f 
            ? Vector3.Cross(up, Vector3.right).normalized 
            : Vector3.Cross(up, Vector3.forward).normalized;
        Vector3 lastPos = center + perpendicular * radius;

        for (int i = 0; i <= segments; i++)
        {
            Quaternion rotation = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, up);
            Vector3 newPos = center + rotation * (perpendicular * radius);
            Gizmos.DrawLine(lastPos, newPos);
            lastPos = newPos;
            angle += 2 * Mathf.PI / segments;
        }
    }
    
    public static bool LineIntersect2D(Vector2 p1, Vector2 q1, Vector2 p2, Vector2 q2)
    {
        // Helper to find orientation of the triplet (p, q, r)
        int Orientation(Vector2 p, Vector2 q, Vector2 r)
        {
            float val = (q.y - p.y) * (r.x - q.x) - (q.x - p.x) * (r.y - q.y);
            if (Mathf.Abs(val) < Mathf.Epsilon) return 0; // Collinear
            return (val > 0) ? 1 : 2; // Clockwise or Counterclockwise
        }

        // Check if point q lies on segment pr
        bool OnSegment(Vector2 p, Vector2 q, Vector2 r)
        {
            return q.x <= Mathf.Max(p.x, r.x) && q.x >= Mathf.Min(p.x, r.x) &&
                   q.y <= Mathf.Max(p.y, r.y) && q.y >= Mathf.Min(p.y, r.y);
        }

        // Find orientations
        int o1 = Orientation(p1, q1, p2);
        int o2 = Orientation(p1, q1, q2);
        int o3 = Orientation(p2, q2, p1);
        int o4 = Orientation(p2, q2, q1);

        // General case
        if (o1 != o2 && o3 != o4)
            return true;

        // Special cases
        if (o1 == 0 && OnSegment(p1, p2, q1)) return true;
        if (o2 == 0 && OnSegment(p1, q2, q1)) return true;
        if (o3 == 0 && OnSegment(p2, p1, q2)) return true;
        if (o4 == 0 && OnSegment(p2, q1, q2)) return true;

        return false; // No intersection
    }
}
