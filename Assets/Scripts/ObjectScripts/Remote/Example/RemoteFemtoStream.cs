using Newtonsoft.Json.Linq;
using UnityCEClient;
using UnityEngine;

public class RemoteFemtoStream : RemoteObject
{
    public string url;

    protected override void Awake()
    {
        base.Awake();
        valueFunctions.Add("url", HandleURL);
    }

    private void HandleURL(JToken variableMember)
    {
        string newUrl = variableMember.Value<string>("url");
        if ( newUrl != url )
        {
            url = newUrl;
            Debug.Log("TODO: INITIALIZE FEMTO STREAM");
        }
    }
}
