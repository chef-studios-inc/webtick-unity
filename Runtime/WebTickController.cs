using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.NetCode.Hybrid;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.Events;
using WebTick.Core.Server.HealthReporter;
using WebTick.Livekit.Standalone;
using WebTick.Transport;

namespace WebTick {
    [Flags]
    public enum Mode {
        None = 0,
        Client = 1 << 0,
        Server = 1 << 1,
        ThinClient = 1 << 2
    }

    public struct ServerSettings {
        public string token;
        public string url;
        public ushort status_port;
    }

    [Serializable]
    public struct ClientSettings {
        public string url;
        public string token;
    }

    public class WebTickController : MonoBehaviour {
        [Header("Editor Only In WebTick For Local Testing")]
        public List<string> editorClientTokens;
        public string editorServerToken;
        public string editorWsUrl;
        private int editorClientTokenIndex = 0;

        public static WebTickController instance;
        public List<ScriptableObject> spawners;
        public UnityEvent<World> onWorldCreated = new UnityEvent<World>();

        private HashSet<World> createdInvokedWorlds = new HashSet<World>();
        private WaitForSeconds wait = new WaitForSeconds(0.25f);

        private ClientSettings clientSettings = new ClientSettings();
        private ServerSettings serverSettings = new ServerSettings();

        private HealthServer healthServer = null;
        private WaitForSeconds healthLoopWait = new WaitForSeconds(0.2f);

        private Dictionary<string, LiveKitDriverConstructor.RTCSettings> engineSettings = new Dictionary<string, LiveKitDriverConstructor.RTCSettings>();

        private void Awake() {
            if(instance != null) {
                Debug.LogError("Already a WebTickController");
            }

            instance = this;
#if UNITY_SERVER
    UnityEngine.Debug.Log("SERVER FLAG SET");
#endif

#if UNITY_CLIENT
        UnityEngine.Debug.Log("CLIENT FLAG SET");
#endif
        }

        private void Start()
        {
            if (Application.isEditor)
            {
                NetworkStreamReceiveSystem.DriverConstructor = new IPCAndSocketDriverConstructor();
            }
            else
            {
                NetworkStreamReceiveSystem.DriverConstructor = new Transport.LiveKitDriverConstructor(engineSettings);
            }
        }

        private void OnDestroy() {
            foreach(var handle in engineSettings)
            {
                RTCEngineManager.CleanupEngine(handle.Value.engineHandle);
            }
            instance = null;
        }

        public void CreateWorlds()
        {
            if(Application.isEditor) {
                CreateWorlds(Mode.Client | Mode.Server);
            }
            else {
                var serverSettingsEnvironment = Environment.GetEnvironmentVariable("WEBTICK_SERVER_SETTINGS");
                if(string.IsNullOrEmpty(serverSettingsEnvironment)) {
                    CreateWorlds(Mode.Client);
                }
                else {
                    CreateWorlds(Mode.Server);
                }
            }

            StartCoroutine(NewThinClientLoop());
        }

        private async void CreateWorlds(Mode mode) {
            var worldName = mode.HasFlag(Mode.Server) ? "Server-" : "Client-";
            if (mode.HasFlag(Mode.Server))
            {
                var serverGo = new GameObject("Server");
                var serverSettingsProvider = serverGo.AddComponent<WebTick.Core.Server.ServerSettingsProvider>();
                var rtcEngine = serverGo.AddComponent<RTCEngine>();
                worldName += rtcEngine.ToString();
                var engineHandle = RTCEngineManager.RegisterEngine(rtcEngine);
                this.serverSettings = await serverSettingsProvider.GetServerSettings();
                if (Application.isEditor)
                {
                    serverSettings.url = editorWsUrl;
                    serverSettings.token = editorServerToken;
                }

                engineSettings[worldName] = new LiveKitDriverConstructor.RTCSettings { engineHandle = engineHandle, token = serverSettings.token, url = serverSettings.url };

                await rtcEngine.Connect(new RTCEngine.ConnectParams { token = serverSettings.token, url = serverSettings.url });
                Debug.LogFormat("Server RTC Connected");
                healthServer = serverGo.AddComponent<HealthServer>();
                healthServer.StartWithServerSettings(this.serverSettings);
                // TODO
                //if (!Application.isEditor)
                //{
                //    wsManager = serverGo.AddComponent<WebSocketManager>();
                //    await wsManager.Connect(serverSettings.ws_data_channel_proxy_url);
                //}

            }

            if (mode.HasFlag(Mode.Client)) {
                var clientGo = new GameObject("Client");
                var clientSettingsProvider = clientGo.AddComponent<Client.ClientSettingsProvider>();
                clientSettings = await clientSettingsProvider.GetClientSettings();
                if(Application.isEditor)
                {
                    clientSettings.token = editorClientTokens[editorClientTokenIndex];
                    clientSettings.url = editorWsUrl;
                }
                editorClientTokenIndex++;
                if(editorClientTokenIndex >= editorWsUrl.Length) {
                    throw new Exception("Too Many Clients");
                }
                uint engineHandle = 0;
#if !UNITY_WEBGL
                var clientRTCEngine = clientGo.AddComponent<RTCEngine>();
                engineHandle = RTCEngineManager.RegisterEngine(clientRTCEngine);
#endif

                worldName += editorClientTokenIndex.ToString();
                engineSettings[worldName] = new LiveKitDriverConstructor.RTCSettings { engineHandle = engineHandle, token = clientSettings.token, url = clientSettings.url };
            }

            if (mode.HasFlag(Mode.Server)) {
                Debug.Log("attempting to create server world");
                var serverWorld = ClientServerBootstrap.CreateServerWorld("Server");
                InitializeWorld(serverWorld);
                InvokeCreatedWorld(serverWorld);
            }

            if(mode.HasFlag(Mode.Client)) {
                Debug.Log("attempting to create client world");
                var clientWorld = ClientServerBootstrap.CreateClientWorld("Client");
                InitializeWorld(clientWorld);
                InvokeCreatedWorld(clientWorld);
            }
        }

        private void InitializeWorld(World world) {
            InitializeGhostPrefabs(world);
        }

        public void Connect(World world) {
            if(world.IsServer()) {
                var nsdQuery = world.EntityManager.CreateEntityQuery(typeof(NetworkStreamDriver));
                var nsd = nsdQuery.GetSingleton<NetworkStreamDriver>();
                var endpoint = NetworkEndpoint.AnyIpv4.WithPort(7000);
                nsd.Listen(endpoint);
                healthServer.status = HealthInfo.Status.Running;
                StartCoroutine(HealthCheckLoop());
            }

            if(world.IsClient()) {
                Debug.LogFormat("NEIL connect");
                var nsdQuery = world.EntityManager.CreateEntityQuery(typeof(NetworkStreamDriver));
                var nsd = nsdQuery.GetSingleton<NetworkStreamDriver>();
                var endpoint = NetworkEndpoint.LoopbackIpv4.WithPort(7000);
                nsd.Connect(world.EntityManager, endpoint);
                Debug.LogFormat("NEIL connect 2");
            }
        }

        private void InitializeGhostPrefabs(World world) {
            Debug.LogFormat("Initializing ghost prefabs for world: {0}", world.Name);
            if(spawners.Count == 0) {
                Debug.LogError("[WebTick] No spawners provided, you probably want to add some to the 'spawners' property of this MonoBehaviour");
                return;
            }

            foreach(var so in spawners) {
                var spawner = so as IWebTickSpawner;
                if(spawner == null) {
                    Debug.LogErrorFormat("[WebTick] ScriptableObject is not an IWebTickSpawner, this is a bug");
                    continue;
                }
                var ghostConfig = spawner.GetGhostConfig();

                var e = spawner.CreateGhostEntity(world.EntityManager);
                world.EntityManager.SetName(e, ghostConfig.Name);

                if(spawner.GetViewPrefab() != null) {
                    var goEntity = world.EntityManager.CreateEntity();
                    world.EntityManager.AddComponentObject(goEntity, new GhostPresentationGameObjectPrefab { Client = spawner.GetViewPrefab() });
                    world.EntityManager.SetName(goEntity, $"{ghostConfig.Name}-GameObject");
                    world.EntityManager.AddComponentData(e, new GhostPresentationGameObjectPrefabReference { Prefab = goEntity });
                }

                GhostPrefabCreation.ConvertToGhostPrefab(world.EntityManager, e, ghostConfig);
                Debug.LogFormat("NEIL created ghost prefab: {0} - {1}", world.EntityManager.HasComponent<PredictedGhost>(e), world.IsClient());
            }
        }

        private void InvokeCreatedWorld(World w) {
            createdInvokedWorlds.Add(w);
            onWorldCreated.Invoke(w);
        }

        private IEnumerator HealthCheckLoop()
        {
            while(true)
            {
                yield return healthLoopWait;
                // TODO health check
                //if(wsManager == null)
                //{
                //    continue;
                //}

                //if(wsManager.isError || wsManager.isClosed)
                //{
                //    healthServer.status = HealthInfo.Status.Failed;
                //}
            }
        }

        private IEnumerator NewThinClientLoop() {
            while(true) {
                yield return wait;
                foreach(var w in World.All) {
                    if(createdInvokedWorlds.Contains(w)) {
                        continue;
                    }
                    if(w.IsThinClient()) {
                        InitializeWorld(w);
                        InvokeCreatedWorld(w);
                    }
                }
            }
        }
    }
}

