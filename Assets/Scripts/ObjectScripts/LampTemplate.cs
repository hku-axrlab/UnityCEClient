using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UnityCEClient
{
	public class LampTemplate : BaseTemplate
	{
		new private Light light;

		protected override void Awake()
		{
			base.Awake();

			light = GetComponent<Light>();

			valueFunctions.Add("color", HandleColor);
			valueFunctions.Add("intensity", HandleIntensity);
			valueFunctions.Add("radius", HandleRadius);
		}

		private void HandleColor(JToken variableMember)
		{
			Color c;
			c.r = variableMember["value"]["r"].Value<float>();
			c.g = variableMember["value"]["g"].Value<float>();
			c.b = variableMember["value"]["b"].Value<float>();
			c.a = variableMember["value"]["a"].Value<float>();

			light.color = c;
		}

		private void HandleIntensity(JToken variableMember)
		{
			float intensity = variableMember["value"].Value<float>();
			light.intensity = intensity;
		}

		private void HandleRadius(JToken variableMember)
		{
			float radius = variableMember["value"].Value<float>();
			light.range = radius;
		}
	}
}