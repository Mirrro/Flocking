using Unity.Entities;
using Unity.Mathematics;

public struct BoidComponent : IComponentData
{
    public float3 velocity;
    public float separationWeight;
    public float alignmentWeight;
    public float cohesionWeight;
    public float homeAttractionWeight;
    public float wanderWeight;
    public float neighborRadius;
    public float maxSpeed;
}