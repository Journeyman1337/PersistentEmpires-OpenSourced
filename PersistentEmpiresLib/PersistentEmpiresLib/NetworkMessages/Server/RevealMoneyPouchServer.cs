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
using TaleWorlds.MountAndBlade.Network.Messages;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class RevealMoneyPouchServer : GameNetworkMessage
    {
        public NetworkCommunicator Player;
        public int Gold;

        public RevealMoneyPouchServer()
        {

        }
        public RevealMoneyPouchServer(NetworkCommunicator Player, int Gold)
        {
            this.Player = Player;
            this.Gold = Gold;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.General;
        }

        protected override string OnGetLogFormat()
        {
            return "Money pouch revealed";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.Player = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);
            this.Gold = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, Int32.MaxValue, true), ref result);

            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteNetworkPeerReferenceToPacket(this.Player);
            GameNetworkMessage.WriteIntToPacket(this.Gold, new CompressionInfo.Integer(0, Int32.MaxValue, true));
        }
    }
}
