using UnityEngine;
using UnityEngine.Video; 

namespace ARKOM.Story
{
    [AddComponentMenu("Story/TV Controller")]
    public class TVController : MonoBehaviour
    {
        [Header("Screen / Visual")] public Renderer screenRenderer; // optional
        public Texture2D introTexture;   // ภาพ/คลิปตอนเปิดข่าว
        public Texture2D staticTexture;  // ภาพซ่า
     

        [Header("Audio")] public AudioSource speaker; // แปะบน TV
        public AudioClip newsClip;       // ข่าวเปิดต้น
        public AudioClip staticClip;     // เสียงซ่า
        public float volume = 1f;

        [Header("Video")]
        public VideoPlayer videoPlayer;// 
        



        private void Awake()
        {
            if (!speaker) speaker = GetComponentInChildren<AudioSource>();
        }

        public void PlayIntro()
        {
            SetScreen(introTexture);
            Play(newsClip);
        }
        public void PowerOff()
        {
            StopVideo();                       // หยุด VideoPlayer
            SetScreen(staticTexture);           // แสดงจอ static/ดำ
            Stop();                             // หยุดเสียง TV ถ้ามี
        }
        public void PreparePostRestoreNews()
        {
            // สามารถเปลี่ยนคลิปข่าวช่วงหลัง restore ได้ ถ้าต้องการ
        }
        public void PlayTimeSkipNews()
        {
            Play(newsClip);
        }

        public void PlayStatic()
        {
            SetScreen(staticTexture);
            Play(staticClip, loop:true);
        }
        public void StopStatic()
        {
            if (speaker && speaker.clip == staticClip) Stop();
            SetScreen(null);
        }

        public void SetScreen(Texture2D tex)
        {
            if (!screenRenderer) return;
            if (screenRenderer.material && screenRenderer.material.HasProperty("_MainTex"))
            {
                screenRenderer.material.SetTexture("_MainTex", tex);
            }
        }

        private void Play(AudioClip clip, bool loop=false)
        {
            if (!speaker || !clip) return;
            speaker.loop = loop;
            speaker.volume = volume;
            speaker.clip = clip;
            speaker.Play();
        }
        private void Stop(){ if (speaker) speaker.Stop(); }


        public void StopVideo()
        {
            if (videoPlayer && videoPlayer.isPlaying)
                videoPlayer.Stop();

            // ล้างภาพค้างจาก RenderTexture
            if (videoPlayer && videoPlayer.targetTexture)
            {
                RenderTexture rt = videoPlayer.targetTexture;
                RenderTexture.active = rt;
                GL.Clear(true, true, Color.black); // จอดำ
                RenderTexture.active = null;
            }

            // ถ้ามี screenRenderer ก็อัปเดตจอเป็นภาพ static ด้วย
            SetScreen(staticTexture);
        }
    }
}
