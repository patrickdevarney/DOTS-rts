using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public static class EntityExtensions
{
    public static bool Exists(this Entity entity)
    {
        return entity != Entity.Null && entity.Index > 0;
    }
}
