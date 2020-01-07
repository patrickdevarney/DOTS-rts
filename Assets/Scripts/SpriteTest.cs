using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;

public class SpriteTest : MonoBehaviour
{
    public static Mesh fooMesh;
    public static Material fooMaterial;
    public Mesh mesh;
    public Material material;
    // Start is called before the first frame update
    void Start()
    {
        fooMesh = mesh;
        fooMaterial = material;
        EntityManager manager = World.Active.EntityManager;
        Entity e = manager.CreateEntity(typeof(LocalToWorld), typeof(Translation));
        manager.AddComponentData(e, new SpriteSheetData
        {
            maxFrameTime = 0.5f,
            totalFrameCount = 4,
        }
        );
        /*manager.AddSharedComponentData(e, new RenderMesh
        {
            mesh = mesh,
            material = material,
        });*/
    }
}
