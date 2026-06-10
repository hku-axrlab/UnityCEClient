using UnityEngine;

namespace UnityCEClient
{
	[CreateAssetMenu(fileName = "RemotePrefabMap", menuName = "Scriptable Objects/RemotePrefabMap")]
	public class RemotePrefabMap : ScriptableObject
	{
		public SerializableDictionary<string, GameObject> map = new SerializableDictionary<string, GameObject>();
	}
}