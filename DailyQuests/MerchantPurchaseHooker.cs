using System;
using System.Linq;
using System.Reflection;
using Duckov.UI;
using Duckov.Economy;
using Duckov.Economy.UI;
using Duckov.Utilities;
using ItemStatsSystem;
using UnityEngine;

namespace DailyQuests
{
    public class MerchantPurchaseHooker : MonoBehaviour
    {
        private const string TargetMerchantName = "神秘商人";

        private void OnEnable()
        {
            StockShop.OnItemPurchased += OnItemPurchased;
        }

        private void OnDisable()
        {
            StockShop.OnItemPurchased -= OnItemPurchased;
        }

        private void OnItemPurchased(StockShop shop, Item item)
        {
            if (shop == null || item == null) return;
            if (!string.Equals(shop.DisplayName, TargetMerchantName, StringComparison.Ordinal)) return;
            int price = shop.ConvertPrice(item, false);
            DailyQuestManager.Instance.OnMerchantPurchase(price);
        }

        private bool IsTargetMerchantOpen()
        {
            var active = TradingUIUtilities.ActiveMerchant as StockShop;
            if (active == null) return false;
            return string.Equals(active.DisplayName, TargetMerchantName, StringComparison.Ordinal);
        }

        

        
    }
}
