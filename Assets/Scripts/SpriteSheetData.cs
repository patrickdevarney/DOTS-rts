using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

public struct SpriteSheetData : IComponentData
{
    public int currentFrame;
    public int totalFrameCount;
    public float frameTimeRemaining;
    public float maxFrameTime;
}

public class SpriteSheetAnimate : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        float deltaTime = Time.DeltaTime;
        return Entities.ForEach((ref SpriteSheetData data) =>
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
        }).Schedule(inputDeps);
    }
}

public class SpriteSheetRender : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((ref SpriteSheetData data, ref Translation translation) =>
        {
            MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
            float uvWidth = 1f / 0.09375f;
            float uvOffsetX = 0.09375f * data.currentFrame;
            float uvHeight = 0.1875f;
            float uvOffsetY = 0f;
            Vector4 uv = new Vector4(uvWidth, uvHeight, uvOffsetX, uvOffsetY);
            materialPropertyBlock.SetVectorArray("_MainTex_UV", new Vector4[] { uv });

            Graphics.DrawMesh(SpriteTest.fooMesh, translation.Value, Quaternion.identity, SpriteTest.fooMaterial, 0, Camera.main, 0, materialPropertyBlock);
        });
    }
}
