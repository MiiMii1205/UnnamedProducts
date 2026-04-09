using UnityEngine;
using UnityEngine.Splines.ExtrusionShapes;

namespace UnnamedProducts.Extensions;

public static class VectorExt
{
    public static float SquareDistance(this Vector3 a, Vector3 b)
    {
        var num1 = a.x - b.x;
        var num2 = a.y - b.y;
        var num3 = a.z - b.z;
        return num1 *  num1 + num2 * num2 +
                                 num3 * num3;
    }
    public static bool CompareDistanceFast(this Vector3 a, Vector3 b, float distance)
    {
        return a.SquareDistance(b) <= (distance * distance);
    }
}