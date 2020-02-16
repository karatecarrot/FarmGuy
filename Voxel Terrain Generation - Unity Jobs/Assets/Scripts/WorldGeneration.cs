using UnityEngine;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using System.Collections.Generic;
using Unity.Mathematics;

public class WorldGeneration
{
    public ChunkCoord chunkCoord;

    private EntityManager entityManager;
    private Entity voxelEntity;
    
    private int vertexIndex = 0;

    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private List<Vector2> uvs = new List<Vector2>();

    private WorldData worldData;

    public WorldGeneration(ChunkCoord _ChunkCoord, WorldData _WorldData, EntityManager _EntityManager)
    {
        entityManager = _EntityManager;
        chunkCoord = _ChunkCoord;
        worldData = _WorldData;

        GenerateVoxelEntity();

        CreateChunkData();
    }

    /// <summary>
    /// Is the given voxel true or false
    /// <para> If true then there is a block in selected place, else there is no block </para>
    /// </summary>
    private byte[,,] voxelMap = new byte[VoxelData.ChunkWidth, VoxelData.ChunkHeight, VoxelData.ChunkWidth];

    private void PopulateVoxelMap()
    {
        for (int x = 0; x < VoxelData.ChunkWidth; x++)
        {
            for (int y = 0; y < VoxelData.ChunkHeight; y++)
            {
                for (int z = 0; z < VoxelData.ChunkWidth; z++)
                {
                    voxelMap[x, y, z] = worldData.GetVoxel(new Vector3(x, y, z) + ChunkPosition);
                }
            }
        }
    }

    /// <summary>
    /// Get/Set the entities active state 
    /// </summary>
    public bool DetermineChunkActiveState
    {
        get { return entityManager.GetEnabled(voxelEntity); }
        set { entityManager.SetEnabled(voxelEntity, value); }
    }

    /// <summary>
    /// Access position data from chunk entity
    /// </summary>
    public Vector3 ChunkPosition
    {
        get { return entityManager.GetComponentData<Translation>(voxelEntity).Value; }
    }

    /// <summary>
    /// Checks if <paramref name="positionToCheck"/> is currently occupied by a voxel.
    /// If returns true then there is already a voxel in <paramref name="positionToCheck"/>
    /// <para>Essentially if the face of the voxel is blocked off out of view then don't render it. Only draws the outside of the chunk</para>
    /// </summary>
    /// <param name="positionToCheck"></param>
    /// <returns></returns>
    private bool CheckVoxel(Vector3 positionToCheck)
    {
        int x = Mathf.FloorToInt(positionToCheck.x);
        int y = Mathf.FloorToInt(positionToCheck.y);
        int z = Mathf.FloorToInt(positionToCheck.z);

        if (!IsVoxelInChunk(x, y, z))
        {
            //return false;
            return worldData.blockTypes[worldData.GetVoxel(positionToCheck + ChunkPosition)].isSolid;
        }

        return worldData.blockTypes[voxelMap[x, y, z]].isSolid;
    }

    /// <summary>
    /// Checks the local chunk to see if the containing voxels are next to other voxels.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    private bool IsVoxelInChunk(int x, int y, int z)
    {
        // If its not in the chunk return false
        if (x < 0 || x > VoxelData.ChunkWidth - 1 || y < 0 || y > VoxelData.ChunkHeight - 1 || z < 0 || z > VoxelData.ChunkWidth - 1)
            return false;
        else
            return true;
    }

    public void CreateChunkData()
    {
        PopulateVoxelMap();

        for (int x = 0; x < VoxelData.ChunkWidth; x++)
        {
            for (int y = 0; y < VoxelData.ChunkHeight; y++)
            {
                for (int z = 0; z < VoxelData.ChunkWidth; z++)
                {
                    AddVoxelDataToChunk(new Vector3(x, y, z));
                }
            }
        }

        VoxelEntityData();
    }

    /// <summary>
    /// Adds voxel to a chunk with <paramref name="positionInWorld"/> being the reletive starting position of the voxel 
    /// </summary>
    /// <param name="positionInWorld"></param>
    private void AddVoxelDataToChunk(Vector3 positionInWorld)
    {
        for (byte face = 0; face < 6; face++)
        {
            if (!CheckVoxel(positionInWorld + VoxelData.faceChecks[face]))
            {
                #region New Method

                byte blockID = voxelMap[(int)positionInWorld.x, (int)positionInWorld.y, (int)positionInWorld.z];

                vertices.Add(positionInWorld + VoxelData.voxelVerts[VoxelData.voxelTris[face, 0]]);
                vertices.Add(positionInWorld + VoxelData.voxelVerts[VoxelData.voxelTris[face, 1]]);
                vertices.Add(positionInWorld + VoxelData.voxelVerts[VoxelData.voxelTris[face, 2]]);
                vertices.Add(positionInWorld + VoxelData.voxelVerts[VoxelData.voxelTris[face, 3]]);

                AddTextureToFace(worldData.blockTypes[blockID].GetTextureID(face));

                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 3);

                vertexIndex += 4;

                #endregion
            }
        }
    }

    /// <summary>
    /// Adds data and components to <see cref="voxelEntity"/>
    /// </summary>
    private void VoxelEntityData()
    {
        Mesh voxelMesh = GenerateVoxelMesh();

        entityManager.SetSharedComponentData(voxelEntity, new RenderMesh
        {
            mesh = voxelMesh,
            material = worldData.voxelMaterialSheet
        });
    }

    /// <summary>
    /// Converts generated voxel into unity Entity
    /// </summary>
    private void GenerateVoxelEntity()
    {
        var voxelArchetype = entityManager.CreateArchetype(
            typeof(RenderMesh),
            typeof(Translation),
            typeof(LocalToWorld)
            );

        voxelEntity = entityManager.CreateEntity(voxelArchetype);
        entityManager.SetName(voxelEntity, "Chunk " + chunkCoord.x + ", " + chunkCoord.z);

        entityManager.SetComponentData(voxelEntity, new Translation
        {
            Value = new float3(chunkCoord.x * VoxelData.ChunkWidth, 0, chunkCoord.z * VoxelData.ChunkWidth)
        });
    }

    private Mesh GenerateVoxelMesh()
    {
        Mesh voxelMesh = new Mesh();
        voxelMesh.name = "Voxel Cube";

        voxelMesh.vertices = vertices.ToArray();
        voxelMesh.triangles = triangles.ToArray();
        voxelMesh.uv = uvs.ToArray();

        voxelMesh.RecalculateNormals();

        return voxelMesh;
    }

    /// <summary>
    /// Adds texture to the face based off <paramref name="textureID"/>. The ID is calculated from top left corner to bottem left corner, going left to right.
    /// </summary>
    /// <param name="textureID"></param>
    private void AddTextureToFace(int textureID)
    {
        float y = textureID / VoxelData.TextureAtlasSizeInBlocks;
        float x = textureID - (y * VoxelData.TextureAtlasSizeInBlocks);

        x *= VoxelData.NormalizedBlockTextureSize;
        y *= VoxelData.NormalizedBlockTextureSize;

        //Reverse atlas to start from top left corner instead of bottom left corner
        y = 1f - y - VoxelData.NormalizedBlockTextureSize;

        uvs.Add(new Vector2(x, y));
        uvs.Add(new Vector2(x, y + VoxelData.NormalizedBlockTextureSize));
        uvs.Add(new Vector2(x + VoxelData.NormalizedBlockTextureSize, y));
        uvs.Add(new Vector2(x + VoxelData.NormalizedBlockTextureSize, y + VoxelData.NormalizedBlockTextureSize));
    }
}

public class ChunkCoord
{
    public int x;
    public int z;

    public ChunkCoord(int _x, int _z)
    {
        x = _x;
        z = _z;
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

