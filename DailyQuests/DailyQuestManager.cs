using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Duckov;
using Duckov.UI;
using Duckov.Utilities;
using ItemStatsSystem;
using Duckov.Quests;
using UnityEngine;

namespace DailyQuests
{
    public class DailyQuestManager : MonoBehaviour, Saves.ISaveDataProvider
    {
        [Serializable]
        public struct SaveData
        {
            public string date;
            public List<int> questIds;
            public List<int> accepted;
            public List<int> finished;
            [Serializable]
            public struct TaskSave
            {
                public int id;
                public DailyTaskType type;
                public int targetItemId;
                public int requiredWeaponItemId;
                public string requireEnemyNameKey;
                public string requireEnemyDisplayName;
                public string title;
                public string description;
                public int requiredAmount;
                public int progress;
                public bool accepted;
                public bool finished;
                public int difficulty;
                public string rewardPreviewText;
                public int rewardCashAmount;
                public int rewardExpAmount;
                public int rewardItemTypeId;
                public int rewardItemCount;
                public List<int> rewardItemTypeIds;
                public List<int> rewardItemCounts;
                public bool rewardClaimed;
            }
            public List<TaskSave> tasksFull;
        }

        private static DailyQuestManager _instance = null!;
        public static DailyQuestManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("DailyQuestManager");
                    go.transform.SetParent(LevelManager.Instance.transform, false);
                    _instance = go.AddComponent<DailyQuestManager>();
                }
                return _instance;
            }
        }

        private const int DailyCount = 24;
        private List<DailyTask> tasks = new List<DailyTask>();
        private string currentDateKey = string.Empty;
        private string lastSavedDate = string.Empty;
        private Duckov.Quests.QuestGiver jeff = null!;
        private float updateTimer = 0f;

        public void Initialize()
        {
            VerifyConfig();
            currentDateKey = DateTime.UtcNow.ToString("yyyy-MM-dd");
            Load();
            BackfillRewardsForAllTasks();
            CheckDayChange();
        }

        private void VerifyConfig()
        {
            ValidateAndCleanup(DailyQuestConfig.AllowedUseItemIds, "AllowedUseItemIds");
            ValidateAndCleanup(DailyQuestConfig.AllowedSubmitItemIds, "AllowedSubmitItemIds");
            ValidateAndCleanup(DailyQuestConfig.AllowedRewardItemIds, "AllowedRewardItemIds");
            ValidateAndCleanup(DailyQuestConfig.AllowedAmmoIds, "AllowedAmmoIds");
            ValidateAndCleanup(DailyQuestConfig.HighValueRewardItemIds, "HighValueRewardItemIds");
            ValidateAndCleanup(DailyQuestConfig.HighValueAmmoIds, "HighValueAmmoIds");
        }

        private void ValidateAndCleanup(HashSet<int> list, string listName)
        {
            var toRemove = new List<int>();
            foreach (var id in list)
            {
                var meta = ItemAssetsCollection.GetMetaData(id);
                if (id != 0 && meta.id == 0)
                {
                    Debug.LogWarning($"[DailyQuests] Config Warning: Item ID {id} in {listName} is invalid or missing. Removing from list.");
                    toRemove.Add(id);
                }
            }
            foreach (var id in toRemove)
            {
                list.Remove(id);
            }
        }

        private void Update()
        {
            updateTimer += Time.deltaTime;
            if (updateTimer > 60f) // 每分钟检查一次
            {
                updateTimer = 0f;
                CheckDayChange();
            }
        }

        private void CheckDayChange()
        {
            string now = DateTime.UtcNow.ToString("yyyy-MM-dd");
            if (now != currentDateKey)
            {
                currentDateKey = now;
            }
            
            if (tasks == null || tasks.Count == 0 || currentDateKey != lastSavedDate)
            {
                GenerateNewDaily();
                Save();
            }
        }

        public void AttachJeff(QuestGiver j)
        {
            jeff = j;
        }

        public List<DailyTask> GetDailyTasks() => tasks;

        public bool IsAccepted(int id) => tasks.Find(t => t.id == id)?.accepted == true;
        public bool IsFinished(int id) => tasks.Find(t => t.id == id)?.finished == true;
        public bool IsRewardClaimed(int id) => tasks.Find(t => t.id == id)?.rewardClaimed == true;

        public void Accept(int id)
        {
            var t = tasks.Find(x => x.id == id);
            if (t == null || t.accepted) return;
            t.accepted = true;
            Save();
        }

        public void Abandon(int id)
        {
            var t = tasks.Find(x => x.id == id);
            if (t == null) return;
            t.accepted = false;
            t.progress = 0;
            Save();
        }

        public void ClaimReward(int id)
        {
            var t = tasks.Find(x => x.id == id);
            if (t == null) return;
            if (!t.finished || t.rewardClaimed) return;
            GrantReward(t);
            t.rewardClaimed = true;
            tasks.RemoveAll(x => x.id == id);
            Save();
            NotificationText.Push("已领取任务奖励");
        }

        public void RefreshUnaccepted()
        {
            // 1. 任务刷新死锁修复：如果列表已满，允许放弃过期的任务
            var keep = tasks.Where(t => t.accepted || (t.finished && !t.rewardClaimed)).ToList();
            
            // 如果玩家囤积了太多任务（>= DailyCount），强制放弃最旧的已接取但未完成的任务
            // 为至少3个新任务腾出空间
            if (keep.Count >= DailyCount)
            {
                int slotsNeeded = 3;
                int slotsFreed = 0;
                
                // 如果ID方案稳定，按ID排序是创建时间的粗略代理，
                // 但由于我们没有时间戳，我们只移除未完成的已接取任务
                for (int i = keep.Count - 1; i >= 0; i--)
                {
                    var t = keep[i];
                    if (t.accepted && !t.finished)
                    {
                        keep.RemoveAt(i);
                        slotsFreed++;
                        if (keep.Count <= DailyCount - slotsNeeded) break;
                    }
                }
            }

            if (keep.Count >= DailyCount)
            {
                tasks = keep.Take(DailyCount).ToList();
                Save();
                return;
            }
            
            // 5. ID冲突修复：传递现有ID以从新池中排除它们
            var existingIds = new HashSet<int>(keep.Select(k => k.id));
            var candidates = BuildPool(existingIds);
            
            Shuffle(candidates);
            var newOnes = AssembleWithQuotas(candidates, keep);
            tasks = keep.Concat(newOnes).ToList();
            Save();
        }

        public void OnQuestCompleted(Duckov.Quests.Quest quest)
        {
            // 无操作：原生任务不参与每日任务
        }

        public object GenerateSaveData()
        {
            var data = new SaveData
            {
                date = currentDateKey,
                questIds = new List<int>(),
                accepted = new List<int>(),
                finished = new List<int>(),
                tasksFull = new List<SaveData.TaskSave>()
            };
            foreach (var t in tasks)
            {
                data.questIds.Add(t.id);
                if (t.accepted) data.accepted.Add(t.id);
                if (t.finished) data.finished.Add(t.id);
                data.tasksFull.Add(new SaveData.TaskSave
                {
                    id = t.id,
                    type = t.type,
                    targetItemId = t.targetItemId,
                    requiredWeaponItemId = t.requiredWeaponItemId,
                    requireEnemyNameKey = t.requireEnemyNameKey,
                    requireEnemyDisplayName = t.requireEnemyDisplayName,
                    title = t.title,
                    description = t.description,
                    requiredAmount = t.requiredAmount,
                    progress = t.progress,
                    accepted = t.accepted,
                    finished = t.finished,
                    difficulty = (int)t.difficulty,
                    rewardPreviewText = t.rewardPreviewText,
                    rewardCashAmount = t.rewardCashAmount,
                    rewardExpAmount = t.rewardExpAmount,
                    rewardItemTypeId = (t.rewardItems != null && t.rewardItems.Count > 0) ? t.rewardItems[0].typeId : 0,
                    rewardItemCount = (t.rewardItems != null && t.rewardItems.Count > 0) ? t.rewardItems[0].count : 0,
                    rewardItemTypeIds = (t.rewardItems != null) ? t.rewardItems.ConvertAll(ri => ri.typeId) : new List<int>(),
                    rewardItemCounts = (t.rewardItems != null) ? t.rewardItems.ConvertAll(ri => ri.count) : new List<int>(),
                    rewardClaimed = t.rewardClaimed
                });
            }
            return data;
        }

        public void SetupSaveData(object data)
        {
            if (data is SaveData d)
            {
                lastSavedDate = d.date;
                if (d.tasksFull != null && d.tasksFull.Count > 0)
                {
                    tasks = new List<DailyTask>();
                    foreach (var s in d.tasksFull)
                    {
                        var t = new DailyTask
                        {
                            id = s.id,
                            type = s.type,
                            targetItemId = s.targetItemId,
                            requiredWeaponItemId = s.requiredWeaponItemId,
                            requireEnemyNameKey = s.requireEnemyNameKey,
                            requireEnemyDisplayName = s.requireEnemyDisplayName,
                            title = s.title,
                            description = s.description,
                            requiredAmount = s.requiredAmount,
                            progress = s.progress,
                            accepted = s.accepted,
                            finished = s.finished,
                            difficulty = (DailyTaskDifficulty)s.difficulty,
                            rewardPreviewText = s.rewardPreviewText,
                            rewardCashAmount = s.rewardCashAmount,
                            rewardExpAmount = s.rewardExpAmount,
                            rewardClaimed = s.rewardClaimed
                        };
                        
                        // 恢复奖励物品列表
                        if (s.rewardItemTypeIds != null && s.rewardItemCounts != null && s.rewardItemTypeIds.Count == s.rewardItemCounts.Count)
                        {
                            t.rewardItems = new List<DailyTask.RewardItem>();
                            for(int i=0; i<s.rewardItemTypeIds.Count; i++)
                            {
                                t.rewardItems.Add(new DailyTask.RewardItem{ typeId = s.rewardItemTypeIds[i], count = s.rewardItemCounts[i] });
                            }
                        }
                        else if (s.rewardItemTypeId > 0 && s.rewardItemCount > 0)
                        {
                            t.rewardItems = new List<DailyTask.RewardItem>
                            {
                                new DailyTask.RewardItem { typeId = s.rewardItemTypeId, count = s.rewardItemCount }
                            };
                        }

                        // 如果缺失则回退生成标题
                        if (string.IsNullOrEmpty(t.title))
                        {
                            RegenerateTaskTitle(t);
                        }
                        tasks.Add(t);
                    }
                }
                else
                {
                    // 旧版本支持
                    tasks = new List<DailyTask>();
                    foreach (var id in d.questIds ?? new List<int>())
                    {
                        tasks.Add(CreateTaskById(id));
                    }
                    foreach (var a in d.accepted ?? new List<int>())
                    {
                        var t = tasks.Find(x => x.id == a);
                        if (t != null) t.accepted = true;
                    }
                    foreach (var f in d.finished ?? new List<int>())
                    {
                        var t = tasks.Find(x => x.id == f);
                        if (t != null) t.finished = true;
                    }
                }
            }
        }

        private void RegenerateTaskTitle(DailyTask t)
        {
            if (t.type == DailyTaskType.KillEnemy)
            {
                if (!string.IsNullOrEmpty(t.requireEnemyDisplayName))
                {
                    t.title = $"击杀 {t.requireEnemyDisplayName}";
                    t.description = $"击杀 {t.requireEnemyDisplayName} {t.requiredAmount} 名";
                }
                else
                {
                    t.title = "击杀敌人";
                    t.description = $"击杀白名单敌人 {t.requiredAmount} 名";
                }
            }
            else if (t.type == DailyTaskType.ChallengeKill)
            {
                var wmeta = ItemAssetsCollection.GetMetaData(t.requiredWeaponItemId);
                string wname = wmeta.id > 0 ? wmeta.DisplayName : "指定武器";
                string ename = !string.IsNullOrEmpty(t.requireEnemyDisplayName) ? t.requireEnemyDisplayName : "指定敌人";
                t.title = $"挑战击杀：使用 {wname} 击杀 {ename}";
                t.description = $"使用 {wname} 击杀 {ename} {t.requiredAmount} 名";
            }
            else if (t.type == DailyTaskType.SubmitItem || t.type == DailyTaskType.UseItem)
            {
                var meta = ItemAssetsCollection.GetMetaData(t.targetItemId);
                if (t.type == DailyTaskType.SubmitItem)
                {
                    t.title = $"提交 {meta.DisplayName}";
                    t.description = $"提交 {meta.DisplayName} x{t.requiredAmount}";
                }
                else
                {
                    t.title = $"使用 {meta.DisplayName}";
                    t.description = $"在任意场景使用 {meta.DisplayName} {t.requiredAmount} 次";
                }
            }
        }

        private void Save()
        {
            Saves.SavesSystem.Save("DailyQuests", "Data", GenerateSaveData());
        }

        private void Load()
        {
            try
            {
                var data = Saves.SavesSystem.Load<SaveData>("DailyQuests", "Data");
                SetupSaveData(data);
            }
            catch
            {
            }
        }

        private void GenerateNewDaily()
        {
            var keep = tasks.Where(t => t.accepted || (t.finished && !t.rewardClaimed)).ToList();
            
            // 1. 任务刷新死锁修复（每日重置的重复逻辑）
            if (keep.Count >= DailyCount)
            {
                int slotsNeeded = 5; // 每日重置时更激进
                for (int i = keep.Count - 1; i >= 0; i--)
                {
                    var t = keep[i];
                    if (t.accepted && !t.finished)
                    {
                        keep.RemoveAt(i);
                        if (keep.Count <= DailyCount - slotsNeeded) break;
                    }
                }
            }

            // 5. ID冲突修复
            var existingIds = new HashSet<int>(keep.Select(k => k.id));
            var pool = BuildPool(existingIds);
            
            Shuffle(pool);
            var result = AssembleWithQuotas(pool, keep);
            tasks = keep.Concat(result).Take(DailyCount).ToList();
            lastSavedDate = currentDateKey;
        }

        private List<DailyTask> BuildPool(HashSet<int>? excludedIds = null)
        {
            var result = new List<DailyTask>();
            if (excludedIds == null) excludedIds = new HashSet<int>();
            
            var sourceItemIds = GetSourceItemIds();

            result.AddRange(BuildUseAndSubmitTasks(sourceItemIds, excludedIds));
            result.AddRange(BuildKillEnemyTasks(excludedIds));
            result.AddRange(BuildChallengeTasks(excludedIds));
            result.AddRange(BuildSpendTasks(excludedIds));

            // 如果提交列表为空，确保至少有一个现金提交任务
            if (!result.Exists(t => t.type == DailyTaskType.SubmitItem))
            {
                var cashTask = CreateCashSubmitTask();
                if (!excludedIds.Contains(cashTask.id))
                {
                    result.Add(cashTask);
                }
            }

            Shuffle(result);
            return result;
        }

        private List<int> GetSourceItemIds()
        {
            var unlocked = GameplayDataSettings.Economy.UnlockedItemByDefault;
            List<int> sourceItemIds = new List<int>(unlocked);
            if (sourceItemIds.Count == 0)
            {
                var owned = ItemUtilities.FindAllBelongsToPlayer(e => e != null);
                foreach (var it in owned)
                {
                    if (it == null) continue;
                    if (!sourceItemIds.Contains(it.TypeID)) sourceItemIds.Add(it.TypeID);
                }
            }
            return sourceItemIds;
        }

        private List<DailyTask> BuildUseAndSubmitTasks(List<int> sourceItemIds, HashSet<int> excludedIds)
        {
            var tasks = new List<DailyTask>();
            for (int i = 0; i < sourceItemIds.Count; i++)
            {
                int itemId = sourceItemIds[i];
                var meta = ItemAssetsCollection.GetMetaData(itemId);
                
                // 使用物品任务
                if (DailyQuestConfig.AllowedUseItemIds.Contains(itemId))
                {
                    int id = 100000 + itemId;
                    if (!excludedIds.Contains(id))
                    {
                        int rollUse = UnityEngine.Random.Range(0, 100);
                        DailyTaskDifficulty useDiff = (rollUse < 33) ? DailyTaskDifficulty.Easy : ((rollUse < 66) ? DailyTaskDifficulty.Normal : DailyTaskDifficulty.Hard);
                        int useAmount = useDiff == DailyTaskDifficulty.Easy ? UnityEngine.Random.Range(3, 9) : (useDiff == DailyTaskDifficulty.Normal ? UnityEngine.Random.Range(9, 15) : UnityEngine.Random.Range(15, 21));
                        
                        var useTask = new DailyTask
                        {
                            id = id,
                            type = DailyTaskType.UseItem,
                            targetItemId = itemId,
                            requiredAmount = useAmount,
                            difficulty = useDiff,
                            title = $"使用 {meta.DisplayName}",
                            description = $"在任意场景使用 {meta.DisplayName} {useAmount} 次"
                        };
                        AssignRewardPreview(useTask);
                        AssignDifficulty(useTask); 
                        AdjustTaskByDifficulty(useTask);
                        tasks.Add(useTask);
                    }
                }

                // 提交物品任务
                if (DailyQuestConfig.AllowedSubmitItemIds.Contains(itemId))
                {
                    int id = 200000 + itemId;
                    if (!excludedIds.Contains(id))
                    {
                        bool isAmmo = DailyQuestConfig.AllowedAmmoIds.Contains(itemId);
                        DailyTaskDifficulty subDiff;
                        int submitAmount = 0;
                        int rollSubmit = UnityEngine.Random.Range(0, 100);
                        
                        if (isAmmo)
                        {
                            subDiff = rollSubmit < 40 ? DailyTaskDifficulty.Easy : (rollSubmit < 75 ? DailyTaskDifficulty.Normal : DailyTaskDifficulty.Hard);
                            submitAmount = (subDiff == DailyTaskDifficulty.Easy) ? 60 : (subDiff == DailyTaskDifficulty.Normal ? 120 : 180);
                        }
                        else
                        {
                            subDiff = rollSubmit < 25 ? DailyTaskDifficulty.Easy : (rollSubmit < 55 ? DailyTaskDifficulty.Normal : (rollSubmit < 85 ? DailyTaskDifficulty.Hard : DailyTaskDifficulty.Epic));
                            submitAmount = subDiff == DailyTaskDifficulty.Easy ? UnityEngine.Random.Range(2, 6) : (subDiff == DailyTaskDifficulty.Normal ? UnityEngine.Random.Range(6, 10) : (subDiff == DailyTaskDifficulty.Hard ? UnityEngine.Random.Range(10, 15) : UnityEngine.Random.Range(15, 26)));
                        }
                        
                        var submitTask = new DailyTask
                        {
                            id = id,
                            type = DailyTaskType.SubmitItem,
                            targetItemId = itemId,
                            requiredAmount = submitAmount,
                            difficulty = subDiff,
                            title = $"提交 {meta.DisplayName}",
                            description = $"提交 {meta.DisplayName} x{submitAmount}"
                        };
                        AssignRewardPreview(submitTask);
                        AssignDifficulty(submitTask);
                        AdjustTaskByDifficulty(submitTask);
                        tasks.Add(submitTask);
                    }
                }
            }
            return tasks;
        }

        private List<DailyTask> BuildKillEnemyTasks(HashSet<int> excludedIds)
        {
            var tasks = new List<DailyTask>();
            var presets = GameplayDataSettings.CharacterRandomPresetData?.presets ?? new List<CharacterRandomPreset>();
            presets = presets.Where(p => p != null && (DailyQuestConfig.AllowedEnemyNames.Contains(p.DisplayName) || DailyQuestConfig.AllowedEnemyNames.Contains(p.nameKey))).ToList();
            int killBase = 300000;
            
            // 如果可能，随机化起始索引以避免ID冲突循环，
            // 不过 excludedIds 检查才是真正的修复
            int idx = UnityEngine.Random.Range(0, 500); 

            foreach (var preset in presets)
            {
                int id = killBase + idx++;
                if (excludedIds.Contains(id)) continue;

                bool isBoss = DailyQuestConfig.BossEnemyNames.Contains(preset.DisplayName) || DailyQuestConfig.BossEnemyNames.Contains(preset.nameKey);
                int amount;
                if (isBoss)
                {
                    amount = UnityEngine.Random.Range(3, 10);
                }
                else
                {
                    int roll = UnityEngine.Random.Range(0, 100);
                    if (roll < 30) amount = UnityEngine.Random.Range(3, 9);
                    else if (roll < 70) amount = UnityEngine.Random.Range(9, 14);
                    else amount = UnityEngine.Random.Range(14, 20);
                }
                
                var t = new DailyTask
                {
                    id = id,
                    type = DailyTaskType.KillEnemy,
                    requiredAmount = amount,
                    requireEnemyNameKey = preset.nameKey,
                    requireEnemyDisplayName = preset.DisplayName,
                    title = $"击杀 {preset.DisplayName}",
                    description = $"击杀 {preset.DisplayName} {amount} 名"
                };
                AssignDifficulty(t);
                AssignRewardPreview(t);
                tasks.Add(t);
            }
            return tasks;
        }

        private List<DailyTask> BuildChallengeTasks(HashSet<int> excludedIds)
        {
            var tasks = new List<DailyTask>();
            var presets = GameplayDataSettings.CharacterRandomPresetData?.presets ?? new List<CharacterRandomPreset>();
            presets = presets.Where(p => p != null && (DailyQuestConfig.AllowedEnemyNames.Contains(p.DisplayName) || DailyQuestConfig.AllowedEnemyNames.Contains(p.nameKey))).ToList();
            
            var ownedItems = ItemUtilities.FindAllBelongsToPlayer(e => e != null);
            var ownedWeaponIds = new List<int>();
            foreach (var it in ownedItems)
            {
                try
                {
                    if (it != null && it.Tags != null && it.Tags.Contains("Weapon"))
                    {
                        if (!ownedWeaponIds.Contains(it.TypeID)) ownedWeaponIds.Add(it.TypeID);
                    }
                }
                catch { }
            }

            int challengeBase = 500000;
            int cidx = UnityEngine.Random.Range(0, 500);

            foreach (var preset in presets)
            {
                if (ownedWeaponIds.Count == 0) break;
                
                int id = challengeBase + cidx++;
                if (excludedIds.Contains(id)) continue;

                int pickWeaponIndex = UnityEngine.Random.Range(0, ownedWeaponIds.Count);
                int weaponId = ownedWeaponIds[pickWeaponIndex];
                var wmeta = ItemAssetsCollection.GetMetaData(weaponId);
                bool isBoss = DailyQuestConfig.BossEnemyNames.Contains(preset.DisplayName) || DailyQuestConfig.BossEnemyNames.Contains(preset.nameKey);
                int amount;
                DailyTaskDifficulty diff;
                
                if (isBoss)
                {
                    amount = UnityEngine.Random.Range(3, 9);
                    diff = DailyTaskDifficulty.Epic;
                }
                else
                {
                    int roll = UnityEngine.Random.Range(0, 100);
                    if (roll < 25) { amount = UnityEngine.Random.Range(3, 8); diff = DailyTaskDifficulty.Easy; }
                    else if (roll < 65) { amount = UnityEngine.Random.Range(8, 13); diff = DailyTaskDifficulty.Normal; }
                    else { amount = UnityEngine.Random.Range(12, 19); diff = DailyTaskDifficulty.Hard; }
                }

                var ct = new DailyTask
                {
                    id = id,
                    type = DailyTaskType.ChallengeKill,
                    targetItemId = weaponId, 
                    requiredWeaponItemId = weaponId,
                    requiredAmount = amount,
                    requireEnemyNameKey = preset.nameKey,
                    requireEnemyDisplayName = preset.DisplayName,
                    difficulty = diff,
                    title = $"挑战击杀：使用 {wmeta.DisplayName} 击杀 {preset.DisplayName}",
                    description = $"使用 {wmeta.DisplayName} 击杀 {preset.DisplayName} {amount} 名"
                };
                AdjustTaskByDifficulty(ct);
                AssignRewardPreview(ct);
                tasks.Add(ct);
            }
            return tasks;
        }

        private List<DailyTask> BuildSpendTasks(HashSet<int> excludedIds)
        {
            var tasks = new List<DailyTask>();
            int cashTypeId = GameplayDataSettings.ItemAssets.CashItemTypeID;
            for (int s = 0; s < 10; s++)
            {
                int id = 400000 + s;
                if (excludedIds.Contains(id)) continue;

                int roll = UnityEngine.Random.Range(0, 100);
                DailyTaskDifficulty diff = roll < 30 ? DailyTaskDifficulty.Easy : (roll < 65 ? DailyTaskDifficulty.Normal : DailyTaskDifficulty.Hard);
                int needAmount = diff == DailyTaskDifficulty.Easy ? UnityEngine.Random.Range(10000, 25001) : (diff == DailyTaskDifficulty.Normal ? UnityEngine.Random.Range(30000, 65001) : UnityEngine.Random.Range(80000, 150001));
                
                var spend = new DailyTask
                {
                    id = id,
                    type = DailyTaskType.SpendCashAtMerchant,
                    targetItemId = cashTypeId,
                    requiredAmount = needAmount,
                    title = "在神秘商人处消费现金",
                    description = $"在NPC 神秘商人处购买物品，消费 现金 x{needAmount}",
                    difficulty = diff
                };
                AdjustTaskByDifficulty(spend);
                AssignRewardPreview(spend);
                tasks.Add(spend);
            }
            return tasks;
        }

        private DailyTask CreateCashSubmitTask()
        {
            int cashId = GameplayDataSettings.ItemAssets.CashItemTypeID;
            var cashMeta = ItemAssetsCollection.GetMetaData(cashId);
            var task = new DailyTask
            {
                id = 200000 + cashId,
                type = DailyTaskType.SubmitItem,
                targetItemId = cashId,
                requiredAmount = UnityEngine.Random.Range(1, 3),
                title = $"提交 {cashMeta.DisplayName}",
                description = $"提交 {cashMeta.DisplayName}"
            };
            AssignRewardPreview(task);
            return task;
        }

        private List<DailyTask> AssembleWithQuotas(List<DailyTask> candidates, List<DailyTask> keep)
        {
            int level = EXPManager.Level;
            int quotaEasy, quotaNormal, quotaHard, quotaEpic;

            if (level >= 1 && level <= 10) { quotaEasy = 24; quotaNormal = 0; quotaHard = 0; quotaEpic = 0; }
            else if (level >= 11 && level <= 20) { quotaEasy = 18; quotaNormal = 6; quotaHard = 0; quotaEpic = 0; }
            else if (level >= 21 && level <= 30) { quotaEasy = 10; quotaNormal = 10; quotaHard = 4; quotaEpic = 0; }
            else if (level >= 31 && level <= 40) { quotaEasy = 8; quotaNormal = 8; quotaHard = 5; quotaEpic = 3; }
            else { quotaEasy = 4; quotaNormal = 6; quotaHard = 10; quotaEpic = 4; }

            int usedEasy = keep.Count(t => t.difficulty == DailyTaskDifficulty.Easy);
            int usedNormal = keep.Count(t => t.difficulty == DailyTaskDifficulty.Normal);
            int usedHard = keep.Count(t => t.difficulty == DailyTaskDifficulty.Hard);
            int usedEpic = keep.Count(t => t.difficulty == DailyTaskDifficulty.Epic);

            int needEasy = Mathf.Max(0, quotaEasy - usedEasy);
            int needNormal = Mathf.Max(0, quotaNormal - usedNormal);
            int needHard = Mathf.Max(0, quotaHard - usedHard);
            int needEpic = Mathf.Max(0, quotaEpic - usedEpic);

            var taken = new HashSet<int>(keep.ConvertAll(k => k.id));
            var pick = new List<DailyTask>();

            void take(DailyTaskDifficulty d, int count)
            {
                if (count <= 0) return;
                for (int i = 0; i < candidates.Count && count > 0; i++)
                {
                    var c = candidates[i];
                    if (c.difficulty != d) continue;
                    if (taken.Contains(c.id)) continue;
                    pick.Add(c);
                    taken.Add(c.id);
                    count--;
                }
            }

            take(DailyTaskDifficulty.Easy, needEasy);
            take(DailyTaskDifficulty.Normal, needNormal);
            take(DailyTaskDifficulty.Hard, needHard);
            take(DailyTaskDifficulty.Epic, needEpic);

            int remaining = Mathf.Max(0, DailyCount - (keep.Count + pick.Count));
            for (int i = 0; i < candidates.Count && remaining > 0; i++)
            {
                var c = candidates[i];
                if (taken.Contains(c.id)) continue;
                pick.Add(c);
                taken.Add(c.id);
                remaining--;
            }
            return pick;
        }

        private void AdjustTaskByDifficulty(DailyTask t)
        {
            if (t == null) return;
            switch (t.type)
            {
                case DailyTaskType.UseItem:
                    switch (t.difficulty)
                    {
                        case DailyTaskDifficulty.Easy: t.requiredAmount = Mathf.Clamp(t.requiredAmount, 3, 8); break;
                        case DailyTaskDifficulty.Normal: t.requiredAmount = Mathf.Clamp(t.requiredAmount, 9, 14); break;
                        case DailyTaskDifficulty.Hard: t.requiredAmount = Mathf.Clamp(t.requiredAmount, 15, 20); break;
                        case DailyTaskDifficulty.Epic: t.difficulty = DailyTaskDifficulty.Hard; t.requiredAmount = Mathf.Clamp(t.requiredAmount, 15, 20); break;
                    }
                    {
                        var meta = ItemAssetsCollection.GetMetaData(t.targetItemId);
                        t.title = $"使用 {meta.DisplayName}";
                        t.description = $"在任意场景使用 {meta.DisplayName} {t.requiredAmount} 次";
                    }
                    break;
                case DailyTaskType.SubmitItem:
                    bool isAmmo = DailyQuestConfig.AllowedAmmoIds.Contains(t.targetItemId);
                    if (isAmmo)
                    {
                        switch (t.difficulty)
                        {
                            case DailyTaskDifficulty.Easy: t.requiredAmount = 60; break;
                            case DailyTaskDifficulty.Normal: t.requiredAmount = 120; break;
                            case DailyTaskDifficulty.Hard: t.requiredAmount = 180; break;
                            case DailyTaskDifficulty.Epic: t.difficulty = DailyTaskDifficulty.Hard; t.requiredAmount = 180; break;
                        }
                    }
                    else
                    {
                        switch (t.difficulty)
                        {
                            case DailyTaskDifficulty.Easy: t.requiredAmount = Mathf.Clamp(t.requiredAmount, 2, 5); break;
                            case DailyTaskDifficulty.Normal: t.requiredAmount = Mathf.Clamp(t.requiredAmount, 6, 9); break;
                            case DailyTaskDifficulty.Hard: t.requiredAmount = Mathf.Clamp(t.requiredAmount, 10, 14); break;
                            case DailyTaskDifficulty.Epic: t.requiredAmount = Mathf.Max(15, t.requiredAmount); break;
                        }
                    }
                    {
                        var meta = ItemAssetsCollection.GetMetaData(t.targetItemId);
                        t.title = $"提交 {meta.DisplayName}";
                        t.description = $"提交 {meta.DisplayName} x{t.requiredAmount}";
                    }
                    break;
                case DailyTaskType.SpendCashAtMerchant:
                    switch (t.difficulty)
                    {
                        case DailyTaskDifficulty.Easy: t.requiredAmount = Mathf.Max(1000, t.requiredAmount); break;
                        case DailyTaskDifficulty.Normal: t.requiredAmount = Mathf.Max(2000, t.requiredAmount); break;
                        case DailyTaskDifficulty.Hard: t.requiredAmount = Mathf.Max(3000, t.requiredAmount); break;
                        case DailyTaskDifficulty.Epic: t.difficulty = DailyTaskDifficulty.Hard; t.requiredAmount = Mathf.Max(3000, t.requiredAmount); break;
                    }
                    t.title = "在神秘商人处消费现金";
                    t.description = $"在NPC 神秘商人处购买物品，消费 现金 x{t.requiredAmount}";
                    break;
                case DailyTaskType.KillEnemy:
                    break;
                case DailyTaskType.ChallengeKill:
                    if (!string.IsNullOrEmpty(t.requireEnemyDisplayName))
                    {
                        var wmeta = ItemAssetsCollection.GetMetaData(t.requiredWeaponItemId);
                        string wname = wmeta.id > 0 ? wmeta.DisplayName : "指定武器";
                        t.title = $"挑战击杀：使用 {wname} 击杀 {t.requireEnemyDisplayName}";
                        t.description = $"使用 {wname} 击杀 {t.requireEnemyDisplayName} {t.requiredAmount} 名";
                    }
                    break;
            }
        }

        private void AssignDifficulty(DailyTask t)
        {
            switch (t.type)
            {
                case DailyTaskType.UseItem:
                    if (t.requiredAmount <= 8) t.difficulty = DailyTaskDifficulty.Easy;
                    else if (t.requiredAmount <= 14) t.difficulty = DailyTaskDifficulty.Normal;
                    else t.difficulty = DailyTaskDifficulty.Hard;
                    break;
                case DailyTaskType.SubmitItem:
                    if (DailyQuestConfig.AllowedAmmoIds.Contains(t.targetItemId))
                    {
                        if (t.requiredAmount <= 60) t.difficulty = DailyTaskDifficulty.Easy;
                        else if (t.requiredAmount <= 120) t.difficulty = DailyTaskDifficulty.Normal;
                        else t.difficulty = DailyTaskDifficulty.Hard;
                    }
                    else
                    {
                        if (t.requiredAmount <= 5) t.difficulty = DailyTaskDifficulty.Easy;
                        else if (t.requiredAmount <= 9) t.difficulty = DailyTaskDifficulty.Normal;
                        else if (t.requiredAmount <= 14) t.difficulty = DailyTaskDifficulty.Hard;
                        else t.difficulty = DailyTaskDifficulty.Epic;
                    }
                    break;
                case DailyTaskType.KillEnemy:
                {
                    if (!string.IsNullOrEmpty(t.requireEnemyDisplayName) && DailyQuestConfig.BossEnemyNames.Contains(t.requireEnemyDisplayName))
                    {
                        t.difficulty = DailyTaskDifficulty.Epic;
                    }
                    else
                    {
                        if (t.requiredAmount <= 8) t.difficulty = DailyTaskDifficulty.Easy;
                        else if (t.requiredAmount <= 13) t.difficulty = DailyTaskDifficulty.Normal;
                        else if (t.requiredAmount <= 19) t.difficulty = DailyTaskDifficulty.Hard;
                        else t.difficulty = DailyTaskDifficulty.Epic;
                    }
                    break;
                }
                case DailyTaskType.ChallengeKill:
                {
                    if (!string.IsNullOrEmpty(t.requireEnemyDisplayName) && (DailyQuestConfig.BossEnemyNames.Contains(t.requireEnemyDisplayName) || (!string.IsNullOrEmpty(t.requireEnemyNameKey) && DailyQuestConfig.BossEnemyNames.Contains(t.requireEnemyNameKey))))
                    {
                        t.difficulty = DailyTaskDifficulty.Epic;
                    }
                    else
                    {
                        if (t.requiredAmount <= 7) t.difficulty = DailyTaskDifficulty.Easy;
                        else if (t.requiredAmount <= 12) t.difficulty = DailyTaskDifficulty.Normal;
                        else t.difficulty = DailyTaskDifficulty.Hard;
                    }
                    break;
                }
                case DailyTaskType.SpendCashAtMerchant:
                    if (t.requiredAmount <= 25000) t.difficulty = DailyTaskDifficulty.Easy;
                    else if (t.requiredAmount <= 65000) t.difficulty = DailyTaskDifficulty.Normal;
                    else t.difficulty = DailyTaskDifficulty.Hard;
                    break;
                default:
                    t.difficulty = DailyTaskDifficulty.Normal;
                    break;
            }
        }

        private void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                var tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }

        private DailyTask CreateTaskById(int id)
        {
            if (id >= 300000 && id < 400000)
            {
                return new DailyTask { id = id, type = DailyTaskType.KillEnemy, requiredAmount = 5, title = "击杀任意敌人", description = "在任意场景击杀敌人" };
            }
            if (id >= 200000 && id < 300000)
            {
                int itemId = id - 200000;
                var meta = ItemAssetsCollection.GetMetaData(itemId);
                return new DailyTask { id = id, type = DailyTaskType.SubmitItem, targetItemId = itemId, requiredAmount = 3, title = $"提交 {meta.DisplayName}", description = $"提交 {meta.DisplayName}" };
            }
            if (id >= 100000)
            {
                int itemId = id - 100000;
                var meta = ItemAssetsCollection.GetMetaData(itemId);
                return new DailyTask { id = id, type = DailyTaskType.UseItem, targetItemId = itemId, requiredAmount = 2, title = $"使用 {meta.DisplayName}", description = $"使用 {meta.DisplayName}" };
            }
            return new DailyTask { id = id, title = "任务", description = "", requiredAmount = 1 };
        }

        private void OnEnable()
        {
            Item.onUseStatic += OnItemUsed;
            Health.OnDead += OnHealthDead;
        }

        private void OnDisable()
        {
            Item.onUseStatic -= OnItemUsed;
            Health.OnDead -= OnHealthDead;
        }

        private void OnItemUsed(Item item, object user)
        {
            var ctrl = user as CharacterMainControl;
            if (ctrl == null || !ctrl.IsMainCharacter()) return;
            foreach (var t in tasks)
            {
                if (!t.accepted || t.finished) continue;
                if (t.type == DailyTaskType.UseItem && t.targetItemId == item.TypeID)
                {
                    t.progress = Mathf.Min(t.requiredAmount, t.progress + 1);
                    var meta = ItemAssetsCollection.GetMetaData(t.targetItemId);
                    string msg = $"使用进度：{meta.DisplayName} {t.progress}/{t.requiredAmount}";
                    ctrl.PopText(msg, 12f);
                    if (t.progress >= t.requiredAmount) Complete(t);
                }
            }
            Save();
        }

        private void OnHealthDead(Health health, DamageInfo info)
        {
            if (health.team == Teams.player) return;
            if (info.fromCharacter == null || !info.fromCharacter.IsMainCharacter()) return;
            
            var character = health.TryGetCharacter();
            if (character == null || character.characterPreset == null) return;
            
            string enemyDisplayName = character.characterPreset.DisplayName;
            string enemyNameKey = character.characterPreset.nameKey;
            bool isAllowed = DailyQuestConfig.AllowedEnemyNames.Contains(enemyDisplayName) || DailyQuestConfig.AllowedEnemyNames.Contains(enemyNameKey);

            foreach (var t in tasks)
            {
                if (!t.accepted || t.finished) continue;
                
                if (t.type == DailyTaskType.KillEnemy)
                {
                    if (!string.IsNullOrEmpty(t.requireEnemyNameKey))
                    {
                        if (enemyNameKey != t.requireEnemyNameKey) continue;
                    }
                    else
                    {
                        if (!isAllowed) continue;
                    }
                    
                    t.progress = Mathf.Min(t.requiredAmount, t.progress + 1);
                    string ename = !string.IsNullOrEmpty(t.requireEnemyDisplayName) ? t.requireEnemyDisplayName : enemyDisplayName;
                    string msg = $"击杀进度：{ename} {t.progress}/{t.requiredAmount}";
                    info.fromCharacter?.PopText(msg, 12f);
                    if (t.progress >= t.requiredAmount) Complete(t);
                }
                else if (t.type == DailyTaskType.ChallengeKill)
                {
                    if (!string.IsNullOrEmpty(t.requireEnemyNameKey))
                    {
                         if (enemyNameKey != t.requireEnemyNameKey) continue;
                    }
                    else
                    {
                         if (!isAllowed) continue;
                    }

                    if (info.fromWeaponItemID != t.requiredWeaponItemId) continue;
                    
                    t.progress = Mathf.Min(t.requiredAmount, t.progress + 1);
                    var wmeta = ItemAssetsCollection.GetMetaData(t.requiredWeaponItemId);
                    string wname = wmeta.id > 0 ? wmeta.DisplayName : "指定武器";
                    string ename2 = !string.IsNullOrEmpty(t.requireEnemyDisplayName) ? t.requireEnemyDisplayName : enemyDisplayName;
                    string msg2 = $"挑战击杀进度：使用 {wname} 击杀 {ename2} {t.progress}/{t.requiredAmount}";
                    info.fromCharacter?.PopText(msg2, 12f);
                    if (t.progress >= t.requiredAmount) Complete(t);
                }
            }
            Save();
        }

        public void SubmitItemsForTask(int id)
        {
            var t = tasks.Find(x => x.id == id);
            if (t == null || t.type != DailyTaskType.SubmitItem || !t.accepted || t.finished) return;
            
            var items = ItemUtilities.FindAllBelongsToPlayer(e => e != null && e.TypeID == t.targetItemId);
            
            // 安全措施：过滤掉已装备的物品以防止意外
            var main = CharacterMainControl.Main;
            if (main != null)
            {
                items.RemoveAll(it => 
                    (main.PrimWeaponSlot() != null && main.PrimWeaponSlot().Content == it) ||
                    (main.SecWeaponSlot() != null && main.SecWeaponSlot().Content == it) ||
                    (main.MeleeWeaponSlot() != null && main.MeleeWeaponSlot().Content == it) ||
                    (main.agentHolder != null && main.agentHolder.CurrentHoldItemAgent != null && main.agentHolder.CurrentHoldItemAgent.Item == it)
                );
            }

            int need = t.requiredAmount - t.progress;
            int consumed = 0;
            foreach (var item in items)
            {
                if (consumed >= need) break;
                int take = Math.Min(item.StackCount, need - consumed);
                if (item.StackCount > take)
                {
                    item.StackCount -= take;
                }
                else
                {
                    item.Detach();
                    item.DestroyTree();
                }
                consumed += take;
            }
            t.progress += consumed;
            if (t.progress >= t.requiredAmount) Complete(t);
            Save();
        }

        public void OnMerchantPurchase(int cashSpent)
        {
            foreach (var t in tasks)
            {
                if (!t.accepted || t.finished) continue;
                if (t.type != DailyTaskType.SpendCashAtMerchant) continue;
                if (cashSpent <= 0) continue;
                t.progress = Mathf.Min(t.requiredAmount, t.progress + cashSpent);
                if (t.progress >= t.requiredAmount) Complete(t);
            }
            Save();
        }

        private void Complete(DailyTask t)
        {
            t.finished = true;
            try
            {
                CharacterMainControl.Main?.PopText($"任务完成：{t.title}", 12f);
            }
            catch {}
        }

        private void AssignRewardPreview(DailyTask t)
        {
            if (t.type == DailyTaskType.SpendCashAtMerchant)
            {
                t.rewardCashAmount = UnityEngine.Random.Range(500, 3000);
                t.rewardExpAmount = UnityEngine.Random.Range(200, 1500);
            }
            else
            {
                switch (t.difficulty)
                {
                    case DailyTaskDifficulty.Easy:
                        t.rewardCashAmount = UnityEngine.Random.Range(2000, 5001);
                        t.rewardExpAmount = UnityEngine.Random.Range(1000, 2101);
                        break;
                    case DailyTaskDifficulty.Normal:
                        t.rewardCashAmount = UnityEngine.Random.Range(5000, 9001);
                        t.rewardExpAmount = UnityEngine.Random.Range(2200, 3001);
                        break;
                    case DailyTaskDifficulty.Hard:
                        t.rewardCashAmount = UnityEngine.Random.Range(11000, 19001);
                        t.rewardExpAmount = UnityEngine.Random.Range(3100, 3801);
                        break;
                    case DailyTaskDifficulty.Epic:
                        t.rewardCashAmount = UnityEngine.Random.Range(21000, 29001);
                        t.rewardExpAmount = UnityEngine.Random.Range(4100, 4801);
                        break;
                    default:
                        t.rewardCashAmount = UnityEngine.Random.Range(5000, 9001);
                        t.rewardExpAmount = UnityEngine.Random.Range(2200, 3001);
                        break;
                }
            }

            var unlocked = GameplayDataSettings.Economy.UnlockedItemByDefault;
            var items = new List<DailyTask.RewardItem>();
            var rewardPool = new List<int>(DailyQuestConfig.AllowedRewardItemIds);
            if (t.difficulty != DailyTaskDifficulty.Epic)
            {
                rewardPool.RemoveAll(id => DailyQuestConfig.HighValueRewardItemIds.Contains(id) || DailyQuestConfig.HighValueAmmoIds.Contains(id));
            }
            if (rewardPool.Count == 0 && unlocked != null) rewardPool.AddRange(unlocked);
            int itemCandidates = rewardPool.Count;
            int bonusVariety = t.difficulty == DailyTaskDifficulty.Hard ? 1 : (t.difficulty == DailyTaskDifficulty.Epic ? 2 : 0);
            int count = UnityEngine.Random.Range(1, 4) + bonusVariety;
            for (int i = 0; i < count; i++)
            {
                int typeId = 0;
                if (itemCandidates > 0)
                {
                    int idx = UnityEngine.Random.Range(0, itemCandidates);
                    typeId = rewardPool[idx];
                }
                else if (t.targetItemId > 0)
                {
                    typeId = t.targetItemId;
                }
                else
                {
                    var owned = ItemUtilities.FindAllBelongsToPlayer(e => e != null);
                    if (owned.Count > 0)
                    {
                        int pick = UnityEngine.Random.Range(0, owned.Count);
                        typeId = owned[pick].TypeID;
                    }
                }
                int c;
                if (DailyQuestConfig.AllowedAmmoIds.Contains(typeId) && !DailyQuestConfig.HighValueAmmoIds.Contains(typeId))
                {
                    c = PickNonHighValueAmmoCount(t.difficulty);
                }
                else if (DailyQuestConfig.HighValueAmmoIds.Contains(typeId))
                {
                    c = 30;
                }
                else if (DailyQuestConfig.HighValueRewardItemIds.Contains(typeId))
                {
                    c = 1;
                }
                else
                {
                    int baseCount = UnityEngine.Random.Range(1, 4);
                    float mult = GetDifficultyMultiplier(t.difficulty);
                    c = Mathf.Clamp(Mathf.RoundToInt(baseCount * mult), 1, 10);
                }
                if (typeId > 0)
                {
                    items.Add(new DailyTask.RewardItem { typeId = typeId, count = c });
                }
            }
            t.rewardItems = items;
            
            if (t.type == DailyTaskType.SpendCashAtMerchant)
            {
                float rMult = GetDifficultyMultiplier(t.difficulty);
                t.rewardCashAmount = Mathf.RoundToInt(t.rewardCashAmount * rMult);
                t.rewardExpAmount = Mathf.RoundToInt(t.rewardExpAmount * rMult);
            }

            if (t.type == DailyTaskType.SpendCashAtMerchant)
            {
                t.rewardCashAmount = Mathf.Min(t.rewardCashAmount, Mathf.RoundToInt(t.requiredAmount * 0.35f));
                t.rewardExpAmount = Mathf.Min(t.rewardExpAmount, Mathf.RoundToInt(t.requiredAmount * 0.5f));
            }

            var sb = new System.Text.StringBuilder();
            sb.Append($"现金 {t.rewardCashAmount}，经验 {t.rewardExpAmount}");
            for (int i = 0; i < items.Count; i++)
            {
                var meta = ItemAssetsCollection.GetMetaData(items[i].typeId);
                sb.Append('\n');
                sb.Append($"物品 {meta.DisplayName} x{items[i].count}");
            }
            t.rewardPreviewText = sb.ToString();
        }

        private float GetDifficultyMultiplier(DailyTaskDifficulty d)
        {
            switch (d)
            {
                case DailyTaskDifficulty.Easy: return 1.0f;
                case DailyTaskDifficulty.Normal: return 1.25f;
                case DailyTaskDifficulty.Hard: return 1.6f;
                case DailyTaskDifficulty.Epic: return 2.0f;
                default: return 1.0f;
            }
        }

        private int PickNonHighValueAmmoCount(DailyTaskDifficulty d)
        {
            switch (d)
            {
                case DailyTaskDifficulty.Easy:
                {
                    int[] s = new[] { 30, 35, 40 };
                    return s[UnityEngine.Random.Range(0, s.Length)];
                }
                case DailyTaskDifficulty.Normal:
                {
                    int[] s = new[] { 42, 55, 60 };
                    return s[UnityEngine.Random.Range(0, s.Length)];
                }
                case DailyTaskDifficulty.Hard:
                {
                    int[] s = new[] { 62, 68, 75 };
                    return s[UnityEngine.Random.Range(0, s.Length)];
                }
                case DailyTaskDifficulty.Epic:
                {
                    int[] s = new[] { 76, 80, 88 };
                    return s[UnityEngine.Random.Range(0, s.Length)];
                }
                default:
                {
                    int[] s = new[] { 30, 35, 40 };
                    return s[UnityEngine.Random.Range(0, s.Length)];
                }
            }
        }

        private void GrantReward(DailyTask t)
        {
            if (t.rewardCashAmount > 0)
            {
                int cashId = GameplayDataSettings.ItemAssets.CashItemTypeID;
                var cash = ItemAssetsCollection.InstantiateSync(cashId);
                if (cash != null)
                {
                    cash.StackCount = t.rewardCashAmount;
                    ItemUtilities.SendToPlayer(cash, false, true);
                    NotificationText.Push($"奖励 现金 {t.rewardCashAmount}");
                }
            }
            if (t.rewardExpAmount > 0)
            {
                EXPManager.AddExp(t.rewardExpAmount);
                NotificationText.Push($"奖励 经验 {t.rewardExpAmount}");
            }
            var list = t.rewardItems;
            if (list == null || list.Count == 0)
            {
                #pragma warning disable 618
                if (t.rewardItemTypeId > 0 && t.rewardItemCount > 0)
                {
                    list = new List<DailyTask.RewardItem> { new DailyTask.RewardItem { typeId = t.rewardItemTypeId, count = t.rewardItemCount } };
                }
                #pragma warning restore 618
            }
            var safeList = list ?? new List<DailyTask.RewardItem>();
            for (int i = 0; i < safeList.Count; i++)
            {
                int typeId = safeList[i].typeId;
                int count = Mathf.Max(1, safeList[i].count);
                
                // 在实例化之前验证物品是否存在
                var meta = ItemAssetsCollection.GetMetaData(typeId);
                if (meta.id == 0) continue;

                var first = ItemAssetsCollection.InstantiateSync(typeId);
                if (first == null) continue;

                if (first.Stackable)
                {
                    first.StackCount = count;
                    ItemUtilities.SendToPlayer(first, false, true);
                }
                else
                {
                    first.StackCount = 1;
                    ItemUtilities.SendToPlayer(first, false, true);
                    for (int k = 1; k < count; k++)
                    {
                        var extra = ItemAssetsCollection.InstantiateSync(typeId);
                        if (extra != null)
                        {
                            extra.StackCount = 1;
                            ItemUtilities.SendToPlayer(extra, false, true);
                        }
                    }
                }
                NotificationText.Push($"奖励 物品 {meta.DisplayName} x{count}");
            }
        }

        private void BackfillRewardsForAllTasks()
        {
            bool changed = false;
            for (int i = 0; i < tasks.Count; i++)
            {
                var t = tasks[i];
                if (t == null) continue;
                bool need = false;
                if (t.rewardCashAmount <= 0 || t.rewardExpAmount <= 0) need = true;
                if (t.rewardItems == null || t.rewardItems.Count == 0)
                {
                    #pragma warning disable 618
                    if (t.rewardItemTypeId > 0 && t.rewardItemCount > 0)
                    {
                         // 验证旧版单个物品
                         var meta = ItemAssetsCollection.GetMetaData(t.rewardItemTypeId);
                         if (meta.id != 0)
                         {
                             t.rewardItems = new List<DailyTask.RewardItem> { new DailyTask.RewardItem { typeId = t.rewardItemTypeId, count = t.rewardItemCount } };
                         }
                         else
                         {
                             need = true; // 无效物品，重新生成
                             t.rewardItemTypeId = 0;
                         }
                    }
                    else
                    {
                        need = true;
                    }
                    #pragma warning restore 618
                }
                else
                {
                    // 验证现有列表
                    t.rewardItems.RemoveAll(ri => ItemAssetsCollection.GetMetaData(ri.typeId).id == 0);
                    if (t.rewardItems.Count == 0) need = true;
                }

                if (need)
                {
                    AssignRewardPreview(t);
                    changed = true;
                }
            }
            if (changed) Save();
        }
    }
}
