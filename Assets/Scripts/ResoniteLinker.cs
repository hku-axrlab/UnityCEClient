using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.Toolbars;
using UnityEngine;

public class ResoniteLinker : MonoBehaviour
{
    [SerializeField] private string ipAddress = "localhost";
    [SerializeField] private uint port = 0;

	[SerializeField] private GameObject rootPrefab; // do we need one of these?
	[SerializeField] private ResonitePrefabMap prefabMap;

	private ClientWebSocket socket = new ClientWebSocket();
    private CancellationTokenSource cts = new CancellationTokenSource();

    private Dictionary<string, GameObject> slotObjects = new Dictionary<string, GameObject>();

    [System.Serializable]
    struct ConnectMsg
    {
        public int msgType;
		public string clientType;
		public int sendRate;

        public ConnectMsg( int _sendRate ) 
        {
            msgType = 0;
            clientType = "unity";
            sendRate = _sendRate;
		}
    }

    async void Start()
    {
		Application.runInBackground = true;
		Debug.Log("Starting resonite linker");

        // connect asynch with CalibrationEnv 
        _ = ConnectLoop();
    }

    private async Task ConnectLoop()
    {
        while (!cts.Token.IsCancellationRequested)
        {
            try
            {
                // attempt setup socket to client
                socket = new ClientWebSocket();
                Debug.Log($"Attempting connection to ws://{ipAddress}:{port}");
                await socket.ConnectAsync(new Uri($"ws://{ipAddress}:{port}"), cts.Token);
                Debug.Log("Connected to CalibrationEnv");

				ConnectMsg connectMsg = new ConnectMsg(30);
                await SendJsonFromObject(connectMsg);

				// start receiving on this client
				await ReceiveLoop();
            }
            catch (Exception ex)
            {
                // throw error, wait before retrying connection
                Debug.LogError("WebSocket connection error: " + ex.Message);
                await Task.Delay(1000); 
            }
        }
    }

    private async Task SendJsonFromObject(object o)
    {
		JObject json = JObject.FromObject(o);
		var encoded = Encoding.UTF8.GetBytes(json.ToString());
		var buffer = new ArraySegment<Byte>(encoded, 0, encoded.Length);

		await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
	}

    private async Task ReceiveLoop()
    {
        // TODO: consider buffer size, 
        // but works for now, ey.
        var buffer = new byte[8192];

        try
        {
            while (socket.State == WebSocketState.Open)
            {
                using var ms = new System.IO.MemoryStream();
                WebSocketReceiveResult result;

                // keep receiving until end of msg
                do
                {
                    result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Debug.Log("Socket closed by server");
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                        return;
                    }

                    ms.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                // get and parse msg
                string msg = Encoding.UTF8.GetString(ms.ToArray());
                // Debug.Log("Received message:\n" + msg);
                ParseJSON(msg);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("WebSocket receive error: " + ex.Message);
        }
    }

    private void ParseJSON(string msg)
    {
        if (string.IsNullOrWhiteSpace(msg)) 
            return;

        try
        {
            var root = JObject.Parse(msg);
            var objects = root["objects"];
			var users = root["users"];

			foreach (var item in objects)
            {
                // Recursive parse van de root slot
                ParseObject(item);
            }

			foreach (var item in users)
			{
				// Recursive parse van de root slot
				// TODO:
                // ParseUser(item);
			}
		}
        catch (Exception ex)
        {
            Debug.LogError("Failed to parse JSON: " + ex);
        }
    }

    private void ParseObject(JToken objectJson, Transform parent = null)
    {
        if (objectJson == null) return;

        // define vars 
        Vector3 position = Vector3.zero;
        Vector3 scale = Vector3.one;
        Quaternion rotation = Quaternion.identity;

        // grab data from slot 
        var id = objectJson["id"]?.Value<string>();
        var nameToken = objectJson["name"];
        var tagToken = objectJson["tag"];
		var homeToken = objectJson["home"]?.Value<string>();
		var transform = objectJson["transform"];
        var posToken = transform["position"];
        var rotToken = transform["rotation"];
        var scaleToken = transform["scale"];

        // skip if no id and/or tag
        // TODO: non tagged shouldn't be send from CalibrationEnv
        // NOTE: this makes an exception for the root!! 
        var tagValue = tagToken != null ? tagToken.Value<string>() : "Untagged";
        if (id != "Root" && (id == null || string.IsNullOrEmpty(tagValue))) return; 

        // process data
        if (posToken != null)
        {
            float x = posToken["X"]?.Value<float>() ?? 0f;
            float y = posToken["Y"]?.Value<float>() ?? 0f;
            float z = posToken["Z"]?.Value<float>() ?? 0f;
            position = new Vector3(x, y, z);
        }

        if (rotToken != null)
        {
            float x = rotToken["X"]?.Value<float>() ?? 0f;
            float y = rotToken["Y"]?.Value<float>() ?? 0f;
            float z = rotToken["Z"]?.Value<float>() ?? 0f;
            float w = rotToken["W"]?.Value<float>() ?? 1f;
            rotation = new Quaternion(x, y, z, w);
        }

        if (scaleToken != null)
        {
            float x = scaleToken["X"]?.Value<float>() ?? 1f;
            float y = scaleToken["Y"]?.Value<float>() ?? 1f;
            float z = scaleToken["Z"]?.Value<float>() ?? 1f;
            scale = new Vector3(x, y, z);
        }
        
        // TODO: assign vRoot based on home...
        if (tagValue == "vRoot")
        {
            // set proxy position & rotation
            VirtualRoot.SetProxyPosition(homeToken, position);
            VirtualRoot.SetProxyRotation(homeToken, rotation);
        }

        // setup or reuse GO based on id
        GameObject obj;
        if (!slotObjects.TryGetValue(id, out obj))
        {
            // setup object based on tag,
            // but make exception for root
            if (id == "Root")
            {
                obj = Instantiate(rootPrefab);
            }
            else
            {
                // All tag values should be in map
                if (prefabMap.map.Contains(tagValue))
                {
                    obj = Instantiate(prefabMap.map[tagValue]);
                }
                else
                {
                    // ignore or debug log missing tag
                    // Debug.LogWarning($"Tag missing from prefabMap: {tagValue}");
                    return;
                }
            }

            obj.name = id;
            slotObjects[id] = obj;
        }

		// update parent and transform
        // FIXME: this GetComponent feels slow here, maybe we can cache it for spawned objects?
		BaseTemplate baseTemplate = obj.GetComponent<BaseTemplate>();
        obj.name = nameToken != null ? nameToken.Value<string>() : "no_name";
        obj.tag = !string.IsNullOrEmpty(tagValue) ? tagValue : "Untagged";
        if (baseTemplate == null || baseTemplate.IsLive())
        {
            obj.transform.position = VirtualRoot.TransformPosition(homeToken, position);
            obj.transform.rotation = VirtualRoot.TransformRotation(homeToken, rotation);
            obj.transform.localScale = scale;
        }

        // Send component data to object for optional parsing
        if (baseTemplate != null )
			baseTemplate.HandleVariables(objectJson["data"]);
        
        // set parent if passed 
        // NOTE: rn only the root can be a parent
        // since we only call to depth = 0
        if (parent != null) obj.transform.parent = parent;

        // recursive call for children
        // TODO: don't know if this is still usefull
        // used to do this when started at root 
        /*var children = slotNode["children"];
        if (children != null)
        {
            foreach (var child in children)
            {
                ParseSlot(child, obj.transform);
            }
        }*/
    }

    private void OnDestroy()
    {
        cts.Cancel();
        socket?.Dispose();
    }
}
