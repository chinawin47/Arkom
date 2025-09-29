using UnityEngine;

namespace ARKOM.Core
{
    /// <summary>
    /// ตัวช่วยพิมพ์ Log สำหรับลำดับเหตุการณ์ (Game Sequence)
    /// ปิด / เปิด ได้ด้วย StoryDebug.Enabled = true/false
    /// ใช้แทน Debug.Log เพื่อรวมรูปแบบเดียวกัน
    /// </summary>
    public static class StoryDebug
    {
        /// <summary>
        /// ถ้า false จะไม่พิมพ์อะไรเลย (เอาไว้ปิดตอน Build จริง)
        /// </summary>
        public static bool Enabled = true; // เปลี่ยนเป็น false เพื่อปิดทั้งหมด

        private const string PREFIX = "[ลำดับเรื่อง] ";

        /// <summary>
        /// พิมพ์ข้อความทั่วไป (msg) ถ้ามี ctx จะคลิกย้อนวัตถุใน Console ได้
        /// </summary>
        public static void Log(string msg, Object ctx = null)
        {
            if (!Enabled) return;
            if (ctx) Debug.Log(PREFIX + msg, ctx); else Debug.Log(PREFIX + msg);
        }

        /// <summary>
        /// ใช้พิมพ์เวลาเปลี่ยน State (แยกให้ดูง่าย)
        /// </summary>
        public static void LogState(string stateName, Object ctx = null)
        {
            Log("สถานะ -> " + stateName, ctx);
        }

        /// <summary>
        /// ใช้พิมพ์เหตุการณ์ (Event) ที่เกิดขึ้น
        /// </summary>
        public static void LogEvent(string eventName, Object ctx = null)
        {
            Log("เหตุการณ์: " + eventName, ctx);
        }
    }
}
