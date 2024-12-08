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
}
