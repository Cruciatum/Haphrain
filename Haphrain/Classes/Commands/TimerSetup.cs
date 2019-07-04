using System.Timers;

namespace Haphrain.Classes.Commands
{
    internal static class Extensions
    {
        internal static Timer StartTimer(this Timer t, ElapsedEventHandler handler, ulong interval)
        {
            t.Interval = interval;
            t.Elapsed += handler;
            t.Enabled = true;

            return t;
        }
        internal static Timer StartTimer(this Timer t, ElapsedEventHandler handler, int interval)
        {
            t.Interval = interval;
            t.Elapsed += handler;
            t.Enabled = true;

            return t;
        }
    }
}
