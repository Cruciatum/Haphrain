using Discord.Rest;
using Haphrain.Classes.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace Haphrain.Classes.JsonObjects
{
    internal static class CustomSerialize
    {
        internal static SerializedPoll CreateSerializedPoll(Poll p)
        {
            return new SerializedPoll(p);
        }
    }

    [Serializable]
    internal class SerializedPoll
    {
        private static readonly string pollPath = null; //Edit this to use DB

        public uint PollId { get; set; }
        public RestUserMessage PollMsg { get; set; }


        internal SerializedPoll(Poll p)
        {
            PollId = p.PollId;
            PollMsg = p.PollMessage;
        }

        internal static void Serialize(SerializedPoll poll)
        {
            if (!Directory.Exists(pollPath)) Directory.CreateDirectory(pollPath);
            string filePath = pollPath + $"\\Poll-{poll.PollId}.bin";

            using (Stream ms = File.OpenWrite(filePath))
            {
                BinaryFormatter form = new BinaryFormatter();

                form.Serialize(ms, poll);
                ms.Flush();
            }
        }
    }
}
