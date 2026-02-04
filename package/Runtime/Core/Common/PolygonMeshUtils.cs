using System.Collections.Generic;
using UnityEngine;

namespace HanokBuildingSystem
{
    public static class PolygonMeshUtils
    {
        public static bool IsClockwiseXZ(IReadOnlyList<Vector3> points)
        {
            float sum = 0f;

            for (int i = 0; i < points.Count; i++)
            {
                Vector3 a = points[i];
                Vector3 b = points[(i + 1) % points.Count];

                sum += (b.x - a.x) * (b.z + a.z);
            }

            // sum > 0  → clockwise
            // sum < 0  → counter-clockwise
            return sum > 0f;
        }

        public static void EnsureCounterClockwiseXZ(List<Vector3> points)
        {
            if (IsClockwiseXZ(points))
                points.Reverse();
        }

        public static void EnsureClockwiseXZ(List<Vector3> points)
        {
            if (!IsClockwiseXZ(points))
                points.Reverse();
        }

        public static Mesh CreateMeshFromVertics(
        List<List<Vector3>> lineList,
        Matrix4x4 worldToLocal)
        {
            if (lineList == null || lineList.Count < 3)
                return null;

            List<Vector3> points = new();
            foreach (var line in lineList)
            {
                if (line != null && line.Count > 0)
                    points.Add(line[0]);
            }

            if (points.Count < 3)
                return null;

            // CW로 통일 (Unity XZ 평면에서 법선이 Y+ 위쪽을 향하도록)
            if (!IsClockwiseXZ(points))
                points.Reverse();

            Mesh mesh = new Mesh { name = "BoundaryMesh" };

            Vector3[] vertices = new Vector3[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                vertices[i] = worldToLocal.MultiplyPoint3x4(points[i]);
            }

            int[] triangles = new int[(points.Count - 2) * 3];
            for (int i = 0; i < points.Count - 2; i++)
            {
                triangles[i * 3]     = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

    }   
}
