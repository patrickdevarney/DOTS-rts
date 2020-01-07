using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class SpawnOnLoad : MonoBehaviour
{
    public AssetReference prefab;

    // Start is called before the first frame update
    void Start()
    {
        Addressables.InstantiateAsync(prefab);
    }
}
