using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Mathematics;

public class NavigationSystem : JobComponentSystem
{
    EntityCommandBufferSystem bufferSystem;
    protected override void OnCreate()
    {
        base.OnCreate();
        bufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        EntityCommandBuffer.Concurrent buffer = bufferSystem.CreateCommandBuffer().ToConcurrent();

        // Assign nav target to entities that need it
        var jobAssignNavTarget = Entities
            .WithNone<NavAgentTarget>()
            .WithAll<NavAgent>()
            .ForEach((Entity e, int entityInQueryIndex, in CombatTarget target) =>
            {
                buffer.AddComponent(entityInQueryIndex, e, new NavAgentTarget { position = target.position });
            }).Schedule(inputDeps);

        // Update NavTarget position for things with combat targets (target may be moving)
        var jobUpdateNavTargets = Entities
            .ForEach((ref NavAgentTarget target, in CombatTarget combatTarget) =>
            {
                target.position = combatTarget.position;
            }).Schedule(jobAssignNavTarget);

        // Move things toward nav targets
        float deltaTime = Time.DeltaTime;
        var jobMoveTowardsTarget = Entities
            .ForEach((ref Translation translation, in NavAgentTarget target, in NavAgent agent) =>
            {
                if (math.distance(translation.Value, target.position) < agent.stoppingDistance)
                {
                    // Has arrived
                    return;
                }

                // Set this translation data to be closer to the target
                float3 newPosition = translation.Value + (math.normalize(target.position - translation.Value) * agent.speed * deltaTime);
                translation.Value = newPosition;
                // TOOD: don't overshoot the target, clamp to agent.stopping distance
                //buffer.SetComponent(entityInQueryIndex, e, new Translation { Value = newPosition });
            }).Schedule(jobUpdateNavTargets);

        return jobMoveTowardsTarget;
    }
}