using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace wServer.realm.entities.player.commands
{
    interface ICommand
    {
        string Command { get; }
        bool RequirePerm { get; }
        void Execute(Player player, string[] args);
    }
}
