using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public struct Health : IComponentData
{
    public float currentHealth;
    public float maxHealth;
}

[InternalBufferCapacity(16)]
public struct DamageEvent : IBufferElementData
{
    public Entity instagator;
    public float damage;
    
    public static void AddEvent(DynamicBuffer<DamageEvent> damageBuffer, Entity instagator, float damage)
    {
        DamageEvent e;
        e.damage = damage;
        e.instagator = instagator;

        if (damageBuffer.Length == damageBuffer.Capacity)
        {
            // Remove oldest element from the buffer?
        }

        damageBuffer.Add(e);
    }
}
