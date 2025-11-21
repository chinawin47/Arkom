using System;
using System.Collections.Generic;
using ARKOM.Anomalies.Runtime;

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
                var snapshot = list.ToArray();
                for (int i = 0; i < snapshot.Length; i++)
                    ((Action<T>)snapshot[i]).Invoke(evt);
            }
        }
    }

    public readonly struct AnomalyResolvedEvent { public readonly string Id; public readonly Anomaly Source; public AnomalyResolvedEvent(string id){ Id=id; Source=null; } public AnomalyResolvedEvent(string id, Anomaly source){ Id=id; Source=source; } }
    public readonly struct NightCompletedEvent { }
    public readonly struct LoudNoiseDetectedEvent { public readonly float Level; public LoudNoiseDetectedEvent(float level)=>Level=level; }
    public readonly struct GameStateChangedEvent { public readonly GameState State; public GameStateChangedEvent(GameState s)=>State=s; }
    public readonly struct QTEResultEvent { public readonly bool Success; public QTEResultEvent(bool success)=>Success=success; }
    public readonly struct AnomalyProgressEvent { public readonly int Resolved; public readonly int Total; public AnomalyProgressEvent(int r,int t){Resolved=r;Total=t;} }
    public readonly struct AnomalySpawnBatchEvent { public readonly int Spawned; public readonly int Active; public readonly int Target; public AnomalySpawnBatchEvent(int s,int a,int t){Spawned=s;Active=a;Target=t;} }
    public readonly struct VictoryEvent { }
    public readonly struct QTEFailGameOverEvent { public readonly string PointId; public QTEFailGameOverEvent(string pointId) { PointId = pointId; } }

    public readonly struct PlayerVoiceRequestEvent { public readonly string LineId; public readonly int? ForcePriority; public PlayerVoiceRequestEvent(string id, int? p=null){ LineId=id; ForcePriority=p; } }
    public readonly struct PlayerVoicePlayedEvent { public readonly string LineId; public PlayerVoicePlayedEvent(string id){ LineId=id; } }
}