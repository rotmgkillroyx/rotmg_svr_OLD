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
        public bool RequirePerm { get { return false; } }

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
        public bool RequirePerm { get { return false; } }

        public void Execute(Player player, string[] args)
        {
            StringBuilder sb = new StringBuilder("Players online: ");
            var copy = player.Owner.Players.Values.ToArray();
            for (int i = 0; i < copy.Length; i++)
            {
                if (i != 0) sb.Append(", ");
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

    class ServerCommand : ICommand
    {
        public string Command { get { return "server"; } }
        public bool RequirePerm { get { return false; } }

        public void Execute(Player player, string[] args)
        {
            player.Client.SendPacket(new TextPacket()
            {
                BubbleTime = 0,
                Stars = -1,
                Name = "",
                Text = player.Owner.Name
            });
        }
    }

    class PauseCommand : ICommand
    {
        public string Command { get { return "pause"; } }
        public bool RequirePerm { get { return false; } }

        public void Execute(Player player, string[] args)
        {
            if (player.HasConditionEffect(ConditionEffects.Paused))
            {
                player.ApplyConditionEffect(new ConditionEffect()
                {
                    Effect = ConditionEffectIndex.Paused,
                    DurationMS = 0
                });
                player.Client.SendPacket(new TextPacket()
                {
                    BubbleTime = 0,
                    Stars = -1,
                    Name = "",
                    Text = "Game resumed."
                });
            }
            else
            {
                if (player.Owner.EnemiesCollision.HitTest(player.X, player.Y, 8).OfType<Enemy>().Any())
                    player.Client.SendPacket(new TextPacket()
                    {
                        BubbleTime = 0,
                        Stars = -1,
                        Name = "*Error*",
                        Text = "Not safe to pause."
                    });
                else
                {
                    player.ApplyConditionEffect(new ConditionEffect()
                    {
                        Effect = ConditionEffectIndex.Paused,
                        DurationMS = -1
                    });
                    player.Client.SendPacket(new TextPacket()
                    {
                        BubbleTime = 0,
                        Stars = -1,
                        Name = "",
                        Text = "Game paused."
                    });
                }
            }
        }
    }
}
