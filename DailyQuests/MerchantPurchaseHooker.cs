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

            // 修复：获取商店模板物品以正确计算价格
            // 商店模板物品保留了正确的 StackCount，而事件传入的 item 是新创建的实例，StackCount 可能为默认值
            var templateItem = shop.GetItemInstanceDirect(item.TypeID);
            int price;
            if (templateItem != null)
            {
                // 使用商店模板物品计算价格，确保包含正确的堆叠数量
                price = shop.ConvertPrice(templateItem, false);
            }
            else
            {
                // 后备方案：使用事件传入的物品（可能不准确）
                price = shop.ConvertPrice(item, false);
            }

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
