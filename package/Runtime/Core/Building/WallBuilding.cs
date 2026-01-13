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

        private WallGenerator wallGenerator;
        private BuildingMemberCatalog memberCatalog;

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
            base.ShowModelBuilding(plot, transform);

            if(wallGenerator == null)
            {
                wallGenerator = FindFirstObjectByType<WallGenerator>();
            }

            if(wallGenerator == null)
            {
                Debug.LogWarning($"Null exception [WallGenerator]");
                return;
            }

            // WallGenerator가 기존 벽을 재사용하고, 필요한 만큼 추가/제거함
            Walls = wallGenerator.GenerateWallsForPlot(plot, this);

            if (Body != null)
            {
                Body.SetActive(false);
            }
        }

        /// <summary>
        /// WallBuilding이 비활성화될 때 벽들을 정리
        /// </summary>
        private void OnDisable()
        {
            // Walls를 풀로 반환
            if (Walls != null && Walls.Count > 0)
            {
                if (memberCatalog == null)
                {
                    memberCatalog = HanokBuildingSystem.Instance?.BuildingMemberCatalog;
                }

                if (memberCatalog != null)
                {
                    for (int i = Walls.Count - 1; i >= 0; i--)
                    {
                        GameObject wall = Walls[i];
                        if (wall != null)
                        {
                            RemoveBuildingMember(wall);
                            memberCatalog.ReturnMember(wall);
                        }
                    }
                }

                Walls.Clear();
            }
        }
    }
}
