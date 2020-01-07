using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Collections;

public class SpriteTest : MonoBehaviour
{
    public static Camera mainCamera;
    public static Mesh fooMesh;
    public static Material fooMaterial;
    public Mesh mesh;
    public Material material;
    public int spawns = 100;
    public float frameTime = 0.1f;
    // Start is called before the first frame update
    void Start()
    {
        mainCamera = Camera.main;
        fooMesh = mesh;
        fooMaterial = material;
        EntityManager manager = World.Active.EntityManager;
        /*Entity e = manager.CreateEntity(typeof(LocalToWorld), typeof(Translation));
        manager.AddComponentData(e, new SpriteSheetData
        {
            maxFrameTime = 0.5f,
            totalFrameCount = 4,
        }
        );*/

        EntityArchetype archetype = manager.CreateArchetype(
            typeof(Translation),
            typeof(SpriteSheetData)
            );
        NativeArray<Entity> entities = new NativeArray<Entity>(spawns, Allocator.Temp);
        manager.CreateEntity(archetype, entities);

        foreach(Entity entity in entities)
        {
            manager.SetComponentData(entity, new Translation { Value = new Unity.Mathematics.float3(UnityEngine.Random.Range(-5f, 5f), UnityEngine.Random.Range(-3f, 3f), 0f) });
            manager.SetComponentData(entity, new SpriteSheetData
            {
                currentFrame = UnityEngine.Random.Range(0, 4),
                totalFrameCount = 4,
                frameTimeRemaining = 0.5f,
                maxFrameTime = frameTime,
            });
        }

        entities.Dispose();
    }
}
