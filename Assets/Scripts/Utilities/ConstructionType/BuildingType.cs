namespace HanokBuildingSystem
{
    /// <summary>
    /// [DEPRECATED] 건물 타입
    ///
    /// 이 enum은 더 이상 사용되지 않습니다.
    /// 대신 BuildingTypeData ScriptableObject를 사용하세요.
    ///
    /// 마이그레이션 방법:
    /// 1. Project 창에서 Create > Hanok Building System > Type Definitions > Building Type
    /// 2. 생성된 ScriptableObject에 건물 정보 입력
    /// 3. 기존 enum 대신 ScriptableObject 참조 사용
    /// </summary>
    [System.Obsolete("Use BuildingTypeData ScriptableObject instead. See Assets/Scripts/Core/TypeDefinitions/BuildingTypeData.cs")]
    public enum BuildingType
    {
        None,
        안채,
        사랑채,
        행랑채,
        대문,
        바깥담,
        바깥담_중앙,
        바깥담_코너,
        바깥담_끝,
        쪽문,
        안쪽담,
        장독대,
        뒷간,
    }
}
