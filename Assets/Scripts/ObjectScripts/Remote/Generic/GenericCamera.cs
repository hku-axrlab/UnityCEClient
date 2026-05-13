using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UnityCEClient
{
    [RequireComponent(typeof(Camera))]
    public class GenericCamera : RemoteObject
	{
		new private Camera camera;

		protected override void Awake()
		{
			base.Awake();

			camera = GetComponent<Camera>();
			camera.usePhysicalProperties = true;
			camera.sensorSize = new Vector2(64, 36);    // 16:9 aspect, manually configured by comparing values from Resonite
			valueFunctions.Add("fov", HandleFOV);
		}

		private void HandleFOV(JToken variableMember)
		{
            // obtained value is actually in mm already
			// even though JSON value is called "fov"
            float focalLength = variableMember["value"].Value<float>();
            camera.focalLength = focalLength;
		}
	}
}