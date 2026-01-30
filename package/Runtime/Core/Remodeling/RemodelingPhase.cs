namespace HanokBuildingSystem
{
    /// <summary>
    /// 리모델링 시스템의 현재 단계
    /// </summary>
    public enum RemodelingPhase
    {
        Idle,               // 리모델링 비활성
        Active,             // 리모델링 활성 (빌딩 미선택)
        Moving,             // 드래그 중
        Adding,             // 새 빌딩
        Erasing             // 빌딩 제거
    }

    /// <summary>
    /// 충돌 시 반응 타입
    /// </summary>
    public enum CollisionResponseType
    {
        None,             // 충돌해도 반응 없음
        ResetPosition,    // 충돌하면 원래 자리로 되돌림
        SwapTarget,       // 충돌체가 Building 인 경우 바꿔들음. 아니면 반응 없음.
    }

    /// <summary>
    /// 배치가 유효하지 않은 이유
    /// </summary>
    [System.Flags]
    public enum PlacementInvalidReason
    {
        None = 0,
        OutOfBounds = 1 << 0,      // 하우스 영역 이탈
        Collision = 1 << 1,         // 다른 오브젝트와 충돌
        CustomRule = 1 << 2,        // 커스텀 룰 위반
    }
}
