using System.Collections.Generic;
using UnityEngine;

public static class EvidenceRegistry
{
    private static readonly List<EvidencePickup> list = new();

    public static void Register(EvidencePickup p)
    {
        if (p && !list.Contains(p)) list.Add(p);
    }

    public static void Unregister(EvidencePickup p)
    {
        if (p) list.Remove(p);
    }

    public static void ResetAll(bool clearFlags)
    {
        foreach (var p in list)
            if (p) p.ResetEvidence(clearFlags);
    }
}