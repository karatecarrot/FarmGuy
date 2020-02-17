using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Collections;
using Unity.Mathematics;

public class TestingChunkGeneration : MonoBehaviour
{
    private EntityManager entityManager;

    void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }

    // <summary>
    /// Converts generated voxel into unity Entity
    /// </summary>
    private void GenerateVoxelEntity()
    {
        var voxelArchetype = entityManager.CreateArchetype(
            typeof(RenderMesh),
            typeof(Translation),
            typeof(LocalToWorld)
            );

        NativeArray<Entity> entityArray = new NativeArray<Entity>(VoxelData.ChunkWidth * VoxelData.ChunkWidth, Allocator.Temp);
        entityManager.CreateEntity(voxelArchetype, entityArray);

        for (int i = 0; i < entityArray.Length; i++)
        {
            Entity currentEntity = entityArray[i];
            //entityManager.SetName(currentEntity, "Chunk " + chunkCoord.x + ", " + chunkCoord.z);
            entityManager.SetName(currentEntity, "Chunk " + currentEntity.Index);
        }

        entityArray.Dispose();

        //voxelEntity = entityManager.CreateEntity(voxelArchetype);
        //entityManager.SetName(voxelEntity, "Chunk " + chunkCoord.x + ", " + chunkCoord.z);

        //entityManager.SetComponentData(voxelEntity, new Translation
        //{
        //    Value = new float3(chunkCoord.x * VoxelData.ChunkWidth, 0, chunkCoord.z * VoxelData.ChunkWidth)
        //});
    }
}
