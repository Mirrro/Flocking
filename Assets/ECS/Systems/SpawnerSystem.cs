using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;

[BurstCompile]
public partial struct SpawnerSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingletonEntity<SpawnerComponent>(out Entity entity))
            return;

        RefRW<SpawnerComponent> spawnerComponent = SystemAPI.GetComponentRW<SpawnerComponent>(entity);
        var entityManager = state.EntityManager;
        
        int currentBoidCount = SystemAPI.QueryBuilder()
            .WithAll<BoidComponent>()
            .Build()
            .CalculateEntityCount();

        int desiredBoidCount = spawnerComponent.ValueRO.spawnCount;
        int delta = desiredBoidCount - currentBoidCount;

        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        if (delta > 0)
        {
            for (int i = 0; i < delta; i++)
            {
                Entity newBoid = ecb.Instantiate(spawnerComponent.ValueRO.prefab);

                float3 velocity = UnityEngine.Random.insideUnitSphere * 1.5f;
                float3 spawnOffset = new float3(
                    UnityEngine.Random.Range(-1f, 1f),
                    UnityEngine.Random.Range(-1f, 1f),
                    UnityEngine.Random.Range(-1f, 1f)
                );

                ecb.AddComponent(newBoid, new BoidComponent
                {
                    velocity = velocity,
                    separationWeight = 1.5f,
                    alignmentWeight = 1.0f,
                    cohesionWeight = 1.0f,
                    wanderWeight = 1.0f,
                    homeAttractionWeight = 1f,
                    neighborRadius = .5f,
                    maxSpeed = UnityEngine.Random.Range(1, 1.4f)
                });

                ecb.SetComponent(newBoid, LocalTransform.FromPosition(
                    spawnerComponent.ValueRO.spawnPosition + spawnOffset
                ));
            }
        }
        else if (delta < 0)
        {
            var boidQuery = SystemAPI.QueryBuilder()
                .WithAll<BoidComponent>()
                .Build();

            var boidEntities = boidQuery.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < math.min(-delta, boidEntities.Length); i++)
            {
                ecb.DestroyEntity(boidEntities[i]);
            }

            boidEntities.Dispose();
        }

        ecb.Playback(entityManager);
        ecb.Dispose();
    }
}
