using System;
using System.Collections.Generic;
using Duckov.UI;
using Duckov.Utilities;
using ItemStatsSystem;
using UnityEngine;

namespace DailyQuests
{
    public enum DailyTaskDifficulty
    {
        Easy,
        Normal,
        Hard,
        Epic
    }
    public enum DailyTaskType
    {
        UseItem,
        KillEnemy,
        SubmitItem,
        SpendCashAtMerchant,
        ChallengeKill
    }

    [Serializable]
    public class DailyTask
    {
        public int id;
        public string title = string.Empty;
        public string description = string.Empty;
        public DailyTaskType type;
        public int targetItemId;
        public string requireEnemyNameKey = string.Empty;
        public string requireEnemyDisplayName = string.Empty;
        public int requiredAmount;
        public int progress;
        public bool accepted;
        public bool finished;
        public DailyTaskDifficulty difficulty;
        public string rewardPreviewText = string.Empty;
        public int rewardCashAmount;
        public int rewardExpAmount;
        
        [Obsolete("Use rewardItems list instead.")]
        public int rewardItemTypeId;
        [Obsolete("Use rewardItems list instead.")]
        public int rewardItemCount;
        
        public List<RewardItem> rewardItems = new List<RewardItem>();
        public int requiredWeaponItemId;
        public bool rewardClaimed;

        [Serializable]
        public struct RewardItem
        {
            public int typeId;
            public int count;
        }

        /// <summary>
        /// 验证数据完整性并迁移旧版字段。
        /// 应在加载数据后调用。
        /// </summary>
        public void ValidateState()
        {
            // 迁移旧版奖励字段
            if (rewardItems == null) rewardItems = new List<RewardItem>();
            
            #pragma warning disable 612, 618
            if (rewardItems.Count == 0 && rewardItemTypeId > 0 && rewardItemCount > 0)
            {
                rewardItems.Add(new RewardItem { typeId = rewardItemTypeId, count = rewardItemCount });
                rewardItemTypeId = 0;
                rewardItemCount = 0;
            }
            #pragma warning restore 612, 618

            // 限制进度并同步完成状态
            if (requiredAmount > 0)
            {
                if (progress > requiredAmount) progress = requiredAmount;
                if (progress < 0) progress = 0;
                
                if (progress >= requiredAmount) finished = true;
            }

            // 安全措施：如果奖励已领取，任务必须已完成
            if (rewardClaimed) finished = true;
        }

        public Sprite Icon
        {
            get
            {
                if (targetItemId > 0)
                {
                    return ItemAssetsCollection.GetMetaData(targetItemId).icon;
                }
                return GameplayDataSettings.UIStyle.FallbackItemIcon;
            }
        }
    }
}
