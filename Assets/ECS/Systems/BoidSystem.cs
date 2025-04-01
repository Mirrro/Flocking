using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
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

        Entity spawnerEntity = SystemAPI.GetSingletonEntity<SpawnerComponent>();
        SpawnerComponent spawner = SystemAPI.GetComponent<SpawnerComponent>(spawnerEntity);

        var query = SystemAPI.QueryBuilder()
            .WithAll<BoidComponent, LocalTransform>()
            .Build();

        var boidComponents = query.ToComponentDataArray<BoidComponent>(Allocator.TempJob);
        var boidTransforms = query.ToComponentDataArray<LocalTransform>(Allocator.TempJob);

        int boidCount = boidComponents.Length;
        var randomArray = new NativeArray<Unity.Mathematics.Random>(boidCount, Allocator.TempJob);

        uint seed = (uint)(SystemAPI.Time.ElapsedTime * 1000);
        for (int i = 0; i < boidCount; i++)
        {
            randomArray[i] = new Unity.Mathematics.Random(seed + (uint)i);
        }

        var job = new BoidUpdateJob
        {
            deltaTime = deltaTime,
            spawner = spawner,
            boidComponents = boidComponents,
            boidTransforms = boidTransforms,
            randomArray = randomArray
        };

        state.Dependency = job.ScheduleParallel(state.Dependency);

        boidComponents.Dispose(state.Dependency);
        boidTransforms.Dispose(state.Dependency);
        randomArray.Dispose(state.Dependency);
    }
}
