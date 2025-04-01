using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct BoidUpdateJob : IJobEntity
{
    public float deltaTime;
    public float cellSize;
    [ReadOnly] public SpawnerComponent spawner;

    [ReadOnly] public NativeArray<BoidComponent> boidComponents;
    [ReadOnly] public NativeArray<LocalTransform> transforms;
    [ReadOnly] public NativeParallelMultiHashMap<int, Entity> spatialHashMap;
    [ReadOnly] public NativeHashMap<Entity, int> entityToIndexMap;

    public void Execute(ref BoidComponent boid, ref LocalTransform transform, [EntityIndexInQuery] int index, Entity self)
    {
        float3 position = transform.Position;
        float3 velocity = boid.velocity;

        float3 separation = float3.zero;
        float3 alignment = float3.zero;
        float3 cohesion = float3.zero;
        int neighborCount = 0;

        int3 myCell = SpatialHash.GridPosition(position, cellSize);

        for (int x = -1; x <= 1; x++)
        for (int y = -1; y <= 1; y++)
        for (int z = -1; z <= 1; z++)
        {
            int3 neighborCell = myCell + new int3(x, y, z);
            int hash = SpatialHash.Hash(neighborCell);

            if (spatialHashMap.TryGetFirstValue(hash, out Entity neighbor, out var it))
            {
                do
                {
                    if (neighbor == self || !entityToIndexMap.TryGetValue(neighbor, out int neighborIndex))
                        continue;

                    float3 otherPos = transforms[neighborIndex].Position;
                    float3 otherVel = boidComponents[neighborIndex].velocity;

                    float dist = math.distance(position, otherPos);
                    if (dist < boid.neighborRadius)
                    {
                        separation += (position - otherPos) / math.max(dist, 0.001f);
                        alignment += otherVel;
                        cohesion += otherPos;
                        neighborCount++;
                    }


                } while (spatialHashMap.TryGetNextValue(out neighbor, ref it));
            }
        }

        if (neighborCount > 0)
        {
            separation /= neighborCount;
            alignment /= neighborCount;
            cohesion = (cohesion / neighborCount) - position;
        }

        float3 toSpawner = spawner.spawnPosition - position;
        float3 homeForce = math.normalize(toSpawner) * math.length(toSpawner);
        float3 wanderDirection = noise.cnoise(position);
        float3 acceleration =
            separation * boid.separationWeight +
            alignment * boid.alignmentWeight +
            cohesion * boid.cohesionWeight +
            homeForce * boid.homeAttractionWeight +
            math.normalize(wanderDirection) * boid.wanderWeight;

        velocity += acceleration * deltaTime;

        float speed = math.length(velocity);
        if (speed > boid.maxSpeed)
            velocity = math.normalize(velocity) * boid.maxSpeed;

        position += velocity * deltaTime;

        boid.velocity = velocity;
        transform.Position = position;

        if (!math.all(velocity == float3.zero))
        {
            float3 forward = math.normalize(velocity);
            quaternion targetRotation = quaternion.LookRotationSafe(forward, math.up());
            transform.Rotation = math.slerp(transform.Rotation, targetRotation, deltaTime * 5f);
        }
    }
}