using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ResonitePrefabMap", menuName = "Scriptable Objects/ResonitePrefabMap")]
public class ResonitePrefabMap : ScriptableObject
{
	public SerializableDictionary<string, GameObject> map = new SerializableDictionary<string, GameObject>();
}