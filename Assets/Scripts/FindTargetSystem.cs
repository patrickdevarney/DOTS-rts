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

    protected override void OnCreate()
    {
        base.OnCreate();
        bufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        sampleNonForEach = UnityEngine.Profiling.CustomSampler.Create("Non FOr Each");
        sampleForEach = UnityEngine.Profiling.CustomSampler.Create("For Each stuff");
    }

    JobHandle ForEachSolution(JobHandle inputDeps, EntityCommandBuffer.Concurrent buffer)
    {
        EntityQuery team1AliveUnitsQuery = GetEntityQuery(ComponentType.ReadOnly<Team>(), ComponentType.ReadOnly<Translation>(), ComponentType.Exclude<NavAgentTarget>());
        team1AliveUnitsQuery.SetSharedComponentFilter(new Team { Value = 0 });

        EntityQuery team2AliveUnitsQuery = GetEntityQuery(ComponentType.ReadOnly<Team>(), ComponentType.ReadOnly<Translation>(), ComponentType.Exclude<NavAgentTarget>());
        team2AliveUnitsQuery.SetSharedComponentFilter(new Team { Value = 1 });

        NativeArray<Entity> team1NewTargets = new NativeArray<Entity>(team1AliveUnitsQuery.CalculateEntityCount(), Allocator.TempJob);
        NativeArray<Translation> team1PotentialTargetPositions = team2AliveUnitsQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
        NativeArray<Entity> team1PotentialTargets = team2AliveUnitsQuery.ToEntityArray(Allocator.TempJob);

        var jobFindTeam1Targets = Entities
            .WithDeallocateOnJobCompletion(team1PotentialTargetPositions)
            .WithDeallocateOnJobCompletion(team1PotentialTargets)
            .WithSharedComponentFilter(new Team { Value = 0 })
            .WithNone<NavAgentTarget>()
            .ForEach((Entity e, int entityInQueryIndex, in Translation translation) =>
            {
                float3 sourcePosition = translation.Value;
                Entity closestEntity = Entity.Null;
                float3 closestPosition = float3.zero;

                for (int i = 0; i < team1PotentialTargets.Length; i++)
                {
                    Entity targetEntity = team1PotentialTargets[i];
                    float3 targetPosition = team1PotentialTargetPositions[i].Value;
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
                    buffer.AddComponent(entityInQueryIndex, e, new NavAgentTarget { Value = closestEntity });
            }).Schedule(inputDeps);

        /*
        var jobAssignTeam1Targets = Entities
            .WithoutBurst()
            .WithSharedComponentFilter(new Team { Value = 0 })
            .WithNone<NavAgentTarget>()
            .ForEach((Entity e, int entityInQueryIndex, in Translation translation) =>
        {
            Debug.Log("assigning team 1 target to entity " + e.Index);
        }).Schedule(jobFindTeam1Targets);

        jobAssignTeam1Targets.Complete();
        */
        jobFindTeam1Targets.Complete();
        team1NewTargets.Dispose();

        return inputDeps;
    }

    JobHandle NotForEachSolution(JobHandle inputDeps, EntityCommandBuffer.Concurrent buffer)
    {
        EntityQuery team1AliveUnitsQuery = GetEntityQuery(ComponentType.ReadOnly<Team>(), ComponentType.ReadOnly<Translation>(), ComponentType.Exclude<NavAgentTarget>());
        team1AliveUnitsQuery.SetSharedComponentFilter(new Team { Value = 0 });

        EntityQuery team2AliveUnitsQuery = GetEntityQuery(ComponentType.ReadOnly<Team>(), ComponentType.ReadOnly<Translation>(), ComponentType.Exclude<NavAgentTarget>());
        team2AliveUnitsQuery.SetSharedComponentFilter(new Team { Value = 1 });

        NativeArray<Entity> team1NewTargets = new NativeArray<Entity>(team1AliveUnitsQuery.CalculateEntityCount(), Allocator.TempJob);
        NativeArray<Translation> team1PotentialTargetPositions = team2AliveUnitsQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
        NativeArray<Entity> team1PotentialTargets = team2AliveUnitsQuery.ToEntityArray(Allocator.TempJob);

        // Find closest targets for everything on team 1 and team 2
        var findClosestTargetJob = new FindClosestTarget
        {
            targetEntities = team1PotentialTargets,
            targetPositions = team1PotentialTargetPositions,
            retVal = team1NewTargets,
        }.Schedule(team1AliveUnitsQuery, inputDeps);

        /*NativeArray<Entity> team2NewTargets = new NativeArray<Entity>(team1AliveUnitsQuery.CalculateEntityCount(), Allocator.TempJob);
        NativeArray<Translation> team2PotentialTargetPositions = team1AliveUnitsQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
        NativeArray<Entity> team2PotentialTargets = team1AliveUnitsQuery.ToEntityArray(Allocator.TempJob);
        var findClosestTargetJob2 = new FindClosestTarget
        {
            targetEntities = team2PotentialTargets,
            targetPositions = team2PotentialTargetPositions,
            retVal = team2NewTargets,
        }.Schedule(team2AliveUnitsQuery, inputDeps);*/


        // Assign targets
        var assignTargetsTeam1Job = new AddNavTaget
        {
            newTargets = team1NewTargets,
            buffer = buffer,
        }.Schedule(team1AliveUnitsQuery, findClosestTargetJob);
        assignTargetsTeam1Job.Complete();

        /*var assignTargetsTeam2Job = new AddNavTaget
        {
            newTargets = team2NewTargets,
            buffer = buffer,
        }.Schedule(team2AliveUnitsQuery, findClosestTargetJob2);
        assignTargetsTeam2Job.Complete();*/

        inputDeps = JobHandle.CombineDependencies(inputDeps, assignTargetsTeam1Job);//, assignTargetsTeam2Job);
        /*var jobAssignNewTargets = Entities
            .WithAll<Health>()
            .WithNone<NavAgentTarget>()
            .ForEach((Entity e, int entityInQueryIndex, in Translation position) =>
            {
                buffer.AddComponent(entityInQueryIndex, e, new NavAgentTarget { });
            }).Schedule(jobCleanupDeadTargets);
            */
        //jobAssignNewTargets.Complete();
        return inputDeps;
    }

    bool doForEach;
    bool doClear;
    float timeUntilTarget = 1f;
    UnityEngine.Profiling.CustomSampler sampleForEach;
    UnityEngine.Profiling.CustomSampler sampleNonForEach;

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        timeUntilTarget -= Time.DeltaTime;
        //if (timeUntilTarget > 0f)
           // return inputDeps;
           if (timeUntilTarget < 0f)
        {
        timeUntilTarget = 1f;
            doForEach = !doForEach;
        }

        
        //Debug.Log("fooooooo");
        //if (doClear)
        {
            //Debug.Log("Clearing foreach? " + doForEach);
            EntityCommandBuffer.Concurrent buffer2 = bufferSystem.CreateCommandBuffer().ToConcurrent();

            //timeUntilTarget = 1f;
            //doClear = false;
            //doForEach = !doForEach;
            var job = Entities
            .ForEach((Entity e, int entityInQueryIndex, ref NavAgentTarget target) =>
            {
                //if (target.Value == Entity.Null)
                {
                    buffer2.RemoveComponent<NavAgentTarget>(entityInQueryIndex, e);
                }
            }).Schedule(inputDeps);
            job.Complete();
            //return inputDeps;
        }

        doClear = true;
        
        EntityCommandBuffer.Concurrent buffer = bufferSystem.CreateCommandBuffer().ToConcurrent();

        // For each thing with a target that is now dead, remove its component
        /*
        var jobCleanupDeadTargets = Entities
            .ForEach((Entity e, int entityInQueryIndex, ref NavAgentTarget target) =>
            {
                if (target.Value == Entity.Null)
                {
                    buffer.RemoveComponent<NavAgentTarget>(entityInQueryIndex, e);
                }
            }).Schedule(inputDeps);

        jobCleanupDeadTargets.Complete();
        */

        // Grab all Translation-Entity pairs of acceptable targets
        if (doForEach)
        {
            sampleForEach.Begin();
            inputDeps =  ForEachSolution(inputDeps, buffer);
            sampleForEach.End();
            return inputDeps;

        }
        else
        {
            sampleNonForEach.Begin();
             inputDeps = NotForEachSolution(inputDeps, buffer);
            sampleNonForEach.End();
            return inputDeps;
        }
    }

    public struct AddNavTaget : IJobForEachWithEntity<Translation>
    {
        [DeallocateOnJobCompletion]
        [ReadOnly]
        public NativeArray<Entity> newTargets;
        public EntityCommandBuffer.Concurrent buffer;

        public void Execute(Entity entity, int index, [ReadOnly] ref Translation translation)
        {
            //Debug.Log("foo");
            if (newTargets[index] != Entity.Null)
            {
                buffer.AddComponent(index, entity, new NavAgentTarget { Value = newTargets[index] });
            }
        }
    }

    [Unity.Burst.BurstCompile]
    public struct FindClosestTarget : IJobForEachWithEntity<Translation>
    {
        [DeallocateOnJobCompletion]
        [ReadOnly]
        public NativeArray<Entity> targetEntities;

        [DeallocateOnJobCompletion]
        [ReadOnly]
        public NativeArray<Translation> targetPositions;

        public NativeArray<Entity> retVal;

        public void Execute(Entity entity, int index, [ReadOnly] ref Translation translation)
        {
            //Debug.Log("foo1");
            float3 sourcePosition = translation.Value;
            Entity closestEntity = Entity.Null;
            float3 closestPosition = float3.zero;

            for (int i = 0; i < targetEntities.Length; i++)
            {
                Entity targetEntity = targetEntities[i];
                float3 targetPosition = targetPositions[i].Value;
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

            retVal[index] = closestEntity;
        }
    }
}