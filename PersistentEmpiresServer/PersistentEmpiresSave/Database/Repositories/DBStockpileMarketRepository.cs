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

using Dapper;
using PersistentEmpiresLib.Database.DBEntities;
using PersistentEmpiresLib.PersistentEmpiresMission.MissionBehaviors;
using PersistentEmpiresLib.SceneScripts;
using PersistentEmpiresLib.SceneScripts.Extensions;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;

namespace PersistentEmpiresSave.Database.Repositories
{
    public class DBStockpileMarketRepository
    {
        public static void Initialize()
        {
            SaveSystemBehavior.OnGetAllStockpileMarkets += GetAllStockpileMarkets;
            SaveSystemBehavior.OnGetStockpileMarket += GetStockpileMarket;
            SaveSystemBehavior.OnUpsertStockpileMarkets += UpsertStockpileMarkets;
        }

        private static DBStockpileMarket CreateDBStockpileMarket(PE_StockpileMarket stockpileMarket)
        {
            Debug.Print("[Save Module] CREATE DB STOCKPILE MARKET (" + stockpileMarket != null ? " " + stockpileMarket.GetMissionObjectHash() : "STOCKPILE MARKET IS NULL !)");
            return new DBStockpileMarket
            {
                MissionObjectHash = stockpileMarket.GetMissionObjectHash(),
                MarketItemsSerialized = stockpileMarket.SerializeStocks()
            };
        }
        public static IEnumerable<DBStockpileMarket> GetAllStockpileMarkets()
        {
            Debug.Print("[Save Module] LOADING ALL STOCKPILE MARKETS FROM DB");
            return DBConnection.Connection.Query<DBStockpileMarket>("SELECT * FROM StockpileMarkets");
        }
        public static DBStockpileMarket GetStockpileMarket(PE_StockpileMarket stockpileMarket)
        {
            Debug.Print("[Save Module] LOAD STOCKPILE FROM DB (" + stockpileMarket.GetMissionObjectHash() + ")");
            IEnumerable<DBStockpileMarket> result = DBConnection.Connection.Query<DBStockpileMarket>("SELECT * FROM StockpileMarkets WHERE MissionObjectHash = @MissionObjectHash", new { MissionObjectHash = stockpileMarket.GetMissionObjectHash() });
            Debug.Print("[Save Module] LOAD STOCKPILE FROM DB (" + stockpileMarket.GetMissionObjectHash() + ") RESULT COUNT " + result.Count());
            if (result.Count() == 0) return null;
            return result.First();
        }
        public static void UpsertStockpileMarkets(List<PE_StockpileMarket> markets)
        {
            Debug.Print($"[Save Module] INSERT/UPDATE FOR {markets.Count()} MARKETS TO DB");
            if (markets.Any())
            {
                string query = @"
            INSERT INTO StockpileMarkets (MissionObjectHash, MarketItemsSerialized)
            VALUES ";

                foreach (var market in markets)
                {
                    var dbMarket = CreateDBStockpileMarket(market);

                    query += $"('{dbMarket.MissionObjectHash}', '{dbMarket.MarketItemsSerialized}'),";
                }
                // remove last ","
                query = query.TrimEnd(',');
                query += @" 
                    ON DUPLICATE KEY UPDATE
                    MarketItemsSerialized = VALUES(MarketItemsSerialized)";

                DBConnection.Connection.Execute(query);
            }
        }
    }
}
