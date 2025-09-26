using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Audio;
using NUnit.Framework.Constraints;
using TMPro;

public class StartUIManager : MonoBehaviour
{

    public GameObject StartUI;
    public GameObject SettingUI;
    public GameObject PlayerRoomUI;

    [Header("Audio")]
    public AudioMixer gameMixer;
    public Slider volumeSlider;
    public string exposedParam = "MasterVolume";

    private const string VOLUME_KEY = "MaterVolumePref";

    void Start()
    {
        StartUI.SetActive(true);
        SettingUI.SetActive(false);

        if(PlayerPrefs.HasKey(VOLUME_KEY))
        {
            float savedVolume = PlayerPrefs.GetFloat(VOLUME_KEY);
            volumeSlider.value = savedVolume;
            SetMasterVolume(savedVolume);
        }
        else
        {
            SetMasterVolume(volumeSlider.value);
            PlayerPrefs.SetFloat(VOLUME_KEY, volumeSlider.value);
        }

        volumeSlider.onValueChanged.AddListener(SetMasterVolume);
    }

    public void SetMasterVolume(float value)
    {
        if (gameMixer == null) return;

        float volume = Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20;
        gameMixer.SetFloat(exposedParam, volume);

        PlayerPrefs.SetFloat(VOLUME_KEY, value);
        PlayerPrefs.Save();
    }
    
    public void OnStartButtonClicked()
    {
        StartUI.SetActive(false);
        SceneManager.LoadScene("MainScene");
        //SettingUI.SetActive(true);
    }

    public void OnQuitButtonClicked()
    {
        Application.Quit();
    }

    // Open the setting UI
    public void OnSettingButtonClicked()
    {
        SettingUI.SetActive(true);
    }

    public void OnCloseSettingButtonClicked()
    {
        SettingUI.SetActive(false);
    }

    public void OnPlayerRoomOpen()
    {
        StartUI.SetActive(false);
        PlayerRoomUI.SetActive(true);
    }
}
