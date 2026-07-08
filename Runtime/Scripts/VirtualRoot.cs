using System.Collections.Generic;
using UnityEngine;

namespace UnityCEClient
{
    public class VirtualRoot : LocalObject
    {
        private static VirtualRoot Instance { get; set; }

        public static Dictionary<string, Vector3> proxyPositions = new Dictionary<string, Vector3>();
        public static Dictionary<string, Quaternion> proxyRotations = new Dictionary<string, Quaternion>();

        protected void Awake()
        {
            Instance = this;
        }

        public static void SetProxyPosition(string home, Vector3 position)
        {
            if (proxyPositions.ContainsKey(home))
                proxyPositions[home] = position;
            else
                proxyPositions.Add(home, position);
        }

        public static void SetProxyRotation(string home, Quaternion rotation)
        {
            if (proxyRotations.ContainsKey(home))
                proxyRotations[home] = rotation;
            else
                proxyRotations.Add(home, rotation);
        }

        public static Vector3 TransformPosition(string fromHome, Vector3 position)
        {
            if (Instance == null || !proxyPositions.ContainsKey(fromHome) || string.IsNullOrEmpty(fromHome))
                return position;
            else
                // position the object in the same relative position as the source vRoot (stored in proxyPosition)
                return Instance.transform.position + Instance.transform.rotation * Quaternion.Inverse(proxyRotations[fromHome]) * (position - proxyPositions[fromHome]);
        }

        public static Quaternion TransformRotation(string fromHome, Quaternion rotation)
        {
            if (Instance == null || !proxyRotations.ContainsKey(fromHome) || string.IsNullOrEmpty(fromHome))
                return rotation;
            else
                // rotate the object in the same relative orientation as the source vRoot (stored in proxyRotations)
                return Instance.transform.rotation * Quaternion.Inverse(proxyRotations[fromHome]) * rotation;
        }

        public static Vector3 RelativePositionTo(Vector3 position)
        {
            if (Instance == null) return Vector3.zero;
            else return Instance.transform.position - position;
        }
    }
}