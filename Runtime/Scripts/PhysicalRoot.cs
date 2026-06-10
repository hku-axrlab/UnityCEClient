using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityCEClient
{
    /// <summary>
    /// This is the class that represents the co-located center, which is synchronized from a "primary" host (currently just the PlaySpace Calibrator in Resonite)
    ///  In future, it should be possible to have any client provide this primary position/rotation, and other clients will follow and update accordingly
    /// </summary>
    public class PhysicalRoot : MonoBehaviour
    {
        private static PhysicalRoot Instance { get; set; }

        // Our "maser" pRoot can differ in theory (currently just Resonite is supported), so store the ones we find from every possible host
        public static Dictionary<string, Vector3> proxyPositions = new Dictionary<string, Vector3>();
        public static Dictionary<string, Quaternion> proxyRotations = new Dictionary<string, Quaternion>();

        [Tooltip("Should this client treat itself as the \"Colocation Host\"? If yes, it will ignore all co-location updates from other clients (such as the Playspace in Resonite)")]
        public bool isPrimary = false;
        private string primaryID = string.Empty;

        private void Awake()
        {
            Instance = this;
        }

        public static void SetProxyPosition(string home, Vector3 position)
        {
            if (proxyPositions.ContainsKey(home))
                proxyPositions[home] = position;
            else
                proxyPositions.Add(home, position);

            Instance?.CheckPrimary(home);
        }

        public static void SetProxyRotation(string home, Quaternion rotation)
        {
            if (proxyRotations.ContainsKey(home))
                proxyRotations[home] = rotation;
            else
                proxyRotations.Add(home, rotation);

            Instance?.CheckPrimary(home);
        }

        private void CheckPrimary(string externalID)
        {
            // FIXME: For now, just accept first external primary if we are not primary
            if (!isPrimary && string.IsNullOrEmpty(primaryID))
                primaryID = externalID;
        }

        private void LateUpdate()
        {
            // Follow the master pRoot (if there is one)
            if (isPrimary || string.IsNullOrEmpty(primaryID)) return;

            // Position ourselves relative to the primary pRoot, in relation to its own position relative to its vRoot
            transform.position = VirtualRoot.TransformPosition(primaryID, proxyPositions[primaryID]);
            transform.rotation = VirtualRoot.TransformRotation(primaryID, proxyRotations[primaryID]);
        }
    }
}