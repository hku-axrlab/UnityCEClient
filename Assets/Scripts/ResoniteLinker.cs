using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class ResoniteLinker : MonoBehaviour
{
    [SerializeField] private uint port = 0;
    
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
                socket = new ClientWebSocket();
                Debug.Log($"Attempting connection to ws://localhost:{port}");
                await socket.ConnectAsync(new Uri($"ws://localhost:{port}"), cts.Token);
                Debug.Log("Connected to CalibrationEnv");

                await ReceiveLoop();
            }
            catch (Exception ex)
            {
                Debug.LogError("WebSocket connection error: " + ex.Message);
                await Task.Delay(1000); // wait before retrying connections
            }
        }
    }

    private async Task ReceiveLoop()
    {
        var buffer = new byte[8192];

        try
        {
            while (socket.State == WebSocketState.Open)
            {
                using var ms = new System.IO.MemoryStream();
                WebSocketReceiveResult result;

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
            var dataNode = root["data"];
            if (dataNode == null) return;

            // Recursive parse van de root slot
            ParseSlot(dataNode);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to parse JSON: " + ex);
        }
    }

    private void ParseSlot(JToken slotNode, Transform parent = null)
    {
        if (slotNode == null) return;

        // Pak id en position
        var id = slotNode["id"]?.Value<string>();
        var posToken = slotNode["position"]?["value"];
        if (id == null || posToken == null) return;

        float x = posToken["x"]?.Value<float>() ?? 0f;
        float y = posToken["y"]?.Value<float>() ?? 0f;
        float z = posToken["z"]?.Value<float>() ?? 0f;
        Vector3 position = new Vector3(x, y, z);

        // Setup of reuse GO
        GameObject obj;
        if (!slotObjects.TryGetValue(id, out obj))
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = id;
            slotObjects[id] = obj;
        }

        // Update parent en position
        obj.transform.position = position;
        if (parent != null) obj.transform.parent = parent;

        // Recurse children
        var children = slotNode["children"];
        if (children != null)
        {
            foreach (var child in children)
            {
                ParseSlot(child, obj.transform);
            }
        }
    }

    private void OnDestroy()
    {
        cts.Cancel();
        socket?.Dispose();
    }
}
