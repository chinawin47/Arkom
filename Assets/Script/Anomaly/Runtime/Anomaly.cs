using UnityEngine;
using ARKOM.Anomalies.Data;
using ARKOM.Core;
using ARKOM.QTE; // เพิ่มสำหรับเรียก QTE
using ARKOM.Anomalies.Runtime; // ใช้หา AnomalyPoint

namespace ARKOM.Anomalies.Runtime
{
    // คอมโพเนนต์บนวัตถุในฉากที่สามารถกลายเป็น Anomaly
    public class Anomaly : Interactable
    {
        [Tooltip("ข้อมูลตั้งค่าจาก ScriptableObject")]
        public AnomalyData data;

        private bool active;

        // ตัวแปรเก็บสภาพเดิม (เพื่อ revert)
        private bool cachedOriginal;
        private Vector3 originalPos;
        private Quaternion originalRot;
        private Vector3 originalScale;
        private Color? originalColor;
        private Renderer cachedRenderer;
        private bool rendererInitiallyEnabled;
        private GameObject spawnedPrefab;

        // ===== QTE Integration =====
        private static Anomaly pendingQTE;         // เก็บ anomaly ที่รอผล QTE (มีได้ครั้งละ 1)
        private bool waitingQTE;                   // ตัวนี้อยู่ระหว่างรอ QTE ผลหรือไม่
        private bool qteSubscribed;                // ป้องกัน unsubscribe ซ้ำ

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
            waitingQTE = false;
            if (pendingQTE == this) pendingQTE = null;
            UnsubscribeQTE();
        }

        public override bool CanInteract(object interactor)
        {
            if (!active) return false;
            if (data != null && data.requiresQTE && waitingQTE) return false; // ระหว่างทำ QTE ห้ามกดซ้ำ
            return base.CanInteract(interactor);
        }

        protected override void OnInteract(object interactor)
        {
            if (data == null)
                return;

            if (data.requiresQTE)
            {
                // ป้องกันซ้อน ถ้ามี QTE อื่นค้างอยู่
                if (pendingQTE != null && pendingQTE != this)
                {
                    return;
                }

                // เริ่ม QTE
                var qte = FindObjectOfType<QTEManager>();
                if (qte == null)
                {
                    Debug.LogWarning("[Anomaly] QTEManager not found; fallback resolve directly.");
                    ResolveDirect();
                    return;
                }

                pendingQTE = this;
                waitingQTE = true;
                SubscribeQTE();
                qte.StartQTE(); // QTEManager จะ Publish GameStateChangedEvent(QTE)
                return;
            }

            // ผู้เล่นตรวจพบ (ไม่ต้อง QTE) → แจ้ง Event ให้ Manager รู้
            ResolveDirect();
        }

        private void ResolveDirect()
        {
            // ใช้ pointId ถ้ามี เพื่อให้ระบบนับระดับ "จุด"
            string id = data != null ? data.anomalyId : null;
            var point = GetComponent<AnomalyPoint>();
            if (point != null && !string.IsNullOrEmpty(point.pointId))
                id = point.pointId;

            if (!string.IsNullOrEmpty(id))
                EventBus.Publish(new AnomalyResolvedEvent(id));

            Deactivate();

            // แจ้งจุดให้เริ่มคูลดาวน์
            if (point != null)
                point.OnResolved();
        }

        private void SubscribeQTE()
        {
            if (qteSubscribed) return;
            EventBus.Subscribe<QTEResultEvent>(OnQTEResult);
            qteSubscribed = true;
        }

        private void UnsubscribeQTE()
        {
            if (!qteSubscribed) return;
            EventBus.Unsubscribe<QTEResultEvent>(OnQTEResult);
            qteSubscribed = false;
        }

        private void OnQTEResult(QTEResultEvent evt)
        {
            if (pendingQTE != this) return; // ไม่ใช่ของเรา ข้าม
            waitingQTE = false;
            pendingQTE = null;
            UnsubscribeQTE();

            if (evt.Success)
            {
                ResolveDirect();
            }
            else
            {
                // ถ้า Fail แล้วต้อง Game Over → ให้บอก GameManager ผ่าน Event ใหม่
                if (data != null && data.qteFailGameOver)
                {
                    string pid = GetComponent<AnomalyPoint>()?.pointId ?? (data?.anomalyId ?? "");
                    EventBus.Publish(new QTEFailGameOverEvent(pid));
                }
                // ถ้าไม่บังคับ Game Over → ไม่ resolve, anomaly ยัง active ให้ผู้เล่นลองใหม่ได้
            }
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
                            spawnedPrefab = Instantiate(data.effectPrefab, transform.position, transform.rotation, transform);
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
                        if (spawnedPrefab) Destroy(spawnedPrefab);
                        break;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (data != null && data.type == AnomalyData.AnomalyType.Position)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, transform.position + transform.TransformVector(data.positionOffset));
            }
        }

        private void OnDisable()
        {
            if (waitingQTE && pendingQTE == this)
                pendingQTE = null;
            UnsubscribeQTE();
        }
    }
}