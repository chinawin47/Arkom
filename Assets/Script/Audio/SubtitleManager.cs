using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;

namespace ARKOM.Audio
{
 [AddComponentMenu("Audio/Subtitle Manager")] public class SubtitleManager : MonoBehaviour
 {
 public static SubtitleManager Instance { get; private set; }
 [Header("UI")] public Canvas rootCanvas; public CanvasGroup canvasGroup; public TextMeshProUGUI subtitleText;
 [Range(0f,1f)] public float fadeDuration =0.2f; [Tooltip("ซ่อนหลังจบแต่ละบรรทัดหรือค้างจนคลิปจบ")] public bool hideBetweenLines = false;
 [Tooltip("สร้าง Canvas+TMP อัตโนมัติถ้าไม่ตั้งค่าใน Inspector")] public bool autoCreateIfMissing = true;

 [Header("Cue Library")] [Tooltip("ลาก VoiceCue ที่ต้องการใช้เข้ามา (เลือกเล่นด้วยดัชนีหรือตามชื่อ)")] public VoiceCue[] cueLibrary;
 [System.Serializable] public class NamedCue { public string key; public VoiceCue cue; public Transform worldPos; }
 [Tooltip("ออปชัน: ใช้คีย์เรียกเล่นคิวได้")] public NamedCue[] namedCues;
 [Header("Auto Play Sequence")] public bool autoPlayOnStart = false; [Tooltip("หน่วงก่อนเริ่ม (วินาที)")] public float autoPlayStartDelay =0f;
 [System.Serializable] public class AutoPlayEntry { public VoiceCue cue; public Transform worldPos; public float delayAfter =0.5f; }
 [Tooltip("ลำดับเล่นอัตโนมัติ (ตามลำดับ array)")] public AutoPlayEntry[] autoSequence;

 private Coroutine runRoutine; private Coroutine autoRoutine;

 void Awake()
 {
 if (Instance && Instance != this) { Destroy(gameObject); return; }
 Instance = this;
 EnsureUI();
 }

 void Start()
 {
 if (autoPlayOnStart && autoSequence != null && autoSequence.Length >0)
 {
 if (autoRoutine != null) StopCoroutine(autoRoutine);
 autoRoutine = StartCoroutine(RunAutoSequence());
 }
 }

 private void EnsureUI()
 {
 if (!rootCanvas)
 {
 rootCanvas = GetComponentInParent<Canvas>();
 if (!rootCanvas && autoCreateIfMissing)
 {
 var cgo = new GameObject("SubtitleCanvas"); cgo.transform.SetParent(transform);
 rootCanvas = cgo.AddComponent<Canvas>(); rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
 cgo.AddComponent<CanvasScaler>(); cgo.AddComponent<GraphicRaycaster>();
 }
 }
 if (!canvasGroup)
 {
 var cgGO = new GameObject("SubtitleCanvasGroup"); cgGO.transform.SetParent(rootCanvas ? rootCanvas.transform : transform);
 canvasGroup = cgGO.AddComponent<CanvasGroup>(); canvasGroup.interactable = false; canvasGroup.blocksRaycasts = false; canvasGroup.alpha =0f;
 }
 if (!subtitleText)
 {
 var txtGO = new GameObject("SubtitleText"); txtGO.transform.SetParent(canvasGroup.transform);
 subtitleText = txtGO.AddComponent<TextMeshProUGUI>(); subtitleText.text = string.Empty; subtitleText.alignment = TextAlignmentOptions.Center; subtitleText.enableWordWrapping = true; subtitleText.color = Color.white;
 if (TMP_Settings.instance && TMP_Settings.defaultFontAsset) subtitleText.font = TMP_Settings.defaultFontAsset;
 var rt = subtitleText.rectTransform; rt.anchorMin = new Vector2(0.1f,0.05f); rt.anchorMax = new Vector2(0.9f,0.2f); rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
 }
 }

 // ===== Public API =====
 public void PlayVoiceCue(VoiceCue cue, Transform worldPos = null)
 {
 if (!cue || !cue.clip) return; if (runRoutine != null) StopCoroutine(runRoutine); runRoutine = StartCoroutine(RunCue(cue, worldPos)); }

 public void PlayVoiceCueIndex(int index, Transform worldPos = null)
 { if (cueLibrary == null || index <0 || index >= cueLibrary.Length) return; PlayVoiceCue(cueLibrary[index], worldPos); }

 public void PlayVoiceCueKey(string key)
 {
 if (string.IsNullOrEmpty(key) || namedCues == null) return;
 for (int i=0;i<namedCues.Length;i++)
 {
 var nc = namedCues[i]; if (nc != null && nc.cue && nc.key == key) { PlayVoiceCue(nc.cue, nc.worldPos); return; }
 }
 }

 public bool IsPlaying => runRoutine != null;
 public void StopCurrent()
 { if (runRoutine != null) { StopCoroutine(runRoutine); runRoutine = null; } StartCoroutine(FadeTo(0f)); }

 private IEnumerator RunCue(VoiceCue cue, Transform worldPos)
 {
 // play audio
 if (VoiceManager.Instance)
 {
 if (cue.play3D && worldPos) VoiceManager.Instance.PlayFrom(worldPos, cue.clip, cue.volume, cue.minDistance, cue.maxDistance);
 else VoiceManager.Instance.PlayOneShot(cue.clip, cue.volume);
 }

 float startTime = Time.time;
 for (int i=0;i<cue.lines.Length;i++)
 {
 var line = cue.lines[i];
 float absStart = startTime + line.start;
 // wait until start
 while (Time.time < absStart) yield return null;
 // show
 subtitleText.text = line.text;
 yield return FadeTo(1f);
 // hold
 float holdEnd = absStart + line.duration;
 while (Time.time < holdEnd) yield return null;
 // hide between lines if option
 if (hideBetweenLines || i == cue.lines.Length -1)
 {
 yield return FadeTo(0f);
 }
 }
 runRoutine = null;
 }

 private IEnumerator RunAutoSequence()
 {
 if (autoPlayStartDelay >0f) yield return new WaitForSeconds(autoPlayStartDelay);
 for (int i=0;i<autoSequence.Length;i++)
 {
 var entry = autoSequence[i]; if (!entry.cue) continue;
 PlayVoiceCue(entry.cue, entry.worldPos);
 // wait for current cue to finish (approx by clip length) + gap
 float wait = entry.cue.clip ? entry.cue.clip.length :0f; yield return new WaitForSeconds(wait + Mathf.Max(0f, entry.delayAfter));
 }
 autoRoutine = null;
 }

 private IEnumerator FadeTo(float target)
 {
 float t =0f; float start = canvasGroup.alpha;
 while (t < fadeDuration)
 { t += Time.deltaTime; canvasGroup.alpha = Mathf.Lerp(start, target, t / fadeDuration); yield return null; }
 canvasGroup.alpha = target;
 }
 }
}
