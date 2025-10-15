namespace ARKOM.Story
{
    public readonly struct KeyPickedEvent
    {
        public readonly string KeyId;
        public KeyPickedEvent(string id){ KeyId = id; }
    }
}
