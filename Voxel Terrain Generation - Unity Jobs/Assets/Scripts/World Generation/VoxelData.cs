using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Rendering;

public static class VoxelData
{
    public static readonly int2 worldSizeInChunks = new int2(50, 50);

    public static readonly Vector3Int ChunkSize = new Vector3Int(16, 120, 16);

    // Lighting
    public static float minLightLevel = 0.15f;
    public static float maxLightLevel = 0.9f;
    public static float lightFalloff = 0.0f;

    /// <summary>
    /// How many voxel (block) textures are in the width and height of the atlas, E.G. 4 blocks in the width and height of the atlas.
    /// </summary>
    public static readonly int TextureAtlasSizeInBlocks = 4;

    /// <summary>
    /// Normalizes the texture atlas (0 to 1) then devides it by <seealso cref="TextureAtlasSizeInBlocks"/>
    /// <para>MUST BE SQUARE AND EVENLY SPACED TEXTURE ATLAS!</para>
    /// </summary>
    public static float NormalizesTextureSize
    {
        get { return 1f / TextureAtlasSizeInBlocks; }
    }

    public static readonly Vector3[] voxelVerts = new Vector3[8] 
    {
		new Vector3(0.0f, 0.0f, 0.0f),
		new Vector3(1.0f, 0.0f, 0.0f),
		new Vector3(1.0f, 1.0f, 0.0f),
		new Vector3(0.0f, 1.0f, 0.0f),
		new Vector3(0.0f, 0.0f, 1.0f),
		new Vector3(1.0f, 0.0f, 1.0f),
		new Vector3(1.0f, 1.0f, 1.0f),
		new Vector3(0.0f, 1.0f, 1.0f)
	};

	public static readonly Vector3Int[] faceChecks = new Vector3Int[6] 
    {
		new Vector3Int( 0,  0, -1),
		new Vector3Int( 0,  0,  1),
		new Vector3Int( 0,  1,  0),
		new Vector3Int( 0, -1,  0),
		new Vector3Int(-1,  0,  0),
		new Vector3Int( 1,  0,  0)
	};

	public static readonly int[,] voxelTris = new int[6,4] 
    {
        // Order of faces being made
        // Back, Front, Top, Bottom, Left, Right

		{0, 3, 1, 2}, // Back Face
		{5, 6, 4, 7}, // Front Face
		{3, 7, 2, 6}, // Top Face
		{1, 5, 0, 4}, // Bottom Face
		{4, 7, 0, 3}, // Left Face
		{1, 2, 5, 6} // Right Face
	};

	public static readonly Vector2[] voxelUvs = new Vector2[4] 
    {
		new Vector2 (0.0f, 0.0f),
		new Vector2 (0.0f, 1.0f),
		new Vector2 (1.0f, 0.0f),
		new Vector2 (1.0f, 1.0f)
	};

    public static float Get2DPerlin(Vector2 position, float offset, float scale)
    {
        return Mathf.PerlinNoise((position.x + 0.1f) / ChunkSize.x * scale + offset, (position.y + 0.1f) / ChunkSize.z * scale + offset);
    }

    public static bool Get3DPerlin(Vector3 position, float offset, float scale, float threshold)
    {
        float x = (position.x + offset + 0.1f) * scale;
        float y = (position.y + offset + 0.1f) * scale;
        float z = (position.z + offset + 0.1f) * scale;

        float AB = Mathf.PerlinNoise(x, y);
        float BC = Mathf.PerlinNoise(y, z);
        float AC = Mathf.PerlinNoise(x, z);
        float BA = Mathf.PerlinNoise(y, x);
        float CB = Mathf.PerlinNoise(z, y);
        float CA = Mathf.PerlinNoise(z, x);

        if ((AB + BC + AC + BA + CB + CA) / 6f > threshold)
            return true;
        else
            return false;
    }
}
