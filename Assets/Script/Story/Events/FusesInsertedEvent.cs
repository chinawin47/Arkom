namespace ARKOM.Story
{
    public readonly struct FusesInsertedEvent
    {
        public readonly int Inserted;
        public readonly int Required;
        public FusesInsertedEvent(int inserted, int required)
        {
            Inserted = inserted;
            Required = required;
        }
    }
}
