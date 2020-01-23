using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Collections;
using Unity.Mathematics;

public class FindTargetSystem : JobComponentSystem
{
    EntityCommandBufferSystem bufferSystem;
    UnityEngine.Profiling.CustomSampler sampleForEach;
    EntityQuery teamUnitsAlive;

    protected override void OnCreate()
    {
        base.OnCreate();
        bufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        sampleForEach = UnityEngine.Profiling.CustomSampler.Create("SAMPLE ForEach stuff");

        teamUnitsAlive = GetEntityQuery(ComponentType.ReadOnly<Team>(), ComponentType.ReadOnly<Translation>(), ComponentType.ReadOnly<Health>());
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        EntityCommandBuffer.Concurrent buffer = bufferSystem.CreateCommandBuffer().ToConcurrent();

        // For each thing with a target that is now gone, remove its component
        // TODO: feels like a hack, checking if the Health component exists, but the CombatTarget.entity field will always be vaild even after the entity is destroyed :/
        ComponentDataFromEntity<Health> healthFromEntities = GetComponentDataFromEntity<Health>(true);

        var jobCleanupDeadTargets = Entities
            .WithReadOnly(healthFromEntities)
            .WithAll<NavAgentTarget>()
            .ForEach((Entity e, int entityInQueryIndex, ref CombatTarget target) =>
            {
                if (!healthFromEntities.Exists(target.entity))
                {
                    buffer.RemoveComponent<CombatTarget>(entityInQueryIndex, e);
                    buffer.RemoveComponent<NavAgentTarget>(entityInQueryIndex, e);
                }
            }).Schedule(inputDeps);

        var jobCleanupDeadTargets2 = Entities
            .WithReadOnly(healthFromEntities)
            .WithNone<NavAgentTarget>()
            .ForEach((Entity e, int entityInQueryIndex, ref CombatTarget target) =>
            {
                if (!healthFromEntities.Exists(target.entity))
                {
                    buffer.RemoveComponent<CombatTarget>(entityInQueryIndex, e);
                }
            }).Schedule(jobCleanupDeadTargets);

        sampleForEach.Begin();
        // TODO: Skip searching for targets if none exist
        var jobAssignTeam1Targets = AssignTargetsForTeamUnits(new Team { Value = 1 }, new Team { Value = 0 }, jobCleanupDeadTargets2, buffer);
        var jobAssignTeam2Targets = AssignTargetsForTeamUnits(new Team { Value = 1 }, new Team { Value = 0 }, jobAssignTeam1Targets, buffer);
        sampleForEach.End();
        return JobHandle.CombineDependencies(jobAssignTeam1Targets, jobAssignTeam2Targets);
    }

    JobHandle AssignTargetsForTeamUnits(Team teamToTarget, Team sourceTeam, JobHandle inputDeps, EntityCommandBuffer.Concurrent buffer)
    {
        teamUnitsAlive.SetSharedComponentFilter(teamToTarget);
        NativeArray<Translation> potentialTargetPositions = teamUnitsAlive.ToComponentDataArray<Translation>(Allocator.TempJob);
        NativeArray<Entity> potentialTargetEntities = teamUnitsAlive.ToEntityArray(Allocator.TempJob);

        return Entities
            .WithDeallocateOnJobCompletion(potentialTargetPositions)
            .WithDeallocateOnJobCompletion(potentialTargetEntities)
            .WithSharedComponentFilter(sourceTeam)
            .WithNone<NavAgentTarget>()
            .WithName("MyFancyJob")
            .WithAll<CombatStats>()
            .ForEach((Entity e, int entityInQueryIndex, in Translation translation) =>
            {
                float3 sourcePosition = translation.Value;
                Entity closestEntity = Entity.Null;
                float3 closestPosition = float3.zero;

                for (int i = 0; i < potentialTargetEntities.Length; i++)
                {
                    Entity targetEntity = potentialTargetEntities[i];
                    float3 targetPosition = potentialTargetPositions[i].Value;
                    if (closestEntity == Entity.Null)
                    {
                        closestEntity = targetEntity;
                        closestPosition = targetPosition;
                    }
                    else
                    {
                        if (math.distance(sourcePosition, targetPosition) < math.distance(sourcePosition, closestPosition))
                        {
                            closestEntity = targetEntity;
                            closestPosition = targetPosition;
                        }
                    }
                }

                if (closestEntity != Entity.Null)
                    buffer.AddComponent(entityInQueryIndex, e, new CombatTarget { entity = closestEntity, position = closestPosition });
            }).Schedule(inputDeps);
    }
}