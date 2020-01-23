using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

public struct NavAgentTarget : IComponentData
{
    public float3 position;
}
