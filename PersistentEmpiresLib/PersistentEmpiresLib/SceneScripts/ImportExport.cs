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
using System.Collections.Generic;
using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace PersistentEmpiresLib.SceneScripts
{
    public struct GoodItem
    {
        public ItemObject ItemObj;
        public int ExportPrice;
        public int ImportPrice;
        public GoodItem(ItemObject itemObj, int exportPrice, int importPrice)
        {
            this.ItemObj = itemObj;
            this.ExportPrice = exportPrice;
            this.ImportPrice = importPrice;
        }
    }
    public class PE_ImportExport : PE_UsableFromDistance
    {
        public string XmlFile = "importexport";
        public string ModuleFolder = "PersistentEmpires";
        public string TradeableItems { get; private set; }
        private List<GoodItem> goodItems;
        private ImportExportComponent importExportComponent;
        protected override void OnInit()
        {
            base.OnInit();
            this.importExportComponent = Mission.Current.GetMissionBehavior<ImportExportComponent>();
            TextObject actionMessage = new TextObject("Export/Import some goods for fixed price");
            base.ActionMessage = actionMessage;
            TextObject descriptionMessage = new TextObject("Press {KEY} To Export/Import");
            descriptionMessage.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
            base.DescriptionMessage = descriptionMessage;
            Debug.Print("Initiating ImportExport Market With " + this.ModuleFolder + " Module");
            string xmlPath = ModuleHelper.GetXmlPath(this.ModuleFolder, "Markets/" + this.XmlFile);
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(xmlPath);

            this.TradeableItems = xmlDocument.DocumentElement.InnerText.Trim();
            this.goodItems = new List<GoodItem>();
            foreach (string goodStr in this.TradeableItems.Split('|'))
            {
                string[] goodSplitted = goodStr.Split(',');
                ItemObject itemObject = MBObjectManager.Instance.GetObject<ItemObject>(goodSplitted[0]);
                if (itemObject == null) continue;
                int exportPrice = int.Parse(goodSplitted[1]);
                int importPrice = int.Parse(goodSplitted[2]);
                this.goodItems.Add(new GoodItem(itemObject, exportPrice, importPrice));
            }
        }

        public List<GoodItem> GetGoodItems()
        {
            return this.goodItems;
        }
        public override string GetDescriptionText(GameEntity gameEntity = null)
        {
            return "Import/Export";
        }

        public override void OnUse(Agent userAgent)
        {
            Debug.Print("[USING LOG] AGENT USE " + this.GetType().Name);

            if (!base.IsUsable(userAgent))
            {
                userAgent.StopUsingGameObjectMT(false);
                return;
            }
            base.OnUse(userAgent);
            if (GameNetwork.IsServer)
            {
                this.importExportComponent.OpenImportExportForPeer(userAgent.MissionPeer.GetNetworkPeer(), this);
                userAgent.StopUsingGameObjectMT(true);
            }
        }
    }
}
