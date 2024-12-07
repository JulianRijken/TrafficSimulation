using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using math = Unity.Mathematics.math;


public class MathExtentions : MonoBehaviour
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
}
