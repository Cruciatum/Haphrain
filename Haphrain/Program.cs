using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Haphrain
{
    class Program
    {
        private DiscordSocketClient Client;
        private CommandService Commands;
        private IServiceProvider Provider;
        private XmlDocument GuildsFile = new XmlDocument();
        private readonly string GuildsFileLoc = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location).Replace(@"bin\Debug\netcoreapp2.1", @"Data\Guilds.xml");

        static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

        private async Task MainAsync()
        {
            Client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Debug
            });

            Commands = new CommandService(new CommandServiceConfig
            {
                CaseSensitiveCommands = false,
                DefaultRunMode = RunMode.Async,
                LogLevel = LogSeverity.Debug
            });

            Provider = new ServiceCollection()
                .AddSingleton(Client)
                .AddSingleton(Commands)
                .BuildServiceProvider();

            Client.MessageReceived += Client_MessageReceived;
            await Commands.AddModulesAsync(Assembly.GetEntryAssembly(), null);

            Client.Ready += Client_Ready;
            Client.Log += Client_Log;
            Client.JoinedGuild += Client_JoinedGuild;
            Client.LeftGuild += Client_LeftGuild;

            string Token = "";
            using (var s = new FileStream((Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)).Replace(@"bin\Debug\netcoreapp2.1", @"Data\Token.txt"), FileMode.Open, FileAccess.Read))
            {
                using (var r = new StreamReader(s))
                {
                    Token = r.ReadToEnd();
                }
            }
            await Client.LoginAsync(TokenType.Bot, Token);
            await Client.StartAsync();

            await Task.Delay(-1);
        }

        private async Task CheckGuildsStartup()
        {
            GuildsFile.Load(GuildsFileLoc);
            var root = GuildsFile.DocumentElement;
            foreach (SocketGuild g in Client.Guilds)
            {
                if (GuildsFile.SelectSingleNode($"/Guilds/Guild[@GuildID='{g.Id}']") == null)
                {
                    await Client_JoinedGuild(g);
                }
            }
        }

        private async Task Client_LeftGuild(SocketGuild arg)
        {
            Console.WriteLine($"{DateTime.Now} -> Left guild: {arg.Id}");

            GuildsFile.Load(GuildsFileLoc);
            var root = GuildsFile.DocumentElement;
            var guildNode = GuildsFile.SelectSingleNode($"/Guilds/Guild[@GuildID='{arg.Id}']");

            root.RemoveChild(guildNode);

            GuildsFile.Save(GuildsFileLoc);

            await Task.Delay(100);

        }

        private async Task Client_JoinedGuild(SocketGuild arg)
        {
            Console.WriteLine($"{DateTime.Now} -> Joined guild: {arg.Id}");

            GuildsFile.Load(GuildsFileLoc);
            //add new guildobject to Guilds file
            var root = GuildsFile.DocumentElement;

            var guildNode = GuildsFile.CreateElement("Guild");
            var guildID = GuildsFile.CreateAttribute("GuildID");
            var prefixNode = GuildsFile.CreateElement("Prefix");
            var nameNode = GuildsFile.CreateElement("GuildName");
            var ownerID = GuildsFile.CreateElement("OwnerID");

            guildID.Value = arg.Id.ToString();
            guildNode.Attributes.Append(guildID);

            nameNode.InnerText = arg.Name;
            guildNode.AppendChild(nameNode);

            ownerID.InnerText = arg.Owner.Id.ToString();
            guildNode.AppendChild(ownerID);
            
            prefixNode.InnerText = "]";
            guildNode.AppendChild(prefixNode);

            root.AppendChild(guildNode);

            GuildsFile.Save(GuildsFileLoc);

            await Task.Delay(100);
        }

        private async Task Client_Log(LogMessage arg)
        {
            Console.WriteLine($"{DateTime.Now} at {arg.Source} -> {arg.Message}");
        }

        private async Task Client_Ready()
        {
            await Client.SetGameAsync("Developing this badboy");
            await CheckGuildsStartup();
        }

        private async Task Client_MessageReceived(SocketMessage arg)
        {
            var msg = arg as SocketUserMessage;
            var context = new SocketCommandContext(Client, msg);

            if (context.Message == null || context.Message.Content == "") return;
            if (context.User.IsBot) return;

            int argPos = 0;
            string guildPrefix = "]";

            GuildsFile.Load(GuildsFileLoc);
            var guildNode = GuildsFile.SelectSingleNode($"/Guilds/Guild[@GuildID='{context.Guild.Id}']");
            var prefixNode = guildNode.ChildNodes.Cast<XmlNode>().SingleOrDefault(n => n.Name == "Prefix");

            if (prefixNode != null) guildPrefix = prefixNode.InnerText;

            if (!(msg.HasStringPrefix(guildPrefix, ref argPos)) && !(msg.HasMentionPrefix(Client.CurrentUser, ref argPos))) return;

            var Result = await Commands.ExecuteAsync(context, argPos, Provider);
            if (!Result.IsSuccess)
            {
                Console.WriteLine($"{DateTime.Now} at Commands -> Something went wrong when executing a command.");
                Console.WriteLine($"Command text: {context.Message.Content} | Error: {Result.ErrorReason}");
            }
        }
    }
}
