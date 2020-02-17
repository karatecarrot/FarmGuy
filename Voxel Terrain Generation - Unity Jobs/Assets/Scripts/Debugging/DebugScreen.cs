using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DebugScreen : MonoBehaviour
{
    public WorldData worldData;
    public TextMeshProUGUI text;

    private float frameRate;
    private float timer;

    private int halfWorldSizeInVoxels;
    private int halfWorldSizeInChunks;

    private void Start()
    {
        halfWorldSizeInChunks = VoxelData.WorldSizeInChunks / 2;
        halfWorldSizeInVoxels = VoxelData.WorldSizeInVoxels / 2;
    }

    private void Update()
    {
        string debugText = "Debug Screen";
        debugText += "\n";
        debugText += frameRate + " FPS";
        debugText += "\n";
        debugText += "XYZ: " + (Mathf.FloorToInt(worldData.player.Position.x) - halfWorldSizeInVoxels) + " / " + Mathf.FloorToInt(worldData.player.Position.y) + " / " + (Mathf.FloorToInt(worldData.player.Position.z) - halfWorldSizeInVoxels);
        debugText += "\n";
        debugText += "Current Chunk: X " + (worldData.playerChunkCoord.x) + " / Z " + (worldData.playerChunkCoord.z);

        text.text = debugText;

        if (timer > 1f)
        {
            frameRate = (int)(1f / Time.unscaledDeltaTime);
            timer = 0;
        }
        else
            timer += Time.deltaTime;
    }
}
