using ARKOM.Story;

namespace ARKOM.Story
{
    // Event published every time SequenceController changes StoryState
    public struct StoryStateChangedEvent
    {
        public SequenceController.StoryState Previous;
        public SequenceController.StoryState Current;
        public StoryStateChangedEvent(SequenceController.StoryState previous, SequenceController.StoryState current)
        {
            Previous = previous;
            Current = current;
        }
    }
}
