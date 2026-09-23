using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityCEClient
{
    [Serializable]
    public struct UserData
    {
        public string id;
        public string name;
        public List<string> boneNames;
        public List<TransformData> boneTransforms;
        public ObjectVariable[] variables;
    }

    [Serializable]
    public class Vector3Data
    {
        public float x, y, z;

        public Vector3Data()
        {
            x = y = z = 0;
        }

        public Vector3Data(Vector3 source)
        {
            x = source.x;
            y = source.y;
            z = source.z;
        }

        public void Update(Vector3 source)
        {
            x = source.x;
            y = source.y;
            z = source.z;
        }

        public static implicit operator Vector3(Vector3Data source)
        {
            Vector3 p;
            p.x = source.x;
            p.y = source.y;
            p.z = source.z;
            return p;
        }
    }

    [Serializable]
    public class QuaternionData
    {
        public float x, y, z, w;

        public QuaternionData()
        {
            x = y = z = w = 0;
        }

        public QuaternionData(Quaternion source)
        {
            x = source.x;
            y = source.y;
            z = source.z;
            w = source.w;
        }

        public void Update(Quaternion source)
        {
            x = source.x;
            y = source.y;
            z = source.z;
            w = source.w;
        }

        public static implicit operator Quaternion(QuaternionData source)
        {
            Quaternion r;
            r.x = source.x;
            r.y = source.y;
            r.z = source.z;
            r.w = source.w;
            return r;
        }

        public static implicit operator QuaternionData(Quaternion source)
        {
            QuaternionData r = new QuaternionData();
            r.x = source.x;
            r.y = source.y;
            r.z = source.z;
            r.w = source.w;
            return r;
        }
    }

    [Serializable]
    public class TransformData
    {
        public Vector3Data position;
        public QuaternionData rotation;
        public Vector3Data scale;

        public TransformData()
        {
            position = new Vector3Data();
            rotation = new QuaternionData();
            scale = new Vector3Data();
        }

        public static TransformData From(Transform t)
        {
            TransformData data = new TransformData();

            if (t != null)
            {
                data.position = new Vector3Data(t.position);
                data.rotation = new QuaternionData(t.rotation);
                data.scale = new Vector3Data(t.localScale);
            }

            return data;
        }

        public void Update(Transform t)
        {
            if (t == null) return;

            position.Update(t.position);
            rotation.Update(t.rotation);
            scale.Update(t.localScale);
        }
    }

    [Serializable]
    struct UserMsg
    {
        public int msgType;
        public UserData[] users;

        public UserMsg(LocalUser[] data)
        {
            msgType = (int)MessageType.ClientData;
            users = new UserData[data.Length];
            for (int i = 0; i < data.Length; ++i)
            {
                users[i] = data[i].GetUserData();
            }
        }
    }

    /// <summary>
    /// Base class for any kind of user that needs to be sent.
    /// Inherit from this class, and simply add your transforms to the list of bones.
    /// Make sure they have the correct names that you want to send.
    /// 
    /// Name doesn't need to be unique, but can for instance represent the "kind of user" you want to display.
    /// </summary>
    public class LocalUser : MonoBehaviour
    {
        public const int USER_SEND_DELAY = 33;

        internal string userType = "Default";
        public string avatarType = "UnityUser";
        public string id = "";

        public List<Transform> bones = new List<Transform>();

        [Tooltip("Set this to make rotations relative to this object, for instance the initial-hips positions of a character")]
        public Transform relativeToRotation = null;

        protected Dictionary<string, ValueFunction> userCustomData = new Dictionary<string, ValueFunction>();

        private UserData data = new UserData();
        public UserData GetUserData()
        {
            if (data.boneNames == null || data.boneTransforms == null)
                UpdateData();

            return data;
        }

        protected virtual void Start()
        {
            if (string.IsNullOrEmpty(id))
                id = Utils.GenerateId();
            else
                id += "-" + Utils.GenerateId();

            data.id = id;
            data.name = avatarType;

            if (relativeToRotation != null)
            {
                // Create a Dummy transform with the initial rotation of the target
                GameObject g = new GameObject("RelativeDummy");
                g.transform.parent = transform;
                g.transform.rotation = relativeToRotation.rotation;
                g.transform.position = relativeToRotation.position;
                relativeToRotation = g.transform;
            }

            userCustomData.Add("userType", GetUserType);

            // Force a specific tag for now
            CalibrationEnvLinker.RegisterUser(this);
        }

        public (System.Type, object) GetUserType()
        {
            return (typeof(string), userType);
        }

        protected virtual void LateUpdate()
        {
            UpdateData();
        }

        public ObjectVariable[] GetVariables()
        {
            List<ObjectVariable> vars = new List<ObjectVariable>(userCustomData.Keys.Count);

            foreach (var pair in userCustomData)
            {
                ObjectVariable objVar = new ObjectVariable();
                objVar.name = pair.Key;
                var (type, value) = pair.Value.Invoke();
                // TODO: standardize these to specific types?
                objVar.type = type.ToString();
                objVar.value = value;
                vars.Add(objVar);
            }

            return vars.ToArray();
        }
        
        private void UpdateData()
        {
            if (data.boneNames == null || data.boneNames.Count != bones.Count)
            {
                data.boneNames = new List<string>(bones.Count);
                data.boneTransforms = new List<TransformData>(bones.Count);

                foreach (Transform t in bones)
                {
                    data.boneNames.Add(t.name);
                    data.boneTransforms.Add(TransformData.From(t));
                }
            }
            else
            {
                // We assume the bone counts didn't change
                for (int i = 0; i < bones.Count; ++i)
                {
                    data.boneTransforms[i].Update(bones[i]);
                    if (relativeToRotation)
                    {
                        data.boneTransforms[i].rotation *= Quaternion.Inverse(relativeToRotation.rotation * Quaternion.Inverse(transform.rotation));
                    }
                }
                data.variables = GetVariables();
            }
        }
    }
}