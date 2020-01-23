using Unity.Entities;
using Unity.Mathematics;

public struct CombatTarget : IComponentData
{
    public Entity entity;
    public float3 position;
}

public struct CombatStats : IComponentData
{
    public float damage;
    public float timeUntilNextAttack;
    public float baseAttackTime;
    public float attackRange;
}
