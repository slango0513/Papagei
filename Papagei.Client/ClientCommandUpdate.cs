using MessagePack;

namespace Papagei.Client
{
    [MessagePackObject]
    public class ClientCommandUpdate : CommandUpdate, IPoolable<ClientCommandUpdate>
    {
        [IgnoreMember]
        public IPool<ClientCommandUpdate> Pool { get; set; }

        public void Reset()
        {
            ResetCore();
        }

        [IgnoreMember]
        public ClientEntity Entity { get; set; }
    }
}
