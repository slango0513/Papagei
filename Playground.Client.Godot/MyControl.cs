using Papagei.Client;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MiniUDP;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Playground.Client
{
    public class MyControl : Control
    {
        public override void _Ready()
        {
            var button = GetNode<Button>("Panel/Button");
            button.Connect("pressed", this, nameof(OnButtonPressed));
            var button2 = GetNode<Button>("Panel/Button2");
            button2.Connect("pressed", this, nameof(OnButton2Pressed));
        }

        private void OnButtonPressed()
        {
            _ = Task.Run(async () =>
            {
                await Task.CompletedTask;

                var array = Enumerable.Range(0, 100);
                var asyncArray = array.ToAsyncEnumerable();
                var length = 0;

                await foreach (var item in asyncArray)
                {
                    length += item;
                }
                unsafe
                {
                    Console.WriteLine($"length: {length}");
                }
            });

            Console.WriteLine("OnButtonPressed");

            //var scene = GD.Load<PackedScene>("res://MyCSGBox.tscn");
            //var node = scene.Instance() as MyCSGBox;
            //var parent = GetParent<Node>() as MySpatial;
            //parent.AddChild(node);

            //var random = new Random();
            //node.Translate(new Vector3(random.Next(-10, 10), 0, random.Next(-5, 5)));
        }

        public NetCore network;
        public ClientWorld world;

        private void OnButton2Pressed()
        {
            Console.WriteLine("OnButton2Pressed");

            var host = new ClientWorldHost();
            world = host.ServiceProvider.GetRequiredService<ClientWorld>();

            var loggerFactory = host.ServiceProvider.GetService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<MyControl>();

            // Unity:
            // GameObject[] prefabs
            // var go = Instantiate(prefabs[archetypeId])
            // go.GetComponent<Controlled/Dummy/MimicEntityBehaviour>().Controlled/Dummy/MimicEntity = entity;
            host.Client_OnControlledCreated += entity =>
            {
                logger.LogInformation($"ControlledCreated entity: {entity.Id} ArchetypeId: {entity.State.ArchetypeId}");

                var scene = GD.Load<PackedScene>("res://Entities/ControlledEntitySpatial.tscn");
                var node = scene.Instance() as ControlledEntitySpatial;

                node.Entity = entity;

                var parent = GetParent<Node>() as MySpatial;
                parent.AddChild(node);
            };
            host.Client_OnDummyCreated += entity =>
            {
                logger.LogInformation($"DummyCreated entity: {entity.Id} ArchetypeId: {entity.State.ArchetypeId}");

                var scene = GD.Load<PackedScene>("res://Entities/DummyEntitySpatial.tscn");
                var node = scene.Instance() as DummyEntitySpatial;

                node.Entity = entity;

                var parent = GetParent<Node>() as MySpatial;
                parent.AddChild(node);
            };
            host.Client_OnMimicCreated += entity =>
            {
                logger.LogInformation($"MimicCreated entity: {entity.Id} ArchetypeId: {entity.State.ArchetypeId}");

                var scene = GD.Load<PackedScene>("res://Entities/MimicEntitySpatial.tscn");
                var node = scene.Instance() as MimicEntitySpatial;

                node.Entity = entity;

                var parent = GetParent<Node>() as MySpatial;
                parent.AddChild(node);
            };

            host.Client_OnGameActionEvent += evnt =>
            {
                //logger.LogWarning($"evnt {evnt.Key}");
            };


            NetConfig.LatencySimulation = true;

            network = new NetCore("NetDemo1.0", false);
            network.PeerConnected += (peer, token) =>
            {
                Console.WriteLine($"PeerConnected peer: {peer.EndPoint} token: {token}");
                var wrapper = new MiniUDPConnection(peer);
                world.SetConnection(wrapper);
            };
            network.PeerClosed += (peer, reason, userKickReason, error) =>
            {
                Console.WriteLine($"PeerClosed peer: {peer.EndPoint} reason: {reason} userKickReason: {userKickReason} error: {error}");
            };
            network.PeerPayload += (peer, data, dataLength) =>
            {
                //Console.WriteLine($"PeerPayload data: {data} dataLength: {dataLength}");
                receivedThisFrame += dataLength;
            };
            network.PeerNotification += (peer, data, dataLength) =>
            {
                //Console.WriteLine($"PeerNotification data: {data} dataLength: {dataLength}");
            };

            logger.LogInformation("Client Connecting...");
            var _peer = network.Connect(NetUtil.StringToEndPoint("127.0.0.1:44325"), "SampleAuthToken");
            logger.LogInformation("Client Connected.");

            //network.Stop();
        }

        const int BANDWIDTH_WINDOW_SIZE = 60;
        readonly int[] bandwidthWindow = new int[BANDWIDTH_WINDOW_SIZE];
        int framesActive = 0;
        int receivedThisFrame = 0;

        public override void _PhysicsProcess(float delta)
        {
            if (network != default && world != default)
            {
                network.PollEvents();
                world.Update();

                UpdateBandwidth();

                // if Input.GetKey...
                // NetConfig.LatencySimulation = true;

                void UpdateBandwidth()
                {
                    bandwidthWindow[framesActive % BANDWIDTH_WINDOW_SIZE] = receivedThisFrame;
                    framesActive++;

                    var sum = 0;
                    foreach (var bytes in bandwidthWindow)
                    {
                        sum += bytes;
                    }

                    var average = sum / (float)BANDWIDTH_WINDOW_SIZE;
                    var kbps = average / delta / 1024.0f;

                    //logger.LogInformation($"framesActive: {framesActive} receivedThisFrame: {receivedThisFrame} KBps: {kbps}");
                    receivedThisFrame = 0;
                }
            }
        }
    }
}
