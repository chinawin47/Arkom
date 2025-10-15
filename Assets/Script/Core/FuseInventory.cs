using System.Collections.Generic;
using UnityEngine;
using ARKOM.Story;

namespace ARKOM.Core
{
    /// <summary>
    /// Global fuse store that tracks both count and the origin (location) of each fuse in pickup order.
    /// </summary>
    public static class FuseInventory
    {
        public static int Required { get; private set; } = 3;
        public static int Count => _fuses.Count;

        // store fuse origins in pickup order (FIFO)
        private static readonly Queue<FuseLocation> _fuses = new Queue<FuseLocation>(8);

        public static void ConfigureRequired(int required)
        {
            Required = Mathf.Max(0, required);
            EventBus.Publish(new FuseCountChangedEvent(Count, Required));
        }

        public static void Reset(int? required = null)
        {
            if (required.HasValue) Required = Mathf.Max(0, required.Value);
            _fuses.Clear();
            EventBus.Publish(new FuseCountChangedEvent(Count, Required));
        }

        // legacy add: add anonymous fuses (defaults to Outside)
        public static int Add(int amount = 1)
        {
            Add(FuseLocation.Outside, amount);
            return Count;
        }

        public static int Add(FuseLocation location, int amount = 1)
        {
            int a = Mathf.Max(1, amount);
            for (int i = 0; i < a; i++) _fuses.Enqueue(location);
            EventBus.Publish(new FuseCountChangedEvent(Count, Required));
            return Count;
        }

        // legacy remove by amount (consumes and discards locations)
        public static int Remove(int amount = 1)
        {
            int a = Mathf.Max(1, amount);
            for (int i = 0; i < a && _fuses.Count > 0; i++) _fuses.Dequeue();
            EventBus.Publish(new FuseCountChangedEvent(Count, Required));
            return Count;
        }

        public static bool RemoveOne(out FuseLocation location)
        {
            if (_fuses.Count > 0)
            {
                location = _fuses.Dequeue();
                EventBus.Publish(new FuseCountChangedEvent(Count, Required));
                return true;
            }
            location = default;
            return false;
        }

        public static bool HasEnough => Count >= Required;
    }

    public readonly struct FuseCountChangedEvent
    {
        public readonly int Count;
        public readonly int Required;
        public FuseCountChangedEvent(int count, int required)
        {
            Count = count;
            Required = required;
        }
    }
}
