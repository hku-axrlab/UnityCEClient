using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UnityCEClient
{
    [RequireComponent(typeof(Light))]
    public class GenericSpot : RemoteObject
	{
		new private Light light;

		protected override void Awake()
		{
			base.Awake();

			light = GetComponent<Light>();
			if ( light.type != LightType.Spot )
			{
				Debug.LogWarning("Light prefab not set to SpotLight. Auto updating.");
                light.type = LightType.Spot;
            }

			valueFunctions.Add("color", HandleColor);
			valueFunctions.Add("intensity", HandleIntensity);
			valueFunctions.Add("radius", HandleRadius);
            valueFunctions.Add("angle", HandleAngle);
        }

		private void HandleColor(JToken variableMember)
		{
			Color c;
			c.r = variableMember["r"].Value<float>();
			c.g = variableMember["g"].Value<float>();
			c.b = variableMember["b"].Value<float>();
			c.a = variableMember["a"].Value<float>();

			light.color = c;
		}

		private void HandleIntensity(JToken variableMember)
		{
			float intensity = variableMember.Value<float>();
			light.intensity = intensity;
		}

		private void HandleRadius(JToken variableMember)
		{
			float radius = variableMember.Value<float>();
			light.range = radius;
		}

        private void HandleAngle(JToken variableMember)
        {
            float angle = variableMember.Value<float>();
            light.spotAngle = angle;
        }
    }
}