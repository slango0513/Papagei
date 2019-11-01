using Papagei;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MiniUDP;
using System.Threading.Tasks;

namespace Playground
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var program = new Program();
            await program.RunAsync();
        }

        public async Task RunAsync()
        {
            await Task.CompletedTask;

            var host = new ServerWorldHost();
            var world = host.ServiceProvider.GetRequiredService<ServerWorld>();

            var network = new NetCore("NetDemo1.0", true);
            // Responsible for interpreting events from the socket and communicating them to the server.
            network.PeerConnected += (peer, token) =>
            {
                var wrapper = new MiniUDPConnection(peer);
                peer.UserData = wrapper;
                world.AddConnection(wrapper);
            };
            network.PeerClosed += (peer, reason, userKickReason, error) =>
            {
                var wrapper = (MiniUDPConnection)peer.UserData;
                world.RemovePeer(wrapper);
            };

            var clock = new Clock(0.02f);
            clock.OnFixedUpdate += () =>
            {
                network.PollEvents();
                world.Update();

                var evnt = (GameActionEvent)world._pools.CreateEvent(typeof(GameActionEvent));
                evnt.Key = (int)(clock.Time);
                world.QueueEventBroadcast(evnt);
            };

            var loggerFactory = host.ServiceProvider.GetService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<Program>();

            logger.LogInformation("Server Starting...");
            network.Host(44325);
            logger.LogInformation("Server Started.");

            clock.Start();
            while (true)
            {
                clock.Tick();
            }
            network.Stop();
            logger.LogInformation("Server Stopped.");
        }
    }
}
