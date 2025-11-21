using System.Collections.Generic;
using UnityEngine;
using ARKOM.Core;
using ARKOM.Story;
using System.Collections;

namespace ARKOM.Player.Voice
{
    [AddComponentMenu("Player/Player Voice Manager")]
    public class PlayerVoiceManager : MonoBehaviour
    {
        [Header("Sources / References")] public AudioSource voiceSource;
        [Tooltip("??? VoiceLine ????????????? (??????????????????)")] public List<PlayerVoiceLineSet> voiceSets = new();

        [Header("StoryState Mapping")] 
        [Tooltip("Map StoryState -> lineId ???? sequence (??????????????????????)")] public List<StoryStateVoiceMap> storyStateVoiceMaps = new();

        [Header("Ambient Settings")] public bool enableAmbient = true; public float ambientInterval = 30f; [Tooltip("?????? lineId ?????? Ambient ????")] public List<string> ambientIds = new();

        [Header("Global Cooldowns")] public float globalCooldownBetweenLines = 1.2f;

        private float lastPlayTime;
        private readonly Dictionary<string, float> lastPlayPerId = new();
        private PlayerVoiceLine currentLine;
        private float ambientTimer;

        // Sequence tracking
        private bool sequenceActive; private Coroutine sequenceCoroutine;

        [System.Serializable]
        public class StoryStateVoiceMap
        {
            public SequenceController.StoryState state;
            [Tooltip("lineId ??? VoiceLine (?????????????? sequenceIds)")] public string lineId;
            [Tooltip("???????? sequenceIds (?????????????????????????????)")] public bool useSequence = false;
            [Tooltip("?????? lineId ????????????????????????????? state ???")] public List<string> sequenceIds = new();
            [Tooltip("?????????????????????????????????? (??????) ????????? postDelay ???????????????")] public float sequenceGap = 0.35f;
            [Tooltip("Override priority (???????? = ?????? line)")] public int overridePriority = 0; public bool useOverridePriority = false;
            [Header("Delay Overrides")]
            [Tooltip("??? startDelay / postDelay ????????????????? mapping ??? (????????????? VoiceLine)")] public bool overrideLineDelays = false;
            [Tooltip("??????????????????????? (??????) ??? overrideLineDelays=true")] public float startDelay = 0f;
            [Tooltip("????????????????????????????????? (??????) ??? overrideLineDelays=true")] public float postDelay = 0f;
        }

        void Awake()
        {
            if (!voiceSource)
            {
                voiceSource = gameObject.AddComponent<AudioSource>();
                voiceSource.playOnAwake = false; voiceSource.loop = false; voiceSource.spatialBlend = 0f;
            }
        }

        void OnEnable()
        {
            EventBus.Subscribe<PlayerVoiceRequestEvent>(OnVoiceRequest);
            EventBus.Subscribe<StoryStateChangedEvent>(OnStoryStateChanged);
        }
        void OnDisable()
        {
            EventBus.Unsubscribe<PlayerVoiceRequestEvent>(OnVoiceRequest);
            EventBus.Unsubscribe<StoryStateChangedEvent>(OnStoryStateChanged);
        }

        void Update()
        {
            if (enableAmbient && ambientIds != null && ambientIds.Count > 0 && !sequenceActive)
            {
                ambientTimer -= Time.deltaTime;
                if (ambientTimer <= 0f && !voiceSource.isPlaying)
                {
                    var id = ambientIds[Random.Range(0, ambientIds.Count)];
                    TryPlay(id);
                    ambientTimer = ambientInterval;
                }
            }
        }

        private void OnStoryStateChanged(StoryStateChangedEvent e)
        {
            // ????? sequence ???????????????? state ??????? -> ????????
            if (sequenceActive && sequenceCoroutine != null)
            {
                StopCoroutine(sequenceCoroutine);
                sequenceCoroutine = null;
                sequenceActive = false;
                // ????????????????????? (???????????????)
                if (voiceSource && voiceSource.isPlaying)
                {
                    voiceSource.Stop();
                }
                currentLine = null; // clear reference
            }
            // ?? mapping ??????????????????
            for (int i =0; i < storyStateVoiceMaps.Count; i++)
            {
                var m = storyStateVoiceMaps[i]; if (m == null) continue;
                if (m.state == e.Current)
                {
                    StartStateVoice(m);
                    break;
                }
            }
        }

        private void StartStateVoice(StoryStateVoiceMap map)
        {
            // cancel previous sequence
            if (sequenceCoroutine != null) StopCoroutine(sequenceCoroutine);
            sequenceActive = false;
            if (map.useSequence && map.sequenceIds != null && map.sequenceIds.Count > 0)
            {
                sequenceCoroutine = StartCoroutine(SequenceRoutine(map));
            }
            else if (!string.IsNullOrEmpty(map.lineId))
            {
                sequenceCoroutine = StartCoroutine(SingleLineRoutine(map));
            }
        }

        private IEnumerator SingleLineRoutine(StoryStateVoiceMap map)
        {
            sequenceActive = true;
            var lineAsset = FindLine(map.lineId);
            float startDelay = map.overrideLineDelays ? map.startDelay : (lineAsset ? lineAsset.startDelay : 0f);
            if (startDelay > 0f) yield return new WaitForSeconds(startDelay);
            if (map.useOverridePriority) TryPlay(map.lineId, map.overridePriority, true); else TryPlay(map.lineId);
            float clipLen = (voiceSource.clip ? voiceSource.clip.length : 0f);
            float postDelay = map.overrideLineDelays ? map.postDelay : (lineAsset ? lineAsset.postDelay : 0f);
            if (clipLen + postDelay > 0f) yield return new WaitForSeconds(clipLen + postDelay);
            sequenceActive = false; sequenceCoroutine = null;
        }

        private IEnumerator SequenceRoutine(StoryStateVoiceMap map)
        {
            sequenceActive = true;
            for (int i = 0; i < map.sequenceIds.Count; i++)
            {
                string id = map.sequenceIds[i];
                var lineAsset = FindLine(id);
                float startDelay = map.overrideLineDelays ? map.startDelay : (lineAsset ? lineAsset.startDelay : 0f);
                if (startDelay > 0f) yield return new WaitForSeconds(startDelay);
                bool played = map.useOverridePriority ? TryPlay(id, map.overridePriority, true) : TryPlay(id);
                float clipLen = (played && voiceSource.clip ? voiceSource.clip.length : 0f);
                float postDelay = map.overrideLineDelays ? map.postDelay : (lineAsset ? lineAsset.postDelay : 0f);
                // ??? postDelay + sequenceGap (?????????????)
                float waitTotal = clipLen + postDelay + (i < map.sequenceIds.Count -1 ? map.sequenceGap : 0f);
                if (waitTotal > 0f) yield return new WaitForSeconds(waitTotal);
            }
            sequenceActive = false; sequenceCoroutine = null;
        }

        private void OnVoiceRequest(PlayerVoiceRequestEvent e)
        {
            if (sequenceActive) return; // ??????? sequence
            if (e.ForcePriority.HasValue)
            {
                TryPlay(e.LineId, e.ForcePriority.Value, forcePriorityOverride:true);
            }
            else
            {
                TryPlay(e.LineId);
            }
        }

        public bool TryPlay(string lineId, int forcedPriority = -9999, bool forcePriorityOverride = false)
        {
            var line = FindLine(lineId);
            if (!line || !line.clip) return false;

            if (Time.time - lastPlayTime < globalCooldownBetweenLines) return false;
            if (lastPlayPerId.TryGetValue(line.lineId, out var lastIdTime))
            {
                if (Time.time - lastIdTime < line.cooldown) return false;
            }

            int newPriority = forcePriorityOverride ? forcedPriority : line.priority;
            int currentPriority = currentLine ? currentLine.priority : int.MinValue;

            if (voiceSource.isPlaying && currentLine)
            {
                if (newPriority < currentPriority) return false;
                if (!currentLine.interruptible && newPriority <= currentPriority) return false;
            }

            voiceSource.Stop();
            voiceSource.clip = line.clip;
            voiceSource.Play();
            currentLine = line;
            lastPlayTime = Time.time;
            lastPlayPerId[line.lineId] = Time.time;
            ambientTimer = ambientInterval;

            EventBus.Publish(new PlayerVoicePlayedEvent(line.lineId));

            if (!string.IsNullOrEmpty(line.subtitle))
            {
                var hint = FindObjectOfType<ARKOM.UI.HintPresenter>();
                if (hint)
                {
                    hint.Show(line.subtitle, voiceSource.clip.length);
                }
            }
            return true;
        }

        private PlayerVoiceLine FindLine(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < voiceSets.Count; i++)
            {
                var set = voiceSets[i]; if (!set) continue;
                var line = set.Find(id); if (line) return line;
            }
            return null;
        }
    }
}
