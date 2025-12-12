namespace HanokBuildingSystem
{
    /// <summary>
    /// [DEPRECATED] 건축에 사용하고 싶은 자원 타입
    ///
    /// 이 enum은 더 이상 사용되지 않습니다.
    /// 대신 ResourceTypeData ScriptableObject를 사용하세요.
    ///
    /// 마이그레이션 방법:
    /// 1. Project 창에서 Create > Hanok Building System > Type Definitions > Resource Type
    /// 2. 생성된 ScriptableObject에 자원 정보 입력
    /// 3. 기존 enum 대신 ScriptableObject 참조 사용
    /// </summary>
    [System.Obsolete("Use ResourceTypeData ScriptableObject instead. See Assets/Scripts/Core/TypeDefinitions/ResourceTypeData.cs")]
    public enum ResourceType
    {
        None,
        Wood,
        Stone,
        Mud,
        GI_WA,
        HANG_A_RI
    }
}
