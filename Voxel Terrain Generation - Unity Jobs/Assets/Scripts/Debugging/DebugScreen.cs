using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Mathematics;

public class DebugScreen : MonoBehaviour
{
    public WorldGenerator world;
    public BlockDatabase database;
    public TextMeshProUGUI text;

    private float frameRate;
    private float timer;

    private int2 halfWorldSizeInVoxels;
    private int2 halfWorldSizeInChunks;

    private void Start()
    {
        halfWorldSizeInChunks = VoxelData.worldSizeInChunks / 2;
        halfWorldSizeInVoxels = world.WorldSizeInVoxels / 2;
    }

    private void Update()
    {
        string debugText = "Debug Screen";
        debugText += "\n";
        debugText += frameRate + " FPS";
        debugText += "\n";
        debugText += "XYZ: " + (Mathf.FloorToInt(world.player.Position.x) - halfWorldSizeInVoxels.x) + " / " + Mathf.FloorToInt(world.player.Position.y) + " / " + (Mathf.FloorToInt(world.player.Position.z) - halfWorldSizeInVoxels.y);
        debugText += "\n";
        debugText += "Current Chunk: X " + (world.playerChunkCoord.x - halfWorldSizeInChunks.x) + " / Z " + (world.playerChunkCoord.z - halfWorldSizeInChunks.y);
        debugText += "\n\n";
        debugText += "Player arm reach " + world.player.playerReach + " blocks";
        debugText += "\n";
        debugText += "Selected Block " + database.blockDatabase[world.player.selectedBlockIndex].name;

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
