using System.Collections.Specialized;
using UnityEngine;
using UnityEngine.UIElements;

public class VirtualRoot : MonoBehaviour
{
    private static VirtualRoot Instance { get; set; }

    public static Vector3 proxyPosition = Vector3.zero;
    public static Quaternion proxyRotation = Quaternion.identity;

    private void Awake()
    {
        Instance = this;
    }

    public static Vector3 TransformPosition(Vector3 position)
    {
        if (Instance == null )
            return position;
        else
            // position the object in the same relative position as the source vRoot (stored in proxyPosition)
            return Instance.transform.position + Instance.transform.rotation * Quaternion.Inverse(proxyRotation) * ( position - proxyPosition );
    }

    public static Quaternion TransformRotation(Quaternion rotation)
    {
        if (Instance == null)
            return rotation;
        else
            // rotate the object in the same relative orientation as the source vRoot (stored in proxyEuler)
            return Instance.transform.rotation  * Quaternion.Inverse(proxyRotation) * rotation;
    }
}