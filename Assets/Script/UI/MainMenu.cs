using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // �ѧ��ѹ������¡����͡����� "Play"
    public void PlayGame()
    {
        SceneManager.LoadScene(1); // ������ Scene ����� �� "Level1"
    }
    public void QuitGame()
    {
       Application.Quit(); 
    }
}
