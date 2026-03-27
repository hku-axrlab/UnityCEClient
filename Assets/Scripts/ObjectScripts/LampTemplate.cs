using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
		c.r = variableMember["Value"]["value"]["r"].Value<float>();
		c.g = variableMember["Value"]["value"]["g"].Value<float>();
		c.b = variableMember["Value"]["value"]["b"].Value<float>();
		c.a = variableMember["Value"]["value"]["a"].Value<float>();
		
		light.color = c;
	}

	private void HandleIntensity (JToken variableMember)
	{
		float intensity = variableMember["Value"]["value"].Value<float>();
		light.intensity = intensity;
	}

	private void HandleRadius(JToken variableMember)
	{
		float radius = variableMember["Value"]["value"].Value<float>();
		light.range = radius;
	}
}