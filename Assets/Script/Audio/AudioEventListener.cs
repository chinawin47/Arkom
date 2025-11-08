using UnityEngine;
using ARKOM.Core;
using ARKOM.Story;

namespace ARKOM.Audio
{
 [AddComponentMenu("Audio/Audio Event Listener")]
 public class AudioEventListener : MonoBehaviour
 {
 [Header("Playback")] public bool use3D = false; public float volume =1f; public float minDistance =1f; public float maxDistance =20f;
 [Tooltip("ถ้าเป็น3D จะเล่นที่ตำแหน่งของ GameObject นี้")] public Transform playAt;

 [Header("On Player Caught")] public bool playOnPlayerCaught = true; public AudioClip playerCaughtClip;
 [Header("On Upstairs Door Unlocked")] public bool playOnUpstairsUnlocked = true; public AudioClip upstairsUnlockedClip;
 [Header("On Game Over / Victory")] public bool playOnGameOver = true; public AudioClip gameOverClip; public bool playOnVictory = true; public AudioClip victoryClip;

 private void Awake()
 {
 if (!playAt) playAt = transform;
 }
 private void OnEnable()
 {
 EventBus.Subscribe<PlayerCaughtEvent>(OnPlayerCaught);
 EventBus.Subscribe<UpstairsDoorUnlockedEvent>(OnUpstairsDoorUnlocked);
 EventBus.Subscribe<GameStateChangedEvent>(OnGameState);
 }
 private void OnDisable()
 {
 EventBus.Unsubscribe<PlayerCaughtEvent>(OnPlayerCaught);
 EventBus.Unsubscribe<UpstairsDoorUnlockedEvent>(OnUpstairsDoorUnlocked);
 EventBus.Unsubscribe<GameStateChangedEvent>(OnGameState);
 }

 private void OnPlayerCaught(PlayerCaughtEvent e)
 {
 if (!playOnPlayerCaught || !playerCaughtClip) return;
 PlayClip(playerCaughtClip, e.Ghost ? e.Ghost.position : playAt.position);
 }
 private void OnUpstairsDoorUnlocked(UpstairsDoorUnlockedEvent e)
 {
 if (!playOnUpstairsUnlocked || !upstairsUnlockedClip) return;
 PlayClip(upstairsUnlockedClip, playAt.position);
 }
 private void OnGameState(GameStateChangedEvent e)
 {
 if (e.State == GameState.GameOver)
 {
 if (playOnGameOver && gameOverClip) PlayClip(gameOverClip, playAt.position);
 }
 else if (e.State == GameState.Victory)
 {
 if (playOnVictory && victoryClip) PlayClip(victoryClip, playAt.position);
 }
 }

 private void PlayClip(AudioClip clip, Vector3 pos)
 {
 if (!clip || AudioManager.Instance == null) return;
 if (use3D) AudioManager.Instance.Play3D(clip, pos, volume,1f,1f, minDistance, maxDistance);
 else AudioManager.Instance.Play2D(clip, volume,1f,1f);
 }
 }
}
