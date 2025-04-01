using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;

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
        
        var boidQuery = SystemAPI.QueryBuilder().WithAll<BoidComponent, LocalTransform>().Build();

        var boidComponents = boidQuery.ToComponentDataArray<BoidComponent>(Allocator.TempJob);
        var transforms = boidQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
        Entity spawnerEntity = SystemAPI.GetSingletonEntity<SpawnerComponent>();
        SpawnerComponent spawner = SystemAPI.GetComponent<SpawnerComponent>(spawnerEntity);

        foreach (var (boid, transform) in SystemAPI.Query<RefRW<BoidComponent>, RefRW<LocalTransform>>())
        {
            float3 position = transform.ValueRO.Position;
            float3 velocity = boid.ValueRO.velocity;

            float3 separation = float3.zero;
            float3 alignment = float3.zero;
            float3 cohesion = float3.zero;

            int neighborCount = 0;

            for (int i = 0; i < boidComponents.Length; i++)
            {
                float3 otherPos = transforms[i].Position;
                float3 otherVel = boidComponents[i].velocity;

                if (math.all(otherPos == position)) continue;

                float dist = math.distance(position, otherPos);
                if (dist < boid.ValueRO.neighborRadius)
                {
                    separation += (position - otherPos) / math.max(dist, 0.001f);
                    alignment += otherVel;
                    cohesion += otherPos;
                    neighborCount++;
                }
            }

            if (neighborCount > 0)
            {
                separation /= neighborCount;
                alignment /= neighborCount;
                cohesion = (cohesion / neighborCount) - position;

                float3 toSpawner = spawner.spawnPosition - position;
                float distance = math.length(toSpawner);

                float3 homeForce = math.normalize(toSpawner) * distance * distance;
                
                float3 randomDir = UnityEngine.Random.insideUnitSphere;
                float3 wanderForce = randomDir;
                
                float3 acceleration =
                    separation * boid.ValueRO.separationWeight +
                    alignment * boid.ValueRO.alignmentWeight +
                    cohesion * boid.ValueRO.cohesionWeight +
                    homeForce * boid.ValueRO.homeAttractionWeight +
                    wanderForce * boid.ValueRO.wanderWeight;

                velocity += acceleration * deltaTime;

                float speed = math.length(velocity);
                if (speed > boid.ValueRO.maxSpeed)
                {
                    velocity = math.normalize(velocity) * boid.ValueRO.maxSpeed;
                }

                position += velocity * deltaTime;
                
                boid.ValueRW.velocity = velocity;
                transform.ValueRW.Position = position;
            }
            
            if (!math.all(velocity == float3.zero))
            {
                float3 forward = math.normalize(velocity);
                float3 up = math.up();

                quaternion currentRotation = transform.ValueRO.Rotation;
                quaternion targetRotation = quaternion.LookRotationSafe(forward, up);

                float turnSpeed = 5f;
                quaternion smoothedRotation = math.slerp(currentRotation, targetRotation, deltaTime * turnSpeed);

                transform.ValueRW.Rotation = smoothedRotation;
            }
        }

        boidComponents.Dispose();
        transforms.Dispose();
    }
}
