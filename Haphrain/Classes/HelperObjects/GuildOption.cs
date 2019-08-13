namespace Haphrain.Classes.HelperObjects
{
    internal class GuildOption
    {
        internal ulong GuildID { get; set; }
        internal string GuildName { get; set; }
        internal ulong OwnerID { get; set; }
        internal string Prefix { get; set; }
        internal Options Options { get; set; } = new Options();
    }
}