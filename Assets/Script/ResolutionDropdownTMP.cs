using UnityEngine;
using TMPro;
using System.Linq;

public class ResolutionDropdownTMP : MonoBehaviour
{
    public TMP_Dropdown resolutionDropdown;
    private Resolution[] resolutions;

    void Start()
    {

        foreach (var res in Screen.resolutions)
        {
            Debug.Log($"Resolution: {res.width} x {res.height} @ {res.refreshRate}Hz");
        }

        // ดึงรายชื่อความละเอียดทั้งหมดของหน้าจอ
        resolutions = Screen.resolutions
            .Select(res => new Resolution { width = res.width, height = res.height })
            .Distinct()
            .ToArray();

        resolutionDropdown.ClearOptions();

        // แปลงเป็นข้อความแสดงผล
        var options = resolutions
            .Select(res => res.width + " x " + res.height)
            .ToList();

        resolutionDropdown.AddOptions(options);

        // ตั้งค่า default เป็นความละเอียดปัจจุบัน
        int currentResolutionIndex = 0;
        for (int i = 0; i < resolutions.Length; i++)
        {
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
                break;
            }
        }

        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        // ผูก event เวลาเปลี่ยนค่า
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
    }

    void SetResolution(int index)
    {
        Resolution resolution = resolutions[index];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        Debug.Log($"เปลี่ยนความละเอียดเป็น {resolution.width}x{resolution.height}");
    }
}
