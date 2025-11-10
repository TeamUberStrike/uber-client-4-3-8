using UnityEngine;

public enum EaseType
{
    None = 0,
    In,
    Out,
    InOut,
    Berp
};

//Advanced Math class
public static class Mathfx
{
    public const float PI = 3.14159f;
    public const float FOUR_PI = PI * 4;
    public const float TWO_PI = PI * 2;
    public const float PI_HALF = PI * 0.5f;
    public const float PI_FOURTH = PI * 0.25f;

    public static float NormAmplitude(float a)
    {
        return (a + 1) * 0.5f;
    }

    public static float Hermite(float start, float end, float value)
    {
        return Mathf.Lerp(start, end, value * value * (3.0f - 2.0f * value));
    }

    public static float Gauss(float start, float end, float value)
    {
        return Mathf.Lerp(start, end, (1 + Mathf.Cos(value - Mathf.PI)) / 2f);
    }

    public static float Sinerp(float start, float end, float value)
    {
        return Mathf.Lerp(start, end, Mathf.Sin(value * Mathf.PI * 0.5f));
    }

    public static float Berp(float start, float end, float value)
    {
        value = Mathf.Clamp01(value);
        value = (Mathf.Sin(value * Mathf.PI * (0.2f + 2.5f * value * value * value)) * Mathf.Pow(1f - value, 2.2f) + value) * (1f + (1.2f * (1f - value)));
        return start + (end - start) * value;
    }

    public static float SmoothStep(float x, float min, float max)
    {
        x = Mathf.Clamp(x, min, max);
        float v1 = (x - min) / (max - min);
        float v2 = (x - min) / (max - min);
        return -2 * v1 * v1 * v1 + 3 * v2 * v2;
    }

    public static float Lerp(float start, float end, float value)
    {
        return ((1.0f - value) * start) + (value * end);
    }

    public static Vector3 NearestPoint(Vector3 lineStart, Vector3 lineEnd, Vector3 point)
    {
        Vector3 lineDirection = Vector3.Normalize(lineEnd - lineStart);
        float closestPoint = Vector3.Dot((point - lineStart), lineDirection) / Vector3.Dot(lineDirection, lineDirection);
        return lineStart + (closestPoint * lineDirection);
    }

    public static Vector3 NearestPointStrict(Vector3 lineStart, Vector3 lineEnd, Vector3 point)
    {
        Vector3 fullDirection = lineEnd - lineStart;
        Vector3 lineDirection = Vector3.Normalize(fullDirection);
        float closestPoint = Vector3.Dot((point - lineStart), lineDirection) / Vector3.Dot(lineDirection, lineDirection);
        return lineStart + (Mathf.Clamp(closestPoint, 0.0f, Vector3.Magnitude(fullDirection)) * lineDirection);
    }
    public static float Bounce(float x)
    {
        return Mathf.Abs(Mathf.Sin(6.28f * (x + 1f) * (x + 1f)) * (1f - x));
    }

    // test for value that is near specified float (due to floating point inprecision)
    // all thanks to Opless for this!
    public static bool Approx(float val, float about, float range)
    {
        return ((Mathf.Abs(val - about) < range));
    }

    // test if a Vector3 is close to another Vector3 (due to floating point inprecision)
    // compares the square of the distance to the square of the range as this 
    // avoids calculating a square root which is much slower than squaring the range
    public static bool Approx(Vector3 val, Vector3 about, float range)
    {
        return ((val - about).sqrMagnitude < range * range);
    }

    private static readonly Quaternion _rotate90 = Quaternion.AngleAxis(90, Vector3.up);
    public static float ProjectedAngle(Vector3 a, Vector3 b)
    {
        float angle = Vector3.Angle(a, b);

        return Vector3.Dot(a, _rotate90 * b) < 0 ? 360 - angle : angle;
    }

    public static Vector3 ProjectVector3(Vector3 v, Vector3 normal)
    {
        //U = V - (V dot N)N 
        return v - (Vector3.Dot(v, normal)) * normal;
    }

    public static float ClampAngle(float angle, float min, float max)
    {
        //angle = angle % 360;
        //if (angle < -360F) angle += 360F;
        //else if (angle > 360F) angle -= 360F;
        return Mathf.Clamp(angle % 360, min, max);
    }

    public static int Sign(float s)
    {
        return s == 0 ? 0 : (s < 0 ? -1 : 1);
    }

    public static short Clamp(short v, short min, short max)
    {
        if (v < min) return min;
        else if (v > max) return max;
        else return v;
    }

    public static int Clamp(int v, int min, int max)
    {
        if (v < min) return min;
        else if (v > max) return max;
        else return v;
    }

    public static float Clamp(float v, float min, float max)
    {
        if (v < min) return min;
        else if (v > max) return max;
        else return v;
    }

    public static byte Clamp(byte v, byte min, byte max)
    {
        if (v < min) return min;
        else if (v > max) return max;
        else return v;
    }

    public static float Ease(float t, EaseType easeType)
    {
        switch (easeType)
        {
            case EaseType.In:
                return Mathf.Lerp(0.0f, 1.0f, 1.0f - Mathf.Cos(t * Mathf.PI * 0.5f));
            case EaseType.Out:
                return Mathf.Lerp(0.0f, 1.0f, Mathf.Sin(t * Mathf.PI * 0.5f));
            case EaseType.InOut:
                return Mathf.SmoothStep(0.0f, 1.0f, t);
            case EaseType.Berp:
                return Berp(0.0f, 1.0f, t);
            default:
                return t;
        }
    }
}