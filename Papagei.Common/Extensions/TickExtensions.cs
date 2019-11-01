namespace Papagei
{
    public static class TickExtensions
    {
        public static bool IsSendTick(this Tick tick)
        {
            return tick.IsValid ? tick.RawValue % Config.NETWORK_SEND_RATE == 0 : false;
        }
    }
}
