using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Collections;
using System;

public struct SpriteSheetData : IComponentData
{
    public int currentFrame;
    public int totalFrameCount;
    public float frameTimeRemaining;
    public float maxFrameTime;

    public Vector4 uv;
    public Matrix4x4 matrix;
}

public struct SpriteRenderer : ISharedComponentData, IEquatable<SpriteRenderer>
{
    public Material material;
    public Mesh mesh;

    public bool Equals(SpriteRenderer other)
    {
        return material == other.material &&
            mesh == other.mesh;
    }

    public override int GetHashCode()
    {
        int hash = 0;
        if (!ReferenceEquals(material, null)) hash ^= material.GetHashCode();
        if (!ReferenceEquals(mesh, null)) hash ^= mesh.GetHashCode();
        return hash;
    }
}
