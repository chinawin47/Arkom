using UnityEngine;
using System.Collections.Generic;
using ARKOM.Core;
using ARKOM.UI; // added for HintPresenter

namespace ARKOM.Story
{
    public class ProgressiveHintController : MonoBehaviour
    {
        [System.Serializable]
        public class HintTier
        {
            public float time; // ���ҷ���ҹ仵������� state
            [TextArea] public string text; // ��ͤ���
            public float showDuration = 3.5f;
        }

        [System.Serializable]
        public class StateHints
        {
            public SequenceController.StoryState state;
            public List<HintTier> tiers = new();
        }

        public HintPresenter presenter;
        public SequenceController controller;
        public List<StateHints> hintSets = new();
        public bool showOncePerTier = true;

        private float stateTimer;
        private SequenceController.StoryState lastState;
        private HashSet<string> shownKeys = new();

        void Awake()
        {
            if (!controller) controller = FindObjectOfType<SequenceController>();
        }

        void Update()
        {
            if (!controller) return;
            var cur = controller.CurrentState;
            if (cur != lastState)
            {
                stateTimer = 0f;
                lastState = cur;
            }
            else
            {
                stateTimer += Time.deltaTime;
            }

            var set = hintSets.Find(s => s.state == cur);
            if (set == null) return;

            foreach (var tier in set.tiers)
            {
                if (stateTimer >= tier.time)
                {
                    string key = cur + ":" + tier.time;
                    if (showOncePerTier && shownKeys.Contains(key)) continue;
                    // �ʴ������� key
                    presenter?.Show(tier.text, tier.showDuration);
                    shownKeys.Add(key);
                    // �ʴ� tier ���֧�������� ���ѧ�Դ�͡�� tier �Ѵ�����������Թ
                }
            }
        }
    }
}
