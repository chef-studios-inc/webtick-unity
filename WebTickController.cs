using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.NetCode.Hybrid;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.Events;

namespace WebTick {
    [Flags]
    public enum Mode {
        None = 0,
        Client = 1 << 0,
        Server = 1 << 1,
        ThinClient = 1 << 2
    }

    public struct ServerSettings {
        public string url;
        public ushort port;
        public ushort udpPort;
        public ushort tcpPort;
        public ushort statusPort;
        public string apiKey;
        public string apiSecret;
        public string room;
    }

    [Serializable]
    public struct ClientSettings {
        public string url;
        public ushort port;
        public string token;
    }

    public class WebTickController : MonoBehaviour {
        public static WebTickController instance;
        public List<ScriptableObject> spawners;
        public UnityEvent<World> onWorldCreated = new UnityEvent<World>();

        private HashSet<World> createdInvokedWorlds = new HashSet<World>();
        private WaitForSeconds wait = new WaitForSeconds(0.25f);
        private ClientSettings clientSettings = new ClientSettings();
        private ServerSettings serverSettings = new ServerSettings();

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

        // Start is called before the first frame update
        void Start() {
#if !UNITY_WEBGL || UNITY_EDITOR
            // TODO when we have livekit
            // Unity.WebRTC.WebRTC.Initialize();
            // StartCoroutine(Unity.WebRTC.WebRTC.Update());
#endif
        }

        private void OnDestroy() {
#if !UNITY_WEBGL || UNITY_EDITOR
            // TODO When we have livekit
            // Unity.WebRTC.WebRTC.Dispose();
#endif
        }

        public void CreateWorlds()
        {
            if(Application.isEditor) {
                CreateWorlds(Mode.Client);
                CreateWorlds(Mode.Server);
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
            if (mode.HasFlag(Mode.Client)) { 
                var clientGo = new GameObject("Client");
                var clientSettingsProvider = clientGo.AddComponent<Client.ClientSettingsProvider>();
                clientSettings = await clientSettingsProvider.GetClientSettings();
            }
            if(mode.HasFlag(Mode.Server))
            {
                var serverGo = new GameObject("Server");
                var serverSettingsProvider = serverGo.AddComponent<WebTick.Core.Server.ServerSettingsProvider>();
                serverSettings = await serverSettingsProvider.GetServerSettings();
                var healthServer = serverGo.AddComponent<WebTick.Core.Server.HealthReporter.HealthServer>();
                healthServer.StartWithServerSettings(serverSettings);
            }

            NetworkStreamReceiveSystem.DriverConstructor = new Transport.LiveKitDriverConstructor(
                new Transport.LiveKitDriverConstructor.ClientSettings { host = clientSettings.url, port = clientSettings.port },
                new Transport.LiveKitDriverConstructor.ServerSettings { port = serverSettings.port });

            if (mode.HasFlag(Mode.Server)) {
                Debug.Log("attemping to create server world");
                var serverWorld = ClientServerBootstrap.CreateServerWorld("Server");
                InitializeWorld(serverWorld);
                InvokeCreatedWorld(serverWorld);
            }

            if(mode.HasFlag(Mode.Client)) {
                Debug.Log("attemping to create client world");
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
                var endpoint = NetworkEndpoint.AnyIpv4.WithPort(serverSettings.port);
                nsd.Listen(endpoint);
                // TODO when we add livekit
                //var server = serverGo.AddComponent<Server.WebTickServer>();
                //await server.Listen(serverSettings.url, serverSettings.port, serverSettings.udpPort, serverSettings.tcpPort, serverSettings.apiKey, serverSettings.apiSecret, world);

            }

            if(world.IsClient()) {
                Debug.LogFormat("NEIL - Connecting {0}", clientSettings.url);
                // TODO when we add livekit
                //var client = clientGo.AddComponent<Client.WebTickClient>();
                //await client.Connect(clientSettings.url, clientSettings.port, clientSettings.token, world);
                var nsdQuery = world.EntityManager.CreateEntityQuery(typeof(NetworkStreamDriver));
                var nsd = nsdQuery.GetSingleton<NetworkStreamDriver>();
                var endpoint = NetworkEndpoint.Parse(clientSettings.url, clientSettings.port);
                nsd.Connect(world.EntityManager, endpoint);
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
            }
        }

        private void InvokeCreatedWorld(World w) {
            createdInvokedWorlds.Add(w);
            onWorldCreated.Invoke(w);
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

