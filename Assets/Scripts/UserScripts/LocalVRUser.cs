using UnityEngine;

namespace UnityCEClient
{
	/// <summary>
	/// Sends head & hands transforms, on top of LocalUser data
	/// </summary>
	public class LocalVRUser : LocalUser
	{
		public Transform head, leftHand, rightHand;

		protected override void Start()
		{
			base.Start();

			// add head & hand transforms
			bones.Add(head);
			bones.Add(leftHand);
			bones.Add(rightHand);
		}
	}
}