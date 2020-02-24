using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Game Settings")]
    public Settings settings;

    [Header("Lighting")]
    [Range(0, 1)] public float globalLightLevel;
    public Color dayColor;
    public Color nightColor;

    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning("More then on instance of - GameManager on (" + instance.gameObject.name + ") Gameobject");
            return;
        }
        else
            instance = this;
    }

    private void Start()
    {
        //string jsonExport = JsonUtility.ToJson(settings);
        //Debug.Log("Saving JSON, output: " + jsonExport);
        //File.WriteAllText(Application.dataPath + "/Resources/Settings.cfg", jsonExport);

        //string jsonImport = File.ReadAllText("Assets/Resources/Settings.cfg");
        //settings = JsonUtility.FromJson<Settings>(jsonImport);

        Shader.SetGlobalFloat("MinGlobalLightLevel", VoxelData.minLightLevel);
        Shader.SetGlobalFloat("MaxGlobalLightLevel", VoxelData.maxLightLevel);
        SetGlobalLightValue();
    }

    private void Update()
    {
        
    }

    public void SetGlobalLightValue()
    {
        Shader.SetGlobalFloat("GlobalLightLevel", globalLightLevel);
        Camera.main.backgroundColor = Color.Lerp(nightColor, dayColor, globalLightLevel);
    }
}
