using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;

public class TitleMenu : MonoBehaviour
{
    public LevelLoader levelLoader;
    [Space]
    public GameObject mainMenuObject;
    public GameObject settingsMenuObject;

    [Header("Main Menu UI")]
    public TextMeshProUGUI seedField;

    [Header("Settings Menu UI")]
    public Slider viewDistSlider;
    public TextMeshProUGUI viewDistText;
    [Space]
    public Slider mouseSensitivitySlider;
    public TextMeshProUGUI mouseSensitivityText;
    [Space]
    public Toggle threadingToggle;
    public Toggle animatedChunkToggle;

    private Settings settings;

    private void Awake()
    {
        if(!File.Exists(Application.dataPath + "/Resources/Settings.cfg"))
        {
            Debug.Log("No settings file found, creating new one.");

            settings = new Settings();
            string jsonExport = JsonUtility.ToJson(settings);
            File.WriteAllText(Application.dataPath + "/Resources/Settings.cfg", jsonExport);
        }
        else
        {
            Debug.Log("Loading Settings.");

            string jsonImport = File.ReadAllText("Assets/Resources/Settings.cfg");
            settings = JsonUtility.FromJson<Settings>(jsonImport);
        }
    }

    public void StartGame()
    {
        VoxelData.seed = Mathf.Abs(seedField.text.GetHashCode()) / 200;
        // Load Scene
        levelLoader.LoadLevel("SampleScene");
    }

    public void EnterSetting()
    {
        viewDistSlider.value = settings.ViewDistanceInChunks;
        UpdateViewDistSlider();

        mouseSensitivitySlider.value = settings.mouseSensitivity;
        UpdateMouseSlider();

        threadingToggle.isOn = settings.enableThreading;
        animatedChunkToggle.isOn = settings.enableChunkAnimation;

        mainMenuObject.SetActive(false);
        settingsMenuObject.SetActive(true);
    }

    public void DoneSettings()
    {
        settings.ViewDistanceInChunks = (int)viewDistSlider.value;
        settings.mouseSensitivity = mouseSensitivitySlider.value;
        settings.enableThreading = threadingToggle.isOn;
        settings.enableChunkAnimation = animatedChunkToggle.isOn;

        string jsonExport = JsonUtility.ToJson(settings);
        Debug.Log("Saving JSON, output: " + jsonExport);
        File.WriteAllText(Application.dataPath + "/Resources/Settings.cfg", jsonExport);

        mainMenuObject.SetActive(true);
        settingsMenuObject.SetActive(false);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void UpdateViewDistSlider()
    {
        viewDistText.text = "View Distance: " + viewDistSlider.value;
    }

    public void UpdateMouseSlider()
    {
        mouseSensitivityText.text = "Mouse Sensitivity: " + mouseSensitivitySlider.value.ToString("F1");
    }
}
