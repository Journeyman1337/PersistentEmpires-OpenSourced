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
 
using PersistentEmpiresLib.SceneScripts;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class UpdateCastle : GameNetworkMessage
    {
        public PE_CastleBanner CastleBanner { get; set; }
        public UpdateCastle()
        {
        }

        public UpdateCastle(PE_CastleBanner CastleBanner)
        {
            this.CastleBanner = CastleBanner;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }

        protected override string OnGetLogFormat()
        {
            return "Castle Updated";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.CastleBanner = (PE_CastleBanner)Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
            this.CastleBanner.CastleIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 200), ref result);
            this.CastleBanner.FactionIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(-1, 200), ref result);

            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(this.CastleBanner.Id);
            GameNetworkMessage.WriteIntToPacket(this.CastleBanner.CastleIndex, new CompressionInfo.Integer(0, 200));
            GameNetworkMessage.WriteIntToPacket(this.CastleBanner.FactionIndex, new CompressionInfo.Integer(-1, 200));
        }
    }
}
