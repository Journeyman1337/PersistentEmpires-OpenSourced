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

using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using PersistentEmpiresLib.PersistentEmpiresMission.MissionBehaviors;
using PersistentEmpiresSave.Database;
using PersistentEmpiresSave.Database.Migrations;
using PersistentEmpiresSave.Database.Repositories;
using System;
using System.Diagnostics;
using System.Xml;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;

namespace PersistentEmpiresSave
{
    public class PersistentEmpireSaveSubModule : MBSubModuleBase
    {
        public static string XmlFile = "SaveConfig";
        public static string ConnectionString = "";
        public static string GetConnectionString()
        {
            string xmlPath = ModuleHelper.GetXmlPath("PersistentEmpires", "Configs/" + XmlFile);
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(xmlPath);
            XmlNode element = xmlDocument.SelectSingleNode("/DatabaseConfig/ConnectionString");
            return element.InnerText;
        }
        private static IServiceProvider CreateServices()
        {
            return new ServiceCollection()
                // Add common FluentMigrator services
                .AddFluentMigratorCore()
                .ConfigureRunner(rb => rb
                    .AddMySql5()
                    .WithGlobalConnectionString(ConnectionString)
                    .ScanIn(typeof(InitialTables_20221912001).Assembly).For.Migrations())
                .AddLogging(lb => lb.AddFluentMigratorConsole())
                .BuildServiceProvider(false);
        }
        public static void RunMigrator()
        {
            Debug.Print("[Persistent Empires Save System] Initializing Save System !");
            DBConnection.InitializeSqlConnection(ConnectionString);
            IServiceProvider serviceProvider = CreateServices();
            IMigrationRunner runner = serviceProvider.GetRequiredService<IMigrationRunner>();
            runner.ListMigrations();
            runner.MigrateUp();

            DBBanRecordRepository.Initialize();
            DBLogRepository.Initialize();
            DBPlayerRepository.Initialize();
            DBInventoryRepository.Initialize();
            DBFactionRepository.Initialize();
            DBUpgradeableBuildingRepository.Initialize();
            DBStockpileMarketRepository.Initialize();
            DBHorseMarketRepository.Initialize();
            DBPlayerNameRepository.Initialize();
            DBCastleRepository.Initialize();
            DBBankingRepository.Initialize();
            DBWhitelistRepository.Initialize();

            Debug.Print("[Persistent Empires Save System] Initialization Done !");
        }
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            SaveSystemBehavior.OnStartMigration += RunMigrator;
            ConnectionString = GetConnectionString();
        }
    }
}
