using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BiomeAttribute", menuName = "New Biome")]
public class BiomeAttributes : ScriptableObject
{
    [Header("Biome Data")]
    /// <summary>
    /// Name of the biome
    /// </summary>
    public string biomeName;

    public int offset;
    public float scale;

    /// <summary>
    /// Height of the terrain, starting from the <see cref="solidGroundHeight"/> to the heighest point the terrain will generate
    /// </summary>
    public int terrainHeight;

    /// <summary>
    /// Passed into <see cref="Noise.Get2DPerlin(Vector2, float, terrainScale)"/>.
    /// </summary>
    public float terrainScale;

    public byte surfaceBlockID;
    public byte subSurfaceBlockID;

    [Space]
    [Header("Cave and Ore Data")]
    public Lode[] lodes;

}

/// <summary>
/// Class that helps the generations to generate ore clusters
/// </summary>
[System.Serializable]
public class Lode
{
    public string nodeName;
    public byte blockID;

    /// <summary>
    /// The minimum height that the voxel of type blockID is generated
    /// </summary>
    public int minHeight;
    /// <summary>
    /// The maximum height that the voxel of type blockID is generated
    /// </summary>
    public int maxHeight;

    public float scale;
    public float threshold;
    public float noiseOffset;
}
