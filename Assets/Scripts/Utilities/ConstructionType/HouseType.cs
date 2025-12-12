namespace HanokBuildingSystem
{
    /// <summary>
    /// [DEPRECATED] 주택 타입
    ///
    /// 이 enum은 더 이상 사용되지 않습니다.
    /// 대신 HouseTypeData ScriptableObject를 사용하세요.
    ///
    /// 마이그레이션 방법:
    /// 1. Project 창에서 Create > Hanok Building System > Type Definitions > House Type
    /// 2. 생성된 ScriptableObject에 주택 타입 정보 입력
    /// 3. 기존 enum 대신 ScriptableObject 참조 사용
    /// </summary>
    [System.Obsolete("Use HouseTypeData ScriptableObject instead. See Assets/Scripts/Core/TypeDefinitions/HouseTypeData.cs")]
    public enum HouseType
    {
        None = 0,
        Housing = 1,
        ResourceProduction = 2,
        Processing = 3,
        PublicServices = 4,
    }
}
