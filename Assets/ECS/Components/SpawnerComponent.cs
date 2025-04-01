using Unity.Entities;
using Unity.Mathematics;

public struct SpawnerComponent : IComponentData
{
    public Entity prefab;
    public float3 spawnPosition;
    public int spawnCount;
}
