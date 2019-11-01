using Papagei;

namespace Playground
{
    //[RegisterEvent]
    public class GameActionEvent : Event
    {
        public int Key;

        public override void Reset()
        {
            base.Reset();
            Key = 0;
        }

        public override void SetDataFrom(Event other)
        {
            var _other = (GameActionEvent)other;
            Key = _other.Key;
        }

        public override void EncodeData(BitBuffer buffer, Tick packetTick)
        {
            buffer.WriteInt(Key);
        }

        public override void DecodeData(BitBuffer buffer, Tick packetTick)
        {
            Key = buffer.ReadInt();
        }
    }
}
