using System.Collections.Generic;
using UnityEngine;

namespace UnityCEClient
{
    public delegate (System.Type, object) ValueFunction();

    [System.Serializable]
    public struct ObjectVariable
    {
        public string name;
        public string type;
        public object value;
    }
    
    [System.Serializable]
    public struct ObjectData
    {
        public string id;
        public string name;
        public string tag;
        public TransformData transform;
        public ObjectVariable[] variables;

        public ObjectData(ref LocalObject obj )
        {
            id = obj.id;
            name = obj.mName;
            tag = obj.mTag;
            transform = obj.transformData;
            variables = obj.GetVariables();
        }
    }

    [System.Serializable]
    public struct ObjectMsg
    {
        public MessageType msgType;
        public ObjectData[] objects;

        public ObjectMsg(LocalObject[] objList)
        {
            msgType = MessageType.ClientData;
            objects = new ObjectData[objList.Length];
            for( int i = 0; i < objList.Length; i++ )
            {
                objects[i] = new ObjectData(ref objList[i]);
            }
        }
    }

    /// <summary>
    /// Base class for the implementation of objects that send data back into the CalibrationEnv.
    /// Inherit from it and register your own variable data functions.
    /// </summary>
    public class LocalObject : MonoBehaviour
    {
        public string id = string.Empty;
        public bool isActive = true;
        public bool isLive = true;
        public string mName = "";
        public string mTag = "";
        public TransformData transformData;

        protected Dictionary<string, ValueFunction> objectData = new Dictionary<string, ValueFunction>();

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        protected virtual void Start()
        {
            mName = name;
            mTag = tag;
            transformData = new TransformData();

            if (string.IsNullOrEmpty(id))
                id = System.Guid.NewGuid().ToString();

            objectData.Add("visible", GetVisible);
            objectData.Add("live", GetLive);

            CalibrationEnvLinker.RegisterObject(this);
        }

        protected virtual void Update()
        {
            transformData.Update(transform);
        }

        private void OnDestroy()
        {
            CalibrationEnvLinker.UnregisterObject(this);
        }

        private void OnEnable()
        {
            isActive = true;
        }

        private void OnDisable()
        {
            isActive = false;
        }

        public ObjectVariable[] GetVariables()
        {
            List<ObjectVariable> vars = new List<ObjectVariable>(objectData.Keys.Count);
            
            foreach( var pair in objectData )
            {
                ObjectVariable objVar = new ObjectVariable();
                objVar.name = pair.Key;
                var (type,value) = pair.Value.Invoke();
                // TODO: standardize these to specific types?
                objVar.type = type.ToString();
                objVar.value = value;

                vars.Add(objVar);
            }

            return vars.ToArray();
        }

        public (System.Type, object) GetLive()
        {
            return (typeof(bool), isLive);
        }

        public (System.Type, object) GetVisible()
        {
            return (typeof(bool), isActive);
        }
    }
}