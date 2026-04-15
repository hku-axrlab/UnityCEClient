using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UnityCEClient
{
	public class CameraTemplate : BaseTemplate
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
			float fov = variableMember["value"].Value<float>();
			// TODO: implement mm to fov conversion
			// 13mm = 112 degrees, 250mm = 10 degrees
			camera.focalLength = fov;
		}
	}
}