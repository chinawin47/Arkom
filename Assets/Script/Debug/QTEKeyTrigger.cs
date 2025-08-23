using UnityEngine;
using UnityEngine.InputSystem;
using ARKOM.QTE;
public class QTEKeyTrigger : MonoBehaviour {
    QTEManager mgr;
    void Start() { mgr = FindObjectOfType<QTEManager>(); }
    void Update() {
        if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
            mgr?.StartQTE();
    }
}