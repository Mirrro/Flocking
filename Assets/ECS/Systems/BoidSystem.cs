using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct BoidSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SpawnerComponent>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        float cellSize = .1f;

        Entity spawnerEntity = SystemAPI.GetSingletonEntity<SpawnerComponent>();
        SpawnerComponent spawner = SystemAPI.GetComponent<SpawnerComponent>(spawnerEntity);

        var query = SystemAPI.QueryBuilder()
            .WithAll<BoidComponent, LocalTransform>()
            .Build();

        var entities = query.ToEntityArray(Allocator.TempJob);
        var transforms = query.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
        var boidComponents = query.ToComponentDataArray<BoidComponent>(Allocator.TempJob);

        var entityToIndexMap = new NativeHashMap<Entity, int>(entities.Length, Allocator.TempJob);
        for (int i = 0; i < entities.Length; i++)
        {
            entityToIndexMap.TryAdd(entities[i], i);
        }

        var spatialHashMap = new NativeParallelMultiHashMap<int, Entity>(entities.Length, Allocator.TempJob);

        var buildHashJob = new BuildSpatialHashMapJob
        {
            entities = entities,
            transforms = transforms,
            spatialHashMap = spatialHashMap.AsParallelWriter(),
            cellSize = cellSize
        };

        var buildHandle = buildHashJob.Schedule(entities.Length, 64, state.Dependency);

        var updateJob = new BoidUpdateJob
        {
            deltaTime = deltaTime,
            spawner = spawner,
            transforms = transforms,
            boidComponents = boidComponents,
            spatialHashMap = spatialHashMap,
            entityToIndexMap = entityToIndexMap,
            cellSize = cellSize
        };

        state.Dependency = updateJob.ScheduleParallel(query, buildHandle);

        entities.Dispose(state.Dependency);
        transforms.Dispose(state.Dependency);
        boidComponents.Dispose(state.Dependency);
        spatialHashMap.Dispose(state.Dependency);
        entityToIndexMap.Dispose(state.Dependency);
    }
}


[BurstCompile]
public struct BuildSpatialHashMapJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<Entity> entities;
    [ReadOnly] public NativeArray<LocalTransform> transforms;
    public NativeParallelMultiHashMap<int, Entity>.ParallelWriter spatialHashMap;
    public float cellSize;

    public void Execute(int index)
    {
        float3 pos = transforms[index].Position;
        int3 cell = SpatialHash.GridPosition(pos, cellSize);
        int hash = SpatialHash.Hash(cell);

        spatialHashMap.Add(hash, entities[index]);
    }
}


public static class SpatialHash
{
    public static int3 GridPosition(float3 position, float cellSize)
    {
        return (int3)math.floor(position / cellSize);
    }

    public static int Hash(int3 gridPos)
    {
        unchecked
        {
            return gridPos.x * 73856093 ^ gridPos.y * 19349663 ^ gridPos.z * 83492791;
        }
    }
}
