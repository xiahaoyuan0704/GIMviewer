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
        var scale = GetS(trs);

        if (Mathf.Abs(scale.x) < 1e-6f || Mathf.Abs(scale.y) < 1e-6f || Mathf.Abs(scale.z) < 1e-6f)
        {
            return Quaternion.identity;
        }

        right /= scale.x;
        up /= scale.y;
        forward /= scale.z;

        Vector3.OrthoNormalize(ref forward, ref up, ref right);
        return Quaternion.LookRotation(forward, up);
    }

    public static Vector3 GetS(this Matrix4x4 trs)
    {
        Vector3 right = new Vector3(trs.m00, trs.m10, trs.m20);
        Vector3 up = new Vector3(trs.m01, trs.m11, trs.m21);
        Vector3 forward = new Vector3(trs.m02, trs.m12, trs.m22);

        var sx = right.magnitude;
        var sy = up.magnitude;
        var sz = forward.magnitude;

        var signX = Mathf.Sign(Vector3.Dot(Vector3.Cross(up, forward), right));
        var signY = Mathf.Sign(Vector3.Dot(Vector3.Cross(forward, right), up));
        var signZ = Mathf.Sign(Vector3.Dot(Vector3.Cross(right, up), forward));

        if (signX == 0) signX = 1;
        if (signY == 0) signY = 1;
        if (signZ == 0) signZ = 1;

        return new Vector3(sx * signX, sy * signY, sz * signZ);
    }
}
