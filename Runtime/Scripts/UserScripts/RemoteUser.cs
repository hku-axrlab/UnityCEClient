using System.Collections.Generic;
using UnityCEClient;
using UnityEngine;

public class RemoteUser : MonoBehaviour
{
    public string id;
    public string home;
    public new string name;
    public bool spawnMissingTransforms = false;

    private Dictionary<string, Transform> foundChildren;

    public void ApplyTransforms(string[] boneNames, TransformData[] boneTransforms)
    {
        if (foundChildren == null)
        {
            InitializeDictionary(boneNames);
        }

        for( int i = 0; i < boneNames.Length; i++ )
        {
            if (foundChildren.ContainsKey(boneNames[i]))
            {
                foundChildren[boneNames[i]].SetPositionAndRotation(boneTransforms[i].position, boneTransforms[i].rotation);
                foundChildren[boneNames[i]].localScale = boneTransforms[i].scale;
            }
        }
    }

    private void InitializeDictionary(string[] boneNames)
    {
        foundChildren = new Dictionary<string, Transform>();
        foreach (string boneName in boneNames)
        {
            Transform t = transform.Find(boneName);
            if ( t ) foundChildren[boneName] = t;
            else if (spawnMissingTransforms)
            {
                GameObject child = new GameObject(boneName);
                child.transform.parent = transform;
            }
        }
    }
}
