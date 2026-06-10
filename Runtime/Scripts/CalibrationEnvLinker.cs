using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UnityCEClient
{
    public enum MessageType
    { 
        Connect = 0,
        ResoniteData = 1,
        ClientData = 2
    }

    public class CalibrationEnvLinker : MonoBehaviour
    {
        [SerializeField] private string ipAddress = "localhost";
        [SerializeField] private uint port = 0;
        [Space]
        [SerializeField] private RemotePrefabMap objectMap;
        [SerializeField] private RemotePrefabMap userMap;
        [Space]
        [SerializeField] private bool showDebugs = false;

        private static CalibrationEnvLinker _instance;

        private ClientWebSocket socket = new ClientWebSocket();
        private CancellationTokenSource cts = new CancellationTokenSource();

        private Dictionary<string, GameObject> spawnedObjects = new Dictionary<string, GameObject>();
        private Dictionary<string, RemoteObject> spawnedObjectTemplateScripts = new Dictionary<string, RemoteObject>();
        private Dictionary<string, LocalUser> localUsers = new Dictionary<string, LocalUser>();
        private Dictionary<string, LocalObject> localObjects = new Dictionary<string, LocalObject>();
        private Dictionary<string, RemoteUser> remoteUsers = new Dictionary<string, RemoteUser>();

        [Serializable]
        struct ConnectMsg
        {
            public int msgType;
            public string clientType;
            public int sendRate;

            public ConnectMsg(int _sendRate)
            {
                msgType = (int)MessageType.Connect;
                clientType = "unity";
                sendRate = _sendRate;
            }
        }

		private void Awake()
		{
			_instance = this;
		}

        private void OnDestroy()
        {
            cts.Cancel();
            socket?.Dispose();
        }

        private void Start()
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

                    var mainCtx = SynchronizationContext.Current;

                    // start receiving on this client
                    Task recTask = Task.Run(() => ReceiveLoop(cts.Token, mainCtx), cts.Token);
                    Task sendTask = Task.Run(() => SendLoop(cts.Token), cts.Token);

					// Block until EITHER task finishes (normally or with an error)
					Task finished = await Task.WhenAny(recTask, sendTask);

					// Re-await the finished task to surface any exception it carried
					await finished;
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

        private async Task ReceiveLoop(CancellationToken token, SynchronizationContext syncCtx)
        {
            // TODO: consider buffer size, 
            // but works for now, ey.
            var buffer = new byte[8192];

            try
            {
                while (socket.State == WebSocketState.Open && !token.IsCancellationRequested)
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
                    ParseJSON(msg, syncCtx);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("WebSocket receive error: " + ex.Message);
            }
        }

		private async Task SendLoop(CancellationToken token)
		{
			try
			{
				while (socket.State == WebSocketState.Open && !token.IsCancellationRequested)
				{
                    // TODO: figure out if we need to do more accurate timing here...
                    if (localObjects.Count > 0)
                        await SendJsonFromObject(new ObjectMsg(localObjects.Values.ToArray()));
                    if (localUsers.Count > 0)
                        await SendJsonFromObject(new UserMsg(localUsers.Values.ToArray()));
                    await Task.Delay(LocalUser.USER_SEND_DELAY);    // Calculate based on desired sendRate?
				}
			}
			catch (Exception ex)
			{
				Debug.LogError("WebSocket receive error: " + ex.Message + ex.StackTrace);
			}
		}

		private void ParseJSON(string msg, SynchronizationContext syncCtx)
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
                    ParseObject(item, syncCtx);
                }

                foreach (var item in users)
                {
                    // Recursive parse van de root slot
                    // TODO:
                    ParseUser(item, syncCtx);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to parse JSON: " + ex);
            }
        }

        private void ParseUser(JToken userJson, SynchronizationContext syncCtx)
        {
            if (userJson == null) return;

            // grab data from slot 
            var id = userJson["id"]?.Value<string>();
            var name = userJson["name"].Value<string>();
            var home = userJson["home"].Value<string>();
            var boneNames = userJson["boneNames"];
            var boneTransforms = userJson["boneTransforms"];

            RemoteUser user;
            // Find or create a RemoteUser for this user
            if (!remoteUsers.ContainsKey(id)){
                if (userMap.map.ContainsKey(name))
                    syncCtx.Send(_ => CreateUser(userMap.map[name], id, name, home), null);
                else
                    syncCtx.Send(_ => CreateUser((GameObject)Resources.Load("Prefabs/Users/defaultRemoteUser"), id, name, home), null);
            }
            user = remoteUsers[id];

            List<string> parsedBoneNames = new List<string>();
            List<TransformData> parsedBoneTransforms = new List<TransformData>();

            foreach (var boneName in boneNames) {
                parsedBoneNames.Add(boneName.Value<string>());
            }

            foreach(var boneTransform in boneTransforms.AsEnumerable())
            {
                var position = boneTransform["position"];
                var rotation = boneTransform["rotation"];
                var scale = boneTransform["scale"];

                TransformData data = new TransformData();
                data.position.x = position["X"].Value<float>();
                data.position.y = position["Y"].Value<float>();
                data.position.z = position["Z"].Value<float>();

                data.rotation.x = rotation["X"].Value<float>();
                data.rotation.y = rotation["Y"].Value<float>();
                data.rotation.z = rotation["Z"].Value<float>();
                data.rotation.w = rotation["W"].Value<float>();

                data.scale.x = scale["X"].Value<float>();
                data.scale.y = scale["Y"].Value<float>();
                data.scale.z = scale["Z"].Value<float>();

                parsedBoneTransforms.Add(data);
            }

            syncCtx.Post(_ => user.ApplyTransforms(parsedBoneNames.ToArray(), parsedBoneTransforms.ToArray()), null);
        }

        private void ParseObject(JToken objectJson, SynchronizationContext syncCtx, Transform parent = null)
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

            // skip if no id and/or tag - shouldn't been send from CalibrationEnv anyways
            var tagValue = tagToken != null ? tagToken.Value<string>() : "Untagged";
            if (id == null || string.IsNullOrEmpty(tagValue)) return;

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
            else if ( tagValue == "pRoot" )
            {
                // set proxy position & rotation
                PhysicalRoot.SetProxyPosition(homeToken, position);
                PhysicalRoot.SetProxyRotation(homeToken, rotation);

                // TODO: Do we want to support arbitrarily scaling of the physical root?
                //          Could be cool to support, but I'm not sure it's entirely viable
            }

            // setup or reuse GO based on id
            GameObject obj;
            if (!spawnedObjects.TryGetValue(id, out obj))
            {
                // All tag values should be in map
                if (objectMap.map.Contains(tagValue))
                {
                    syncCtx.Send(_ => CreateObject(objectMap.map[tagValue], id, nameToken != null ? nameToken.Value<string>() : "no_name", tagValue), null);
                    obj = spawnedObjects[id];
                }
                else
                {
                    // ignore or debug log missing tag
                    // Debug.LogWarning($"Tag missing from prefabMap: {tagValue}");
                    return;
                }
            }

            // update parent and transform
            RemoteObject baseTemplate = spawnedObjectTemplateScripts[id];           
            if (baseTemplate == null || baseTemplate.IsLive())
            {
                syncCtx.Post(_ => TransformObject(obj, VirtualRoot.TransformPosition(homeToken, position), VirtualRoot.TransformRotation(homeToken, rotation), scale), null);
            }

            // Send component data to object for optional parsing
            if (baseTemplate != null)
                syncCtx.Post(_ => baseTemplate.HandleVariables(objectJson["data"]), null);

            // set parent if passed 
            // NOTE: rn only the root can be a parent
            // since we only call to depth = 0
            if (parent != null) syncCtx.Post( _ => obj.transform.parent = parent, null);
        }

        private void CreateUser(GameObject prefab, string id, string name, string home)
        {
            GameObject userObj = Instantiate(prefab);
            userObj.name = name;
            RemoteUser user = userObj.GetComponent<RemoteUser>();
            user.name = name;
            user.home = home;
            user.id = id;
            remoteUsers[id] = user;
        }

        private void CreateObject(GameObject prefab, string id, string name, string tag)
        {
            GameObject obj = Instantiate(prefab);
            obj.name = name;
            obj.tag = tag;
            spawnedObjects.Add(id, obj);
            spawnedObjectTemplateScripts.Add(id, obj.GetComponent<RemoteObject>());
        }

        private void TransformObject(GameObject obj, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.transform.localScale = scale;
        }

        public static void RegisterUser(LocalUser user)
        {
            if (_instance == null) return;

            if (!_instance.localUsers.ContainsKey(user.id))
            {
                _instance.localUsers[user.id] = user;
            }
        }

        public static void RegisterObject(LocalObject obj)
        {
            if (_instance == null) return;

            if (!_instance.localObjects.ContainsKey(obj.id))
            {
                _instance.localObjects[obj.id] = obj;
            }
        }

        public static void UnregisterObject(LocalObject obj)
        {
            if (_instance == null) return;

            if (_instance.localObjects.ContainsKey(obj.id))
            {
                _instance.localObjects.Remove(obj.id);
            }
        }
    }
}