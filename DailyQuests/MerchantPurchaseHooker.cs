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
        private const string TargetMerchantKey1 = "MerchantName_Merchant_Myst";
        private const string TargetMerchantKey2 = "Character_Myst";

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
            // 安全检查：防止管理器未初始化导致报错
            if (DailyQuestManager.Instance == null) return;
            
            bool isTarget = false;
            
            // 优先检查 Key (更稳健，支持多语言)
            if (!string.IsNullOrEmpty(shop.DisplayNameKey))
            {
                if (shop.DisplayNameKey == TargetMerchantKey1 || shop.DisplayNameKey == TargetMerchantKey2) isTarget = true;
            }
            
            // 后备检查 DisplayName (仅作兼容)
            if (!isTarget && string.Equals(shop.DisplayName, TargetMerchantName, StringComparison.Ordinal)) 
            {
                isTarget = true;
            }
            
            if (!isTarget) return;

            int price = shop.ConvertPrice(item, false);
            DailyQuestManager.Instance.OnMerchantPurchase(price);
        }

        private bool IsTargetMerchantOpen()
        {
            var active = TradingUIUtilities.ActiveMerchant as StockShop;
            if (active == null) return false;
            
            if (!string.IsNullOrEmpty(active.DisplayNameKey))
            {
                if (active.DisplayNameKey == TargetMerchantKey1 || active.DisplayNameKey == TargetMerchantKey2) return true;
            }
            
            return string.Equals(active.DisplayName, TargetMerchantName, StringComparison.Ordinal);
        }

        

        
    }
}
