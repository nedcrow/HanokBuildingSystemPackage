using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace HanokBuildingSystem
{
    public class WallBuilding : Building
    {
        [Header("Building member for wall")]
        [SerializeField] private GameObject wallCenter;
        [SerializeField] private GameObject wallCorner;
        [SerializeField] private GameObject wallEnd;

        [Header("Settings")]
        [SerializeField] private float wallSegmentLength = 1f;
        [SerializeField] private float doorWidth = 1.5f;
        [SerializeField] private bool allowMultipleDoors = false;
        [SerializeField] private int maxDoors = 1;

        WallGenerator wallGenerator;

        public GameObject WallCenter => wallCenter;
        public GameObject WallCorner => wallCorner;
        public GameObject WallEnd => wallEnd;
        public List<GameObject> Walls;
        public float WallSegmentLength => wallSegmentLength;
        public float DoorWidth => doorWidth;
        public bool AllowMultipleDoors => allowMultipleDoors;
        public int MaxDoors => maxDoors;

        public override void ShowModelBuilding(Plot plot, Transform parent = null)
        {
            if(wallGenerator == null)
            {
                wallGenerator = FindFirstObjectByType<WallGenerator>();
            }

            if(wallGenerator == null)
            {
                Debug.LogWarning($"Null exception [WallGenerator]");
            }

            Walls = wallGenerator.GenerateWallsForPlot(plot, this);

            if (body != null)
            {
                body.SetActive(false);
            }
        }
    }
}
