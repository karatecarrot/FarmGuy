using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Rendering;
using Unity.Mathematics;
using Unity.Transforms;
//using System.Threading;

public class Chunk
{
    private EntityManager entityManager;
    private BlockDatabase blockDatabase;
    private WorldGenerator worldGenerator;
    private GameManager gameManager;

    private Entity chunkEntity;
    private bool isChunkActive;

    public ChunkCoord Coord;
    public Vector3 chunkPosition;

    public VoxelState[,,] voxelMaps = new VoxelState[VoxelData.ChunkSize.x, VoxelData.ChunkSize.y, VoxelData.ChunkSize.z];
    private bool isVoxelMapPopulated = false;

    public Chunk (ChunkCoord chunkCoord, WorldGenerator world, BlockDatabase database, EntityManager manager, GameManager GM)
    {
        Coord = chunkCoord;
        blockDatabase = database;
        entityManager = manager;
        worldGenerator = world;
        gameManager = GM;

        // Create the entity
        InstantiateChunk(new int3(Coord.x * VoxelData.ChunkSize.x, 0, Coord.z * VoxelData.ChunkSize.z));
    }

    public void CreateChunk()
    {
        // Populate the voxelMaps[]
        PopulateVoxelMap();

        // Create the chunk data used for mesh ganeration
        //UpdateChunk();
    }

    #region Chunk Generation
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<Vector2> uvs = new List<Vector2>();
    List<Color> colors = new List<Color>();
    List<Vector3> normals = new List<Vector3>();
    int vertexIndex = 0;

    private void PopulateVoxelMap()
    {
        for (int x = 0; x < VoxelData.ChunkSize.x; x++)
        {
            for (int z = 0; z < VoxelData.ChunkSize.z; z++)
            {
                for (int y = 0; y < VoxelData.ChunkSize.y; y++)
                {
                    voxelMaps[x, y, z] = new VoxelState(worldGenerator.GetVoxel(new Vector3(x, y, z) + chunkPosition));
                }
            }
        }

        isVoxelMapPopulated = true;

        lock (worldGenerator.ChunkUpdateThreadLock)
        {
            worldGenerator.chunksToUpdate.Add(this);
        }
    }

    public void UpdateChunk()
    {
        ClearMeshData();
        CalculateLight();

        for (int x = 0; x < VoxelData.ChunkSize.x; x++)
        {
            for (int z = 0; z < VoxelData.ChunkSize.z; z++)
            {
                for (int y = 0; y < VoxelData.ChunkSize.y; y++)
                {
                    if(blockDatabase.blockDatabase[voxelMaps[x, y, z].id].isSolidBlock)
                        UpdateMeshData(new Vector3(x, y, z));
                }
            }
        }

        lock (worldGenerator.chunksToDraw)
        {
            worldGenerator.chunksToDraw.Enqueue(this);
        }

        // Create the mesh based off the data.
        //CreateMesh(blockDatabase.blockMaterialAtlas);
    }

    private void CalculateLight()
    {
        Queue<Vector3Int> litVoxels = new Queue<Vector3Int>();

        for (int x = 0; x < VoxelData.ChunkSize.x; x++)
        {
            for (int z = 0; z < VoxelData.ChunkSize.z; z++)
            {
                float lightRay = 1;

                for (int y = VoxelData.ChunkSize.y - 1; y >= 0; y--)
                {
                    VoxelState thisVoxel = voxelMaps[x, y, z];

                    if (thisVoxel.id > 0 && blockDatabase.blockDatabase[thisVoxel.id].transparency < lightRay)
                        lightRay = blockDatabase.blockDatabase[thisVoxel.id].transparency;

                    thisVoxel.globalLightPercent = lightRay;

                    voxelMaps[x, y, z] = thisVoxel;

                    if (lightRay > VoxelData.lightFalloff)
                        litVoxels.Enqueue(new Vector3Int(x, y, z));
                }
            }
        }

        while (litVoxels.Count > 0)
        {
            Vector3Int voxel = litVoxels.Dequeue();

            for (int p = 0; p < 6; p++)
            {
                Vector3 currentVoxel = voxel + VoxelData.faceChecks[p];
                Vector3Int neighbor = new Vector3Int((int)currentVoxel.x, (int)currentVoxel.y, (int)currentVoxel.z);

                if(IsVoxelInChunk(new int3(neighbor.x, neighbor.y, neighbor.z)))
                {
                    if(voxelMaps[neighbor.x, neighbor.y, neighbor.z].globalLightPercent < voxelMaps[voxel.x, voxel.y, voxel.z].globalLightPercent - VoxelData.lightFalloff)
                    {
                        voxelMaps[neighbor.x, neighbor.y, neighbor.z].globalLightPercent = voxelMaps[voxel.x, voxel.y, voxel.z].globalLightPercent - VoxelData.lightFalloff;

                        if (voxelMaps[neighbor.x, neighbor.y, neighbor.z].globalLightPercent > VoxelData.lightFalloff)
                            litVoxels.Enqueue(neighbor);
                    }
                }
            }
        }
    }

    private void ClearMeshData ()
    {
        vertexIndex = 0;
        vertices.Clear();
        triangles.Clear();
        colors.Clear();
        normals.Clear();
        uvs.Clear();
    }

    /// <summary>
    /// Get/Set the entities active state 
    /// </summary>
    public bool DetermineChunkActiveState
    {
        get { return isChunkActive; }
        set
        {
            if (chunkEntity != null)
            {
                isChunkActive = value;
                entityManager.SetEnabled(chunkEntity, value);
            }
        }
    }

    public bool IsEditable
    {
        get
        {
            if (!isVoxelMapPopulated)
            {
                return false;
            }
            else
                return true;
        }
    }

    /// <summary>
    /// Is the voxel in a chunk relative to the local co ordinates of the chunk. Checks the local chunk to see if the containing voxels are next to other voxels.
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    private bool IsVoxelInChunk(int3 pos)
    {
        if (pos.x < 0 || pos.x > VoxelData.ChunkSize.x - 1 || pos.y < 0 || pos.y > VoxelData.ChunkSize.y - 1 || pos.z < 0 || pos.z > VoxelData.ChunkSize.z - 1)
            return false;
        else
            return true;
    }

    /// <summary>
    /// Edits the voxel of <paramref name="positionToCheck"/>, and sets its new value to <paramref name="newID"/>.
    /// </summary>
    /// <param name="positionToCheck"></param>
    /// <param name="newID"></param>
    public void EditVoxel(Vector3 positionToCheck, byte newID)
    {
        int xCheck = Mathf.FloorToInt(positionToCheck.x);
        int yCheck = Mathf.FloorToInt(positionToCheck.y);
        int zCheck = Mathf.FloorToInt(positionToCheck.z);

        xCheck -= Mathf.FloorToInt(chunkPosition.x);
        zCheck -= Mathf.FloorToInt(chunkPosition.z);

        voxelMaps[xCheck, yCheck, zCheck].id = newID;

        lock (worldGenerator.ChunkUpdateThreadLock)
        {
            // Adds chunk to the top of the list to Update
            worldGenerator.chunksToUpdate.Insert(0, this);

            // Update Surrounding Chunks
            UpdateSurroundingVoxels(xCheck, yCheck, zCheck);
        }
    }

    /// <summary>
    /// Updates the surrounding blocks so that there are no gaps and missing faces of voxels
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    private void UpdateSurroundingVoxels(int x, int y, int z)
    {
        Vector3 thisVoxel = new Vector3(x, y, z);

        for (int p = 0; p < 6; p++)
        {
            Vector3 currentVoxel = thisVoxel + VoxelData.faceChecks[p];

            if (!IsVoxelInChunk(new int3((int)currentVoxel.x, (int)currentVoxel.y, (int)currentVoxel.z)))
            {
                worldGenerator.chunksToUpdate.Insert(0, worldGenerator.GetChunkFromVector3(currentVoxel + chunkPosition));
            }
        }
    }

    /// <summary>
    /// Checks if <paramref name="positionToCheck"/> is currently occupied by a voxel.
    /// If returns true then there is already a voxel in <paramref name="positionToCheck"/>
    /// <para>Essentially if the face of the voxel is blocked off out of view then don't render it. Only draws the outside of the chunk</para>
    /// </summary>
    /// <param name="positionToCheck"></param>
    /// <returns></returns>
    private VoxelState CheckVoxel(Vector3 positionToCheck)
    {
        int x = Mathf.FloorToInt(positionToCheck.x);
        int y = Mathf.FloorToInt(positionToCheck.y);
        int z = Mathf.FloorToInt(positionToCheck.z);

        if (!IsVoxelInChunk(new int3(x, y, z)))
        {
            return worldGenerator.GetVoxelState(positionToCheck + chunkPosition);
        }

        return voxelMaps[x, y, z];
    }

    /// <summary>
    /// Called from other scripts that gets a voxel and data from a vector3 position (<paramref name="positionToCheck"/>).
    /// Gets the position of a voxel relative to the chunk it is in.
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public VoxelState GetVoxelFromGlobalVector3(Vector3 positionToCheck)
    {
        int xCheck = Mathf.FloorToInt(positionToCheck.x);
        int yCheck = Mathf.FloorToInt(positionToCheck.y);
        int zCheck = Mathf.FloorToInt(positionToCheck.z);

        xCheck -= Mathf.FloorToInt(chunkPosition.x);
        zCheck -= Mathf.FloorToInt(chunkPosition.z);

        return voxelMaps[xCheck, yCheck, zCheck];
    }

    private void UpdateMeshData(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        byte blockID = voxelMaps[x, y, z].id;

        // Make Face
        for (int p = 0; p < 6; p++)
        {
            VoxelState neighborBlock = CheckVoxel(pos + VoxelData.faceChecks[p]);

            if (neighborBlock != null && blockDatabase.blockDatabase[neighborBlock.id].renderSurroundingFaces)
            {
                vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 0]]);
                vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 1]]);
                vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 2]]);
                vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 3]]);

                for (int i = 0; i < 4; i++)
                    normals.Add(VoxelData.faceChecks[p]);

                AddTexture(blockDatabase.blockDatabase[blockID].GetTextureID(p));

                float lightLevel = neighborBlock.globalLightPercent;

                colors.Add(new Color(0, 0, 0, lightLevel));
                colors.Add(new Color(0, 0, 0, lightLevel));
                colors.Add(new Color(0, 0, 0, lightLevel));
                colors.Add(new Color(0, 0, 0, lightLevel));

                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 3);

                vertexIndex += 4;
            }
        }
    }

    private void AddTexture(int textureID)
    {
        float y = textureID / VoxelData.TextureAtlasSizeInBlocks;
        float x = textureID - (y * VoxelData.TextureAtlasSizeInBlocks);

        x *= VoxelData.NormalizesTextureSize;
        y *= VoxelData.NormalizesTextureSize;

        y = 1f - y - VoxelData.NormalizesTextureSize;

        uvs.Add(new Vector2(x, y));
        uvs.Add(new Vector2(x, y + VoxelData.NormalizesTextureSize));
        uvs.Add(new Vector2(x + VoxelData.NormalizesTextureSize, y));
        uvs.Add(new Vector2(x + VoxelData.NormalizesTextureSize, y + VoxelData.NormalizesTextureSize));
    }

    public void CreateMesh(Material voxelMaterial)
    {
        if (blockDatabase.blockMaterialAtlas == null)
        {
            Debug.LogWarning("A Material is missing on BlockDatabase");
            return;
        }

        // Update Mesh
        Mesh voxelMesh = new Mesh();
        voxelMesh.name = "Voxel Cube";

        voxelMesh.vertices = vertices.ToArray();
        voxelMesh.triangles = triangles.ToArray();
        voxelMesh.uv = uvs.ToArray();
        voxelMesh.colors = colors.ToArray();
        voxelMesh.normals = normals.ToArray();

        //voxelMesh.RecalculateNormals();
        //voxelMesh.Optimize();

        entityManager.SetSharedComponentData(chunkEntity, new RenderMesh
        {
            mesh = voxelMesh,
            material = voxelMaterial,
        });
    }

    private void InstantiateChunk(int3 positionToSpawn)
    {
        EntityArchetype archetype;

        if (gameManager.settings.chunkAnimationSpeed == 0)
        {
           archetype = entityManager.CreateArchetype(
           typeof(Translation),
           typeof(LocalToWorld),
           typeof(RenderMesh)
           );
        }
        else
        {
           archetype = entityManager.CreateArchetype(
           typeof(Translation),
           typeof(LocalToWorld),
           typeof(RenderMesh),
           typeof(LoadAnimationSpeedComponent)
           );
        }

        chunkEntity = entityManager.CreateEntity(archetype);
        //entityManager.SetName(chunkEntity, "Chunk: " + positionToSpawn.x + ", " + positionToSpawn.z);

        if (entityManager.HasComponent(chunkEntity, typeof(LoadAnimationSpeedComponent)))
        {
            entityManager.SetComponentData(chunkEntity, new LoadAnimationSpeedComponent
            {
                entity = chunkEntity,
                animationSpeed = gameManager.settings.chunkAnimationSpeed,
                targetPos = positionToSpawn,
                beginUpdate = true,
                waitTimer = UnityEngine.Random.Range(0, 3)
            });
        }

        entityManager.SetComponentData(chunkEntity, new Translation { Value = positionToSpawn });

        chunkPosition = new Vector3(positionToSpawn.x, positionToSpawn.y, positionToSpawn.z);
    }

    #endregion
}

public class ChunkCoord
{
    public int x;
    public int z;

    public ChunkCoord()
    {
        x = 0;
        z = 0;
    }

    public ChunkCoord(int2 pos)
    {
        x = pos.x;
        z = pos.y;
    }

    public ChunkCoord(Vector3 pos)
    {
        int xCheck = Mathf.FloorToInt(pos.x);
        int zCheck = Mathf.FloorToInt(pos.z);

        x = xCheck / VoxelData.ChunkSize.x;
        z = zCheck / VoxelData.ChunkSize.z;
    }

    public bool Equals(ChunkCoord comparedTo)
    {
        if (comparedTo == null)
            return false;
        else if (comparedTo.x == x && comparedTo.z == z)
            return true;
        else
            return false;
    }
}

public class VoxelState
{
    public byte id;
    public float globalLightPercent;

    public VoxelState ()
    {
        id = 0;
        globalLightPercent = 0;
    }

    public VoxelState (byte _ID)
    {
        id = _ID;
        globalLightPercent = 0;
    }
}
