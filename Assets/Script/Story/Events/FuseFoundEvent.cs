namespace ARKOM.Story
{
    public enum FuseLocation
    {
        Outside,
        Upstairs,
        StorageRoom
    }

    public readonly struct FuseFoundEvent
    {
        public readonly FuseLocation Location;
        public FuseFoundEvent(FuseLocation location)
        {
            Location = location;
        }
    }
}
