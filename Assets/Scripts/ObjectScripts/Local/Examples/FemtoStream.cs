using UnityEngine;

namespace UnityCEClient
{
    public class FemtoStream : LocalObject
    {
        public string femtoURL = "not implemented";

        protected override void Start()
        {
            base.Start();

            objectData.Add("url", GetFemtoURL);
        }

        public (System.Type, object) GetFemtoURL()
        {
            return (typeof(System.String), femtoURL);
        }
    }
}

