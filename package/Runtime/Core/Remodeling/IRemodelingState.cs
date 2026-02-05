namespace HanokBuildingSystem
{
    /// <summary>
    /// Phase 내부의 세부 작업 상태 (드래그 중, 프리뷰, 확정 대기 등)
    /// </summary>
    public interface IRemodelingState
    {
        string StateName { get; }
        void OnEnter(RemodelingSystem system);
        void OnExit(RemodelingSystem system);
        void OnUpdate(RemodelingSystem system);
    }
}
