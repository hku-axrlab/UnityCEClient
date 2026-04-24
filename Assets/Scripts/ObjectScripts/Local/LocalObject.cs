using System.Collections.Generic;
using UnityEngine;

namespace UnityCEClient
{
    public delegate (System.Type, object) ValueFunction();

    /// <summary>
    /// Base class for the implementation of objects that send data back into the CalibrationEnv.
    /// Inherit from it and register your own variable data functions.
    /// </summary>
    public class LocalObject : MonoBehaviour
    {
        public string id = string.Empty;
        public bool isActive = true;
        public bool isLive = true;

        protected Dictionary<string, ValueFunction> objectData = new Dictionary<string, ValueFunction>();

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        protected virtual void Start()
        {
            if (string.IsNullOrEmpty(id))
                id = System.Guid.NewGuid().ToString();

            objectData.Add("visible", GetVisible);
            objectData.Add("live", GetLive);

            CalibrationEnvLinker.RegisterObject(this);
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