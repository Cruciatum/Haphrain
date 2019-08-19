using System.IO;
using System.Reflection;

namespace Haphrain
{
    internal static partial class Constants
    {
        internal static readonly string _WORKDIR_ = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

        internal const double _CMDTIMEOUT_ = 5d;

        internal static char slashType = Path.DirectorySeparatorChar;
    }
}
