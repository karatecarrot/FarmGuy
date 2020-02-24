using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public struct LoadAnimationSpeedComponent : IComponentData
{
    public Entity entity;
    public float animationSpeed;
    public float3 targetPos;

    public bool beginUpdate;
    public float waitTimer;
    public float timer;
}
