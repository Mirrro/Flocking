using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct BoidUpdateJob : IJobEntity
{
    public float deltaTime;
    [ReadOnly] public SpawnerComponent spawner;
    [ReadOnly] public NativeArray<BoidComponent> boidComponents;
    [ReadOnly] public NativeArray<LocalTransform> boidTransforms;
    [NativeDisableParallelForRestriction] public NativeArray<Random> randomArray;

    void Execute(ref BoidComponent boid, ref LocalTransform transform, [EntityIndexInQuery] int index)
    {
        float3 position = transform.Position;
        float3 velocity = boid.velocity;

        float3 separation = float3.zero;
        float3 alignment = float3.zero;
        float3 cohesion = float3.zero;
        int neighborCount = 0;

        for (int i = 0; i < boidTransforms.Length; i++)
        {
            float3 otherPos = boidTransforms[i].Position;
            if (math.all(otherPos == position)) continue;

            float dist = math.distance(position, otherPos);
            if (dist < boid.neighborRadius)
            {
                float3 otherVel = boidComponents[i].velocity;
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
        }
        
        float3 homeDir = math.normalize(spawner.spawnPosition - position);
        float distance = math.distance(spawner.spawnPosition, position);
        float3 homeForce = homeDir * distance;
        
        Random rand = randomArray[index];
        float3 wander = rand.NextFloat3Direction();

        float3 acceleration =
            separation * boid.separationWeight +
            alignment * boid.alignmentWeight +
            cohesion * boid.cohesionWeight +
            homeForce * boid.homeAttractionWeight +
            wander * boid.wanderWeight;

        velocity += acceleration * deltaTime;

        float speed = math.length(velocity);
        if (speed > boid.maxSpeed)
        {
            velocity = math.normalize(velocity) * boid.maxSpeed;
        }

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
