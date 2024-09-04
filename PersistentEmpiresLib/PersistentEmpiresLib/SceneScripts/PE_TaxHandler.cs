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

using PersistentEmpiresLib.PersistentEmpiresMission.MissionBehaviors;
using TaleWorlds.MountAndBlade;

namespace PersistentEmpiresLib.SceneScripts
{
    public class PE_TaxHandler : MissionObject
    {
        public int CastleId = -1;
        public int TaxPercentage = 10;

        public void AddTaxFeeToMoneyChest(int amount)
        {
            if (GameNetwork.IsServer == false) return;
            if (this.CastleId == -1) return;
            MoneyChestBehavior behavior = Mission.Current.GetMissionBehavior<MoneyChestBehavior>();
            behavior.AddTaxFromHandler(this, amount);
        }
    }
}
