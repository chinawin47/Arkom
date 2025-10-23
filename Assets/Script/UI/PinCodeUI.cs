using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ARKOM.UI
{
 [AddComponentMenu("UI/Pin Code UI")]
 public class PinCodeUI : MonoBehaviour
 {
 [Header("Refs")] public GameObject panel;
 public TMP_InputField inputField;
 public Button okButton;
 public Button cancelButton;

 [Header("Behavior")] public bool pauseGameOnOpen = true;
 public bool unlockCursorOnOpen = true;

 private string expected;
 private Action<bool> callback;
 private float prevTimeScale =1f; private bool prevCursorVisible; private CursorLockMode prevLockState;

 void Awake()
 {
 if (panel) panel.SetActive(false);
 if (okButton) okButton.onClick.AddListener(OnOk);
 if (cancelButton) cancelButton.onClick.AddListener(OnCancel);
 }
 void OnDestroy()
 {
 if (okButton) okButton.onClick.RemoveListener(OnOk);
 if (cancelButton) cancelButton.onClick.RemoveListener(OnCancel);
 }

 public void Show(string expectedCode, Action<bool> onClosed)
 {
 expected = expectedCode;
 callback = onClosed;
 if (panel) panel.SetActive(true);
 if (inputField)
 {
 inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
 inputField.characterLimit =4;
 inputField.text = string.Empty;
 inputField.Select(); inputField.ActivateInputField();
 }
 if (pauseGameOnOpen) { prevTimeScale = Time.timeScale; Time.timeScale =0f; }
 if (unlockCursorOnOpen)
 {
 prevCursorVisible = Cursor.visible; prevLockState = Cursor.lockState;
 Cursor.visible = true; Cursor.lockState = CursorLockMode.None;
 }
 }

 private void Close(bool ok)
 {
 if (panel) panel.SetActive(false);
 if (pauseGameOnOpen) Time.timeScale = prevTimeScale;
 if (unlockCursorOnOpen) { Cursor.visible = prevCursorVisible; Cursor.lockState = prevLockState; }
 var cb = callback; callback = null; expected = null;
 cb?.Invoke(ok);
 }

 private void OnOk()
 {
 bool ok = inputField ? string.Equals(inputField.text, expected) : false;
 Close(ok);
 }
 private void OnCancel() => Close(false);
 }
}
