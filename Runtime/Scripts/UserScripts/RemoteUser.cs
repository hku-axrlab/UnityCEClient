using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UnityCEClient
{
    public class RemoteUser : MonoBehaviour
    {
        public string id;
        public string home;
        public new string name;
        public bool spawnMissingTransforms = false;
        private bool pRootRelative = false;

        private Dictionary<string, Transform> foundChildren;

        protected Dictionary<string, JTokenHandler> valueFunctions = new Dictionary<string, JTokenHandler>();

        protected virtual void Awake()
        {
            valueFunctions.Add("pRootRelative", HandlePRootRelative);
        }

        public void HandleVariables(JToken data)
        {
            // parse & apply component data
            foreach (var variable in data)
            {
                string varName = variable["name"].Value<string>();
                if (valueFunctions.ContainsKey(varName))
                    valueFunctions[varName](variable["value"]);
            }
        }

        private void HandlePRootRelative(JToken varJson)
        {
            pRootRelative = varJson.Value<bool>();
        }

        public void ApplyTransforms(string[] boneNames, TransformData[] boneTransforms)
        {
            if (foundChildren == null)
            {
                InitializeDictionary(boneNames);
            }

            for (int i = 0; i < boneNames.Length; i++)
            {
                if (foundChildren.ContainsKey(boneNames[i]))
                {
                    Vector3 p;
                    Quaternion r;
                    // Workaround for environments that cannot send global transform data when parented (such as ResoniteLink)
                    if (pRootRelative)
                    {
                        p = PhysicalRoot.TransformPosition(boneTransforms[i].position);
                        r = PhysicalRoot.TransformRotation(boneTransforms[i].rotation);
                    }
                    else
                    {
                        p = VirtualRoot.TransformPosition(home, boneTransforms[i].position);
                        r = VirtualRoot.TransformRotation(home, boneTransforms[i].rotation);
                    }

                    foundChildren[boneNames[i]].SetPositionAndRotation(p, r);
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
                if (t) foundChildren[boneName] = t;
                else if (spawnMissingTransforms)
                {
                    GameObject child = new GameObject(boneName);
                    child.transform.parent = transform;
                }
            }
        }
    }
}