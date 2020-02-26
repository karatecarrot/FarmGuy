using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using System.Threading;

public class WorldGenerator : MonoBehaviour
{
    private EntityManager EntityManager
    {
        get { return World.DefaultGameObjectInjectionWorld.EntityManager; }
    }

    private BlockDatabase BlockDatabase
    {
        get { return BlockDatabase.instance; }
    }

    private GameManager GameManager
    {
        get { return GameManager.instance; }
    }

    [Header ("World Generation Values")]
    public PlayerController player;
    public BiomeAttributes[] biomes;
    [Space]
    private Vector3Int spawnLocation;

    public int2 WorldSizeInVoxels
    {
        get { return new int2(VoxelData.worldSizeInChunks.x * VoxelData.ChunkSize.x, VoxelData.worldSizeInChunks.y * VoxelData.ChunkSize.z); }
    }

    public Chunk[,] chunks = new Chunk[VoxelData.worldSizeInChunks.x, VoxelData.worldSizeInChunks.y];
    private List<ChunkCoord> activeChunks = new List<ChunkCoord>();
    private ChunkCoord playerLastChunkCoord;
    public ChunkCoord playerChunkCoord;

    public Queue<Chunk> chunksToDraw = new Queue<Chunk>();
    private List<ChunkCoord> chunksToCreate = new List<ChunkCoord>();
    public List<Chunk> chunksToUpdate = new List<Chunk>();

    private bool isCreatingChunks = false;

    private Thread ChunkUpdateThread;
    public object ChunkUpdateThreadLock = new object();

    private void Start()
    {
        Debug.Log("Generating new world using seed " + VoxelData.seed);

        UnityEngine.Random.InitState(VoxelData.seed);

        if (GameManager.settings.enableThreading)
        {
            ChunkUpdateThread = new Thread(new ThreadStart(ThreadedUpdate));
            ChunkUpdateThread.Start();
        }

        spawnLocation = new Vector3Int((VoxelData.worldSizeInChunks.x * VoxelData.ChunkSize.x) / 2, VoxelData.ChunkSize.y - 50, (VoxelData.worldSizeInChunks.y * VoxelData.ChunkSize.z) / 2);
        playerLastChunkCoord = GetChunkCoordFromVector3(player.Position);

        GenerateWorld();
    }

    private void Update()
    {
        playerChunkCoord = GetChunkCoordFromVector3(player.Position);

        // Only update the chunks if the player has moved from the chunk they were previously on
        if (!playerChunkCoord.Equals(playerLastChunkCoord))
            CheckViewDistance();

        if (chunksToCreate.Count > 0 && !isCreatingChunks)
            StartCoroutine(CreateChunks());

        if(!GameManager.settings.enableThreading)
        {
            if (chunksToUpdate.Count > 0)
                UpdateChunks();
        }

        if (chunksToDraw.Count > 0)
        {
            if (chunksToDraw.Peek().IsEditable)
                chunksToDraw.Dequeue().CreateMesh(BlockDatabase.blockMaterialAtlas);
        }
    }

    private void GenerateWorld()
    {
        for (int x = (VoxelData.worldSizeInChunks.x / 2) - GameManager.settings.ViewDistanceInChunks; x < (VoxelData.worldSizeInChunks.x / 2) + GameManager.settings.ViewDistanceInChunks; x++)
        {
            for (int z = (VoxelData.worldSizeInChunks.y / 2) - GameManager.settings.ViewDistanceInChunks; z < (VoxelData.worldSizeInChunks.y / 2) + GameManager.settings.ViewDistanceInChunks; z++)
            {
                //for (int x = 0; x < worldSizeInChunks.x; x++)
                //{
                //    for (int z = 0; z < worldSizeInChunks.y; z++)
                //    {

                ChunkCoord newChunkCoord = new ChunkCoord(new int2(x, z));

                chunks[x, z] = new Chunk(newChunkCoord, this, BlockDatabase, EntityManager, GameManager);
                chunksToCreate.Add(newChunkCoord);
            }
        }

        if (player != null)
        {
            player.SpawnPlayer(spawnLocation, BlockDatabase, this, GameManager);
        }

        CheckViewDistance();
    }

    /// <summary>
    /// Functionality used to generate and create the chunks needed, optimised to help with lag spikes.
    /// Generates a chunk per frame, instead of huge amounts of chunks in a single frame.
    /// </summary>
    /// <returns></returns>
    private IEnumerator CreateChunks()
    {
        isCreatingChunks = true;

        while (chunksToCreate.Count > 0)
        {
            ChunkCoord chunk = chunksToCreate[0];
            chunksToCreate.RemoveAt(0);
            chunks[chunk.x, chunk.z].CreateChunk();
            yield return null;
        }

        isCreatingChunks = false;
    }

    private void CreateChunk()
    {
        ChunkCoord chunk = chunksToCreate[0];
        chunksToCreate.RemoveAt(0);
        chunks[chunk.x, chunk.z].CreateChunk();
    }

    private void UpdateChunks()
    {
        bool updated = false;
        int index = 0;

        lock (ChunkUpdateThreadLock)
        {
            while (!updated && index < chunksToUpdate.Count)
            {
                if (chunksToUpdate[index].IsEditable)
                {
                    chunksToUpdate[index].UpdateChunk();
                    if(!activeChunks.Contains(chunksToUpdate[index].Coord))
                        activeChunks.Add(chunksToUpdate[index].Coord);

                    chunksToUpdate.RemoveAt(index);
                    updated = true;
                }
                else
                    index++;
            }
        }
    }

    private void ThreadedUpdate()
    {
        while (true)
        {
            if (chunksToUpdate.Count > 0)
                UpdateChunks();
        }
    }

    private void OnDisable()
    {
        if (GameManager.settings.enableThreading)
        {
            Debug.LogWarning("Aborting Update");
            ChunkUpdateThread.Abort();
        }
    }

    /// <summary>
    /// Returns the chunk co-ordinate of <paramref name="position"/>.
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    private ChunkCoord GetChunkCoordFromVector3(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x / VoxelData.ChunkSize.x);
        int z = Mathf.FloorToInt(position.z / VoxelData.ChunkSize.z);

        return new ChunkCoord(new int2(x, z));
    }

    //// <summary>
    /// Returns the chunk of <paramref name="position"/>.
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public Chunk GetChunkFromVector3(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x / VoxelData.ChunkSize.x);
        int z = Mathf.FloorToInt(position.z / VoxelData.ChunkSize.z);

        return chunks[x, z];
    }

    /// <summary>
    /// Updates chunks on the fly based off the players current position
    /// </summary>
    private void CheckViewDistance()
    {
        ChunkCoord chunkCoordinate = GetChunkCoordFromVector3(player.Position);
        playerLastChunkCoord = playerChunkCoord;

        List<ChunkCoord> previouslyActiveChunks = new List<ChunkCoord>(activeChunks);
        activeChunks.Clear();

        for (int x = chunkCoordinate.x - GameManager.settings.ViewDistanceInChunks; x < chunkCoordinate.x + GameManager.settings.ViewDistanceInChunks; x++)
        {
            for (int z = chunkCoordinate.z - GameManager.settings.ViewDistanceInChunks; z < chunkCoordinate.z + GameManager.settings.ViewDistanceInChunks; z++)
            {
                ChunkCoord newChunkCoord = new ChunkCoord(new int2(x, z));

                // Is the chunk inside the world bounds
                if (IsChunkInWorld(newChunkCoord))
                {
                    // If the chunk hasnt been generated then generate it 
                    if (chunks[x, z] == null)
                    {
                        // Create the new Chunk
                        chunks[x, z] = new Chunk(newChunkCoord, this, BlockDatabase, EntityManager, GameManager);
                        chunksToCreate.Add(newChunkCoord);
                    }
                    else if (!chunks[x, z].DetermineChunkActiveState)
                    {
                        chunks[x, z].DetermineChunkActiveState = true;
                    }
                    
                    activeChunks.Add(newChunkCoord);
                }

                // Any chunk that is in the view distance gets removed from previouslyActiveChunks
                for (int i = 0; i < previouslyActiveChunks.Count; i++)
                {
                    if (previouslyActiveChunks[i].Equals(newChunkCoord))
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
    public bool CheckForVoxel(Vector3 pos)
    {
        ChunkCoord thisChunk = new ChunkCoord(pos);

        //if (!IsChunkInWorld(thisChunk) || pos.y < 0 || pos.y > VoxelData.ChunkSize.y)
        //    return false;

        if (!IsVoxelInWorld(pos))
            return false;

        // Chunk is definetly in world bounds
        if (chunks[thisChunk.x, thisChunk.z] != null && chunks[thisChunk.x, thisChunk.z].IsEditable)
            return BlockDatabase.blockDatabase[chunks[thisChunk.x, thisChunk.z].GetVoxelFromGlobalVector3(pos).id].isSolidBlock;

        // If all else fails run expensive algarithim
        return BlockDatabase.blockDatabase[GetVoxel(pos)].isSolidBlock;
    }

    /// <summary>
    /// Checks to see if the voxel in the Vector3 space (<paramref name="x"/>, <paramref name="y"/>, <paramref name="z"/>) is transparent.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    public VoxelState GetVoxelState (Vector3 pos)
    {
        ChunkCoord thisChunk = new ChunkCoord(pos);

        if (!IsVoxelInWorld(pos))
            return null;

        // Chunk is definetly in world bounds
        if (chunks[thisChunk.x, thisChunk.z] != null && chunks[thisChunk.x, thisChunk.z].IsEditable)
            return chunks[thisChunk.x, thisChunk.z].GetVoxelFromGlobalVector3(pos);

        // If all else fails run expensive algarithim
        return new VoxelState(GetVoxel(pos));
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
            return 6;

        /* BIOME SELECTION */

        /// <summary>
        /// Below <see cref="solidGroundHeight"/> is always solid ground.
        /// </summary>
        int solidGroundHeight = 20;
        float sumOfHeights = 0;
        int count = 0;

        float strongestWeight = 0;
        int strongestBiomeIndex = 0;

        for (int i = 0; i < biomes.Length; i++)
        {
            float weight = VoxelData.Get2DPerlin(new Vector2(pos.x, pos.z), biomes[i].offset, biomes[i].scale);

            // Is the weight the strongest of all the biomes
            if(weight > strongestWeight)
            {
                strongestWeight = weight;
                strongestBiomeIndex = i;
            }

            // Get the height of the current terrain and multiply by the weight to apply smoothing between biomes
            float height = biomes[i].terrainHeight * VoxelData.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biomes[i].terrainScale) * weight;

            if(height > 0)
            {
                sumOfHeights += height;
                count++;
            }
        }

        // Set Biome
        BiomeAttributes biome = biomes[strongestBiomeIndex];

        // Get average heights
        sumOfHeights /= count;

        // Converts a 0 to 1 value into a percentage that gets multiplied by the heignt to create terrain
        int terrainHeight = Mathf.FloorToInt(sumOfHeights + solidGroundHeight);

        /* BASIC TERRAIN */

        byte voxelValue = 0;

        // If top block of chunk, return stone voxel ID. Else if heigher then top block return air voxel ID 
        if (yPos == terrainHeight)
            voxelValue = biome.surfaceBlockID;
        else if (yPos < terrainHeight && yPos > terrainHeight - 4)
            voxelValue = biome.subSurfaceBlockID;
        else if (yPos > terrainHeight)
            return 0;
        else
            voxelValue = 5;

        if ((voxelValue == biome.surfaceBlockID || voxelValue == biome.surfaceBlockID || voxelValue == 5) && biome.lodes.Length > 0)
        {
            foreach (Lode lode in biome.lodes)
            {
                if (yPos > lode.minHeight && yPos < lode.maxHeight)
                {
                    if (VoxelData.Get3DPerlin(pos, lode.noiseOffset, lode.scale, lode.threshold))
                        voxelValue = lode.blockID;
                }
            }
        }

        return voxelValue;
    }

    private bool IsChunkInWorld(ChunkCoord chunkCoord)
    {
        if (chunkCoord.x >= 0 && chunkCoord.x < VoxelData.worldSizeInChunks.x - 1 && chunkCoord.z >= 0 && chunkCoord.z < VoxelData.worldSizeInChunks.y - 1)
            return true;
        else
            return false;
    }

    private bool IsVoxelInWorld(Vector3 pos)
    {
        if (pos.x >= 0 && pos.x < WorldSizeInVoxels.x && pos.y >= 0 && pos.y < VoxelData.ChunkSize.y && pos.z >= 0 && pos.z < WorldSizeInVoxels.y)
            return true;
        else
            return false;

    }
}

[System.Serializable]
public class Settings
{
    [Header("Performance")]
    /// <summary>
    /// View distance for the player, if a chunk is further away then the ViewDistanceInChunks generation will not render.
    /// </summary>
    public int ViewDistanceInChunks = 3;
    public bool enableThreading = true;
    public bool enableChunkAnimation = false;
    public float chunkAnimationSpeed = 50;

    [Header("Controls")]
    [Range(1, 10)] public float mouseSensitivity = 2;
}
