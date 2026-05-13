using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityCEClient
{
	[Serializable]
	public struct UserData
	{
		public string id;
		public string name;
		public List<string> boneNames;
		public List<TransformData> boneTransforms;
	}

	[Serializable]
	public class Vector3Data
	{
		public float x, y, z;

        public Vector3Data()
		{
			x = y = z = 0;
		}

        public Vector3Data(Vector3 source)
		{
			x = source.x;
			y = source.y;
			z = source.z;
		}

		public void Update(Vector3 source)
		{
			x = source.x;
			y = source.y;
			z = source.z;
		}

        public static implicit operator Vector3(Vector3Data source)
        {
            Vector3 p;
            p.x = source.x;
            p.y = source.y;
            p.z = source.z;
            return p;
        }
    }

	[Serializable]
	public class QuaternionData
	{
		public float x, y, z, w;

        public QuaternionData()
        {
            x = y = z = w = 0;
        }

        public QuaternionData(Quaternion source)
		{
			x = source.x;
			y = source.y;
			z = source.z;
			w = source.w;
		}

		public void Update(Quaternion source)
		{
			x = source.x;
			y = source.y;
			z = source.z;
			w = source.w;
		}

        public static implicit operator Quaternion(QuaternionData source)
        {
            Quaternion r;
            r.x = source.x;
            r.y = source.y;
            r.z = source.z;
			r.w	= source.w;
            return r;
        }
    }

	[Serializable]
	public class TransformData
	{
		public Vector3Data position;
		public QuaternionData rotation;
		public Vector3Data scale;

        public TransformData()
		{
			position = new Vector3Data();
			rotation = new QuaternionData();
			scale = new Vector3Data();

        }

        public static TransformData From(Transform t)
		{
			TransformData data = new TransformData();

			if (t != null)
			{
				data.position = new Vector3Data(t.position);
				data.rotation = new QuaternionData(t.rotation);
				data.scale = new Vector3Data(t.localScale);
			}

			return data;
		}

		public void Update(Transform t)
		{
			if (t == null) return;

			position.Update(t.position);
			rotation.Update(t.rotation);
			scale.Update(t.localScale);
		}
	}

	[Serializable]
	struct UserMsg
	{
		public int msgType;
		public UserData[] users;

		public UserMsg( LocalUser[] data )
		{
			msgType = (int)MessageType.ClientData;
			users = new UserData[data.Length];
			for( int i = 0; i < data.Length; ++i )
			{
				users[i] = data[i].GetUserData();
			}
		}
	}

	/// <summary>
	/// Base class for any kind of user that needs to be sent.
	/// Inherit from this class, and simply add your transforms to the list of bones.
	/// Make sure they have the correct names that you want to send.
	/// 
	/// Name doesn't need to be unique, but can for instance represent the "kind of user" you want to display.
	/// </summary>
	public class LocalUser : MonoBehaviour
	{
		public const int USER_SEND_DELAY = 33;

		public string userName = "UnityUser";
		public string id = "";

		public List<Transform> bones = new List<Transform>();

		private UserData data = new UserData();
		public UserData GetUserData()
		{
			return data;
		}

		protected virtual void Start()
		{
			if ( string.IsNullOrEmpty(id) )
				id = Guid.NewGuid().ToString();

            data.id = id;
            data.name = userName;

            // Force a specific tag for now
            CalibrationEnvLinker.RegisterUser(this);
        }

		protected virtual void Update()
		{
			// Update our user data
			if (data.boneNames == null || data.boneTransforms == null)
			{
				data.boneNames = new List<string>(bones.Count);
				data.boneTransforms = new List<TransformData>(bones.Count);

				foreach (Transform t in bones)
				{
					data.boneNames.Add(t.name);
					data.boneTransforms.Add(TransformData.From(t));
				}
			}
			else
			{
				// We assume the bone counts didn't change
				for (int i = 0; i < bones.Count; ++i)
				{
					data.boneTransforms[i].Update(bones[i]);
				}
			}
		}
	}
}