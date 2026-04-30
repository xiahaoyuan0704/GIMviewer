using UnityEngine;

public static class MatrixEx
{
    public static Vector3 GetT(this Matrix4x4 trs)
    {
        return new Vector3(trs.m03, trs.m13, trs.m23);
    }

    public static Quaternion GetR(this Matrix4x4 trs)
    {
        Vector3 right = new Vector3(trs.m00, trs.m10, trs.m20);
        Vector3 up = new Vector3(trs.m01, trs.m11, trs.m21);
        Vector3 forward = new Vector3(trs.m02, trs.m12, trs.m22);

        if (right.sqrMagnitude < 1e-12f || up.sqrMagnitude < 1e-12f || forward.sqrMagnitude < 1e-12f)
        {
            return Quaternion.identity;
        }

        right.Normalize();
        up.Normalize();
        forward.Normalize();

        Vector3.OrthoNormalize(ref forward, ref up, ref right);
        return Quaternion.LookRotation(forward, up);
    }

    public static Vector3 GetS(this Matrix4x4 trs)
    {
        var sx = new Vector3(trs.m00, trs.m10, trs.m20).magnitude;
        var sy = new Vector3(trs.m01, trs.m11, trs.m21).magnitude;
        var sz = new Vector3(trs.m02, trs.m12, trs.m22).magnitude;
        return new Vector3(sx, sy, sz);
    }
}
