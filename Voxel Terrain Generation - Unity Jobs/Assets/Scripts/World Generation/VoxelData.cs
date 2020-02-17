using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VoxelData
{
    /// <summary>
    /// <para>How many individual voxels are in the ChunkWidth</para>
    /// MUST SET VALUE IN CODE! 
    /// </summary> 
    public static readonly int ChunkWidth = 16;
    /// <summary>
    /// <para>How many individual voxels are in the ChunkHeight</para> 
    /// MUST SET VALUE IN CODE!
    /// </summary> 
	public static readonly int ChunkHeight = 128;

    /// <summary>
    /// How many chunks make up the world when generating.
    /// </summary>
    public static readonly int WorldSizeInChunks = 50;

    /// <summary>
    /// How many voxels make up the world when generating.
    /// </summary>
    public static int WorldSizeInVoxels
    {
        get { return WorldSizeInChunks * ChunkWidth; }
    }

    /// <summary>
    /// View distance for the player, if a chunk is further away then the ViewDistanceInChunks generation will not render.
    /// </summary>
    public static readonly int ViewDistanceInChunks = 5;

    /// <summary>
    /// How many voxel (block) textures are in the width and height of the atlas, E.G. 4 blocks in the width and height of the atlas.
    /// </summary>
    public static readonly int TextureAtlasSizeInBlocks = 4;
    /// <summary>
    /// Normalizes the texture atlas (0 to 1) then devides it by <seealso cref="TextureAtlasSizeInBlocks"/>
    /// <para>MUST BE SQUARE AND EVENLY SPACED TEXTURE ATLAS!</para>
    /// </summary>
    public static float NormalizedBlockTextureSize
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

	public static readonly Vector3[] faceChecks = new Vector3[6] 
    {
		new Vector3(0.0f, 0.0f, -1.0f),
		new Vector3(0.0f, 0.0f, 1.0f),
		new Vector3(0.0f, 1.0f, 0.0f),
		new Vector3(0.0f, -1.0f, 0.0f),
		new Vector3(-1.0f, 0.0f, 0.0f),
		new Vector3(1.0f, 0.0f, 0.0f)
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
}
