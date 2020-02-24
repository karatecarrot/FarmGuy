using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;

public class ChunkLoadAnimation : ComponentSystem
{
    protected override void OnCreate()
    {
        Entities.ForEach((ref Translation translation, ref LoadAnimationSpeedComponent moveSpeed) =>
        {
            translation.Value.y = -VoxelData.ChunkSize.y;
        });
    }

    protected override void OnUpdate()
    {
        Entities.ForEach((ref Translation translation, ref LoadAnimationSpeedComponent moveSpeed) =>
        {
            if (moveSpeed.beginUpdate)
            {
                translation.Value.y = -100;
                moveSpeed.beginUpdate = false;
            }
            else
            {
                if (moveSpeed.timer < moveSpeed.waitTimer)
                {
                    moveSpeed.timer += Time.DeltaTime;
                }
                else
                {
                    translation.Value.y += moveSpeed.animationSpeed * Time.DeltaTime;

                    if ((moveSpeed.targetPos.y - translation.Value.y) < 0.05f)
                    {
                        translation.Value = moveSpeed.targetPos;
                        EntityManager.RemoveComponent(moveSpeed.entity, typeof(LoadAnimationSpeedComponent));
                    }
                }
            }
        });
    }
}