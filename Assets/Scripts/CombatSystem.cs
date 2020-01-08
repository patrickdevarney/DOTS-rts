using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

public class CombatSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        return inputDeps;
        // For all alive entities, with a combat target, and a position:
        Entities
            .WithAll<Health>()
            .ForEach((ref CombatTarget target, in Translation position) =>
        {
            // If target is dead, remove target component
            
            // If in range, do damage

        }).Schedule(inputDeps);

    }
}