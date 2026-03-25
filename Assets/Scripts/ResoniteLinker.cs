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
    [SerializeField] private uint port = 0;

    [SerializeField] private GameObject rootPrefab;
    [SerializeField] private GameObject lampPrefab;
    [SerializeField] private GameObject audioPrefab;
    [SerializeField] private List<CustomTagObjects> customTagObjects;

    [Serializable]
    public struct CustomTagObjects
    {
        [SerializeField] public string tag;
        [SerializeField] public GameObject prefab;
    }

    private ClientWebSocket socket = new ClientWebSocket();
    private CancellationTokenSource cts = new CancellationTokenSource();

    private Dictionary<string, GameObject> slotObjects = new Dictionary<string, GameObject>();

    async void Start()
    {
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
                Debug.Log($"Attempting connection to ws://localhost:{port}");
                await socket.ConnectAsync(new Uri($"ws://localhost:{port}"), cts.Token);
                Debug.Log("Connected to CalibrationEnv");

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
                Debug.Log("Received message:\n" + msg);
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
            var responsesNode = root["responses"];

            foreach (var item in responsesNode)
            {
                var dataNode = item["data"];
                if (dataNode == null)
                {
                    Debug.LogError($"dataNode was null, in {item}!");
                    return;
                }

                // Recursive parse van de root slot
                ParseSlot(dataNode);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to parse JSON: " + ex);
        }
    }

    private void ParseSlot(JToken slotNode, Transform parent = null)
    {
        if (slotNode == null) return;

        // define vars 
        Vector3 position = Vector3.zero;
        Vector3 scale = Vector3.one;
        Quaternion rotation = Quaternion.identity;

        // grab data from slot 
        var id = slotNode["id"]?.Value<string>();
        var nameToken = slotNode["name"]?["value"];
        var tagToken = slotNode["tag"]?["value"];
        var posToken = slotNode["position"]?["value"];
        var rotToken = slotNode["rotation"]?["value"];
        var scaleToken = slotNode["scale"]?["value"];

        // skip if no id and/or tag
        // TODO: non tagged shouldn't be send from CalibrationEnv
        // NOTE: this makes an exception for the root!! 
        var tagValue = tagToken != null ? tagToken.Value<string>() : "Untagged";
        if (id != "Root" && (id == null || string.IsNullOrEmpty(tagValue))) return; 

        // process data
        if (posToken != null)
        {
            float x = posToken["x"]?.Value<float>() ?? 0f;
            float y = posToken["y"]?.Value<float>() ?? 0f;
            float z = posToken["z"]?.Value<float>() ?? 0f;
            position = new Vector3(x, y, z);
        }

        if (rotToken != null)
        {
            float x = rotToken["x"]?.Value<float>() ?? 0f;
            float y = rotToken["y"]?.Value<float>() ?? 0f;
            float z = rotToken["z"]?.Value<float>() ?? 0f;
            float w = rotToken["w"]?.Value<float>() ?? 1f;
            rotation = new Quaternion(x, y, z, w);
        }

        if (scaleToken != null)
        {
            float x = scaleToken["x"]?.Value<float>() ?? 1f;
            float y = scaleToken["y"]?.Value<float>() ?? 1f;
            float z = scaleToken["z"]?.Value<float>() ?? 1f;
            scale = new Vector3(x, y, z);
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
                switch (tagValue)
                {
                    case "Camera":
                        obj = Camera.main.gameObject;
                        break;

                    case "Lamp":
                        obj = Instantiate(lampPrefab);
                        break;

                    case "Audio":
                        obj = Instantiate(audioPrefab);
                        break;

                    case "Mesh":
                        obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        break;

                    default:
                        obj = Instantiate(customTagObjects.Find(o => o.tag == tagValue).prefab);
                        break;
                }
            }

            obj.name = id;
            slotObjects[id] = obj;
        }

        // update parent and transform
        obj.name = nameToken != null ? nameToken.Value<string>() : "no_name";
        obj.tag = !string.IsNullOrEmpty(tagValue) ? tagValue : "Untagged";
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.transform.localScale = scale;
        
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
