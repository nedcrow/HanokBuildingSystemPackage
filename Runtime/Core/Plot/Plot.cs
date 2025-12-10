using System;
using System.Collections.Generic;
using UnityEngine;

namespace HanokBuildingSystem
{
    [Serializable]
    public class Plot
    {
        [SerializeField] private List<List<Vector3>> lineList;
        [SerializeField] private List<Vector3> allVertices;
        [SerializeField] private float defaultLineThickness;
        [SerializeField] private float defaultMinAngle;
        [SerializeField] private float defaultMaxAngle;
        [SerializeField] private bool isBuildable;

        public List<List<Vector3>> LineList => lineList;

        public List<Vector3> AllVertices => allVertices;

        public float LineThickness { get => defaultLineThickness; set => defaultLineThickness = value; }
        public float MinAngle { get => defaultMinAngle; set => defaultMinAngle = value; }
        public float MaxAngle { get => defaultMaxAngle; set => defaultMaxAngle = value; }
        public bool IsBuildable { get => isBuildable; set => isBuildable = value; }

        public Plot()
        {
            lineList = new List<List<Vector3>>();
            allVertices = new List<Vector3>();
            defaultLineThickness = 0.1f;
            defaultMinAngle = 45f;
            defaultMaxAngle = 135f;
            isBuildable = false;
        }

        public Plot(List<List<Vector3>> verticesList, float lineThickness = 0.1f, float minAngle = 45f, float maxAngle = 135f)
        {
            this.lineList = new List<List<Vector3>>();
            this.allVertices = new List<Vector3>();
            foreach (var line in verticesList)
            {
                this.lineList.Add(new List<Vector3>(line));
            }
            this.defaultLineThickness = lineThickness;
            this.defaultMinAngle = minAngle;
            this.defaultMaxAngle = maxAngle;
            this.isBuildable = false;
            UpdateAllVertices();
        }

        public void AddLine(List<Vector3> line)
        {
            if (lineList == null)
            {
                lineList = new List<List<Vector3>>();
            }
            lineList.Add(new List<Vector3>(line));
            UpdateBuildableState();
        }

        public void AddVertexToLine(int lineIndex, Vector3 vertex)
        {
            if (lineIndex >= 0 && lineIndex < lineList.Count)
            {
                lineList[lineIndex].Add(vertex);
                UpdateBuildableState();
            }
        }

        public bool RemoveLine(int lineIndex)
        {
            if (lineIndex >= 0 && lineIndex < lineList.Count)
            {
                lineList.RemoveAt(lineIndex);
                UpdateBuildableState();
                return true;
            }
            return false;
        }

        public bool RemoveVertexFromLine(int lineIndex, int vertexIndex)
        {
            if (lineIndex >= 0 && lineIndex < lineList.Count)
            {
                var line = lineList[lineIndex];
                if (vertexIndex >= 0 && vertexIndex < line.Count)
                {
                    line.RemoveAt(vertexIndex);
                    UpdateBuildableState();
                    return true;
                }
            }
            return false;
        }

        public bool UpdateVertex(int lineIndex, int vertexIndex, Vector3 newPosition)
        {
            if (lineIndex >= 0 && lineIndex < lineList.Count)
            {
                var line = lineList[lineIndex];
                if (vertexIndex >= 0 && vertexIndex < line.Count)
                {
                    line[vertexIndex] = newPosition;
                    UpdateBuildableState();
                    return true;
                }
            }
            return false;
        }

        public void Clear()
        {
            lineList.Clear();
            isBuildable = false;
        }

        public int GetLineCount()
        {
            return lineList != null ? lineList.Count : 0;
        }

        public int GetVertexCount(int lineIndex)
        {
            if (lineIndex >= 0 && lineIndex < lineList.Count)
            {
                return lineList[lineIndex].Count;
            }
            return 0;
        }

        public List<Vector3> GetLine(int lineIndex)
        {
            if (lineIndex >= 0 && lineIndex < lineList.Count)
            {
                return lineList[lineIndex];
            }
            return null;
        }

        public Vector3 GetVertex(int lineIndex, int vertexIndex)
        {
            if (lineIndex >= 0 && lineIndex < lineList.Count)
            {
                var line = lineList[lineIndex];
                if (vertexIndex >= 0 && vertexIndex < line.Count)
                {
                    return line[vertexIndex];
                }
            }
            return Vector3.zero;
        }

        private void UpdateBuildableState()
        {
            UpdateAllVertices();
            isBuildable = GetTotalVertexCount() >= 4 && ValidateAngles();
        }

        private void UpdateAllVertices()
        {
            allVertices.Clear();
            foreach (var line in lineList)
            {
                foreach (var vertex in line)
                {
                    allVertices.Add(vertex);
                }
            }
        }

        private int GetTotalVertexCount()
        {
            int count = 0;
            foreach (var line in lineList)
            {
                count += line.Count;
            }
            return count;
        }

        private bool ValidateAngles()
        {
            foreach (var line in lineList)
            {
                if (line.Count < 3) continue;

                for (int i = 0; i < line.Count; i++)
                {
                    int prevIndex = (i - 1 + line.Count) % line.Count;
                    int nextIndex = (i + 1) % line.Count;

                    Vector3 v1 = line[prevIndex] - line[i];
                    Vector3 v2 = line[nextIndex] - line[i];

                    float angle = Vector3.Angle(v1, v2);

                    if (angle < defaultMinAngle || angle > defaultMaxAngle)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public float GetArea()
        {
            float totalArea = 0f;

            foreach (var line in lineList)
            {
                if (line.Count < 3) continue;

                float area = 0f;
                for (int i = 0; i < line.Count; i++)
                {
                    int nextIndex = (i + 1) % line.Count;
                    area += line[i].x * line[nextIndex].z - line[nextIndex].x * line[i].z;
                }
                totalArea += Mathf.Abs(area) * 0.5f;
            }

            return totalArea;
        }

        public Vector3 GetCenter()
        {
            int totalCount = 0;
            Vector3 center = Vector3.zero;

            foreach (var line in lineList)
            {
                foreach (var vertex in line)
                {
                    center += vertex;
                    totalCount++;
                }
            }

            return totalCount > 0 ? center / totalCount : Vector3.zero;
        }
    }
}
