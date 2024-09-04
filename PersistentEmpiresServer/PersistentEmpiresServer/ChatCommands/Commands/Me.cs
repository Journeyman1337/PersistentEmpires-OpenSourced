﻿/*
 *  Persistent Empires Open Sourced - A Mount and Blade: Bannerlord Mod
 *  Copyright (C) 2024  Free Software Foundation, Inc.
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU Affero General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.

 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Affero General Public License for more details.
 *
 *  You should have received a copy of the GNU Affero General Public License
 *  along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using TaleWorlds.MountAndBlade;

namespace PersistentEmpiresServer.ChatCommands.Commands
{
    public class Me : Command
    {
        public bool CanUse(NetworkCommunicator networkPeer)
        {
            return true;
        }

        public string Command()
        {
            return "!me";
        }

        public string Description()
        {
            return "Me command.";
        }

        public bool Execute(NetworkCommunicator player, string[] args)
        {
            if (player.ControlledAgent == null) return false;

            var message = "* " + player.UserName + " " + String.Join(" ", args);

            this.SendMessageToPlayers(player, 30, message);

            return true;
        }
    }
}
