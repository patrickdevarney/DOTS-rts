using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using UnityEngine.Profiling;

public class SpriteSheetRenderSystem : JobComponentSystem
{
    List<Matrix4x4> matricies = new List<Matrix4x4>();
    List<Vector4> uvs = new List<Vector4>();
    CustomSampler sample1;
    CustomSampler sample2;

    protected override void OnCreate()
    {
        base.OnCreate();
        sample1 = CustomSampler.Create("Sample1");
        sample2 = CustomSampler.Create("Sample2");
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        // Schedule job to set animation state
        JobHandle animateJob = (new SpriteSheetAnimateJob { deltaTime = Time.DeltaTime }).Schedule(this, inputDeps);
        animateJob.Complete();

        MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
        int shaderPropertyID = Shader.PropertyToID("_MainTex_UV");

        NativeArray<SpriteSheetData> team1Sprites = new NativeArray<SpriteSheetData>();
        NativeArray<SpriteSheetData> team2Sprites = new NativeArray<SpriteSheetData>();

        // TODO grab renderers from entities/chunks? store in central TeamData location?
        //SpriteRenderer team1Renderer;
        //SpriteRenderer team2Renderer;

        sample1.Begin();
        EntityQuery queryForTeamSprites = GetEntityQuery(new EntityQueryDesc
        {
            All = new[] { ComponentType.ReadOnly<SpriteSheetData>(), ComponentType.ReadOnly<Team>()},
        });
        queryForTeamSprites.SetSharedComponentFilter(new Team { Value = 0 });

        team1Sprites = queryForTeamSprites.ToComponentDataArray<SpriteSheetData>(Allocator.TempJob);

        queryForTeamSprites.SetSharedComponentFilter(new Team { Value = 1 });
        team2Sprites = queryForTeamSprites.ToComponentDataArray<SpriteSheetData>(Allocator.TempJob);
        sample1.End();

        // TODO: cull sprites based on camera frustrum
        // TODO: fix drawing being in wrong order. Entities that have a higher y value should be drawn first so that lower y entities are layered on top (is there some spooky optimization to draw front-back?)
        // TODO: any way to jobify some of this work? can't create MaterialPropertyBlock off main thread

        // Draw
        // Limited to 1023 matrixes per draw call
        int maxMatrixCount = 1023;

        sample2.Begin();
        for (int i = 0; i < team1Sprites.Length; i += maxMatrixCount)
        {
            int matrixCount = Unity.Mathematics.math.min(team1Sprites.Length - i, maxMatrixCount);

            matricies.Clear();
            uvs.Clear();
            for (int j = 0; j < matrixCount; j++)
            {
                matricies.Add(team1Sprites[i + j].matrix);
                uvs.Add(team1Sprites[i + j].uv);
            }
            materialPropertyBlock.SetVectorArray(shaderPropertyID, uvs);
            Graphics.DrawMeshInstanced(SpriteTest.Instance.mesh, 0, SpriteTest.Instance.material, matricies, materialPropertyBlock);
        }
        for (int i = 0; i < team2Sprites.Length; i += maxMatrixCount)
        {
            int matrixCount = Unity.Mathematics.math.min(team2Sprites.Length - i, maxMatrixCount);

            matricies.Clear();
            uvs.Clear();
            for (int j = 0; j < matrixCount; j++)
            {
                matricies.Add(team2Sprites[i + j].matrix);
                uvs.Add(team2Sprites[i + j].uv);
            }
            materialPropertyBlock.SetVectorArray(shaderPropertyID, uvs);
            Graphics.DrawMeshInstanced(SpriteTest.Instance.mesh, 0, SpriteTest.Instance.material2, matricies, materialPropertyBlock);
        }
        sample2.End();
        team1Sprites.Dispose();
        team2Sprites.Dispose();
        return inputDeps;
    }

    struct PopulateArrays : IJobForEach<SpriteSheetData>
    {
        [NativeDisableParallelForRestriction]
        public NativeList<Vector4> uvs;
        [NativeDisableParallelForRestriction]
        public NativeList<Matrix4x4> matricies;
        public void Execute([ReadOnly]ref SpriteSheetData data)
        {
            uvs.Add(data.uv);
            matricies.Add(data.matrix);
        }
    }

    [BurstCompile]
    struct SpriteSheetAnimateJob : IJobForEach<SpriteSheetData, Translation>
    {
        public float deltaTime;
        public void Execute(ref SpriteSheetData data, [ReadOnly]ref Translation translation)
        {
            data.frameTimeRemaining -= deltaTime;
            if (data.frameTimeRemaining < 0f)
            {
                // proceed to next frame
                data.frameTimeRemaining = data.maxFrameTime;
                data.currentFrame = (data.currentFrame + 1) % data.totalFrameCount;
            }
            else
            {
                // stay on this frame
            }

            float uvWidth = 0.09375f;//0.1875f;
            float uvOffsetX = 0.09375f * data.currentFrame;
            float uvHeight = 0.1875f;
            float uvOffsetY = 0.8125f;
            data.uv = new Vector4(uvWidth, uvHeight, uvOffsetX, uvOffsetY);

            data.matrix = Matrix4x4.TRS(translation.Value, Quaternion.identity, Vector3.one);
        }
    }
}
