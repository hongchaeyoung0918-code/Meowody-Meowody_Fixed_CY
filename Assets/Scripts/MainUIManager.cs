using UnityEngine;
using UnityEngine.SceneManagement;

public class MainUIManager : MonoBehaviour
{
    //public static MainUIManager instance;

    public GameObject InGameUI;
    public GameObject GameClearUI;
    public GameObject GameOverUI;
    public GameObject PauseUI;
    //public GameObject Startillust;

    private void Awake()
    {
/*        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }*/
    }

    public void OnPauseBtnClicked()
    {

    }

    public void ShowGameClear()
    {
        GameClearUI.SetActive(true);
    }

    public void OnNextBtnClicked()
    {
        GameClearUI.SetActive(false);
        //Startillust.SetActive(true);
        SceneManager.LoadScene("StartScene");
    }
}
