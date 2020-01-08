using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public struct Health : IComponentData
{
    public float currentHealth;
    public float maxHealth;
}
