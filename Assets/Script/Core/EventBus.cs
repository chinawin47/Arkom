using System;
using System.Collections.Generic;
using ARKOM.Anomalies.Runtime; // เพื่อใช้ type Anomaly ใน Event

namespace ARKOM.Core
{
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> _handlers = new();

        public static void Subscribe<T>(Action<T> handler)
        {
            var t = typeof(T);
            if (!_handlers.TryGetValue(t, out var list))
            {
                list = new List<Delegate>();
                _handlers[t] = list;
            }
            list.Add(handler);
        }

        public static void Unsubscribe<T>(Action<T> handler)
        {
            var t = typeof(T);
            if (_handlers.TryGetValue(t, out var list))
            {
                list.Remove(handler);
            }
        }

        public static void Publish<T>(T evt)
        {
            var t = typeof(T);
            if (_handlers.TryGetValue(t, out var list))
            {
                // copy to avoid modification during iteration
                var snapshot = list.ToArray();
                foreach (var d in snapshot)
                    ((Action<T>)d).Invoke(evt);
            }
        }
    }

    // เปลี่ยน: เพิ่ม Source (อ้างอิงอินสแตนซ์) เพื่อเช็คไม่ให้นับซ้ำ
    public readonly struct AnomalyResolvedEvent
    {
        public readonly string Id;
        public readonly Anomaly Source;
        public AnomalyResolvedEvent(string id, Anomaly source)
        {
            Id = id;
            Source = source;
        }
    }

    public readonly struct NightCompletedEvent { }
    public readonly struct LoudNoiseDetectedEvent { public readonly float Level; public LoudNoiseDetectedEvent(float level) => Level = level; }
    public readonly struct GameStateChangedEvent { public readonly GameState State; public GameStateChangedEvent(GameState s) => State = s; }
    public readonly struct QTEResultEvent { public readonly bool Success; public QTEResultEvent(bool success) => Success = success; }

    // ใหม่: Event ความคืบหน้า (Resolved / Total)
    public readonly struct AnomalyProgressEvent
    {
        public readonly int Resolved;
        public readonly int Total;
        public AnomalyProgressEvent(int resolved, int total)
        {
            Resolved = resolved;
            Total = total;
        }
    }
}