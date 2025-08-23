using UnityEngine;
using ARKOM.Anomalies.Data;
using ARKOM.Core;

namespace ARKOM.Anomalies.Runtime
{
    public class Anomaly : Interactable
    {
        [Tooltip("ข้อมูลตั้งค่าจาก ScriptableObject")]
        public AnomalyData data;

        private bool active;

        // เก็บสภาพเดิม (ย่อสำหรับตัวอย่าง)
        private bool cachedOriginal;
        private Vector3 originalPos;
        private Quaternion originalRot;
        private Vector3 originalScale;
        private Color? originalColor;
        private Renderer cachedRenderer;
        private bool rendererInitiallyEnabled;
        private GameObject spawnedPrefab;

        public void Activate()
        {
            if (data == null || active) return;
            CacheOriginal();
            Apply(true);
            active = true;
        }

        public void Deactivate()
        {
            if (!active) return;
            Apply(false);
            active = false;
        }

        public override bool CanInteract(object interactor) => active && base.CanInteract(interactor);

        protected override void OnInteract(object interactor)
        {
            if (data == null) return;
            // เปลี่ยน: ส่ง both id + this
            EventBus.Publish(new AnomalyResolvedEvent(data.anomalyId, this));
            Deactivate();
        }

        private void CacheOriginal()
        {
            if (cachedOriginal) return;
            originalPos = transform.localPosition;
            originalRot = transform.localRotation;
            originalScale = transform.localScale;
            cachedRenderer = GetComponent<Renderer>();
            if (cachedRenderer)
            {
                rendererInitiallyEnabled = cachedRenderer.enabled;
                if (cachedRenderer.material.HasProperty("_Color"))
                    originalColor = cachedRenderer.material.color;
            }
            cachedOriginal = true;
        }

        private void Apply(bool enable)
        {
            if (data == null) return;

            if (enable)
            {
                switch (data.type)
                {
                    case AnomalyData.AnomalyType.Position:
                        transform.localPosition = originalPos + data.positionOffset;
                        break;
                    case AnomalyData.AnomalyType.Rotation:
                        transform.localRotation = originalRot * Quaternion.Euler(data.rotationOffset);
                        break;
                    case AnomalyData.AnomalyType.Scale:
                        transform.localScale = originalScale * data.scaleMultiplier;
                        break;
                    case AnomalyData.AnomalyType.Color:
                        if (cachedRenderer && cachedRenderer.material.HasProperty("_Color"))
                            cachedRenderer.material.color = data.colorChange;
                        break;
                    case AnomalyData.AnomalyType.Disappear:
                        if (cachedRenderer) cachedRenderer.enabled = false;
                        break;
                    case AnomalyData.AnomalyType.PictureFlip:
                        transform.localRotation = originalRot * Quaternion.Euler(0f, 180f, 0f);
                        break;
                    case AnomalyData.AnomalyType.ShadowMovement:
                    case AnomalyData.AnomalyType.SpawnPrefab:
                        if (data.useEffectPrefab && data.effectPrefab && spawnedPrefab == null)
                            spawnedPrefab = Object.Instantiate(data.effectPrefab, transform.position, transform.rotation, transform);
                        break;
                }

                if (data.soundEffect != null)
                    AudioSource.PlayClipAtPoint(data.soundEffect, transform.position);
            }
            else
            {
                switch (data.type)
                {
                    case AnomalyData.AnomalyType.Position:
                        transform.localPosition = originalPos; break;
                    case AnomalyData.AnomalyType.Rotation:
                    case AnomalyData.AnomalyType.PictureFlip:
                        transform.localRotation = originalRot; break;
                    case AnomalyData.AnomalyType.Scale:
                        transform.localScale = originalScale; break;
                    case AnomalyData.AnomalyType.Color:
                        if (cachedRenderer && originalColor.HasValue && cachedRenderer.material.HasProperty("_Color"))
                            cachedRenderer.material.color = originalColor.Value;
                        break;
                    case AnomalyData.AnomalyType.Disappear:
                        if (cachedRenderer) cachedRenderer.enabled = rendererInitiallyEnabled;
                        break;
                    case AnomalyData.AnomalyType.ShadowMovement:
                    case AnomalyData.AnomalyType.SpawnPrefab:
                        if (spawnedPrefab) Object.Destroy(spawnedPrefab);
                        break;
                }
            }
        }
    }
}