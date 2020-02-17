using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public class WorldData : MonoBehaviour
{
    public int seed;
    public BiomeAttributes biomeData;
    [Space]
    public PlayerController player;
    public Vector3 spawnLocation;

    public Material voxelMaterialSheet;
    public BlockType[] blockTypes;

    private EntityManager entityManager;

    private WorldGeneration[,] chunks = new WorldGeneration[VoxelData.WorldSizeInChunks, VoxelData.WorldSizeInChunks];
    private List<ChunkCoord> activeChunks = new List<ChunkCoord>();
    [HideInInspector] public ChunkCoord playerChunkCoord;
    private ChunkCoord playerLastChunkCoord;

    private List<ChunkCoord> chunksToCreate = new List<ChunkCoord>();
    private bool isCreatingChunks;

    public GameObject debugScreen;

    private void Awake()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }

    private void Start()
    {
        // As long as the seed is the same number then you'll always get the same perlin noise
        Random.InitState(seed);

        // Spawns player in centre of world
        spawnLocation = new Vector3((VoxelData.WorldSizeInChunks * VoxelData.ChunkWidth) / 2f, VoxelData.ChunkHeight - 50, (VoxelData.WorldSizeInChunks * VoxelData.ChunkWidth) / 2f);

        playerLastChunkCoord = GetChunkCoordFromVector3(player.Position);

        GenerateWorld();
    }

    private void Update()
    {
        playerChunkCoord = GetChunkCoordFromVector3(player.Position);

        // Only update the chunks if the player has moved from the chunk they were previously on
        if (!playerChunkCoord.Equals(playerLastChunkCoord))
            CheckViewDistance();

        if(chunksToCreate.Count > 0 && !isCreatingChunks)
            StartCoroutine(CreateChunks());

        if (Input.GetKeyDown(KeyCode.F3))
            debugScreen.SetActive(!debugScreen.activeSelf);
    }

    private void GenerateWorld()
    {
        for (int x = (VoxelData.WorldSizeInChunks / 2) - VoxelData.ViewDistanceInChunks; x < (VoxelData.WorldSizeInChunks / 2) + VoxelData.ViewDistanceInChunks; x++)
        {
            for (int z = (VoxelData.WorldSizeInChunks / 2) - VoxelData.ViewDistanceInChunks; z < (VoxelData.WorldSizeInChunks / 2) + VoxelData.ViewDistanceInChunks; z++)
            {
                //CreateNewChunk(x, z);
                ChunkCoord chunkCoordinate = new ChunkCoord(x, z);
                chunks[x, z] = new WorldGeneration(chunkCoordinate, this, entityManager, true);
                activeChunks.Add(chunkCoordinate);
            }
        }

        player.SpawnPlayer(spawnLocation, this);
    }

    /// <summary>
    /// Functionality used to generate and create the chunks needed, optimised to help with lag spikes.
    /// Generates a chunk per frame, instead of huge amounts of chunks in a single frame.
    /// </summary>
    /// <returns></returns>
    IEnumerator CreateChunks()
    {
        isCreatingChunks = true;

        while (chunksToCreate.Count > 0)
        {
            chunks[chunksToCreate[0].x, chunksToCreate[0].z].InitChunk();
            chunksToCreate.RemoveAt(0);
            yield return null;
        }

        isCreatingChunks = false;
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
        ChunkCoord chunkCoordinate = GetChunkCoordFromVector3(player.Position);
        playerLastChunkCoord = playerChunkCoord;

        List<ChunkCoord> previouslyActiveChunks = new List<ChunkCoord>(activeChunks);

        for (int x = chunkCoordinate.x - VoxelData.ViewDistanceInChunks; x < chunkCoordinate.x + VoxelData.ViewDistanceInChunks; x++)
        {
            for (int z = chunkCoordinate.z - VoxelData.ViewDistanceInChunks; z < chunkCoordinate.z + VoxelData.ViewDistanceInChunks; z++)
            {
                ChunkCoord newChunkCoord = new ChunkCoord(x, z);

                // Is the chunk inside the world bounds
                if (IsChunkInWorld(newChunkCoord))
                {
                    // If the chunk hasnt been generated then generate it 
                    if(chunks[x, z] == null)
                    {
                        // Create the new Chunk
                        chunks[x, z] = new WorldGeneration(newChunkCoord, this, entityManager, false);
                        chunksToCreate.Add(newChunkCoord);
                    }
                    else if(!chunks[x, z].DetermineChunkActiveState)
                    {
                        chunks[x, z].DetermineChunkActiveState = true;
                    }

                    activeChunks.Add(newChunkCoord);
                }

                // Any chunk that is in the view distance gets removed from previouslyActiveChunks
                for (int i = 0; i < previouslyActiveChunks.Count; i++)
                {
                    if(previouslyActiveChunks[i].Equals(newChunkCoord))
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
    /// Checks to see if there is a voxel in the Vector3 space (<paramref name="x"/>, <paramref name="y"/>, <paramref name="z"/>).
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    public bool CheckForVoxelInSpace(Vector3 pos)
    {
        ChunkCoord thisChunk = new ChunkCoord(pos);

        if (!IsChunkInWorld(thisChunk) || pos.y < 0 || pos.y > VoxelData.ChunkHeight)
            return false;

        // Chunk is definetly in world bounds
        if (chunks[thisChunk.x, thisChunk.z] != null && (chunks[thisChunk.x, thisChunk.z].isVoxelMapPopulated))
            return blockTypes[chunks[thisChunk.x, thisChunk.z].GetVoxelFromGlobalVector3(pos)].isSolid;

        // If all else fails run expensive algarithim
        return blockTypes[GetVoxel(pos)].isSolid;

        //// Global position of the voxel
        //int xCheck = Mathf.FloorToInt(x);
        //int yCheck = Mathf.FloorToInt(y);
        //int zCheck = Mathf.FloorToInt(z);

        //// What chunk contains said voxel
        //int xChunk = xCheck / VoxelData.ChunkWidth;
        //int zChunk = zCheck / VoxelData.ChunkWidth;

        //// Converts global voxel position into local, finding the voxel within the main chunk
        //xCheck -= (xChunk * VoxelData.ChunkWidth);
        //zCheck -= (zChunk * VoxelData.ChunkWidth);

        //return blockTypes[chunks[xChunk, zChunk].voxelMap[xCheck, yCheck, zCheck]].isSolid;
    }

    /// <summary>
    /// Algarithim that helps to decide where the gereration is allowed to put textures on voxels, caves, biomes and ect
    /// </summary>
    public byte GetVoxel(Vector3 pos)
    {
        int yPos = Mathf.FloorToInt(pos.y);

        /* MUST CALL */

        // If outside world bounds, return air voxel ID
        if (!IsVoxelInWorld(pos))
            return 0;

        // If bottom block of chunk, return bedrock voxel ID
        if (yPos == 0)
            return 1;

        /* BASIC TERRAIN */

        // Converts a 0 to 1 value into a percentage that gets multiplied by the heignt to create terrain
        int terrainHeight = Mathf.FloorToInt(biomeData.terrainHeight * Noise.Get2DPerlin(new Vector2(pos.x + seed, pos.z + seed), 0, biomeData.terrainScale)) + biomeData.solidGroundHeight;

        byte voxelValue = 0;

        // If top block of chunk, return stone voxel ID. Else if heigher then top block return air voxel ID 
        if (yPos == terrainHeight)
            voxelValue = 3;
        else if (yPos < terrainHeight && yPos > terrainHeight - 4)
            voxelValue = 6;
        else if (yPos > terrainHeight)
            return 0;
        else
            voxelValue = 2;


        if (voxelValue == 2)
        {
            foreach (Lode lode in biomeData.lodes)
            {
                if(yPos > lode.minHeight && yPos < lode.maxHeight)
                {
                    if (Noise.Get3DPerlin(pos, lode.noiseOffset, lode.scale, lode.threshold))
                        voxelValue = lode.blockID;
                }
            }
        }

        return voxelValue;

        #region Old Code
        //if (pos.y < 1)
        //    return 1;                                   // Bedrock
        //else if (pos.y == VoxelData.ChunkHeight - 1)
        //{
        //    float tempPerlinNoise = Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, 0.1f);
        //    if (tempPerlinNoise < 0.5f)
        //        return 3;                               // Grass on top layer
        //    else
        //        return 7;                               // Sand on lower layers
        //}
        //else if (pos.y >= VoxelData.ChunkHeight - 2)
        //    return 6;                                    // Dirt
        //else
        //    return 2;                                   // Stone
        #endregion
    }

    //private void CreateNewChunk(int x, int z)
    //{
    //    ChunkCoord newChunkCoord = new ChunkCoord(x, z);

    //    chunks[x, z] = new WorldGeneration(newChunkCoord, this, entityManager);
    //    activeChunks.Add(newChunkCoord);
    //}

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
