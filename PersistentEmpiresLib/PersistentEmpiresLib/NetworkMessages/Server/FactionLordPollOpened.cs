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

using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class FactionLordPollOpened : GameNetworkMessage
    {
        public FactionLordPollOpened() { }

        public FactionLordPollOpened(NetworkCommunicator pollCreator, NetworkCommunicator lordCandidate)
        {
            this.PollCreator = pollCreator;
            this.LordCandidate = lordCandidate;
        }

        public NetworkCommunicator PollCreator { get; private set; }
        public NetworkCommunicator LordCandidate { get; private set; }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.General;
        }

        protected override string OnGetLogFormat()
        {
            return "Server started a faction lord polling";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.PollCreator = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);
            this.LordCandidate = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);

            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteNetworkPeerReferenceToPacket(this.PollCreator);
            GameNetworkMessage.WriteNetworkPeerReferenceToPacket(this.LordCandidate);

            // throw new NotImplementedException();
        }
    }
}
