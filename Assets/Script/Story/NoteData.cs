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
        public string body;           // ���ͨ��� TextAsset ��������ͧ���
        public Sprite image;          // �ٻ�Ҿ��Сͺ (���ѧ�Ѻ)

        [Header("On Read")]
        [Tooltip("�������ҹ���������� Story Flag ��� (��������ҧ)")]
        public string flagOnRead;
    }
}