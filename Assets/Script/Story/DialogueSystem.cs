using UnityEngine;
using UnityEngine.UI;

public class DialogueSystem : MonoBehaviour
{
    public static DialogueSystem Instance { get; private set; }
    public GameObject panel;
    public Text dialogueText;
    public float autoHideDelay = 0f;

    private float hideTimer = 0f;

    void Awake()
    {
        Instance = this;
        panel.SetActive(false);
    }

    void Update()
    {
        if (panel && panel.activeSelf && autoHideDelay > 0f)
        {
            hideTimer += Time.deltaTime;
            if (hideTimer >= autoHideDelay)
            {
                HideDialogue();
                hideTimer = 0f;
            }
        }
    }

    public void ShowDialogue(string id)
    {
        string text = GetDialogueText(id);
        dialogueText.text = text;
        panel.SetActive(true);
        hideTimer = 0f;
    }

    public void HideDialogue()
    {
        panel.SetActive(false);
    }

    private string GetDialogueText(string id)
    {
        return id switch
        {
            "npc_greeting" => "สวัสดี! ฉันคือ NPC",
            "npc_quest" => "ช่วยหาของให้ฉันหน่อย",
            _ => "..."
        };
    }
}