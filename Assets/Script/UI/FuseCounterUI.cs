using UnityEngine;
using TMPro;
using UnityEngine.UI;
using ARKOM.Core;

namespace ARKOM.UI
{
    [AddComponentMenu("UI/Fuse Counter UI")]
    public class FuseCounterUI : MonoBehaviour
    {
        [Header("References")]
        public TMP_Text tmpLabel;           // TextMeshPro label (preferred)
        public Text uGuiText;               // Legacy uGUI Text (optional fallback)
        public CanvasGroup group;           // Optional for hide/show

        [Header("Display")] 
        [Tooltip("รูปแบบข้อความ {0}=จำนวนที่มี, {1}=จำนวนที่ต้องการ")] public string format = "Fuses {0}/{1}";
        [Tooltip("ซ่อน UI เมื่อเก็บครบตามต้องการ")] public bool hideWhenComplete = false;

        void Awake()
        {
            if (!group) group = GetComponent<CanvasGroup>();
        }

        void OnEnable()
        {
            EventBus.Subscribe<FuseCountChangedEvent>(OnFuseChanged);
            // refresh once on enable
            Refresh(FuseInventory.Count, FuseInventory.Required);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<FuseCountChangedEvent>(OnFuseChanged);
        }

        private void OnFuseChanged(FuseCountChangedEvent e)
        {
            Refresh(e.Count, e.Required);
        }

        private void Refresh(int count, int required)
        {
            string text = string.Format(format, count, required);
            if (tmpLabel) tmpLabel.text = text;
            if (uGuiText) uGuiText.text = text;

            bool done = count >= required && required > 0;
            if (group && hideWhenComplete)
            {
                group.alpha = done ? 0f : 1f;
                group.interactable = !done;
                group.blocksRaycasts = !done;
            }
        }
    }
}
