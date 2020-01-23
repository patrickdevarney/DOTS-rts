using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public struct NavAgent : IComponentData
{
    public float speed;
    public float stoppingDistance;
}
