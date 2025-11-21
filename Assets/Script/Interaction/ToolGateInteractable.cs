using UnityEngine;
using ARKOM.Core;
using ARKOM.Story;

[AddComponentMenu("Interactable/Tool Gate (Requires Tool)")]
public class ToolGateInteractable : Interactable
{
    [Header("Requirement - Tool")] public string requiredToolId = "Pliers";
    [Tooltip("?????????????????????????????? (???)")] public string needToolHint = "?????????????????????";

    [Header("Requirement - Key")] public bool requiresKey = true;
    [Tooltip("ID ??????????????????????????")] public string requiredKeyId = "UpstairsDoorKey";
    [Tooltip("ID ???????????????????????? (??????)")] public string[] additionalAcceptedKeyIds;
    [Tooltip("?????????????? (??? false = ???????????????????????)")] public bool requireAllKeys = false;
    [Tooltip("?????????????????????????") ] public string needKeyHint = "?????????????????";

    [Header("Story Gate")] [Tooltip("??????????????????????????????????????????????????")]
    public bool requireStoryGate = false;
    [Tooltip("?????????????????????????????????")] public SequenceController.StoryState requiredStoryState = SequenceController.StoryState.BreakerFail;
    [Tooltip("?????????????????????????????????????") ] public string notReadyHint = "???????????????????"; public float notReadyHintDuration = 2.5f;

    [Header("Fuse Gate")] [Tooltip("?????????????????? RestorePower (???????????) ?????????????????")]
    public bool checkFusesBeforeUnlock = true;
    [Tooltip("????????????????????????????????") ] public string needFusesHint = "??????????????????"; public float fuseHintDuration = 2.5f;

    [Header("Door Control")] public DoorInteractable doorToOpen;
    [Tooltip("????????????????????????????????") ] public bool autoOpenDoorOnUnlock = true;
    [Tooltip("?????????? Forward ???????????????????") ] public bool forwardToDoorWhenUnlocked = true;

    [Header("Visual Swap")] public GameObject chainVisual; public GameObject lockedVisual; public GameObject unlockedVisual;

    [Header("Audio")] public AudioClip unlockSfx; public float sfxVolume = 1f; public AudioClip chainCutSfx; [Range(0f,1f)] public float chainCutVolume = 1f;

    private bool chainRemoved; private bool unlocked;

    void OnEnable(){ EventBus.Subscribe<KeyPickedEvent>(OnKeyPicked); EventBus.Subscribe<StoryStateChangedEvent>(OnStoryState); RefreshByStory(); }
    void OnDisable(){ EventBus.Unsubscribe<KeyPickedEvent>(OnKeyPicked); EventBus.Unsubscribe<StoryStateChangedEvent>(OnStoryState); }

    private void OnStoryState(StoryStateChangedEvent e){ RefreshByStory(); }

    private void RefreshByStory(){ if (unlocked) return; if (!requireStoryGate || IsStoryAllowed()){ if (chainRemoved){ if (!requiresKey || HasRequiredKeys()){ /* keep waiting for player interact */ } } } }

    public override bool CanInteract(object interactor){ if (oneTime && unlocked) return false; return base.CanInteract(interactor); }

    protected override void OnInteract(object interactor)
    {
        // Block all hints if still in RestorePower (????????????????????????????????) -> ??? hint ??? SequenceController ?????
        var seq = SequenceController.Instance;
        if (seq && seq.CurrentState == SequenceController.StoryState.RestorePower)
        {
            // ??? restorePowerBlockHint ??? SequenceController ????? ??????? fallback needFusesHint
            string block = !string.IsNullOrEmpty(seq.restorePowerBlockHint) ? seq.restorePowerBlockHint : needFusesHint;
            seq.ShowTempHint(block, 3f);
            return;
        }

        if (unlocked)
        {
            if (forwardToDoorWhenUnlocked && doorToOpen) doorToOpen.Interact(interactor); return;
        }

        if (requireStoryGate && !IsStoryAllowed()){ SequenceController.Instance?.ShowTempHint(notReadyHint, notReadyHintDuration); return; }
        if (checkFusesBeforeUnlock && !IsFuseGateSatisfied()){ SequenceController.Instance?.ShowTempHint(needFusesHint, fuseHintDuration); return; }

        // Phase 1: Require tool to cut chain
        if (!chainRemoved)
        {
            if (!Keyring.Has(requiredToolId)){ SequenceController.Instance?.ShowTempHint(needToolHint,2.5f); return; }
            chainRemoved = true; if (chainVisual) chainVisual.SetActive(false); if (chainCutSfx) AudioSource.PlayClipAtPoint(chainCutSfx, transform.position, chainCutVolume);
            if (!requiresKey){ OpenNow(interactor); return; }
            SequenceController.Instance?.ShowTempHint(needKeyHint,2.5f); return;
        }
        // Phase 2: Now need key(s)
        if (requiresKey){ if (!HasRequiredKeys()){ SequenceController.Instance?.ShowTempHint(needKeyHint,2.5f); return; } OpenNow(interactor); return; }
        OpenNow(interactor);
    }

    private void OnKeyPicked(KeyPickedEvent e){ if (unlocked || !requiresKey || !chainRemoved) return; if (requireStoryGate && !IsStoryAllowed()) return; if (checkFusesBeforeUnlock && !IsFuseGateSatisfied()) return; if (HasRequiredKeys()) OpenNow(null); }

    private bool IsStoryAllowed(){ var seq = SequenceController.Instance; if (!seq) return true; return (int)seq.CurrentState >= (int)requiredStoryState; }
    private bool IsFuseGateSatisfied(){ var seq = SequenceController.Instance; if (seq) return (int)seq.CurrentState >= (int)SequenceController.StoryState.InvestigateUpstairs; return FuseInventory.HasEnough; }

    private bool HasRequiredKeys(){ int countRequired=0, countOwned=0; if (!string.IsNullOrEmpty(requiredKeyId)){ countRequired++; if (Keyring.Has(requiredKeyId)) countOwned++; } if (additionalAcceptedKeyIds!=null){ foreach (var id in additionalAcceptedKeyIds){ if (string.IsNullOrEmpty(id)) continue; countRequired++; if (Keyring.Has(id)) countOwned++; } } if (countRequired==0) return true; return requireAllKeys ? countOwned==countRequired : countOwned>0; }

    private void OpenNow(object interactor)
    {
        if (unlocked) return; unlocked = true; if (unlockSfx) AudioSource.PlayClipAtPoint(unlockSfx, transform.position, sfxVolume); if (lockedVisual) lockedVisual.SetActive(false); if (unlockedVisual) unlockedVisual.SetActive(true); EventBus.Publish(new UpstairsDoorUnlockedEvent());
        if (autoOpenDoorOnUnlock && doorToOpen){ if (!doorToOpen.isOpen) doorToOpen.ToggleDoor(); }
        if (forwardToDoorWhenUnlocked && doorToOpen && interactor != null){ doorToOpen.Interact(interactor); }
    }
}
