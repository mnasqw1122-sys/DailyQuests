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
            if (shop == null) return;
            // 安全检查：防止管理器未初始化导致报错
            if (DailyQuestManager.Instance == null) return;
            if (!IsTargetMerchant(shop)) return;

            // 修复1：合并购买时，事件传入的 item 可能已在 AddAndMerge 中被合并销毁
            // （StackCount 归零 → DestroyTree），因此价格计算不依赖该实例。
            // 游戏侧 BuyTask 付款用的就是商店模板实例，这里优先用模板重算，
            // 与游戏实际扣款公式（GetTotalRawValue × PriceFactor）完全一致。
            int typeId = 0;
            try
            {
                if (item != null) typeId = item.TypeID;
            }
            catch (Exception)
            {
                // 物品可能已被标记销毁，无法读取 TypeID
            }

            if (typeId <= 0) return;

            int price = 0;
            var templateItem = shop.GetItemInstanceDirect(typeId);
            if (templateItem != null)
            {
                price = shop.ConvertPrice(templateItem, false);
            }
            else
            {
                // 修复2：模板尚未缓存时（理论防御分支，BuyTask 本身要求模板已缓存），
                // 用一次性实例重算并立即销毁，避免旧逻辑用事件物品导致价格偏低
                var temp = ItemAssetsCollection.InstantiateSync(typeId);
                if (temp != null && temp.TypeID == typeId)
                {
                    try
                    {
                        price = shop.ConvertPrice(temp, false);
                    }
                    finally
                    {
                        if (temp != null) Destroy(temp.gameObject);
                    }
                }
                else if (item != null)
                {
                    price = shop.ConvertPrice(item, false);
                }
            }

            if (price <= 0) return;
            DailyQuestManager.Instance.OnMerchantPurchase(price);
        }

        private bool IsTargetMerchant(StockShop shop)
        {
            // 优先检查 Key (更稳健，支持多语言)
            if (!string.IsNullOrEmpty(shop.DisplayNameKey))
            {
                if (shop.DisplayNameKey == TargetMerchantKey1 || shop.DisplayNameKey == TargetMerchantKey2) return true;
            }

            // 后备检查 DisplayName (仅作兼容)
            return string.Equals(shop.DisplayName, TargetMerchantName, StringComparison.Ordinal);
        }
    }
}