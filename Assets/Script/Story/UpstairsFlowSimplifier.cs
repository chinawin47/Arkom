using System.Collections;
using UnityEngine;
using ARKOM.Core;

namespace ARKOM.Story
{
 [AddComponentMenu("Story/Upstairs Flow Simplifier")]
 public class UpstairsFlowSimplifier : MonoBehaviour
 {
 [Header("Simplify Options")]
 public bool autoUnlockOnInvestigateUpstairs = true;
 public bool autoStartRadioOnUnlock = true;
 [Tooltip("ตั้ง false เพื่อให้ต้องอ่านไดอารี่ก่อน (ค่าเริ่ม true = ข้ามขั้นตอนอ่านไดอารี่)")]
 public bool forceSkipDiaryRequirement = true;
 [Tooltip("ตั้ง >0 เพิ่อให้วิทยุหยุดเองหลังดีเลย์ (จะกระตุ้นให้ไปขั้นถัดไป)")]
 public float autoStopRadioDelay =0f;

 [Header("References (Optional)")]
 public RadioInteractable upstairsRadio;

 private void OnEnable()
 {
 EventBus.Subscribe<StoryStateChangedEvent>(OnStoryStateChanged);
 EventBus.Subscribe<UpstairsDoorUnlockedEvent>(OnUpstairsUnlocked);
 }

 private void OnDisable()
 {
 EventBus.Unsubscribe<StoryStateChangedEvent>(OnStoryStateChanged);
 EventBus.Unsubscribe<UpstairsDoorUnlockedEvent>(OnUpstairsUnlocked);
 }

 private void Start()
 {
 if (forceSkipDiaryRequirement && SequenceController.Instance)
 {
 SequenceController.Instance.requireDiaryBeforeOoy = false;
 }
 }

 private void OnStoryStateChanged(StoryStateChangedEvent e)
 {
 if (e.Current == SequenceController.StoryState.InvestigateUpstairs)
 {
 if (autoUnlockOnInvestigateUpstairs)
 {
 EventBus.Publish(new UpstairsDoorUnlockedEvent());
 }

 if (autoStartRadioOnUnlock)
 {
 TryStartRadio();
 }
 }
 }

 private void OnUpstairsUnlocked(UpstairsDoorUnlockedEvent _)
 {
 if (autoStartRadioOnUnlock)
 {
 TryStartRadio();
 }
 }

 private void TryStartRadio()
 {
 if (!upstairsRadio)
 {
 upstairsRadio = FindObjectOfType<RadioInteractable>();
 }
 if (upstairsRadio)
 {
 upstairsRadio.StartRadio();
 if (autoStopRadioDelay >0f)
 {
 StopAllCoroutines();
 StartCoroutine(AutoStopRadio());
 }
 }
 }

 private IEnumerator AutoStopRadio()
 {
 yield return new WaitForSeconds(autoStopRadioDelay);
 if (upstairsRadio)
 upstairsRadio.StopRadio();
 }
 }
}
