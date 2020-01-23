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
    public static SpriteTest Instance;
    public Mesh mesh;
    public Material material;
    public Material material2;
    public int team1Spawns = 100;
    public int team2Spawns = 100;
    public float frameTime = 0.1f;

    public int waveSize;
    public bool spawnWaves;
    public bool debugForceSpawnWave;
    public float timeBetweenSpawns;
    float timeUntilSpawn;

    public Vector2 spawnBounds;

    // Start is called before the first frame update
    void Start()
    {
        Instance = this;
        mainCamera = Camera.main;
        Spawn(team1Spawns, team2Spawns);
        timeUntilSpawn = timeBetweenSpawns;
    }

    private void Update()
    {
        if (debugForceSpawnWave)
        {
            debugForceSpawnWave = false;
            Spawn(waveSize / 2, waveSize / 2);
        }

        if (spawnWaves)
        {
            timeUntilSpawn -= Time.deltaTime;
            if (timeUntilSpawn < 0f)
            {
                timeUntilSpawn = timeBetweenSpawns;
                Spawn(waveSize / 2, waveSize / 2);
            }
        }
    }

    void Spawn(int team1Spawns, int team2Spawns)
    {
        EntityManager manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityArchetype archetype = manager.CreateArchetype(
            typeof(Translation),
            typeof(SpriteSheetData),
            typeof(Team),
            typeof(Health),
            typeof(SpriteRenderer),
            typeof(NavAgent),
            typeof(CombatStats),
            typeof(DamageEvent)
            );
        NativeArray<Entity> entities = new NativeArray<Entity>(team1Spawns + team2Spawns, Allocator.Temp);
        manager.CreateEntity(archetype, entities);

        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            manager.SetComponentData(entity, new Translation { Value = new Unity.Mathematics.float3(UnityEngine.Random.Range(-spawnBounds.x / 2f, spawnBounds.x / 2f), UnityEngine.Random.Range(-spawnBounds.y / 2f, spawnBounds.y / 2f), 0f) });
            manager.SetComponentData(entity, new SpriteSheetData
            {
                currentFrame = UnityEngine.Random.Range(0, 4),
                totalFrameCount = 4,
                frameTimeRemaining = 0.5f,
                maxFrameTime = frameTime,
            });
            manager.SetComponentData(entity, new CombatStats { attackRange = 0.25f, damage = 1, baseAttackTime = 1f });
            manager.SetComponentData(entity, new Health { currentHealth = 10f, maxHealth = 100f });
            manager.SetComponentData(entity, new NavAgent { speed = 0.5f, stoppingDistance = 0.1f });
            manager.SetSharedComponentData(entity, new Team { Value = (i < team1Spawns ? 0 : 1) });
            manager.SetSharedComponentData(entity, new SpriteRenderer
            {
                material = (i < team1Spawns ? material : material2),
                mesh = mesh,
            });
        }

        entities.Dispose();
    }
}
