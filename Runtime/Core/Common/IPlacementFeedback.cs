namespace HanokBuildingSystem
{
    public enum PlacementVisualState
    {
        None,
        Selected,
        Valid,
        Invalid
    }

    public interface IPlacementFeedback
    {
        void SetPlacementState(PlacementVisualState state);
        void ClearPlacementState();
    }
}
