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

namespace PersistentEmpiresLib.Database.DBEntities
{
    public class DBFactions
    {
        public int Id { get; set; }
        public int FactionIndex { get; set; }
        public string Name { get; set; }
        public string BannerKey { get; set; }
        public string LordId { get; set; }
        public long PollUnlockedAt { get; set; }
        public string Marshalls { get; set; }

    }
}
