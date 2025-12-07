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
        public int rewardItemTypeId;
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
