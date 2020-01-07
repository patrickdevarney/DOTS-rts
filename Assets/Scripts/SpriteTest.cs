using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;

public class SpriteTest : MonoBehaviour
{
    public Mesh mesh;
    public Material material;
    // Start is called before the first frame update
    void Start()
    {
        EntityManager manager = World.Active.EntityManager;
        Entity e = manager.CreateEntity(typeof(LocalToWorld), typeof(Translation));
        manager.AddSharedComponentData<RenderMesh>(e, new RenderMesh
        {
            mesh = mesh,
            material = material,
        });
    }
}
