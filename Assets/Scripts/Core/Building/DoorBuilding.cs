using System.Collections.Generic;
using UnityEngine;

namespace HanokBuildingSystem
{
    public enum DoorType
    {
        SwingDoor,
        SlidingDoor,
        FoldingDoor,
        RevolvingDoor
    }
    public class DoorBuilding : Building
    {
        [Header("Building member for Door")]
        [SerializeField] private List<GameObject> doors;

        [Header("Settings")]
        [SerializeField] private DoorType doortype = DoorType.SwingDoor;


        public List<GameObject> Doors => doors;
        public DoorType Ddoortype => doortype;

        public override void ShowModelBuilding(Plot plot, Transform parent = null)
        {
            // 완성 단계의 모습
        }
    }
}
