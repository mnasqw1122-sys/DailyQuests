using System.Collections.Generic;
using System.Linq;
using Duckov.Quests;
using Duckov.UI;
using ItemStatsSystem;
using Duckov.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace DailyQuests
{
    public class DailyQuestView : View
    {
        private RectTransform root = null!;
        private RectTransform header = null!;
        private RectTransform listParent = null!;
        private ScrollRect scroll = null!;
        private Button btnRefresh = null!;
        private TextMeshProUGUI title = null!;
        private RectTransform leftPanel = null!;
        private RectTransform rightPanel = null!;
        private RectTransform rightContent = null!;
        private RectTransform bottomBar = null!;
        private TextMeshProUGUI detailTitle = null!;
        private TextMeshProUGUI detailDesc = null!;
        private TextMeshProUGUI detailTarget = null!;
        private TextMeshProUGUI detailProgress = null!;
        private TextMeshProUGUI detailRewardCashExp = null!;
        private TextMeshProUGUI detailRewardItems = null!;
        private Button btnAccept = null!;
        private Button btnAbandon = null!;
        private Button btnSubmit = null!;
        private Button btnFinish = null!;
        private readonly List<GameObject> entryObjects = new List<GameObject>();
        private readonly List<int> entryIds = new List<int>();
        private readonly List<TextMeshProUGUI> nameLabels = new List<TextMeshProUGUI>();
        private readonly List<Image> rowBackgrounds = new List<Image>();
        private int selectedIndex = -1;
        private int selectedTaskId = -1;

        private readonly Color rowSelectedColor = new Color(1f, 0.62f, 0.18f, 1f);
        private readonly Color rowUnselectedColor = new Color(0f, 0f, 0f, 0.25f);
        private readonly Color acceptBtnColor = new Color(0.39f, 0.75f, 0.47f, 1f);
        private readonly Color submitBtnColor = new Color(0.87f, 0.96f, 1f, 1f);
        private readonly Color submitTextColor = new Color(0.2f, 0.34f, 0.45f, 1f);

        private class RowClickRelay : MonoBehaviour, IPointerClickHandler
        {
            public DailyQuestView view = null!;
            public int index;
            public int taskId;
            public void OnPointerClick(PointerEventData eventData)
            {
                if (view == null) return;
                view.SetSelection(index);
                view.ShowDetail(taskId);
            }
        }

        protected override void Awake()
        {
            base.Awake();
            BuildUI();
            gameObject.SetActive(false);
        }

        protected override void OnOpen()
        {
            base.OnOpen();
            gameObject.SetActive(true);
            RefreshList();
        }

        protected override void OnClose()
        {
            base.OnClose();
            gameObject.SetActive(false);
        }

        private void BuildUI()
        {
            var cg = gameObject.AddComponent<CanvasGroup>();
            root = gameObject.AddComponent<RectTransform>();
            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(1400, 720);
            root.anchoredPosition = Vector2.zero;

            var headerGo = new GameObject("Header");
            headerGo.transform.SetParent(root, false);
            header = headerGo.AddComponent<RectTransform>();
            header.anchorMin = new Vector2(0, 1);
            header.anchorMax = new Vector2(1, 1);
            header.pivot = new Vector2(0.5f, 1);
            float headerH = 72f;
            header.sizeDelta = new Vector2(0, headerH);

            var titleText = Object.Instantiate(GameplayDataSettings.UIStyle.TemplateTextUGUI);
            titleText.transform.SetParent(header, false);
            titleText.text = "每日任务 V0.93";
            titleText.alignment = TextAlignmentOptions.MidlineLeft;
            titleText.fontSize = 28f;
            titleText.rectTransform.anchorMin = new Vector2(0, 0.5f);
            titleText.rectTransform.anchorMax = new Vector2(0, 0.5f);
            titleText.rectTransform.pivot = new Vector2(0, 0.5f);
            titleText.rectTransform.anchoredPosition = new Vector2(20, 0);
            title = titleText;

            var refreshGo = new GameObject("RefreshButton");
            refreshGo.transform.SetParent(header, false);
            btnRefresh = refreshGo.AddComponent<Button>();
            var img = refreshGo.AddComponent<Image>();
            img.color = new Color(0.2f, 0.6f, 0.9f, 1f);
            var rt = refreshGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(120, 40);
            rt.anchorMin = new Vector2(1, 0.5f);
            rt.anchorMax = new Vector2(1, 0.5f);
            rt.pivot = new Vector2(1, 0.5f);
            rt.anchoredPosition = new Vector2(-20, 0);
            var refreshLabel = Object.Instantiate(GameplayDataSettings.UIStyle.TemplateTextUGUI);
            refreshLabel.transform.SetParent(refreshGo.transform, false);
            refreshLabel.text = "刷新未接取";
            refreshLabel.alignment = TextAlignmentOptions.Center;
            refreshLabel.fontSize = 20f;
            refreshLabel.rectTransform.anchorMin = new Vector2(0, 0);
            refreshLabel.rectTransform.anchorMax = new Vector2(1, 1);
            refreshLabel.rectTransform.offsetMin = Vector2.zero;
            refreshLabel.rectTransform.offsetMax = Vector2.zero;
            btnRefresh.onClick.AddListener(OnRefreshClicked);

            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(root, false);
            var bodyRt = bodyGo.AddComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0, 0);
            bodyRt.anchorMax = new Vector2(1, 1);
            bodyRt.offsetMin = new Vector2(20, 20);
            bodyRt.offsetMax = new Vector2(-20, -(headerH + 16));
            var bodyH = bodyGo.AddComponent<HorizontalLayoutGroup>();
            bodyH.childControlHeight = true;
            bodyH.childForceExpandHeight = true;
            bodyH.childForceExpandWidth = true;
            bodyH.spacing = 12f;
            bodyH.padding = new RectOffset(8, 8, 8, 8);

            var leftGo = new GameObject("LeftPanel");
            leftGo.transform.SetParent(bodyGo.transform, false);
            leftPanel = leftGo.AddComponent<RectTransform>();
            var leftImg = leftGo.AddComponent<Image>();
            leftImg.color = new Color(0, 0, 0, 0.18f);
            leftImg.raycastTarget = false;
            var leftLE = leftGo.AddComponent<LayoutElement>();
            leftLE.preferredWidth = 420f;
            leftLE.flexibleWidth = 0f;

            var rightGo = new GameObject("RightPanel");
            rightGo.transform.SetParent(bodyGo.transform, false);
            rightPanel = rightGo.AddComponent<RectTransform>();
            var rightImg = rightGo.AddComponent<Image>();
            rightImg.color = new Color(0, 0, 0, 0.12f);
            rightImg.raycastTarget = false;
            var rightLE = rightGo.AddComponent<LayoutElement>();
            rightLE.flexibleWidth = 1f;

            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(leftGo.transform, false);
            var scrollRt = scrollGo.AddComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0, 0);
            scrollRt.anchorMax = new Vector2(1, 1);
            scrollRt.offsetMin = new Vector2(8, 8);
            scrollRt.offsetMax = new Vector2(-8, -8);
            scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var viewportRt = viewportGo.AddComponent<RectTransform>();
            viewportRt.anchorMin = new Vector2(0, 0);
            viewportRt.anchorMax = new Vector2(1, 1);
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = Vector2.zero;
            viewportGo.AddComponent<RectMask2D>();
            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewportGo.transform, false);
            listParent = contentGo.AddComponent<RectTransform>();
            listParent.pivot = new Vector2(0, 1);
            listParent.anchorMin = new Vector2(0, 1);
            listParent.anchorMax = new Vector2(1, 1);
            listParent.anchoredPosition = Vector2.zero;
            var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.spacing = 6f;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childAlignment = TextAnchor.UpperLeft;
            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewportRt;
            scroll.content = listParent;

            var rcGo = new GameObject("RightContent");
            rcGo.transform.SetParent(rightGo.transform, false);
            rightContent = rcGo.AddComponent<RectTransform>();
            rightContent.anchorMin = new Vector2(0, 0);
            rightContent.anchorMax = new Vector2(1, 1);
            rightContent.offsetMin = new Vector2(12, 72);
            rightContent.offsetMax = new Vector2(-12, -88);
            var vlg2 = rcGo.AddComponent<VerticalLayoutGroup>();
            vlg2.childControlHeight = true;
            vlg2.childControlWidth = true;
            vlg2.childForceExpandHeight = false;
            vlg2.childForceExpandWidth = true;
            vlg2.spacing = 10f;
            vlg2.padding = new RectOffset(12, 12, 12, 12);
            vlg2.childAlignment = TextAnchor.UpperLeft;

            var dt = Object.Instantiate(GameplayDataSettings.UIStyle.TemplateTextUGUI);
            dt.transform.SetParent(rcGo.transform, false);
            dt.enableWordWrapping = true;
            dt.fontSize = 22f;
            dt.alignment = TextAlignmentOptions.TopLeft;
            detailTitle = dt;

            var dd = Object.Instantiate(GameplayDataSettings.UIStyle.TemplateTextUGUI);
            dd.transform.SetParent(rcGo.transform, false);
            dd.enableWordWrapping = true;
            dd.fontSize = 20f;
            dd.alignment = TextAlignmentOptions.TopLeft;
            detailDesc = dd;

            var tg = Object.Instantiate(GameplayDataSettings.UIStyle.TemplateTextUGUI);
            tg.transform.SetParent(rcGo.transform, false);
            tg.enableWordWrapping = true;
            tg.fontSize = 20f;
            tg.alignment = TextAlignmentOptions.TopLeft;
            detailTarget = tg;

            var pg = Object.Instantiate(GameplayDataSettings.UIStyle.TemplateTextUGUI);
            pg.transform.SetParent(rcGo.transform, false);
            pg.enableWordWrapping = false;
            pg.fontSize = 20f;
            pg.alignment = TextAlignmentOptions.TopLeft;
            detailProgress = pg;

            var rwce = Object.Instantiate(GameplayDataSettings.UIStyle.TemplateTextUGUI);
            rwce.transform.SetParent(rcGo.transform, false);
            rwce.enableWordWrapping = true;
            rwce.fontSize = 20f;
            rwce.alignment = TextAlignmentOptions.TopLeft;
            detailRewardCashExp = rwce;

            var rwit = Object.Instantiate(GameplayDataSettings.UIStyle.TemplateTextUGUI);
            rwit.transform.SetParent(rcGo.transform, false);
            rwit.enableWordWrapping = true;
            rwit.fontSize = 20f;
            rwit.alignment = TextAlignmentOptions.TopLeft;
            detailRewardItems = rwit;

            var bbGo = new GameObject("BottomBar");
            bbGo.transform.SetParent(rightGo.transform, false);
            bottomBar = bbGo.AddComponent<RectTransform>();
            bottomBar.anchorMin = new Vector2(0, 0);
            bottomBar.anchorMax = new Vector2(1, 0);
            bottomBar.pivot = new Vector2(0.5f, 0);
            bottomBar.sizeDelta = new Vector2(0, 64);
            bottomBar.anchoredPosition = new Vector2(0, 12);
            var hb = bbGo.AddComponent<HorizontalLayoutGroup>();
            hb.childControlHeight = true;
            hb.childForceExpandHeight = true;
            hb.childForceExpandWidth = true;
            hb.spacing = 12f;
            hb.padding = new RectOffset(12, 12, 8, 8);

            btnAccept = CreateBottomButton(bbGo, "接取");
            btnAbandon = CreateBottomButton(bbGo, "放弃");
            btnSubmit = CreateBottomButton(bbGo, "提交");
            btnFinish = CreateBottomButton(bbGo, "完成");
            var accImg = btnAccept.GetComponent<Image>(); if (accImg != null) accImg.color = acceptBtnColor;
            var subImg = btnSubmit.GetComponent<Image>(); if (subImg != null) subImg.color = submitBtnColor;
            var subLabel = btnSubmit.GetComponentInChildren<TextMeshProUGUI>(); if (subLabel != null) subLabel.color = submitTextColor;
            var finImg = btnFinish.GetComponent<Image>(); if (finImg != null) finImg.color = acceptBtnColor;
            btnAccept.onClick.AddListener(() => { if (selectedTaskId > 0) { DailyQuestManager.Instance.Accept(selectedTaskId); RefreshList(); ShowDetail(selectedTaskId); } });
            btnAbandon.onClick.AddListener(() => { if (selectedTaskId > 0) { DailyQuestManager.Instance.Abandon(selectedTaskId); RefreshList(); ShowDetail(selectedTaskId); } });
            btnSubmit.onClick.AddListener(() => { if (selectedTaskId > 0) { DailyQuestManager.Instance.SubmitItemsForTask(selectedTaskId); RefreshList(); ShowDetail(selectedTaskId); } });
            btnFinish.onClick.AddListener(() => { if (selectedTaskId > 0) { DailyQuestManager.Instance.ClaimReward(selectedTaskId); RefreshList(); ShowDetail(selectedTaskId); } });
        }

        private void ClearEntries()
        {
            foreach (var e in entryObjects)
            {
                if (e != null) Object.Destroy(e);
            }
            entryObjects.Clear();
            entryIds.Clear();
            nameLabels.Clear();
            rowBackgrounds.Clear();
            selectedIndex = -1;
            selectedTaskId = -1;
            detailTitle.text = "";
            detailDesc.text = "";
            detailTarget.text = "";
            detailProgress.text = "";
            detailRewardCashExp.text = "";
            detailRewardItems.text = "";
            SetBottomButtonsVisible(false, false, false, false);
        }

        private void RefreshList()
        {
            ClearEntries();
            var tasks = DailyQuestManager.Instance.GetDailyTasks();
            for (int i = 0; i < tasks.Count; i++)
            {
                CreateEntry(tasks[i]);
            }
        }

        private void CreateEntry(DailyTask task)
        {
            var entryGo = new GameObject($"Entry_{task.id}");
            entryGo.transform.SetParent(listParent, false);
            var rt = entryGo.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 84);
            var hbg = entryGo.AddComponent<Image>();
            hbg.color = rowUnselectedColor;
            var h = entryGo.AddComponent<HorizontalLayoutGroup>();
            h.childControlHeight = true;
            h.childForceExpandHeight = false;
            h.childForceExpandWidth = true;
            h.spacing = 8f;
            h.padding = new RectOffset(8, 8, 8, 8);
            h.childAlignment = TextAnchor.MiddleLeft;
            entryGo.AddComponent<RectMask2D>();
            var entryLE = entryGo.AddComponent<LayoutElement>();
            entryLE.minHeight = 68f;
            entryLE.preferredHeight = 84f;

            var nameText = Object.Instantiate(GameplayDataSettings.UIStyle.TemplateTextUGUI);
            nameText.transform.SetParent(entryGo.transform, false);
            string diffLabel = task.difficulty == DailyTaskDifficulty.Easy ? "简单" : (task.difficulty == DailyTaskDifficulty.Normal ? "普通" : (task.difficulty == DailyTaskDifficulty.Hard ? "困难" : "史诗"));
            bool accepted = DailyQuestManager.Instance.IsAccepted(task.id);
            bool finished = DailyQuestManager.Instance.IsFinished(task.id);
            if (finished)
            {
                nameText.text = "已完成";
            }
            else if (accepted)
            {
                nameText.text = $"{task.title} [难度 {diffLabel}]  已接取";
            }
            else
            {
                nameText.text = $"{task.title} [难度 {diffLabel}]";
            }
            nameText.alignment = TextAlignmentOptions.MidlineLeft;
            nameText.enableWordWrapping = true;
            nameText.fontSize = 20f;
            var nameLE = nameText.gameObject.AddComponent<LayoutElement>();
            nameLE.minWidth = 360f;
            nameLE.flexibleWidth = 1f;

            var click = entryGo.AddComponent<RowClickRelay>();
            click.view = this;
            click.index = entryObjects.Count;
            click.taskId = task.id;

            float baseH = 68f;
            float computedH = nameText.preferredHeight + 16f;
            int rowH = (int)Mathf.Max(baseH, computedH);
            rt.sizeDelta = new Vector2(0, rowH);
            entryLE.preferredHeight = rowH;
            entryObjects.Add(entryGo);
            entryIds.Add(task.id);
            nameLabels.Add(nameText);
            rowBackgrounds.Add(hbg);
        }

        private void SetSelection(int index)
        {
            selectedIndex = index;
            for (int i = 0; i < nameLabels.Count; i++)
            {
                nameLabels[i].color = (i == selectedIndex) ? new Color(1f, 1f, 1f, 1f) : new Color(1f, 1f, 1f, 0.85f);
                if (i < rowBackgrounds.Count && rowBackgrounds[i] != null)
                {
                    rowBackgrounds[i].color = (i == selectedIndex) ? rowSelectedColor : rowUnselectedColor;
                }
            }
        }

        protected override void OnConfirm()
        {
            if (selectedIndex < 0 || selectedIndex >= entryIds.Count) return;
            ShowDetail(entryIds[selectedIndex]);
        }

        private void ExecuteOperation(int taskId)
        {
            var tasks = DailyQuestManager.Instance.GetDailyTasks();
            var task = tasks.Find(t => t.id == taskId);
            if (task == null) return;
            bool accepted = DailyQuestManager.Instance.IsAccepted(task.id);
            bool finished = DailyQuestManager.Instance.IsFinished(task.id);
            if (!accepted)
            {
                DailyQuestManager.Instance.Accept(task.id);
            }
            else if (finished)
            {
            }
            else if (task.type == DailyTaskType.SubmitItem)
            {
                DailyQuestManager.Instance.SubmitItemsForTask(task.id);
            }
            else
            {
                DailyQuestManager.Instance.Abandon(task.id);
            }
            RefreshList();
            ShowDetail(task.id);
        }

        private void OnRefreshClicked()
        {
            DailyQuestManager.Instance.RefreshUnaccepted();
            RefreshList();
        }

        private Button CreateBottomButton(GameObject parent, string text)
        {
            var go = new GameObject(text);
            go.transform.SetParent(parent.transform, false);
            var btn = go.AddComponent<Button>();
            var img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.12f);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 0);
            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            var label = Object.Instantiate(GameplayDataSettings.UIStyle.TemplateTextUGUI);
            label.transform.SetParent(go.transform, false);
            label.text = text;
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 22f;
            label.rectTransform.anchorMin = new Vector2(0, 0);
            label.rectTransform.anchorMax = new Vector2(1, 1);
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
            return btn;
        }

        private void SetBottomButtonsVisible(bool accept, bool abandon, bool submit, bool finish)
        {
            if (btnAccept != null) btnAccept.gameObject.SetActive(accept);
            if (btnAbandon != null) btnAbandon.gameObject.SetActive(abandon);
            if (btnSubmit != null) btnSubmit.gameObject.SetActive(submit);
            if (btnFinish != null) btnFinish.gameObject.SetActive(finish);
        }

        private void ShowDetail(int taskId)
        {
            selectedTaskId = taskId;
            var tasks = DailyQuestManager.Instance.GetDailyTasks();
            var task = tasks.Find(t => t.id == taskId);
            if (task == null)
            {
                SetBottomButtonsVisible(false, false, false, false);
                return;
            }
            string diffLabel = task.difficulty == DailyTaskDifficulty.Easy ? "简单" : (task.difficulty == DailyTaskDifficulty.Normal ? "普通" : (task.difficulty == DailyTaskDifficulty.Hard ? "困难" : "史诗"));
            detailTitle.text = $"{task.title} [难度 {diffLabel}]";
            detailDesc.text = task.description;
            detailTarget.text = BuildTargetText(task);
            detailProgress.text = $"进度 {task.progress}/{task.requiredAmount}";
            detailRewardCashExp.text = $"奖励：现金 {task.rewardCashAmount}，经验 {task.rewardExpAmount}";
            detailRewardItems.text = BuildRewardItemsText(task);
            bool accepted = DailyQuestManager.Instance.IsAccepted(task.id);
            bool finished = DailyQuestManager.Instance.IsFinished(task.id);
            if (!accepted)
            {
                SetBottomButtonsVisible(true, false, false, false);
            }
            else if (finished)
            {
                bool claimed = DailyQuestManager.Instance.IsRewardClaimed(task.id);
                SetBottomButtonsVisible(false, false, false, !claimed);
            }
            else if (task.type == DailyTaskType.SubmitItem)
            {
                SetBottomButtonsVisible(false, false, true, false);
            }
            else
            {
                SetBottomButtonsVisible(false, true, false, false);
            }
        }

        private string BuildTargetText(DailyTask task)
        {
            if (task.type == DailyTaskType.UseItem)
            {
                var meta = ItemAssetsCollection.GetMetaData(task.targetItemId);
                return $"目标：使用 {meta.DisplayName} {task.requiredAmount} 次";
            }
            if (task.type == DailyTaskType.SubmitItem)
            {
                var meta = ItemAssetsCollection.GetMetaData(task.targetItemId);
                return $"目标：提交 {meta.DisplayName} x{task.requiredAmount}";
            }
            if (task.type == DailyTaskType.KillEnemy)
            {
                string name = string.IsNullOrEmpty(task.requireEnemyDisplayName) ? "白名单敌人" : task.requireEnemyDisplayName;
                return $"目标：击杀 {name} {task.requiredAmount} 名";
            }
            if (task.type == DailyTaskType.SpendCashAtMerchant)
            {
                var meta = ItemAssetsCollection.GetMetaData(GameplayDataSettings.ItemAssets.CashItemTypeID);
                return $"目标：在神秘商人处消费 {meta.DisplayName} x{task.requiredAmount}";
            }
            if (task.type == DailyTaskType.ChallengeKill)
            {
                var wmeta = ItemAssetsCollection.GetMetaData(task.requiredWeaponItemId);
                string ename = string.IsNullOrEmpty(task.requireEnemyDisplayName) ? "白名单敌人" : task.requireEnemyDisplayName;
                return $"目标：使用 {wmeta.DisplayName} 击杀 {ename} {task.requiredAmount} 名";
            }
            return "目标：";
        }

        private string BuildRewardItemsText(DailyTask task)
        {
            if (task.rewardItems == null || task.rewardItems.Count == 0) return "奖励物品：无";
            var names = new List<string>();
            for (int i = 0; i < task.rewardItems.Count; i++)
            {
                var meta = ItemAssetsCollection.GetMetaData(task.rewardItems[i].typeId);
                names.Add($"{meta.DisplayName} x{task.rewardItems[i].count}");
            }
            return "奖励物品：" + string.Join("，", names);
        }
    }
}
