﻿using PersistentEmpiresLib.Data;
using PersistentEmpiresLib.Helpers;
using PersistentEmpiresLib.NetworkMessages.Client;
using PersistentEmpiresLib.NetworkMessages.Server;
using PersistentEmpiresLib.SceneScripts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace PersistentEmpiresLib.PersistentEmpiresMission.MissionBehaviors
{
    public class MarketItem
    {
        public ItemObject Item;
        public int Stock;
        public int MaximumPrice;
        public float CurrentPrice;
        public int MinimumPrice;
        public int Constant;
        public int Stability;
        public int Tier;
        // X is stock Y is price x^(1/stability) * y = k
        public MarketItem(string itemId, int maximumPrice, int minimumPrice, int stability, int tier)
        {
            this.Item = MBObjectManager.Instance.GetObject<ItemObject>(itemId);
            this.MaximumPrice = maximumPrice;
            this.MinimumPrice = minimumPrice;
            this.Constant = MaximumPrice;
            this.Stability = stability;
            this.CurrentPrice = MathF.Pow(this.Constant, 1f / this.Stability);
            this.Tier = tier;
        }
        public void UpdateReserve(int newStock)
        {
            if (newStock > 999) newStock = 999;
            if (newStock < 0) newStock = 0;

            if (newStock < 1)
            {
                this.CurrentPrice = MathF.Pow(this.Constant, 1f / this.Stability);
            }
            else
            {
                this.CurrentPrice = MathF.Pow(this.Constant / (float)newStock, 1f / this.Stability);
            }
            this.Stock = newStock;
        }
        public int BuyPrice()
        {
            float fakeStock = this.Stock;
            if (this.Stock < 2) return this.MaximumPrice;
            float denominator = MathF.Pow(fakeStock - 1, 1f / this.Stability);
            float numerator = this.Constant;
            int buyPrice = Math.Abs((int)((numerator / denominator) - MathF.Pow(this.CurrentPrice, 1f / this.Stability)));
            if (buyPrice < this.MinimumPrice) buyPrice = this.MinimumPrice;
            return buyPrice;
        }
        public int SellPrice()
        {
            float fakeStock = this.Stock;
            if (this.Stock < 1) fakeStock = 1;
            float denominator = MathF.Pow(fakeStock + 1, 1f / this.Stability);
            float numerator = this.Constant;
            int price = Math.Abs((int)(MathF.Pow(this.CurrentPrice, 1f / this.Stability) - (numerator / denominator)));
            if (price < this.MinimumPrice) price = (this.MinimumPrice * 90) / 100;
            return (price * 85) / 100;
        }
    }
    public class CraftingBox
    {
        public ItemObject BoxItem;
        public int MinTierLevel;
        public int MaxTierLevel;
        public CraftingBox(string itemId, string minTierLevel, string maxTierLevel)
        {
            this.BoxItem = MBObjectManager.Instance.GetObject<ItemObject>(itemId);
            this.MinTierLevel = int.Parse(minTierLevel);
            this.MaxTierLevel = int.Parse(maxTierLevel);
        }
    }

    public class StockpileMarketComponent : MissionNetwork
    {
        public long LastSaveAt = DateTimeOffset.Now.ToUnixTimeSeconds();
        public long SaveDuration = 600;

        public delegate void StockpileMarketOpenHandler(PE_StockpileMarket stockpileMarket, Inventory playerInventory);
        public event StockpileMarketOpenHandler OnStockpileMarketOpen;

        public delegate void StockpileMarketUpdateHandler(PE_StockpileMarket stockpileMarket, int itemIndex, int newStock);
        public event StockpileMarketUpdateHandler OnStockpileMarketUpdate;

        public delegate void StockpileMarketUpdateMultiHandler(PE_StockpileMarket stockpileMarket, List<int> indexes, List<int> stocks);
        public event StockpileMarketUpdateMultiHandler OnStockpileMarketUpdateMultiHandler;

        Dictionary<MissionObject, List<NetworkCommunicator>> openedInventories;

        public static List<MarketItem> MarketItems { get; set; } = new List<MarketItem>();
        public static List<CraftingBox> CraftingBoxes { get; set; } = new List<CraftingBox>();
        public static string XmlFile = "examplemarket"; // itemId*minimum*maximum,itemId*minimum*maximum
        private static string ModuleFolder = "PersistentEmpires";

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            this.AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegisterer.RegisterMode.Add);
            openedInventories = new Dictionary<MissionObject, List<NetworkCommunicator>>();
            LoadXmlConfig();
        }

        private void LoadXmlConfig()
        {
            string xmlPath = ModuleHelper.GetXmlPath(ModuleFolder, "Markets/" + XmlFile);
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(xmlPath);

            LoadMarketItems(xmlDocument.SelectSingleNode("/Market/Tier1Items").InnerText, 1);
            LoadMarketItems(xmlDocument.SelectSingleNode("/Market/Tier2Items").InnerText, 2);
            LoadMarketItems(xmlDocument.SelectSingleNode("/Market/Tier3Items").InnerText, 3);
            LoadMarketItems(xmlDocument.SelectSingleNode("/Market/Tier4Items").InnerText, 4);
            LoadCraftingBoxes(xmlDocument.SelectSingleNode("/Market/CraftingBoxes").InnerText);
        }

        protected void LoadMarketItems(string innerText, int tier)
        {

            if (innerText == "") return;
            foreach (string marketItemStr in innerText.Trim().Split('|'))
            {
                string[] values = marketItemStr.Split('*');
                string itemId = values[0];
                int minPrice = int.Parse(values[1]);
                int maxPrice = int.Parse(values[2]);
                int stability = values.Length > 4 ? int.Parse(values[3]) : 10;

                ItemObject item = MBObjectManager.Instance.GetObject<ItemObject>(itemId);
                if (item == null)
                {
                    Debug.Print(" ERROR IN MARKET SERIALIZATION " + XmlFile + " ITEM ID " + itemId + " NOT FOUND !!! ", 0, Debug.DebugColor.Red);
                }
                MarketItems.Add(new MarketItem(itemId, maxPrice, minPrice, stability, tier));
            }
        }

        protected void LoadCraftingBoxes(string innerText)
        {
            if (innerText == "") return;
            foreach (string craftingBoxStr in innerText.Trim().Split('|'))
            {
                string[] values = craftingBoxStr.Split('*');
                string itemId = values[0];
                string minTierLevel = values[1];
                string maxTierLevel = values[2];
                CraftingBoxes.Add(new CraftingBox(itemId, minTierLevel, maxTierLevel));
            }
        }

        public override void OnRemoveBehavior()
        {
            base.OnRemoveBehavior();
            this.AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegisterer.RegisterMode.Remove);
        }

        public void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegisterer.RegisterMode mode)
        {
            GameNetwork.NetworkMessageHandlerRegisterer networkMessageHandlerRegisterer = new GameNetwork.NetworkMessageHandlerRegisterer(mode);
            if (GameNetwork.IsClient)
            {
                networkMessageHandlerRegisterer.Register<OpenStockpileMarket>(this.HandleOpenStockpileMarketFromServer);
                networkMessageHandlerRegisterer.Register<UpdateStockpileStock>(this.HandleUpdateStockpileStockFromServer);
                networkMessageHandlerRegisterer.Register<UpdateStockpileMultiStock>(this.HandleUpdateStockpileMultiStock);
            }
            else
            {
                networkMessageHandlerRegisterer.Register<RequestBuyItem>(this.HandleRequestBuyItemFromClient);
                networkMessageHandlerRegisterer.Register<RequestSellItem>(this.HandleRequestSellItemFromClient);
                networkMessageHandlerRegisterer.Register<RequestCloseStockpileMarket>(this.HandleRequestCloseStockpileMarketFromClient);
                networkMessageHandlerRegisterer.Register<StockpileUnpackBox>(this.HandleStockpileUnpackBoxFromClient);
            }
        }

        private bool HandleStockpileUnpackBoxFromClient(NetworkCommunicator peer, StockpileUnpackBox message)
        {
            PersistentEmpireRepresentative representative = peer.GetComponent<PersistentEmpireRepresentative>();
            if (representative == null) return false;
            PE_StockpileMarket market = message.StockpileMarket;
            Inventory playerInventory = representative.GetInventory();
            if (message.SlotId >= playerInventory.Slots.Count) return false;
            InventorySlot slot = playerInventory.Slots[message.SlotId];

            CraftingBox box = market.CraftingBoxes.Where(c => c.BoxItem.StringId.Equals(slot.Item.StringId)).FirstOrDefault();
            if (box == null) return false;

            int count = slot.Count;

            for (int i = 0; i < message.StockpileMarket.MarketItems.Count; i++)
            {
                Debug.Print((int)message.StockpileMarket.MarketItems[i].Item.Tier + " ITEM min:" + box.MinTierLevel + " max:" + box.MaxTierLevel);

            }

            var updatedMarketItems = message.StockpileMarket.MarketItems.Select((value, index) => new { index, value }).Where(m => m.value.Tier >= box.MinTierLevel && (int)m.value.Tier <= box.MaxTierLevel).ToList();
            int craftingReward = 0;
            foreach (var marketItem in updatedMarketItems)
            {
                Debug.Print(marketItem.index.ToString());
                marketItem.value.UpdateReserve(marketItem.value.Stock + count);
                this.UpdateStockForPeers(message.StockpileMarket, marketItem.index);
                craftingReward += marketItem.value.SellPrice();
            }
            if (updatedMarketItems.Count > 0)
            {
                craftingReward = ((craftingReward / updatedMarketItems.Count) * 6) * count;
                InformationComponent.Instance.SendMessage("Crafting box imported your reward is " + craftingReward + " denar.", Color.ConvertStringToColor("#4CAF50FF").ToUnsignedInteger(), peer);
                representative.GoldGain(craftingReward);
                List<int> updatedSlots = playerInventory.RemoveCountedItemSynced(box.BoxItem, count);
                foreach (int i in updatedSlots)
                {
                    GameNetwork.BeginModuleEventAsServer(peer);
                    GameNetwork.WriteMessage(new UpdateInventorySlot("PlayerInventory_" + i, playerInventory.Slots[i].Item, playerInventory.Slots[i].Count));
                    GameNetwork.EndModuleEventAsServer();
                }
            }

            return true;
        }

        private void HandleUpdateStockpileMultiStock(UpdateStockpileMultiStock message)
        {
            PE_StockpileMarket stockpileMarket = (PE_StockpileMarket)message.Stockpile;
            for (int i = 0; i < message.Indexes.Count; i++)
            {
                int index = message.Indexes[i];
                int stock = message.Stocks[i];
                stockpileMarket.MarketItems[index].Stock = stock;
            }
            if (OnStockpileMarketUpdateMultiHandler != null)
            {
                OnStockpileMarketUpdateMultiHandler(stockpileMarket, message.Indexes, message.Stocks);
            }
            /*stockpileMarket.MarketItems[message.ItemIndex].UpdateReserve(message.NewStock);
            if (this.OnStockpileMarketUpdate != null)
            {
                this.OnStockpileMarketUpdate(stockpileMarket, message.ItemIndex, message.NewStock);
            }*/
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (DateTimeOffset.Now.ToUnixTimeSeconds() > this.LastSaveAt + this.SaveDuration)
            {
                this.AutoSaveAllMarkets();
                this.LastSaveAt = DateTimeOffset.Now.ToUnixTimeSeconds();
            }
        }



        private void HandleUpdateStockpileStockFromServer(UpdateStockpileStock message)
        {
            PE_StockpileMarket stockpileMarket = (PE_StockpileMarket)message.StockpileMarket;
            stockpileMarket.MarketItems[message.ItemIndex].UpdateReserve(message.NewStock);
            if (this.OnStockpileMarketUpdate != null)
            {
                this.OnStockpileMarketUpdate(stockpileMarket, message.ItemIndex, message.NewStock);
            }
        }


        private void RemoveFromUpdateList(NetworkCommunicator peer, MissionObject StockpileMarketEntity = null)
        {
            if (StockpileMarketEntity != null && this.openedInventories.ContainsKey(StockpileMarketEntity) && this.openedInventories[StockpileMarketEntity].Contains(peer))
            {
                LoggerHelper.LogAnAction(peer, LogAction.PlayerClosesStockpile, null, new object[] {
                        (PE_StockpileMarket)StockpileMarketEntity
                 });
                this.openedInventories[StockpileMarketEntity].Remove(peer);
                return;
            }


            foreach (MissionObject mObject in this.openedInventories.Keys.ToList())
            {
                if (this.openedInventories[mObject].Contains(peer))
                {
                    LoggerHelper.LogAnAction(peer, LogAction.PlayerClosesStockpile, null, new object[] {
                        (PE_StockpileMarket)mObject
                    });
                    this.openedInventories[mObject].Remove(peer);
                }
            }
        }


        public override void OnPlayerDisconnectedFromServer(NetworkCommunicator player)
        {
            if (GameNetwork.IsServer)
            {
                this.RemoveFromUpdateList(player);
            }
        }

        private bool HandleRequestCloseStockpileMarketFromClient(NetworkCommunicator peer, RequestCloseStockpileMarket message)
        {
            this.RemoveFromUpdateList(peer, message.StockpileMarketEntity);
            return true;
        }

        private bool HandleRequestBuyItemFromClient(NetworkCommunicator peer, RequestBuyItem message)
        {
            PersistentEmpireRepresentative persistentEmpireRepresentative = peer.GetComponent<PersistentEmpireRepresentative>();
            if (persistentEmpireRepresentative == null) return false;
            PE_StockpileMarket stockpileMarket = (PE_StockpileMarket)message.StockpileMarket;
            PE_TaxHandler taxHandler = stockpileMarket.GameEntity.GetFirstScriptOfType<PE_TaxHandler>();

            if (peer.ControlledAgent == null || peer.ControlledAgent.IsActive() == false) return false;
            if (peer.ControlledAgent.Position.Distance(stockpileMarket.GameEntity.GlobalPosition) > stockpileMarket.Distance) return false;

            MarketItem marketItem = stockpileMarket.MarketItems[message.ItemIndex];
            if (marketItem.Stock == 0)
            {
                InformationComponent.Instance.SendMessage("There is no stocks", new Color(1f, 0f, 0f).ToUnsignedInteger(), peer);
                return false;
            }
            if (persistentEmpireRepresentative.GetInventory().HasEnoughRoomFor(marketItem.Item, 1) == false)
            {
                InformationComponent.Instance.SendMessage("Inventory is full", new Color(1f, 0, 0).ToUnsignedInteger(), peer);
                return false;
            }
            if (!persistentEmpireRepresentative.ReduceIfHaveEnoughGold(marketItem.BuyPrice()))
            {
                InformationComponent.Instance.SendMessage("You don't have enough gold", new Color(1f, 0f, 0f).ToUnsignedInteger(), peer);
                return false;
            }
            List<int> updatedSlots = persistentEmpireRepresentative.GetInventory().AddCountedItemSynced(marketItem.Item, 1, ItemHelper.GetMaximumAmmo(marketItem.Item));
            foreach (int i in updatedSlots)
            {
                GameNetwork.BeginModuleEventAsServer(peer);
                GameNetwork.WriteMessage(new UpdateInventorySlot("PlayerInventory_" + i, persistentEmpireRepresentative.GetInventory().Slots[i].Item, persistentEmpireRepresentative.GetInventory().Slots[i].Count));
                GameNetwork.EndModuleEventAsServer();
            }
            LoggerHelper.LogAnAction(peer, LogAction.PlayerBuysStockpile, null, new object[] {
                marketItem
            });
            if (taxHandler != null && taxHandler.CastleId != -1) taxHandler.AddTaxFeeToMoneyChest((marketItem.BuyPrice() * taxHandler.TaxPercentage) / 100);
            marketItem.UpdateReserve(marketItem.Stock - 1);
            // SaveSystemBehavior.HandleCreateOrSaveStockpileMarket(stockpileMarket);
            this.UpdateStockForPeers(stockpileMarket, message.ItemIndex);
            // Send a message to update ui
            return true;
        }

        public void AutoSaveAllMarkets()
        {
            List<PE_StockpileMarket> markets = base.Mission.GetActiveEntitiesWithScriptComponentOfType<PE_StockpileMarket>().Select(g => g.GetFirstScriptOfType<PE_StockpileMarket>()).ToList();
            foreach (PE_StockpileMarket market in markets)
            {
                SaveSystemBehavior.HandleCreateOrSaveStockpileMarket(market);
            }
        }

        private void UpdateStockForPeers(PE_StockpileMarket stockpileMarket, int itemIndex)
        {
            MarketItem marketItem = stockpileMarket.MarketItems[itemIndex];
            if (this.openedInventories.ContainsKey(stockpileMarket))
            {
                foreach (NetworkCommunicator toPeer in this.openedInventories[stockpileMarket].ToArray())
                {
                    if (toPeer.IsConnectionActive)
                    {
                        GameNetwork.BeginModuleEventAsServer(toPeer);
                        GameNetwork.WriteMessage(new UpdateStockpileStock(stockpileMarket, marketItem.Stock, itemIndex));
                        GameNetwork.EndModuleEventAsServer();
                    }
                }
            }
        }
        private bool HandleRequestSellItemFromClient(NetworkCommunicator peer, RequestSellItem message)
        {
            PersistentEmpireRepresentative persistentEmpireRepresentative = peer.GetComponent<PersistentEmpireRepresentative>();
            if (persistentEmpireRepresentative == null) return false;
            PE_StockpileMarket stockpileMarket = (PE_StockpileMarket)message.StockpileMarket;
            if (peer.ControlledAgent == null || peer.ControlledAgent.IsActive() == false) return false;
            if (peer.ControlledAgent.Position.Distance(stockpileMarket.GameEntity.GlobalPosition) > stockpileMarket.Distance) return false;
            MarketItem marketItem = stockpileMarket.MarketItems[message.ItemIndex];

            if (!persistentEmpireRepresentative.GetInventory().IsInventoryIncludes(marketItem.Item, 1))
            {
                InformationComponent.Instance.SendMessage("You don't have that item in your inventory", new Color(1f, 0, 0).ToUnsignedInteger(), peer);
                return false;
            }

            bool arrowCheckFlag = true;
            if (marketItem.Item.Type == TaleWorlds.Core.ItemObject.ItemTypeEnum.Arrows ||
               marketItem.Item.Type == TaleWorlds.Core.ItemObject.ItemTypeEnum.Bolts ||
               marketItem.Item.Type == TaleWorlds.Core.ItemObject.ItemTypeEnum.Bullets
                )
            {
                foreach (InventorySlot slot in persistentEmpireRepresentative.GetInventory().Slots)
                {
                    if (slot.Item != null && slot.Item.StringId == marketItem.Item.StringId)
                    {
                        int maxAmmo = ItemHelper.GetMaximumAmmo(marketItem.Item);
                        arrowCheckFlag = maxAmmo == slot.Ammo;
                        if (!arrowCheckFlag) break;
                    }
                }
            }

            if (arrowCheckFlag == false)
            {
                InformationComponent.Instance.SendMessage("You can't sell not filled ammo packs", new Color(1f, 0, 0).ToUnsignedInteger(), peer);
                return false;
            }

            List<int> updatedSlots = persistentEmpireRepresentative.GetInventory().RemoveCountedItemSynced(marketItem.Item, 1);
            foreach (int i in updatedSlots)
            {
                GameNetwork.BeginModuleEventAsServer(peer);
                GameNetwork.WriteMessage(new UpdateInventorySlot("PlayerInventory_" + i, persistentEmpireRepresentative.GetInventory().Slots[i].Item, persistentEmpireRepresentative.GetInventory().Slots[i].Count));
                GameNetwork.EndModuleEventAsServer();
            }
            persistentEmpireRepresentative.GoldGain(marketItem.SellPrice());
            // if (marketItem.Stock > 1000) marketItem.Stock = 1000;
            // LoggerHelper.LogAnAction(peer, LogAction.PlayerSellsStockpile, null, new object[] {
            //     marketItem
            // });
            marketItem.UpdateReserve(marketItem.Stock + 1);
            // SaveSystemBehavior.HandleCreateOrSaveStockpileMarket(stockpileMarket);
            this.UpdateStockForPeers(stockpileMarket, message.ItemIndex);


            return true;
        }

        private void HandleOpenStockpileMarketFromServer(OpenStockpileMarket message)
        {
            PE_StockpileMarket market = (PE_StockpileMarket)message.StockpileMarketEntity;
            /*foreach (int stock in message.Stocks)
            {
                for (int i = 0; i < market.MarketItems.Count; i++)
                {
                    market.MarketItems[i].Stock = stock;
                }
            }*/
            if (this.OnStockpileMarketOpen != null)
            {
                this.OnStockpileMarketOpen(market, message.PlayerInventory);
            }
        }

        private static List<List<T>> ChunkBy<T>(List<T> source, int chunkSize)
        {
            return source
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / chunkSize)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();
        }

        public void OpenStockpileMarketForPeer(PE_StockpileMarket entity, NetworkCommunicator networkCommunicator)
        {
            PersistentEmpireRepresentative persistentEmpireRepresentative = networkCommunicator.GetComponent<PersistentEmpireRepresentative>();
            if (persistentEmpireRepresentative == null) return;
            if (!this.openedInventories.ContainsKey(entity))
            {
                this.openedInventories[entity] = new List<NetworkCommunicator>();
            }
            this.openedInventories[entity].Add(networkCommunicator);

            List<int> indexes = new List<int>();
            for (int i = 0; i < entity.MarketItems.Count; i++)
            {

                MarketItem marketItem = entity.MarketItems[i];
                if (marketItem.Stock <= 0) continue;
                indexes.Add(i);
            }

            List<List<int>> chunks = ChunkBy<int>(indexes, 100);
            foreach (List<int> chunkIndex in chunks)
            {
                List<int> stocks = new List<int>();
                for (int i = 0; i < chunkIndex.Count; i++)
                {
                    int index = chunkIndex[i];
                    stocks.Add(entity.MarketItems[index].Stock);
                }

                GameNetwork.BeginModuleEventAsServer(networkCommunicator);
                GameNetwork.WriteMessage(new UpdateStockpileMultiStock(entity, chunkIndex, stocks));
                GameNetwork.EndModuleEventAsServer();
            }

            GameNetwork.BeginModuleEventAsServer(networkCommunicator);
            GameNetwork.WriteMessage(new OpenStockpileMarket(entity, persistentEmpireRepresentative.GetInventory()));
            GameNetwork.EndModuleEventAsServer();

            LoggerHelper.LogAnAction(networkCommunicator, LogAction.PlayerOpensStockpile, null, new object[] { entity });
        }
    }
}
