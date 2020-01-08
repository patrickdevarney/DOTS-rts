using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

/*public class SpriteSheetAnimate : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        float deltaTime = Time.DeltaTime;
        return Entities.ForEach((ref SpriteSheetData data, in Translation translation) =>
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
        }).Schedule(inputDeps);
    }
}
*/
// Want to draw both teams of units, same mesh, but different materials
// Could make a job to draw by chunk, but can't access MaterialPropertyBlock outside of main thread

/*public class TestJob : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        int shaderPropertyID = Shader.PropertyToID("_MainTex_UV");
        Entities.WithoutBurst().ForEach((in SpriteSheetData renderer) =>
        {

        }).Run();
        return inputDeps;
        return Entities.ForEach((in SpriteSheetData data, in Translation translation, in SpriteRenderer renderer) =>
        {
            MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
            materialPropertyBlock.SetVectorArray(shaderPropertyID, new Vector4[] { data.uv });
            Graphics.DrawMesh(renderer.mesh, translation.Value, Quaternion.identity, renderer.material, 0, SpriteTest.mainCamera, 0, materialPropertyBlock);
        }).Schedule(inputDeps);
    }
}*/

public class SpriteSheetRenderSystem : JobComponentSystem
{
    List<Matrix4x4> matricies = new List<Matrix4x4>();
    List<Vector4> uvs = new List<Vector4>();

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        // Schedule job to set animation state
        JobHandle animateJob = (new SpriteSheetAnimateJob { deltaTime = Time.DeltaTime }).Schedule(this, inputDeps);
        animateJob.Complete();

        // Schedule job to render, with the animation job as dependency
        // call complete() on render job
        MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
        int shaderPropertyID = Shader.PropertyToID("_MainTex_UV");

        NativeArray<SpriteSheetData> team1Sprites = new NativeArray<SpriteSheetData>();
        NativeArray<SpriteSheetData> team2Sprites = new NativeArray<SpriteSheetData>();

        // TODO grab renderers from entities/chunks? store in central TeamData location?
        //SpriteRenderer team1Renderer;
        //SpriteRenderer team2Renderer;

        EntityQuery queryForTeamSprites = GetEntityQuery(new EntityQueryDesc
        {
            All = new[] { ComponentType.ReadOnly<SpriteSheetData>(), ComponentType.ReadOnly<Team>()},
        });
        queryForTeamSprites.SetSharedComponentFilter(new Team { Value = 0 });

        // Native array for uvs and matricies
        //NativeList<Vector4> team1uvs = new NativeList<Vector4>(Allocator.TempJob);
        //NativeList<Matrix4x4> team1Matricies = new NativeList<Matrix4x4>(Allocator.TempJob);
        //JobHandle populateTeam1Arrays = new PopulateArrays { uvs = team1uvs, matricies = team1Matricies }.Schedule(queryForTeamSprites, animateJob);
        team1Sprites = queryForTeamSprites.ToComponentDataArray<SpriteSheetData>(Allocator.TempJob);

        queryForTeamSprites.SetSharedComponentFilter(new Team { Value = 1 });
        //NativeList<Vector4> team2uvs = new NativeList<Vector4>(Allocator.TempJob);
        //NativeList<Matrix4x4> team2Matricies = new NativeList<Matrix4x4>(Allocator.TempJob);
        //JobHandle populateTeam2Arrays = new PopulateArrays { uvs = team2uvs, matricies = team2Matricies }.Schedule(queryForTeamSprites, animateJob);
        team2Sprites = queryForTeamSprites.ToComponentDataArray<SpriteSheetData>(Allocator.TempJob);

        //populateTeam1Arrays.Complete();
        //populateTeam2Arrays.Complete();
        // TODO: cull sprites based on camera frustrum
        // TODO: fix drawing being in wrong order. Entities that have a higher y value should be drawn first so that lower y entities are layered on top (is there some spooky optimization to draw front-back?)
        // TODO: any way to jobify some of this work? can't create MaterialPropertyBlock off main thread

        // Draw
        // Limited to 1023 matrixes per draw call
        int maxMatrixCount = 1023;
        /*for (int i = 0; i < team1uvs.Length; i += maxMatrixCount)
        {
            int matrixCount = Unity.Mathematics.math.min(team1uvs.Length - i, maxMatrixCount);

            matricies.Clear();
            uvs.Clear();
            for (int j = 0; j < matrixCount; j++)
            {
                matricies.Add(team1Matricies[i + j]);
                uvs.Add(team1uvs[i + j]);
            }
            materialPropertyBlock.SetVectorArray(shaderPropertyID, uvs);
            Graphics.DrawMeshInstanced(SpriteTest.Instance.mesh, 0, SpriteTest.Instance.material, matricies, materialPropertyBlock);
        }
        for (int i = 0; i < team2uvs.Length; i += maxMatrixCount)
        {
            int matrixCount = Unity.Mathematics.math.min(team2uvs.Length - i, maxMatrixCount);

            matricies.Clear();
            uvs.Clear();
            for (int j = 0; j < matrixCount; j++)
            {
                matricies.Add(team2Matricies[i + j]);
                uvs.Add(team2uvs[i + j]);
            }
            materialPropertyBlock.SetVectorArray(shaderPropertyID, uvs);
            Graphics.DrawMeshInstanced(SpriteTest.Instance.mesh, 0, SpriteTest.Instance.material2, matricies, materialPropertyBlock);
        }
        
        team1Matricies.Dispose();
        team1uvs.Dispose();
        team2Matricies.Dispose();
        team2uvs.Dispose();*/

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

/*public class SpriteSheetRender : ComponentSystem
{
    List<Matrix4x4> matricies = new List<Matrix4x4>();
    List<Vector4> uvs = new List<Vector4>();
    protected override void OnUpdate()
    {
        MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
        int shaderPropertyID = Shader.PropertyToID("_MainTex_UV");

        EntityQuery query = GetEntityQuery(typeof(SpriteSheetData));
        NativeArray<SpriteSheetData> sprites = query.ToComponentDataArray<SpriteSheetData>(Allocator.TempJob);
        // TODO: fix drawing being in wrong order. Entities that have a higher y value should be drawn first so that lower y entities are layered on top (is there some spooky optimization to draw front-back?)
        // TODO: cull sprites based on camera frustrum
        // TODO: any way to jobify some of this work? can't reference MaterialPropertyBlock off of main thread

        // Limited to 1023 matrixes per draw call
        int maxMatrixCount = 1023;
        for (int i = 0; i < sprites.Length; i += maxMatrixCount)
        {
            int matrixCount = Unity.Mathematics.math.min(sprites.Length - i, maxMatrixCount);

            matricies.Clear();
            uvs.Clear();
            for (int j = 0; j < matrixCount; j++)
            {
                matricies.Add(sprites[i + j].matrix);
                uvs.Add(sprites[i + j].uv);
            }
            materialPropertyBlock.SetVectorArray(shaderPropertyID, uvs);
            //Graphics.DrawMeshInstanced(SpriteTest.fooMesh, 0, SpriteTest.team1Material, matricies, materialPropertyBlock);
        }
        sprites.Dispose();

        /*Entities.ForEach((ref SpriteSheetData data, ref Translation translation) =>
        {
            materialPropertyBlock.SetVectorArray(shaderPropertyID, new Vector4[] { data.uv });
            Graphics.DrawMesh(SpriteTest.fooMesh, translation.Value, Quaternion.identity, SpriteTest.fooMaterial, 0, SpriteTest.mainCamera, 0, materialPropertyBlock);
        });*//*
    }
}*/
