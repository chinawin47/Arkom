using System.Collections.Generic;
using UnityEngine;

namespace ARKOM.Core
{
    /// <summary>
    /// Simple global keyring for quest keys.
    /// </summary>
    public static class Keyring
    {
        private static readonly HashSet<string> keys = new HashSet<string>();

        public static bool Add(string keyId)
        {
            if (string.IsNullOrEmpty(keyId)) return false;
            bool added = keys.Add(keyId);
            if (added)
            {
                EventBus.Publish(new ARKOM.Story.KeyPickedEvent(keyId));
            }
            return added;
        }

        public static bool Has(string keyId)
        {
            if (string.IsNullOrEmpty(keyId)) return false;
            return keys.Contains(keyId);
        }

        public static void Reset()
        {
            keys.Clear();
        }
    }
}
