namespace Papagei
{
    public class ServerCommandUpdate : CommandUpdate, IPoolable<ServerCommandUpdate>
    {
        public IPool<ServerCommandUpdate> Pool { get; set; }

        public void Reset()
        {
            ResetCore();
        }
    }
}
