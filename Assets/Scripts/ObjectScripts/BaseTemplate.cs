using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using UnityEngine;

public delegate void JTokenHandler(JToken token);

/// <summary>
/// The base for all tracked objects from Resonite. Handles the core template variables (live, active), to ensure consistency across all implemented object types.
/// Inherit from this for your own custom objects, if you want to also have the live/active features (make sure those variables are present in Resonite on the object root)
/// Make sure to call the base.Awake, and add your own valueFunctions to handle specific variables.
/// </summary>
public class BaseTemplate : MonoBehaviour
{
    protected bool isActive = true;
	protected bool isLive = true;

	protected Dictionary<string, JTokenHandler> valueFunctions = new Dictionary<string, JTokenHandler>();

	public bool IsLive()
	{
		return isLive;
	}

	public void HandleVariables(JToken data)
	{
		// parse & apply component data
		foreach (var variable in data)
		{
			string varName = variable["name"].Value<string>();
			if (isLive || varName == "live")    // only apply if live, excluding live variable itself
			{
				if (valueFunctions.ContainsKey(varName))
					valueFunctions[varName](variable["value"]);
			}
		}
	}

	protected virtual void Awake()
    {
        // TODO: implement these
        valueFunctions.Add("live", HandleLive);
		valueFunctions.Add("visible", HandleActive);
	}

	private void HandleLive( JToken varJson )
	{
		isLive = varJson["value"].Value<bool>();
	}

	private void HandleActive(JToken varJson)
	{
		isActive = varJson["value"].Value<bool>();
		if (!isActive && gameObject.activeInHierarchy )
			gameObject.SetActive(false);
		else
		if (isActive && !gameObject.activeInHierarchy)
			gameObject.SetActive(true);
	}
}