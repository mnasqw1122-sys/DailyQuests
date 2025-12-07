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
                public System.Collections.Generic.List<int> rewardItemTypeIds;
                public System.Collections.Generic.List<int> rewardItemCounts;
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
        private Duckov.Quests.QuestGiver jeff = null!;
        private static readonly HashSet<string> AllowedEnemyNames = new HashSet<string>(new[]
        {
            "三枪哥","三枪弟","劳登","劳登的狗","大兴兴","小小兴","急速团长","拾荒者","暴走拾荒者","校友","校霸","矿长","维达","蝇蝇队长","迷塞尔","高级工程师","狼","行走菇","风暴虫","风暴？","风暴生物","？？？","咕噜咕噜","啪啦啪啦","噗咙噗咙","失控机械蜘蛛","暴走街机","机械蜘蛛","比利比利","路障","BA队长","喷子","普通BA","炸弹狂人","矮鸭","雇佣兵","游荡者","口口口口"
        });
        private static readonly HashSet<string> BossEnemyNames = new HashSet<string>(new[]
        {
            "三枪哥","劳登","急速团长","校霸","矿长","维达","蝇蝇队长","迷塞尔","高级工程师","？？？","咕噜咕噜","啪啦啪啦","噗咙噗咙","比利比利","暴走街机","路障","BA队长","喷子","炸弹狂人","口口口口"
        });
        private static readonly HashSet<int> AllowedUseItemIds = new HashSet<int>(new[]
        {
            // 医疗/增益针剂
            395,875,1247,137,398,408,409,797,798,800,1070,1071,1072,438,856,872,
            // 医疗包/绷带/药品
            10,20,1243,17,16,1244,1245,15,1246,
            // 食物（固体）
            13,84,133,403,1181,
            // 食品与饮品
            12,18,71,449,19,68,69,70,115,132,1180,105,106,428,29,14,107,108,1256,129,1283
        });
        private static readonly HashSet<int> AllowedSubmitItemIds = new HashSet<int>(new[]
        {
            // Weapons
            1175,1174,869,915,1176,407,788,917,680,916,683,782,1089,656,735,306,250,260,652,1209,252,1173,1095,254,391,1074,659,658,657,1248,343,248,784,357,305,772,246,654,1096,256,737,943,258,1172,242,1238,736,1208,1075,653,240,862,262,734,783,365,244,914,787,681,780,1177,785,327,786,781,682,876,946,733,238,437,655,1128,
            // ArmorEquipment
            45,1138,718,40,889,848,895,897,911,912,921,719,891,307,885,1137,46,894,896,908,909,920,1141,858,1149,35,1166,1148,1140,1147,34,859,1139,44,1142,39,1080,1081,1082,1130,1146,1252,2,1077,1250,791,741,27,740,41,1143,36,1136,1078,33,1213,936,679,841,973,26,1144,129,42,379,37,103,32,138,1249,104,
            // Ammo
            616,694,1162,708,640,918,691,607,700,870,598,615,710,702,643,707,709,326,649,634,698,701,871,606,622,597,358,390,650,631,613,142,159,604,595,621,140,143,630,612,594,603,648,944,
            // Attachments
            452,453,455,456,460,457,458,459,462,543,546,544,547,548,837,839,838,840,531,533,532,534,536,537,538,539,553,556,554,557,480,482,475,476,477,481,714,478,479,465,467,463,464,466,712,493,584,716,776,472,474,468,469,473,713,470,471,488,490,484,485,489,715,487,570,571,572,573,568,574,569,509,514,515,508,510,511,516,578,580,576,577,579,651,
            // Totems
            965,967,970,980,985,987,318,322,371,431,433,948,947,949,951,955,957,962,966,968,971,992,320,324,325,430,953,954,959,435,960,961,976,982,989,993,995,319,436,950,963,952,956,958,964,969,972,974,977,983,986,990,994,372,321,323,369,370,432,975,978,981,984,988,991,979,
            // KeysCards
            828,802,887,803,827,886,804,801,756,755,868,849,826,1169,312,754,1150,831,832,1045,1084,1085,1086,155,360,1227,359,1229,154,1087,1226,384,426,440,441,442,443,795,1228,779,1063,74,759,1191,
            // Collectibles
            448,444,120,80,77,124,744,81,78,75,126,1183,82,112,76,134,125,1254,1253,388,446,1182,1255,118,121,1178,83,3,429,447,119,79,111,135,
            // Fish
            1100,1106,1114,1119,1123,1097,1098,1105,1115,1117,1118,1122,1124,1126,1099,1104,1108,1109,1110,1120,1101,1102,1112,1113,1116,1125,1111,1121,1107,
            // Others
            1134,1154,1090,50,49,48,836,21,52,60,61,110,131,51,58,59,128,130,53,54,55,109,380,833,834,56,57,402,8,337,340,11,112,113,336,338,339,746,64,114,298,329,62,63,65,301,328,100
        });
        private static readonly HashSet<int> AllowedRewardItemIds = new HashSet<int>(AllowedUseItemIds.Concat(AllowedSubmitItemIds));
        private static readonly HashSet<int> AllowedAmmoIds = new HashSet<int>(new[]
        {
            603,604,606,607,694,612,613,615,616,640,708,709,630,631,633,634,594,595,597,598,621,622,700,701,648,870,871,1162,650,918,944
        });
        private static readonly HashSet<int> HighValueRewardItemIds = new HashSet<int>(new[]
        {
            1175,1174,869,915,1176,407,782,683,916,680,917,788,1089,244,785,946,327,914,1096,787,786,238,1238,862,45,895,719,894,1141,1148,44,1082,1138,897,891,896,858,1140,718,911,908,1149,40,912,885,909,35,1080,889,921,1137,920,859,1081,46,828,804,801,802,887,756,803,755,827,868,886,1254,118,1253,121,388,1178,446,1182,1183,1090,328,48,836,1128
        });
        private static readonly HashSet<int> HighValueAmmoIds = new HashSet<int>(new[]
        {
            649,326,23,66,67,660,933,941,942,24,702,691,707,710,698,694
        });

        public void Initialize()
        {
            currentDateKey = DateTime.UtcNow.ToString("yyyy-MM-dd");
            Load();
            BackfillRewardsForAllTasks();
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
            var keep = tasks.Where(t => t.accepted || (t.finished && !t.rewardClaimed)).ToList();
            if (keep.Count >= DailyCount)
            {
                tasks = keep.Take(DailyCount).ToList();
                Save();
                return;
            }
            var candidates = BuildPool();
            Shuffle(candidates);
            var newOnes = AssembleWithQuotas(candidates, keep);
            tasks = keep.Concat(newOnes).ToList();
            Save();
        }

        public void OnQuestCompleted(Duckov.Quests.Quest quest)
        {
            // no-op: native quests不参与每日任务
        }

        private void GrantRandomReward()
        {
            int r = UnityEngine.Random.Range(0, 3);
            if (r == 0)
            {
                int amount = UnityEngine.Random.Range(500, 3000);
                int cashId = GameplayDataSettings.ItemAssets.CashItemTypeID;
                var cash = ItemAssetsCollection.InstantiateSync(cashId);
                cash.StackCount = amount;
                ItemUtilities.SendToPlayer(cash, false, true);
                NotificationText.Push($"奖励 现金 {amount}");
            }
            else if (r == 1)
            {
                int exp = UnityEngine.Random.Range(200, 1500);
                EXPManager.AddExp(exp);
                NotificationText.Push($"奖励 经验 {exp}");
            }
            else
            {
                var unlocked = GameplayDataSettings.Economy.UnlockedItemByDefault;
                int idx = UnityEngine.Random.Range(0, unlocked.Count);
                int typeId = unlocked[idx];
                var item = ItemAssetsCollection.InstantiateSync(typeId);
                ItemUtilities.SendToPlayer(item, false, true);
                NotificationText.Push($"奖励 物品 {ItemAssetsCollection.GetMetaData(typeId).DisplayName}");
            }
        }

        private string lastSavedDate = string.Empty;

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
                    rewardItemTypeId = t.rewardItemTypeId,
                    rewardItemCount = t.rewardItemCount,
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
                            rewardItemTypeId = s.rewardItemTypeId,
                            rewardItemCount = s.rewardItemCount,
                            rewardClaimed = s.rewardClaimed
                        };
                        if (string.IsNullOrEmpty(t.title))
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
                        tasks.Add(t);
                    }
                }
                else
                {
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
            var pool = BuildPool();
            Shuffle(pool);
            var result = AssembleWithQuotas(pool, keep);
            tasks = keep.Concat(result).Take(DailyCount).ToList();
            lastSavedDate = currentDateKey;
        }

        private List<DailyTask> BuildPool()
        {
            var useList = new List<DailyTask>();
            var submitList = new List<DailyTask>();
            var killList = new List<DailyTask>();
            var spendList = new List<DailyTask>();
            var challengeList = new List<DailyTask>();

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
            for (int i = 0; i < sourceItemIds.Count; i++)
            {
                int itemId = sourceItemIds[i];
                var meta = ItemAssetsCollection.GetMetaData(itemId);
                int rollUse = UnityEngine.Random.Range(0, 100);
                DailyTaskDifficulty useDiff = (rollUse < 33) ? DailyTaskDifficulty.Easy : ((rollUse < 66) ? DailyTaskDifficulty.Normal : DailyTaskDifficulty.Hard);
                int useAmount = useDiff == DailyTaskDifficulty.Easy ? UnityEngine.Random.Range(3, 9) : (useDiff == DailyTaskDifficulty.Normal ? UnityEngine.Random.Range(9, 15) : UnityEngine.Random.Range(15, 21));
                int submitAmount = 0;
                DailyTask? useTask = null;
                if (AllowedUseItemIds.Contains(itemId))
                {
                    useTask = new DailyTask
                    {
                        id = 100000 + itemId,
                        type = DailyTaskType.UseItem,
                        targetItemId = itemId,
                        requiredAmount = useAmount,
                        difficulty = useDiff,
                        title = $"使用 {meta.DisplayName}",
                        description = $"在任意场景使用 {meta.DisplayName} {useAmount} 次"
                    };
                }
                DailyTask? submitTask = null;
                if (AllowedSubmitItemIds.Contains(itemId))
                {
                    bool isAmmo = AllowedAmmoIds.Contains(itemId);
                    DailyTaskDifficulty subDiff;
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
                    submitTask = new DailyTask
                    {
                        id = 200000 + itemId,
                        type = DailyTaskType.SubmitItem,
                        targetItemId = itemId,
                        requiredAmount = submitAmount,
                        difficulty = subDiff,
                        title = $"提交 {meta.DisplayName}",
                        description = $"提交 {meta.DisplayName} x{submitAmount}"
                    };
                }
                if (useTask != null) AssignRewardPreview(useTask);
                if (submitTask != null) AssignRewardPreview(submitTask);
                if (useTask != null) useList.Add(useTask);
                if (submitTask != null) submitList.Add(submitTask);
                if (useTask != null) { AssignDifficulty(useTask); AdjustTaskByDifficulty(useTask); }
                if (submitTask != null) { AssignDifficulty(submitTask); AdjustTaskByDifficulty(submitTask); }
            }

            var presets = GameplayDataSettings.CharacterRandomPresetData?.presets ?? new System.Collections.Generic.List<CharacterRandomPreset>();
            presets = presets.Where(p => p != null && AllowedEnemyNames.Contains(p.DisplayName)).ToList();
            int killBase = 300000;
            int idx = 0;
            foreach (var preset in presets)
            {
                bool isBoss = BossEnemyNames.Contains(preset.DisplayName);
                int amount;
                if (isBoss)
                {
                    amount = UnityEngine.Random.Range(3, 10); // 3..9 for Epic bosses
                }
                else
                {
                    int roll = UnityEngine.Random.Range(0, 100);
                    if (roll < 30)
                    {
                        amount = UnityEngine.Random.Range(3, 9); // Easy: 3..8
                    }
                    else if (roll < 70)
                    {
                        amount = UnityEngine.Random.Range(9, 14); // Normal: 9..13
                    }
                    else
                    {
                        amount = UnityEngine.Random.Range(14, 20); // Hard: 14..19
                    }
                }
                var t = new DailyTask
                {
                    id = killBase + idx++,
                    type = DailyTaskType.KillEnemy,
                    requiredAmount = amount,
                    requireEnemyNameKey = preset.nameKey,
                    requireEnemyDisplayName = preset.DisplayName,
                    title = $"击杀 {preset.DisplayName}",
                    description = $"击杀 {preset.DisplayName} {amount} 名"
                };
                AssignDifficulty(t);
                AssignRewardPreview(t);
                killList.Add(t);
            }

            // 生成“挑战击杀”：使用指定武器击杀指定敌人
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
            int cidx = 0;
            foreach (var preset in presets)
            {
                if (ownedWeaponIds.Count == 0) break;
                int pickWeaponIndex = UnityEngine.Random.Range(0, ownedWeaponIds.Count);
                int weaponId = ownedWeaponIds[pickWeaponIndex];
                var wmeta = ItemAssetsCollection.GetMetaData(weaponId);
                bool isBoss = BossEnemyNames.Contains(preset.DisplayName);
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
                    if (roll < 25)
                    {
                        amount = UnityEngine.Random.Range(3, 8); // Easy 3..7
                        diff = DailyTaskDifficulty.Easy;
                    }
                    else if (roll < 65)
                    {
                        amount = UnityEngine.Random.Range(8, 13); // Normal 8..12
                        diff = DailyTaskDifficulty.Normal;
                    }
                    else
                    {
                        amount = UnityEngine.Random.Range(12, 19); // Hard 12..18
                        diff = DailyTaskDifficulty.Hard;
                    }
                }
                var ct = new DailyTask
                {
                    id = challengeBase + cidx++,
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
                challengeList.Add(ct);
            }
        

            // 随机生成“在神秘商人处消费现金”任务列表
            int cashTypeId = GameplayDataSettings.ItemAssets.CashItemTypeID;
            for (int s = 0; s < 10; s++)
            {
                int roll = UnityEngine.Random.Range(0, 100);
                DailyTaskDifficulty diff = roll < 30 ? DailyTaskDifficulty.Easy : (roll < 65 ? DailyTaskDifficulty.Normal : DailyTaskDifficulty.Hard);
                int needAmount = diff == DailyTaskDifficulty.Easy ? UnityEngine.Random.Range(10000, 25001) : (diff == DailyTaskDifficulty.Normal ? UnityEngine.Random.Range(30000, 65001) : UnityEngine.Random.Range(80000, 150001));
                var spend = new DailyTask
                {
                    id = 400000 + s,
                    type = DailyTaskType.SpendCashAtMerchant,
                    targetItemId = cashTypeId,
                    requiredAmount = needAmount,
                    title = "在神秘商人处消费现金",
                    description = $"在NPC 神秘商人处购买物品，消费 现金 x{needAmount}",
                    difficulty = diff
                };
                AdjustTaskByDifficulty(spend);
                AssignRewardPreview(spend);
                spendList.Add(spend);
            }

            if (submitList.Count == 0)
            {
                int cashId = GameplayDataSettings.ItemAssets.CashItemTypeID;
                var cashMeta = ItemAssetsCollection.GetMetaData(cashId);
                var submitCash = new DailyTask
                {
                    id = 200000 + cashId,
                    type = DailyTaskType.SubmitItem,
                    targetItemId = cashId,
                    requiredAmount = UnityEngine.Random.Range(1, 3),
                    title = $"提交 {cashMeta.DisplayName}",
                    description = $"提交 {cashMeta.DisplayName}"
                };
                AssignRewardPreview(submitCash);
                submitList.Add(submitCash);
            }
            Shuffle(useList);
            Shuffle(submitList);
            Shuffle(killList);
            Shuffle(spendList);
            Shuffle(challengeList);
            var result = new List<DailyTask>();
            result.AddRange(useList);
            result.AddRange(submitList);
            result.AddRange(killList);
            result.AddRange(challengeList);
            result.AddRange(spendList);
            Shuffle(result);
            return result;
        }

        private List<DailyTask> AssembleWithQuotas(List<DailyTask> candidates, List<DailyTask> keep)
        {
            int level = EXPManager.Level;
            int quotaEasy;
            int quotaNormal;
            int quotaHard;
            int quotaEpic;

            if (level >= 1 && level <= 10)
            {
                quotaEasy = 24; quotaNormal = 0; quotaHard = 0; quotaEpic = 0;
            }
            else if (level >= 11 && level <= 20)
            {
                quotaEasy = 18; quotaNormal = 6; quotaHard = 0; quotaEpic = 0;
            }
            else if (level >= 21 && level <= 30)
            {
                quotaEasy = 10; quotaNormal = 10; quotaHard = 4; quotaEpic = 0;
            }
            else if (level >= 31 && level <= 40)
            {
                quotaEasy = 8; quotaNormal = 8; quotaHard = 5; quotaEpic = 3;
            }
            else
            {
                quotaEasy = 4; quotaNormal = 6; quotaHard = 10; quotaEpic = 4;
            }

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
                    bool isAmmo = AllowedAmmoIds.Contains(t.targetItemId);
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
                    if (AllowedAmmoIds.Contains(t.targetItemId))
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
                    if (!string.IsNullOrEmpty(t.requireEnemyDisplayName) && BossEnemyNames.Contains(t.requireEnemyDisplayName))
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
                    if (!string.IsNullOrEmpty(t.requireEnemyDisplayName) && BossEnemyNames.Contains(t.requireEnemyDisplayName))
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
            foreach (var t in tasks)
            {
                if (!t.accepted || t.finished) continue;
                if (t.type == DailyTaskType.KillEnemy)
                {
                    if (!string.IsNullOrEmpty(t.requireEnemyNameKey))
                    {
                        var ch = health.TryGetCharacter();
                        if (ch == null || ch.characterPreset == null || ch.characterPreset.nameKey != t.requireEnemyNameKey) continue;
                    }
                    else
                    {
                        var ch = health.TryGetCharacter();
                        if (ch == null || ch.characterPreset == null || !AllowedEnemyNames.Contains(ch.characterPreset.DisplayName)) continue;
                    }
                    t.progress = Mathf.Min(t.requiredAmount, t.progress + 1);
                    string ename = !string.IsNullOrEmpty(t.requireEnemyDisplayName) ? t.requireEnemyDisplayName : (health.TryGetCharacter()?.characterPreset?.DisplayName ?? "敌人");
                    string msg = $"击杀进度：{ename} {t.progress}/{t.requiredAmount}";
                    info.fromCharacter?.PopText(msg, 12f);
                    if (t.progress >= t.requiredAmount) Complete(t);
                }
                else if (t.type == DailyTaskType.ChallengeKill)
                {
                    if (!string.IsNullOrEmpty(t.requireEnemyNameKey))
                    {
                        var ch = health.TryGetCharacter();
                        if (ch == null || ch.characterPreset == null || ch.characterPreset.nameKey != t.requireEnemyNameKey) continue;
                    }
                    else
                    {
                        var ch = health.TryGetCharacter();
                        if (ch == null || ch.characterPreset == null || !AllowedEnemyNames.Contains(ch.characterPreset.DisplayName)) continue;
                    }
                    if (info.fromWeaponItemID != t.requiredWeaponItemId) continue;
                    t.progress = Mathf.Min(t.requiredAmount, t.progress + 1);
                    var wmeta = ItemAssetsCollection.GetMetaData(t.requiredWeaponItemId);
                    string wname = wmeta.id > 0 ? wmeta.DisplayName : "指定武器";
                    string ename2 = !string.IsNullOrEmpty(t.requireEnemyDisplayName) ? t.requireEnemyDisplayName : (health.TryGetCharacter()?.characterPreset?.DisplayName ?? "敌人");
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
            var rewardPool = new List<int>(AllowedRewardItemIds);
            if (t.difficulty != DailyTaskDifficulty.Epic)
            {
                rewardPool.RemoveAll(id => HighValueRewardItemIds.Contains(id) || HighValueAmmoIds.Contains(id));
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
                if (AllowedAmmoIds.Contains(typeId) && !HighValueAmmoIds.Contains(typeId))
                {
                    c = PickNonHighValueAmmoCount(t.difficulty);
                }
                else if (HighValueAmmoIds.Contains(typeId))
                {
                    c = 30;
                }
                else if (HighValueRewardItemIds.Contains(typeId))
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
            if (items.Count > 0)
            {
                t.rewardItemTypeId = items[0].typeId;
                t.rewardItemCount = items[0].count;
            }
            else
            {
                t.rewardItemTypeId = 0;
                t.rewardItemCount = 0;
            }

            if (t.type == DailyTaskType.SpendCashAtMerchant)
            {
                float rMult = GetDifficultyMultiplier(t.difficulty);
                t.rewardCashAmount = Mathf.RoundToInt(t.rewardCashAmount * rMult);
                t.rewardExpAmount = Mathf.RoundToInt(t.rewardExpAmount * rMult);
            }

            if (t.type == DailyTaskType.SpendCashAtMerchant)
            {
                // 保证奖励现金不超过消费要求的合理比例
                t.rewardCashAmount = Mathf.Min(t.rewardCashAmount, Mathf.RoundToInt(t.requiredAmount * 0.35f));
                t.rewardExpAmount = Mathf.Min(t.rewardExpAmount, Mathf.RoundToInt(t.requiredAmount * 0.5f));
            }

            // 如果是弹药提交任务，额外奖励同类弹药更大量

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
                cash.StackCount = t.rewardCashAmount;
                ItemUtilities.SendToPlayer(cash, false, true);
                NotificationText.Push($"奖励 现金 {t.rewardCashAmount}");
            }
            if (t.rewardExpAmount > 0)
            {
                EXPManager.AddExp(t.rewardExpAmount);
                NotificationText.Push($"奖励 经验 {t.rewardExpAmount}");
            }
            var list = t.rewardItems;
            if (list == null || list.Count == 0)
            {
                if (t.rewardItemTypeId > 0 && t.rewardItemCount > 0)
                {
                    list = new List<DailyTask.RewardItem> { new DailyTask.RewardItem { typeId = t.rewardItemTypeId, count = t.rewardItemCount } };
                }
            }
            var safeList = list ?? new List<DailyTask.RewardItem>();
            for (int i = 0; i < safeList.Count; i++)
            {
                int typeId = safeList[i].typeId;
                int count = Mathf.Max(1, safeList[i].count);
                var first = ItemAssetsCollection.InstantiateSync(typeId);
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
                        extra.StackCount = 1;
                        ItemUtilities.SendToPlayer(extra, false, true);
                    }
                }
                NotificationText.Push($"奖励 物品 {ItemAssetsCollection.GetMetaData(typeId).DisplayName} x{count}");
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
                    if (t.rewardItemTypeId > 0 && t.rewardItemCount > 0)
                    {
                        t.rewardItems = new List<DailyTask.RewardItem> { new DailyTask.RewardItem { typeId = t.rewardItemTypeId, count = t.rewardItemCount } };
                    }
                    else
                    {
                        need = true;
                    }
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
