using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;

public class DamageSystem : JobComponentSystem
{
    EntityCommandBufferSystem bufferSystem;
    protected override void OnCreate()
    {
        base.OnCreate();
        bufferSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        EntityCommandBuffer.Concurrent buffer = bufferSystem.CreateCommandBuffer().ToConcurrent();

        //If DamageEvent => set Health component
        var jobApplyDamage = Entities
            .ForEach((Entity e, int entityInQueryIndex, ref DynamicBuffer<DamageEvent> damageBuffer, ref Health health) =>
            {
                for(int i = damageBuffer.Length - 1; i >= 0; i--)
                {
                    health.currentHealth -= damageBuffer[i].damage;
                    if (health.currentHealth < 0f)
                    {
                        // DIE!
                        buffer.DestroyEntity(entityInQueryIndex, e);
                        return;
                    }

                    damageBuffer.RemoveAt(i);
                }
            }).Schedule(inputDeps);
        //If health component was just removed => destroy the entity, spawn death FX

        return jobApplyDamage;
    }
}
