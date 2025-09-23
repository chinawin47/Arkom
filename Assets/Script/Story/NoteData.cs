using UnityEngine;

namespace ARKOM.Story
{
    [CreateAssetMenu(fileName = "Note_", menuName = "ARKOM/Story/Note")]
    public class NoteData : ScriptableObject
    {
        [Header("Info")]
        public string noteId;
        public string title;
        [TextArea(5, 20)]
        public string body;           // หรือจะใช้ TextAsset ก็ได้ตามต้องการ
        public Sprite image;          // รูปภาพประกอบ (ไม่บังคับ)

        [Header("On Read")]
        [Tooltip("เมือ่อ่านแล้วให้เติม Story Flag นี้ (ถ้าไม่ว่าง)")]
        public string flagOnRead;
    }
}