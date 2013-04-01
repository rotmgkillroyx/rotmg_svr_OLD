using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using wServer.svrPackets;

namespace wServer.realm.entities.player.commands
{
    class TutorialCommand : ICommand
    {
        public string Command { get { return "tutorial"; } }

        public bool RequirePerm
        {
            get { return true; }
        }

        public void Execute(Player player, string[] args)
        {
            player.Client.Reconnect(new ReconnectPacket()
            {
                Host = "",
                Port = 2050,
                GameId = World.TUT_ID,
                Name = "Tutorial",
                Key = Empty<byte>.Array,
            });
        }
    }

    class WhoCommand : ICommand
    {
        public string Command { get { return "who"; } }

        public bool RequirePerm
        {
            get { return true; }
        }

        public void Execute(Player player, string[] args)
        {
            StringBuilder sb = new StringBuilder("Players online: ");
            var copy = player.Owner.Players.Values.ToArray();
            for (int i = 0; i < copy.Length; i++)
            {
                if (i != 0)sb.Append(", ");
                sb.Append(copy[i].Name);
            }

            player.Client.SendPacket(new TextPacket()
            {
                BubbleTime = 0,
                Stars = -1,
                Name = "",
                Text = sb.ToString()
            });
        }
    }
}
