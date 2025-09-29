using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // ฟังก์ชันนี้เรียกเมื่อกดปุ่ม "Play"
    public void PlayGame()
    {
        SceneManager.LoadScene(1); // ใส่ชื่อ Scene ที่จะไป เช่น "Level1"
    }
    public void QuitGame()
    {
       Application.Quit(); 
    }
}
