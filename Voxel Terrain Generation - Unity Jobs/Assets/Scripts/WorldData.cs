using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public class WorldData : MonoBehaviour
{
    public Transform player;
    public Vector3 spawnLocation;

    public Material voxelMaterialSheet;
    public BlockType[] blockTypes;

    private EntityManager entityManager;

    private WorldGeneration[,] chunks = new WorldGeneration[VoxelData.WorldSizeInChunks, VoxelData.WorldSizeInChunks];
    private List<ChunkCoord> activeChunks = new List<ChunkCoord>();
    private ChunkCoord playerChunkCoord;
    private ChunkCoord playerLastChunkCoord;

    private void Awake()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }

    private void Start()
    {
        // Spawns player in centre of world
        spawnLocation = new Vector3((VoxelData.WorldSizeInChunks * VoxelData.ChunkWidth) / 2f, VoxelData.ChunkHeight + 2, (VoxelData.WorldSizeInChunks * VoxelData.ChunkWidth) / 2f);

        playerLastChunkCoord = GetChunkCoordFromVector3(player.position);

        GenerateWorld();
    }

    private void Update()
    {
        playerChunkCoord = GetChunkCoordFromVector3(player.position);

        if(!playerChunkCoord.Equals(playerLastChunkCoord))
            CheckViewDistance();
    }

    private void GenerateWorld()
    {
        for (int x = (VoxelData.WorldSizeInChunks / 2) - VoxelData.ViewDistanceInChunks; x < (VoxelData.WorldSizeInChunks / 2) + VoxelData.ViewDistanceInChunks; x++)
        {
            for (int z = (VoxelData.WorldSizeInChunks / 2) - VoxelData.ViewDistanceInChunks; z < (VoxelData.WorldSizeInChunks / 2) + VoxelData.ViewDistanceInChunks; z++)
            {
                CreateNewChunk(x, z);
            }
        }

        player.position = spawnLocation;
    }

    /// <summary>
    /// Returns the chunk co-ordinate of <paramref name="position"/>.
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    private ChunkCoord GetChunkCoordFromVector3(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x / VoxelData.ChunkWidth);
        int z = Mathf.FloorToInt(position.z / VoxelData.ChunkWidth);

        return new ChunkCoord(x, z);
    }

    /// <summary>
    /// Updates chunks on the fly based off the players current position
    /// </summary>
    private void CheckViewDistance()
    {
        ChunkCoord chunkCoordinate = GetChunkCoordFromVector3(player.position);

        List<ChunkCoord> previouslyActiveChunks = new List<ChunkCoord>(activeChunks);

        for (int x = chunkCoordinate.x - VoxelData.ViewDistanceInChunks; x < chunkCoordinate.x + VoxelData.ViewDistanceInChunks; x++)
        {
            for (int z = chunkCoordinate.z - VoxelData.ViewDistanceInChunks; z < chunkCoordinate.z + VoxelData.ViewDistanceInChunks; z++)
            {
                // Is the chunk inside the world bounds
                if (IsChunkInWorld(new ChunkCoord(x, z)))
                {
                    // If the chunk hasnt been generated then generate it 
                    if(chunks[x, z] == null)
                    {
                        CreateNewChunk(x, z);
                    }
                    else if(!chunks[x, z].DetermineChunkActiveState)
                    {
                        chunks[x, z].DetermineChunkActiveState = true;
                        activeChunks.Add(new ChunkCoord(x, z));
                    }
                }

                // Any chunk that is in the view distance gets removed from previouslyActiveChunks
                for (int i = 0; i < previouslyActiveChunks.Count; i++)
                {
                    if(previouslyActiveChunks[i].Equals(new ChunkCoord(x, z)))
                    {
                        previouslyActiveChunks.RemoveAt(i);
                    }
                }
            }
        }

        foreach (ChunkCoord chunkCoord in previouslyActiveChunks)
        {
            chunks[chunkCoord.x, chunkCoord.z].DetermineChunkActiveState = false;
        }
    }

    /// <summary>
    /// Algarithim that helps to decide where the gereration is allowed to put textures on voxels, caves, biomes and ect
    /// </summary>
    public byte GetVoxel(Vector3 pos)
    {
        if (!IsVoxelInWorld(pos))
            return 0;

        if (pos.y < 1)
            return 1;                                   // Bedrock
        else if (pos.y == VoxelData.ChunkHeight - 1)
            return 3;                                   // Grass on top layer
        else if (pos.y >= VoxelData.ChunkHeight - 2)
           return 6;                                    // Dirt
        else
            return 2;                                   // Stone
    }

    private void CreateNewChunk(int x, int z)
    {
        ChunkCoord newChunkCoord = new ChunkCoord(x, z);

        chunks[x, z] = new WorldGeneration(newChunkCoord, this, entityManager);
        activeChunks.Add(newChunkCoord);
    }

    private bool IsChunkInWorld(ChunkCoord chunkCoord)
    {
        if (chunkCoord.x > 0 && chunkCoord.x < VoxelData.WorldSizeInChunks - 1 && chunkCoord.z > 0 && chunkCoord.z < VoxelData.WorldSizeInChunks - 1)
            return true;
        else
            return false;
    }

    private bool IsVoxelInWorld(Vector3 pos)
    {
        if (pos.x >= 0 && pos.x < VoxelData.WorldSizeInVoxels && pos.y >= 0 && pos.y < VoxelData.ChunkHeight && pos.z >= 0 && pos.z < VoxelData.WorldSizeInVoxels)
            return true;
        else
            return false;

    }
}

[System.Serializable]
public class BlockType
{
    public string blockName;
    public bool isSolid;

    [Header("Texture Values")]
    public byte backFaceTexture;
    public byte frontFaceTexture;
    public byte topFaceTexture;
    public byte bottomFaceTexture;
    public byte leftFaceTexture;
    public byte rightFaceTexture;

    // Back, Front, Top, Bottom, Left, Right

    /// <summary>
    /// Gets the specific texture for each face, E.G. some voxels may have a differant top face then they do for the bottom face.
    /// </summary>
    /// <param name="faceIndex"></param>
    /// <returns></returns>
    public byte GetTextureID(byte faceIndex)
    {
        switch (faceIndex)
        {
            case 0:
                return backFaceTexture;
            case 1:
                return frontFaceTexture;
            case 2:
                return topFaceTexture;
            case 3:
                return bottomFaceTexture;
            case 4:
                return leftFaceTexture;
            case 5:
                return rightFaceTexture;
            default:
                Debug.LogError("Error in GetTextureID; Invalid face index");
                return 0;
        }
    }
}
