﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using wServer.cliPackets;
using wServer.svrPackets;
using wServer.realm.setpieces;
using wServer.realm.worlds;

namespace wServer.realm.entities.player.commands
{


    class SpawnCommand : ICommand
    {
        public string Command { get { return "spawn"; } }
        public bool RequirePerm { get { return true; } }

        public void Execute(Player player, string[] args)
        {
            int num;
            if (args.Length > 0 && int.TryParse(args[0], out num)) //multi
            {
                string name = string.Join(" ", args.Skip(1).ToArray());
                short objType;
                if (!XmlDatas.IdToType.TryGetValue(name, out objType) ||
                    !XmlDatas.ObjectDescs.ContainsKey(objType))
                    player.Client.SendPacket(new TextPacket()
                    {
                        BubbleTime = 0,
                        Stars = -1,
                        Name = "",
                        Text = "Unknown entity!"
                    });
                else
                {
                    for (int i = 0; i < num; i++)
                    {
                        var entity = Entity.Resolve(objType);
                        entity.Move(player.X, player.Y);
                        player.Owner.EnterWorld(entity);
                    }
                }
            }
            else
            {
                string name = string.Join(" ", args);
                short objType;
                if (!XmlDatas.IdToType.TryGetValue(name, out objType) ||
                    !XmlDatas.ObjectDescs.ContainsKey(objType))
                    player.Client.SendPacket(new TextPacket()
                    {
                        BubbleTime = 0,
                        Stars = -1,
                        Name = "",
                        Text = "Unknown entity!"
                    });
                else
                {
                    var entity = Entity.Resolve(objType);
                    entity.Move(player.X, player.Y);
                    player.Owner.EnterWorld(entity);
                }
            }
        }
    }

    class AddEffCommand : ICommand
    {
        public string Command { get { return "addeff"; } }
        public bool RequirePerm { get { return true; } }

        public void Execute(Player player, string[] args)
        {
            try
            {
                player.ApplyConditionEffect(new ConditionEffect()
                {
                    Effect = (ConditionEffectIndex)Enum.Parse(typeof(ConditionEffectIndex), args[0].Trim()),
                    DurationMS = -1
                });
            }
            catch
            {
                player.Client.SendPacket(new TextPacket()
                {
                    BubbleTime = 0,
                    Stars = -1,
                    Name = "",
                    Text = "Invalid effect!"
                });
            }
        }
    }

    class RemoveEffCommand : ICommand
    {
        public string Command { get { return "removeeff"; } }
        public bool RequirePerm { get { return true; } }

        public void Execute(Player player, string[] args)
        {
            try
            {
                player.ApplyConditionEffect(new ConditionEffect()
                {
                    Effect = (ConditionEffectIndex)Enum.Parse(typeof(ConditionEffectIndex), args[0].Trim()),
                    DurationMS = 0
                });
            }
            catch
            {
                player.Client.SendPacket(new TextPacket()
                {
                    BubbleTime = 0,
                    Stars = -1,
                    Name = "",
                    Text = "Invalid effect!"
                });
            }
        }
    }

    class GimmeCommand : ICommand
    {
        public string Command { get { return "gimme"; } }
        public bool RequirePerm { get { return true; } }

        public void Execute(Player player, string[] args)
        {
            string name = string.Join(" ", args.ToArray()).Trim();
            short objType;
            if (!XmlDatas.IdToType.TryGetValue(name, out objType))
            {
                player.Client.SendPacket(new TextPacket()
                {
                    BubbleTime = 0,
                    Stars = -1,
                    Name = "",
                    Text = "Unknown type!"
                });
                return;
            }
            for (int i = 0; i < player.Inventory.Length; i++)
                if (player.Inventory[i] == null)
                {
                    player.Inventory[i] = XmlDatas.ItemDescs[objType];
                    player.UpdateCount++;
                    return;
                }
        }
    }

    class TpCommand : ICommand
    {
        public string Command { get { return "tp"; } }
        public bool RequirePerm { get { return true; } }

        public void Execute(Player player, string[] args)
        {
            int x, y;
            try
            {
                x = int.Parse(args[0]);
                y = int.Parse(args[1]);
            }
            catch
            {
                player.Client.SendPacket(new TextPacket()
                {
                    BubbleTime = 0,
                    Stars = -1,
                    Name = "",
                    Text = "Invalid coordinates!"
                });
                return;
            }
            player.Move(x + 0.5f, y + 0.5f);
            player.SetNewbiePeriod();
            player.UpdateCount++;
            player.Owner.BroadcastPacket(new GotoPacket()
            {
                ObjectId = player.Id,
                Position = new Position()
                {
                    X = player.X,
                    Y = player.Y
                }
            }, null);
        }
    }

    class SetpieceCommand : ICommand
    {
        public string Command { get { return "setpiece"; } }
        public bool RequirePerm { get { return true; } }

        public void Execute(Player player, string[] args)
        {
            try
            {
                ISetPiece piece = (ISetPiece)Activator.CreateInstance(Type.GetType(
                    "wServer.realm.setpieces." + args[0]));
                piece.RenderSetPiece(player.Owner, new IntPoint((int)player.X + 1, (int)player.Y + 1));
            }
            catch
            {
                player.Client.SendPacket(new TextPacket()
                {
                    BubbleTime = 0,
                    Stars = -1,
                    Name = "",
                    Text = "Cannot apply setpiece!"
                });
            }
        }
    }

    class DebugCommand : ICommand
    {
        public string Command { get { return "debug"; } }
        public bool RequirePerm { get { return true; } }

        class Locater : Enemy
        {
            Player player;
            public Locater(Player player)
                : base(0x0d5d)
            {
                this.player = player;
                MovementBehavior = wServer.logic.NullBehavior.Instance;
                AttackBehavior = wServer.logic.NullBehavior.Instance;
                ReproduceBehavior = wServer.logic.NullBehavior.Instance;
                ApplyConditionEffect(new ConditionEffect()
                {
                    Effect = ConditionEffectIndex.Invincible,
                    DurationMS = -1
                });
            }
            public override void Tick(RealmTime time)
            {
                Move(player.X, player.Y);
                UpdateCount++;
                base.Tick(time);
            }
        }

        public void Execute(Player player, string[] args)
        {
            player.Owner.EnterWorld(new Locater(player));
        }
    }
    class KillAll : ICommand
    {
        public string Command { get { return "killall"; } }
        public bool RequirePerm { get { return true; } }

        public void Execute(Player player, string[] args)
        {
            try
            {                
                 foreach (var i in player.Owner.Enemies)
                    {                        
                        if ((i.Value.ObjectDesc != null )&&                              
                            (i.Value.ObjectDesc.ObjectId != null ) &&
                            (i.Value.ObjectDesc.ObjectId.Contains(args[0])))
                        {                        
                          // i.Value.Damage(player, new RealmTime(), 100 * 1000, true); //may not work for ents/liches
                           i.Value.Owner.LeaveWorld(i.Value);
                        }
                 }
            }
            catch
            {
                player.Client.SendPacket(new TextPacket()
                {
                    BubbleTime = 0,
                    Stars = -1,
                    Name = "",
                    Text = "Cannot killall!"
                });                
            }
        }
    }

    class KillAllX : ICommand //this version gives XP points, but does not work for enemies with evaluation/inv periods
    {
        public string Command { get { return "killallx"; } }
        public bool RequirePerm { get { return true; } }

        public void Execute(Player player, string[] args)
        {
            try
            {
                foreach (var i in player.Owner.Enemies)
                {
                    if ((i.Value.ObjectDesc != null) &&
                        (i.Value.ObjectDesc.ObjectId != null) &&
                        (i.Value.ObjectDesc.ObjectId.Contains(args[0])))
                    {
                        i.Value.Damage(player, new RealmTime(), 100 * 1000, true); //may not work for ents/liches, 
                        //i.Value.Owner.LeaveWorld(i.Value);
                    }
                }
            }
            catch
            {
                player.Client.SendPacket(new TextPacket()
                {
                    BubbleTime = 0,
                    Stars = -1,
                    Name = "",
                    Text = "Cannot killall!"
                });
            }
        }
    }


    class Kick : ICommand
    {
        public string Command { get { return "kick"; } }
        public bool RequirePerm { get { return true; } }

        public void Execute(Player player, string[] args)
        {
            try
            {
                World destworld = RealmManager.GetWorld(World.VAULT_ID);
                foreach (var w in RealmManager.Worlds)
                {
                    World world = w.Value;
                    if (w.Key != 0)
                    {
                        foreach (var i in world.Players)
                        {
                            if (i.Value.Name.ToLower() == args[0].ToLower().Trim())
                            {
                                i.Value.Client.SendPacket(new ReconnectPacket()
                                      {
                                          Host = "",
                                          Port = 2050,
                                          GameId = destworld.Id,
                                          Name = destworld.Name,
                                          Key = Empty<byte>.Array,
                                      });
                            }
                        }
                    }                                                              
                }
            }
            catch
            {
                player.Client.SendPacket(new TextPacket()
                {
                    BubbleTime = 0,
                    Stars = -1,
                    Name = "",
                    Text = "Cannot kick!"
                });
            }
        }
    }

    class GetQuest : ICommand
    {
        public string Command { get { return "getquest"; } }
        public bool RequirePerm { get { return true; } }
        
        public void Execute(Player player, string[] args)
        {
            try
            {
                player.Owner.BroadcastPacket(new TextPacket()
                {
                    BubbleTime = 0,
                    Stars = -1,
                    Name = "Quest",
                    Text = "Loc: " + player.Quest.X + " " + player.Quest.Y
                }, null);
            }
            catch
            {
                player.Client.SendPacket(new TextPacket()
                {
                    BubbleTime = 0,
                    Stars = -1,
                    Name = "",
                    Text = "Cannot find quest!"
                });
            }
        }
    }

     class OryxSay : ICommand
    {
        public string Command { get { return "oryxsay"; } }
        public bool RequirePerm { get { return true; } }     

        public void Execute(Player player, string[] args)
        {
            try
            {
                string saytext = string.Join(" ", args);

                player.Owner.BroadcastPacket(new TextPacket()
                {
                    Name = "#" + "Oryx the Mad God",
                    ObjectId = 0x0932,
                    Stars = -1,
                    BubbleTime = 0,
                    Recipient = "",
                    Text = saytext,
                    CleanText = ""
                }, null);
            }
            catch
            {
                player.Client.SendPacket(new TextPacket()
                {
                    BubbleTime = 0,
                    Stars = -1,
                    Name = "",
                    Text = "Cannot say that!"
                });
            }
        }
    }

     class ListCommands : ICommand
     {
         public string Command { get { return "commands"; } }
         public bool RequirePerm { get { return true; } }

         public void Execute(Player player, string[] args)
         {
             try
             {
                     Dictionary<string, ICommand> cmds = new Dictionary<string, ICommand>();
                             var t = typeof(ICommand);
                             foreach (var i in t.Assembly.GetTypes())
                                 if (t.IsAssignableFrom(i) && i != t)
                                 {
                                     var instance = (ICommand)Activator.CreateInstance(i);
                                     cmds.Add(instance.Command, instance);
                                 }                         

                 StringBuilder sb = new StringBuilder("Commands: ");
                 var copy = cmds.Values.ToArray();
                 for (int i = 0; i < copy.Length; i++)
                 {
                     if (i != 0) sb.Append(", ");
                     sb.Append(copy[i].Command);
                 }

                 player.Client.SendPacket(new TextPacket()
                 {
                     BubbleTime = 0,
                     Stars = -1,
                     Name = "",
                     Text = sb.ToString()
                 });
             }
             catch
             {
                 player.Client.SendPacket(new TextPacket()
                 {
                     BubbleTime = 0,
                     Stars = -1,
                     Name = "",
                     Text = "Cannot say that!"
                 });
             }
         }
     }

    class SWhoCommand : ICommand //get all players from all worlds (this may become too large!)
    {
        public string Command { get { return "swho"; } }
        public bool RequirePerm { get { return true; } }

        public void Execute(Player player, string[] args)
        {
            StringBuilder sb = new StringBuilder("All conplayers: ");

            foreach (var w in RealmManager.Worlds)
            {
                World world = w.Value;
                if (w.Key != 0)
                {
                    var copy = world.Players.Values.ToArray();
                    if (copy.Length != 0)
                    {
                        for (int i = 0; i < copy.Length; i++)
                        {
                            sb.Append(copy[i].Name);
                            sb.Append(", ");
                        }
                    }
                }
            }
            string fixedString = sb.ToString().TrimEnd(',', ' '); //clean up trailing ", "s

            player.Client.SendPacket(new TextPacket()
            {
                BubbleTime = 0,
                Stars = -1,
                Name = "",
                Text = fixedString
            });
        }
    }

    class Announcement : ICommand
    {
        public string Command { get { return "announcement"; } } //msg all players in all worlds
        public bool RequirePerm { get { return true; } }

        public void Execute(Player player, string[] args)
        {

            try
            {
                string saytext = string.Join(" ", args);

                foreach (var w in RealmManager.Worlds)
                {
                    World world = w.Value;
                    if (w.Key != 0)
                    {
                        world.BroadcastPacket(new TextPacket()
                        {
                            Name = "#" + "Announcement",
                            //ObjectId = 0x0932,
                            ObjectId = 0x00,
                            Stars = -1,
                            BubbleTime = 0,
                            //Recipient = "", //this makes yellow text
                            Text = saytext,
                            CleanText = saytext
                        }, null);
                    }
                }          

            }
            catch
            {
                player.Client.SendPacket(new TextPacket()
                {
                    BubbleTime = 0,
                    Stars = -1,
                    Name = "",
                    Text = "Cannot say that in announcement!"
                });
            }
        }
    }

    class RTeleport : ICommand
    {
        public string Command { get { return "rteleport"; } }
        public bool RequirePerm { get { return true; } }

        public void Execute(Player player, string[] args)
        {
            try
            {
                foreach (var i in player.Owner.Players)
                {
                    if (i.Value.Name.ToLower() == args[0].ToLower().Trim())
                    {
                        i.Value.Teleport(new RealmTime(), new cliPackets.TeleportPacket()
                        {
                            ObjectId = player.Id
                        });
                        return;
                    }
                }
                player.Client.SendPacket(new TextPacket()
                {
                    BubbleTime = 0,
                    Stars = -1,
                    Name = "",
                    Text = string.Format("Cannot rteleport, {0} not found!", args[0].Trim())
                });
            }
            catch
            {
                player.Client.SendPacket(new TextPacket()
                {
                    BubbleTime = 0,
                    Stars = -1,
                    Name = "",
                    Text = "Cannot rtp!"
                });
            }
        }
    }

    class KillCommand : ICommand
    {
        public string Command { get { return "kill"; } }
        public bool RequirePerm { get { return true; } }

        public void Execute(Player player, string[] args)
        {
            try
            {                
                foreach (var w in RealmManager.Worlds)
                {
                    World world = w.Value;
                    if (w.Key != 0) // 0 is limbo??
                    {
                        foreach (var i in world.Players) 
                        {
                            //Unnamed becomes a problem: skip them
                            if (i.Value.Name.ToLower() == args[0].ToLower().Trim() && i.Value.NameChosen) 
                            {
                                i.Value.Death("Moderator");                                
                                return;
                            }
                        }
                    }
                }
                player.Client.SendPacket(new TextPacket()
                {
                    BubbleTime = 0,
                    Stars = -1,
                    Name = "",
                    Text = string.Format("Cannot /kill, {0} not found!", args[0].Trim())
                });      
            }
            catch
            {
                player.Client.SendPacket(new TextPacket()
                {
                    BubbleTime = 0,
                    Stars = -1,
                    Name = "",
                    Text = "Cannot kill!"
                });
            }
        }
    }

}
