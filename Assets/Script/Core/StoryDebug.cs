using UnityEngine;

namespace ARKOM.Core
{
    /// <summary>
    /// ��Ǫ��¾���� Log ����Ѻ�ӴѺ�˵ء�ó� (Game Sequence)
    /// �Դ / �Դ ����� StoryDebug.Enabled = true/false
    /// ��᷹ Debug.Log ��������ٻẺ���ǡѹ
    /// </summary>
    public static class StoryDebug
    {
        /// <summary>
        /// ��� false ���������������� (������Դ�͹ Build ��ԧ)
        /// </summary>
        public static bool Enabled = true; // ����¹�� false ���ͻԴ������

        private const string PREFIX = "[�ӴѺ����ͧ] ";

        /// <summary>
        /// ������ͤ�������� (msg) ����� ctx �Ф�ԡ��͹�ѵ��� Console ��
        /// </summary>
        public static void Log(string msg, Object ctx = null)
        {
            if (!Enabled) return;
            if (ctx) Debug.Log(PREFIX + msg, ctx); else Debug.Log(PREFIX + msg);
        }

        /// <summary>
        /// ��������������¹ State (�¡���٧���)
        /// </summary>
        public static void LogState(string stateName, Object ctx = null)
        {
            Log("ʶҹ� -> " + stateName, ctx);
        }

        /// <summary>
        /// �������˵ء�ó� (Event) ����Դ���
        /// </summary>
        public static void LogEvent(string eventName, Object ctx = null)
        {
            Log("�˵ء�ó�: " + eventName, ctx);
        }
    }
}
