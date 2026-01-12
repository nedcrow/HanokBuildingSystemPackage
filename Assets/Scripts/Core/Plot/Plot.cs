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

        public List<List<Vector3>> LineList => lineList;

        public List<Vector3> AllVertices => allVertices;

        public Plot()
        {
            lineList = new List<List<Vector3>>();
            allVertices = new List<Vector3>();
        }

        public Plot(List<List<Vector3>> verticesList)
        {
            this.lineList = new List<List<Vector3>>();
            this.allVertices = new List<Vector3>();
            foreach (var line in verticesList)
            {
                this.lineList.Add(new List<Vector3>(line));
            }
            UpdateAllVertices();
        }

        public void AddLine(List<Vector3> line)
        {
            if (lineList == null)
            {
                lineList = new List<List<Vector3>>();
            }
            lineList.Add(new List<Vector3>(line));
            UpdateAllVertices();
        }

        public void AddVertexToLine(int lineIndex, Vector3 vertex)
        {
            if (lineIndex >= 0 && lineIndex < lineList.Count)
            {
                lineList[lineIndex].Add(vertex);
                UpdateAllVertices();
            }
        }

        public bool RemoveLine(int lineIndex)
        {
            if (lineIndex >= 0 && lineIndex < lineList.Count)
            {
                lineList.RemoveAt(lineIndex);
                UpdateAllVertices();
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
                    UpdateAllVertices();
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
                    UpdateAllVertices();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 라인의 모든 정점을 새로운 리스트로 교체
        /// </summary>
        public bool UpdateVertices(int lineIndex, List<Vector3> newVertices)
        {
            if (lineIndex >= 0 && lineIndex < lineList.Count && newVertices != null)
            {
                lineList[lineIndex] = new List<Vector3>(newVertices);
                UpdateAllVertices();
                return true;
            }
            return false;
        }

        public void Clear()
        {
            lineList.Clear();
            allVertices.Clear();
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
