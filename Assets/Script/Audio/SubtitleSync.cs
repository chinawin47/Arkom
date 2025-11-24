using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Playables;

[System.Serializable]
public class SubtitleLine
{
    public string text;
    public float startTime;
}

public class SubtitleSync : MonoBehaviour
{
    public PlayableDirector director;      // <- Timeline
    public TMP_Text subtitleText;
    public List<SubtitleLine> lines = new List<SubtitleLine>();

    private int currentIndex = 0;

    void Start()
    {
        subtitleText.text = "";
    }

    void Update()
    {
        if (currentIndex >= lines.Count)
            return;

        // ดึงเวลา ณ ปัจจุบันจาก Timeline
        float t = (float)director.time;

        if (t >= lines[currentIndex].startTime)
        {
            subtitleText.text = lines[currentIndex].text;
            currentIndex++;
        }
    }
}
