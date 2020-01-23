using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Mathematics;

public class CombatSystem : JobComponentSystem
{
    EntityCommandBufferSystem bufferSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        bufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        float deltaTime = Time.DeltaTime;
        EntityCommandBuffer.Concurrent buffer = bufferSystem.CreateCommandBuffer().ToConcurrent();

        ComponentDataFromEntity<Translation> translationFromEntity = GetComponentDataFromEntity<Translation>(true);

        var jobUpdateTargetPosition = Entities
            .WithName("Job Update Target Position")
            .WithReadOnly(translationFromEntity)
            .ForEach((Entity e, int entityInQueryIndex, ref CombatTarget target) =>
            {
                if (translationFromEntity.Exists(target.entity))
                {
                    target.position = translationFromEntity[target.entity].Value;
                }
            }).Schedule(inputDeps);

        var jobUpdateAttackTimers = Entities
            .WithName("Job Update Attack Timer")
            .ForEach((ref CombatStats stats) =>
            {
                if (stats.timeUntilNextAttack > 0f)
                {
                    stats.timeUntilNextAttack -= deltaTime;
                }
            }).Schedule(jobUpdateTargetPosition);

        BufferFromEntity<DamageEvent> damageEventsFromEntity = GetBufferFromEntity<DamageEvent>(false);

        var jobAttack = Entities
            .WithName("Job Attack")
            .WithNativeDisableParallelForRestriction(damageEventsFromEntity)
            .ForEach((Entity e, ref CombatStats stats, in CombatTarget target, in Translation position) =>
            {
                if (stats.timeUntilNextAttack > 0f)
                   return;

                if (!damageEventsFromEntity.Exists(target.entity))
                    return;

                // If in range, attack
                if (math.distance(position.Value, target.position) < stats.attackRange)
                {
                    if (stats.damage > 0f)
                    {
                        // TODO: unsafe to assume everything that can be targetted has a damage event buffer?
                        /*if (!damageEventsFromEntity.Exists(target.entity))
                        {
                            // Add the buffer
                            buffer.AddBuffer<DamageEvent>(entityInQueryIndex, target.entity);
                        }*/

                        damageEventsFromEntity[target.entity].Add(new DamageEvent { damage = stats.damage, instagator = e });
                    }
                    // go on cooldown
                    stats.timeUntilNextAttack = stats.baseAttackTime;
                }
            }).Schedule(jobUpdateAttackTimers);

        return jobAttack;
    }
}