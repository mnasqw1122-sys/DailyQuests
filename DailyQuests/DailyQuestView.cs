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
        public static DailyQuestView Instance { get; private set; } = null!;

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
        private RectTransform detailTitle = null!;
        private RectTransform detailDesc = null!;
        private RectTransform detailTarget = null!;
        private RectTransform detailProgress = null!;
        private RectTransform detailRewardCashExp = null!;
        private RectTransform detailRewardItems = null!;
        private Image detailProgressBarBg = null!;
        private Image detailProgressBarFill = null!;
        private TextMeshProUGUI detailProgressText = null!;
        private Button btnAccept = null!;
        private Button btnAbandon = null!;
        private Button btnSubmit = null!;
        private Button btnFinish = null!;
        
        private readonly List<GameObject> activeEntries = new List<GameObject>();
        private readonly Stack<GameObject> entryPool = new Stack<GameObject>();
        
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
        private readonly Color difficultyEasyColor = new Color(0.4f, 0.8f, 0.4f, 1f);
        private readonly Color difficultyNormalColor = new Color(0.4f, 0.6f, 1f, 1f);
        private readonly Color difficultyHardColor = new Color(1f, 0.5f, 0.3f, 1f);
        private readonly Color difficultyEpicColor = new Color(0.8f, 0.3f, 1f, 1f);
        private readonly Color headerGradientTop = new Color(0.15f, 0.15f, 0.2f, 0.95f);
        private readonly Color headerGradientBottom = new Color(0.1f, 0.1f, 0.15f, 0.95f);

        private class RowClickRelay : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
        {
            public DailyQuestView view = null!;
            public int index;
            public int taskId;
            public Image backgroundImage = null!;
            public void OnPointerClick(PointerEventData eventData)
            {
                if (view == null) return;
                view.SetSelection(index);
                view.ShowDetail(taskId);
            }
            public void OnPointerEnter(PointerEventData eventData)
            {
                if (view == null || backgroundImage == null) return;
                if (index != view.selectedIndex)
                {
                    backgroundImage.color = new Color(0.15f, 0.15f, 0.2f, 0.6f);
                }
            }
            public void OnPointerExit(PointerEventData eventData)
            {
                if (view == null || backgroundImage == null) return;
                if (index != view.selectedIndex)
                {
                    backgroundImage.color = view.rowUnselectedColor;
                }
            }
        }

        protected override void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            try
            {
                base.Awake();
            }
            catch
            {
                // 忽略 View.Awake 中的层级错误（例如 ViewTabs 查找）
            }
            BuildUI();
            gameObject.SetActive(false);
        }

        protected override void OnDestroy()
        {
            if (Instance == this) Instance = null!;
            base.OnDestroy();
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

            var bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.1f, 0.95f);

            var headerGo = new GameObject("Header");
            headerGo.transform.SetParent(root, false);
            header = headerGo.AddComponent<RectTransform>();
            header.anchorMin = new Vector2(0, 1);
            header.anchorMax = new Vector2(1, 1);
            header.pivot = new Vector2(0.5f, 1);
            float headerH = 72f;
            header.sizeDelta = new Vector2(0, headerH);
            
            var headerBg = headerGo.AddComponent<Image>();
            headerBg.color = headerGradientTop;
            
            var headerBorder = headerGo.AddComponent<Outline>();
            headerBorder.effectColor = new Color(0.3f, 0.3f, 0.4f, 0.5f);
            headerBorder.effectDistance = new Vector2(0, -2);

            var titleText = Object.Instantiate(GameplayDataSettings.UIStyle.TemplateTextUGUI);
            titleText.transform.SetParent(header, false);
            titleText.text = "每日任务 V0.95";
            titleText.alignment = TextAlignmentOptions.MidlineLeft;
            titleText.fontSize = 28f;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = new Color(1f, 0.9f, 0.7f, 1f);
            titleText.rectTransform.anchorMin = new Vector2(0, 0.5f);
            titleText.rectTransform.anchorMax = new Vector2(0, 0.5f);
            titleText.rectTransform.pivot = new Vector2(0, 0.5f);
            titleText.rectTransform.anchoredPosition = new Vector2(20, 0);
            title = titleText;

            var refreshGo = new GameObject("RefreshButton");
            refreshGo.transform.SetParent(header, false);
            btnRefresh = refreshGo.AddComponent<Button>();
            var img = refreshGo.AddComponent<Image>();
            img.color = new Color(0.3f, 0.65f, 0.95f, 1f);
            var rt = refreshGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(140, 44);
            rt.anchorMin = new Vector2(1, 0.5f);
            rt.anchorMax = new Vector2(1, 0.5f);
            rt.pivot = new Vector2(1, 0.5f);
            rt.anchoredPosition = new Vector2(-20, 0);
            
            var refreshShadow = refreshGo.AddComponent<Shadow>();
            refreshShadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            refreshShadow.effectDistance = new Vector2(2, -2);
            
            var refreshLabel = Object.Instantiate(GameplayDataSettings.UIStyle.TemplateTextUGUI);
            refreshLabel.transform.SetParent(refreshGo.transform, false);
            refreshLabel.text = "刷新未接取";
            refreshLabel.alignment = TextAlignmentOptions.Center;
            refreshLabel.fontSize = 20f;
            refreshLabel.fontStyle = FontStyles.Bold;
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
            leftImg.color = new Color(0, 0, 0, 0.25f);
            leftImg.raycastTarget = false;
            var leftBorder = leftGo.AddComponent<Outline>();
            leftBorder.effectColor = new Color(0.4f, 0.4f, 0.5f, 0.3f);
            leftBorder.effectDistance = new Vector2(2, 2);
            var leftLE = leftGo.AddComponent<LayoutElement>();
            leftLE.preferredWidth = 440f;
            leftLE.flexibleWidth = 0f;

            var rightGo = new GameObject("RightPanel");
            rightGo.transform.SetParent(bodyGo.transform, false);
            rightPanel = rightGo.AddComponent<RectTransform>();
            var rightImg = rightGo.AddComponent<Image>();
            rightImg.color = new Color(0, 0, 0, 0.18f);
            rightImg.raycastTarget = false;
            var rightBorder = rightGo.AddComponent<Outline>();
            rightBorder.effectColor = new Color(0.4f, 0.4f, 0.5f, 0.3f);
            rightBorder.effectDistance = new Vector2(2, 2);
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
            rightContent.offsetMin = new Vector2(16, 80);
            rightContent.offsetMax = new Vector2(-16, -100);
            var vlg2 = rcGo.AddComponent<VerticalLayoutGroup>();
            vlg2.childControlHeight = true;
            vlg2.childControlWidth = true;
            vlg2.childForceExpandHeight = true;
            vlg2.childForceExpandWidth = true;
            vlg2.spacing = 24f;
            vlg2.padding = new RectOffset(16, 16, 16, 16);
            vlg2.childAlignment = TextAnchor.UpperLeft;

            var titleContainer = new GameObject("TitleContainer");
            titleContainer.transform.SetParent(rcGo.transform, false);
            var titleRt = titleContainer.AddComponent<RectTransform>();
            var titleLE = titleContainer.AddComponent<LayoutElement>();
            titleLE.preferredHeight = 60f;
            titleLE.flexibleHeight = 1f;
            var dt = Object.Instantiate(GameplayDataSettings.UIStyle.TemplateTextUGUI);
            dt.transform.SetParent(titleContainer.transform, false);
            dt.rectTransform.anchorMin = new Vector2(0, 0);
            dt.rectTransform.anchorMax = new Vector2(1, 1);
            dt.rectTransform.pivot = new Vector2(0, 0);
            dt.rectTransform.offsetMin = Vector2.zero;
            dt.rectTransform.offsetMax = Vector2.zero;
            dt.enableWordWrapping = true;
            dt.fontSize = 26f;
            dt.fontStyle = FontStyles.Bold;
            dt.alignment = TextAlignmentOptions.TopLeft;
            dt.color = new Color(1f, 0.85f, 0.6f, 1f);
            detailTitle = dt.rectTransform;

            var descContainer = new GameObject("DescContainer");
            descContainer.transform.SetParent(rcGo.transform, false);
            var descRt = descContainer.AddComponent<RectTransform>();
            var descLE = descContainer.AddComponent<LayoutElement>();
            descLE.preferredHeight = 40f;
            descLE.flexibleHeight = 1f;
            var dd = Object.Instantiate(GameplayDataSettings.UIStyle.TemplateTextUGUI);
            dd.transform.SetParent(descContainer.transform, false);
            dd.rectTransform.anchorMin = new Vector2(0, 0);
            dd.rectTransform.anchorMax = new Vector2(1, 1);
            dd.rectTransform.pivot = new Vector2(0, 0);
            dd.rectTransform.offsetMin = Vector2.zero;
            dd.rectTransform.offsetMax = Vector2.zero;
            dd.enableWordWrapping = true;
            dd.fontSize = 18f;
            dd.alignment = TextAlignmentOptions.TopLeft;
            dd.color = new Color(0.9f, 0.9f, 0.95f, 1f);
            detailDesc = dd.rectTransform;

            var separator1 = CreateSeparator(rcGo);
            
            var targetContainer = new GameObject("TargetContainer");
            targetContainer.transform.SetParent(rcGo.transform, false);
            var targetRt = targetContainer.AddComponent<RectTransform>();
            var targetLE = targetContainer.AddComponent<LayoutElement>();
            targetLE.preferredHeight = 40f;
            targetLE.flexibleHeight = 1f;
            var tg = Object.Instantiate(GameplayDataSettings.UIStyle.TemplateTextUGUI);
            tg.transform.SetParent(targetContainer.transform, false);
            tg.rectTransform.anchorMin = new Vector2(0, 0);
            tg.rectTransform.anchorMax = new Vector2(1, 1);
            tg.rectTransform.pivot = new Vector2(0, 0);
            tg.rectTransform.offsetMin = Vector2.zero;
            tg.rectTransform.offsetMax = Vector2.zero;
            tg.enableWordWrapping = true;
            tg.fontSize = 18f;
            tg.alignment = TextAlignmentOptions.TopLeft;
            tg.color = new Color(0.85f, 0.9f, 1f, 1f);
            detailTarget = tg.rectTransform;

            var progressContainer = new GameObject("ProgressContainer");
            progressContainer.transform.SetParent(rcGo.transform, false);
            var progressRt = progressContainer.AddComponent<RectTransform>();
            var progressLE = progressContainer.AddComponent<LayoutElement>();
            progressLE.preferredHeight = 60f;
            
            var progressBgGo = new GameObject("ProgressBarBg");
            progressBgGo.transform.SetParent(progressContainer.transform, false);
            var progressBgRt = progressBgGo.AddComponent<RectTransform>();
            progressBgRt.sizeDelta = new Vector2(0, 18);
            progressBgRt.anchorMin = new Vector2(0, 0.5f);
            progressBgRt.anchorMax = new Vector2(1, 0.5f);
            progressBgRt.pivot = new Vector2(0.5f, 0.5f);
            detailProgressBarBg = progressBgGo.AddComponent<Image>();
            detailProgressBarBg.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);
            
            var progressFillGo = new GameObject("Fill");
            progressFillGo.transform.SetParent(progressBgGo.transform, false);
            var fillRt = progressFillGo.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            detailProgressBarFill = progressFillGo.AddComponent<Image>();
            detailProgressBarFill.color = new Color(0.4f, 0.75f, 0.5f, 1f);
            
            detailProgressText = Object.Instantiate(GameplayDataSettings.UIStyle.TemplateTextUGUI);
            detailProgressText.transform.SetParent(progressContainer.transform, false);
            detailProgressText.alignment = TextAlignmentOptions.Center;
            detailProgressText.fontSize = 16f;
            detailProgressText.fontStyle = FontStyles.Bold;
            detailProgressText.rectTransform.anchorMin = new Vector2(0, 0.5f);
            detailProgressText.rectTransform.anchorMax = new Vector2(1, 0.5f);
            detailProgressText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            detailProgressText.rectTransform.anchoredPosition = Vector2.zero;
            detailProgress = detailProgressText.rectTransform;

            var separator2 = CreateSeparator(rcGo);

            var rewardCashContainer = new GameObject("RewardCashContainer");
            rewardCashContainer.transform.SetParent(rcGo.transform, false);
            var rewardCashRt = rewardCashContainer.AddComponent<RectTransform>();
            var rewardCashLE = rewardCashContainer.AddComponent<LayoutElement>();
            rewardCashLE.preferredHeight = 40f;
            rewardCashLE.flexibleHeight = 1f;
            var rwce = Object.Instantiate(GameplayDataSettings.UIStyle.TemplateTextUGUI);
            rwce.transform.SetParent(rewardCashContainer.transform, false);
            rwce.rectTransform.anchorMin = new Vector2(0, 0);
            rwce.rectTransform.anchorMax = new Vector2(1, 1);
            rwce.rectTransform.pivot = new Vector2(0, 0);
            rwce.rectTransform.offsetMin = Vector2.zero;
            rwce.rectTransform.offsetMax = Vector2.zero;
            rwce.enableWordWrapping = true;
            rwce.fontSize = 18f;
            rwce.alignment = TextAlignmentOptions.TopLeft;
            rwce.color = new Color(1f, 0.85f, 0.4f, 1f);
            detailRewardCashExp = rwce.rectTransform;

            var rewardItemsContainer = new GameObject("RewardItemsContainer");
            rewardItemsContainer.transform.SetParent(rcGo.transform, false);
            var rewardItemsRt = rewardItemsContainer.AddComponent<RectTransform>();
            var rewardItemsLE = rewardItemsContainer.AddComponent<LayoutElement>();
            rewardItemsLE.preferredHeight = 60f;
            rewardItemsLE.flexibleHeight = 1f;
            var rwit = Object.Instantiate(GameplayDataSettings.UIStyle.TemplateTextUGUI);
            rwit.transform.SetParent(rewardItemsContainer.transform, false);
            rwit.rectTransform.anchorMin = new Vector2(0, 0);
            rwit.rectTransform.anchorMax = new Vector2(1, 1);
            rwit.rectTransform.pivot = new Vector2(0, 0);
            rwit.rectTransform.offsetMin = Vector2.zero;
            rwit.rectTransform.offsetMax = Vector2.zero;
            rwit.enableWordWrapping = true;
            rwit.fontSize = 18f;
            rwit.alignment = TextAlignmentOptions.TopLeft;
            rwit.color = new Color(0.85f, 0.95f, 1f, 1f);
            detailRewardItems = rwit.rectTransform;

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
            foreach (var e in activeEntries)
            {
                e.SetActive(false);
                entryPool.Push(e);
            }
            activeEntries.Clear();
            entryIds.Clear();
            nameLabels.Clear();
            rowBackgrounds.Clear();
            selectedIndex = -1;
            selectedTaskId = -1;
            var titleText = detailTitle.GetComponent<TextMeshProUGUI>();
            if (titleText != null) titleText.text = "";
            var descText = detailDesc.GetComponent<TextMeshProUGUI>();
            if (descText != null) descText.text = "";
            var targetText = detailTarget.GetComponent<TextMeshProUGUI>();
            if (targetText != null) targetText.text = "";
            if (detailProgressText != null) detailProgressText.text = "";
            var rewardCashText = detailRewardCashExp.GetComponent<TextMeshProUGUI>();
            if (rewardCashText != null) rewardCashText.text = "";
            var rewardItemsText = detailRewardItems.GetComponent<TextMeshProUGUI>();
            if (rewardItemsText != null) rewardItemsText.text = "";
            if (detailProgressBarFill != null)
            {
                var fillRt = detailProgressBarFill.GetComponent<RectTransform>();
                fillRt.anchorMax = new Vector2(0f, 1f);
            }
            SetBottomButtonsVisible(false, false, false, false);
        }

        private void RefreshList()
        {
            ClearEntries();
            var tasks = DailyQuestManager.Instance.GetDailyTasks();
            var sortedTasks = tasks.OrderByDescending(t => t.accepted).ToList();
            for (int i = 0; i < sortedTasks.Count; i++)
            {
                CreateEntry(sortedTasks[i]);
            }
        }

        private void CreateEntry(DailyTask task)
        {
            GameObject entryGo;
            TextMeshProUGUI nameText;
            Image hbg;
            RowClickRelay click;
            Image? iconImg;
            TextMeshProUGUI? diffLabel;
            Image? progressBarBg;
            Image? progressBarFill;

            if (entryPool.Count > 0)
            {
                entryGo = entryPool.Pop();
                entryGo.SetActive(true);
                entryGo.transform.SetAsLastSibling();
                entryGo.name = $"Entry_{task.id}";
                
                hbg = entryGo.GetComponent<Image>();
                click = entryGo.GetComponent<RowClickRelay>();
                var iconGo = entryGo.transform.Find("Icon");
                iconImg = iconGo != null ? iconGo.GetComponent<Image>() : null;
                var diffLabelGo = entryGo.transform.Find("DiffLabel");
                diffLabel = diffLabelGo != null ? diffLabelGo.GetComponent<TextMeshProUGUI>() : null;
                var progressBgGo = entryGo.transform.Find("ProgressBarBg");
                progressBarBg = progressBgGo != null ? progressBgGo.GetComponent<Image>() : null;
                var progressFillGo = progressBgGo != null ? progressBgGo.Find("Fill") : null;
                progressBarFill = progressFillGo != null ? progressFillGo.GetComponent<Image>() : null;
                
                nameText = entryGo.GetComponentInChildren<TextMeshProUGUI>();
            }
            else
            {
                entryGo = new GameObject($"Entry_{task.id}");
                entryGo.transform.SetParent(listParent, false);
                var rt = entryGo.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(0, 96);
                hbg = entryGo.AddComponent<Image>();
                hbg.color = rowUnselectedColor;
                var entryShadow = entryGo.AddComponent<Shadow>();
                entryShadow.effectColor = new Color(0f, 0f, 0f, 0.3f);
                entryShadow.effectDistance = new Vector2(2, -2);
                var h = entryGo.AddComponent<HorizontalLayoutGroup>();
                h.childControlHeight = true;
                h.childControlWidth = false;
                h.childForceExpandHeight = false;
                h.childForceExpandWidth = false;
                h.spacing = 12f;
                h.padding = new RectOffset(10, 10, 8, 8);
                h.childAlignment = TextAnchor.MiddleLeft;
                entryGo.AddComponent<RectMask2D>();
                var entryLE = entryGo.AddComponent<LayoutElement>();
                entryLE.minHeight = 80f;
                entryLE.preferredHeight = 96f;

                var iconGo = new GameObject("Icon");
                iconGo.transform.SetParent(entryGo.transform, false);
                var iconRt = iconGo.AddComponent<RectTransform>();
                iconRt.sizeDelta = new Vector2(48, 48);
                iconImg = iconGo.AddComponent<Image>();
                iconImg.preserveAspect = true;
                var iconLE = iconGo.AddComponent<LayoutElement>();
                iconLE.minWidth = 48f;
                iconLE.preferredWidth = 48f;

                var textContainer = new GameObject("TextContainer");
                textContainer.transform.SetParent(entryGo.transform, false);
                var textRt = textContainer.AddComponent<RectTransform>();
                var textVlg = textContainer.AddComponent<VerticalLayoutGroup>();
                textVlg.childControlHeight = false;
                textVlg.childControlWidth = true;
                textVlg.childForceExpandHeight = false;
                textVlg.childForceExpandWidth = true;
                textVlg.spacing = 4f;
                textVlg.childAlignment = TextAnchor.UpperLeft;
                var textLE = textContainer.AddComponent<LayoutElement>();
                textLE.flexibleWidth = 1f;

                var topRow = new GameObject("TopRow");
                topRow.transform.SetParent(textContainer.transform, false);
                var topHlg = topRow.AddComponent<HorizontalLayoutGroup>();
                topHlg.childControlHeight = false;
                topHlg.childControlWidth = false;
                topHlg.childForceExpandHeight = false;
                topHlg.childForceExpandWidth = false;
                topHlg.spacing = 8f;
                topHlg.childAlignment = TextAnchor.MiddleLeft;

                nameText = Object.Instantiate(GameplayDataSettings.UIStyle.TemplateTextUGUI);
                nameText.transform.SetParent(topRow.transform, false);
                nameText.name = "NameText";
                nameText.alignment = TextAlignmentOptions.MidlineLeft;
                nameText.enableWordWrapping = true;
                nameText.fontSize = 18f;
                nameText.fontStyle = FontStyles.Bold;
                var nameLE = nameText.gameObject.AddComponent<LayoutElement>();
                nameLE.minWidth = 200f;
                nameLE.preferredWidth = 280f;
                nameLE.flexibleWidth = 1f;

                diffLabel = Object.Instantiate(GameplayDataSettings.UIStyle.TemplateTextUGUI);
                diffLabel.transform.SetParent(topRow.transform, false);
                diffLabel.name = "DiffLabel";
                diffLabel.alignment = TextAlignmentOptions.Center;
                diffLabel.fontSize = 14f;
                diffLabel.fontStyle = FontStyles.Bold;
                diffLabel.enableWordWrapping = false;
                var diffBgGo = new GameObject("DiffBg");
                diffBgGo.transform.SetParent(topRow.transform, false);
                var diffBgRt = diffBgGo.AddComponent<RectTransform>();
                diffBgRt.sizeDelta = new Vector2(50, 22);
                var diffBgImg = diffBgGo.AddComponent<Image>();
                diffBgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                diffLabel.transform.SetParent(diffBgGo.transform, false);
                diffLabel.rectTransform.anchorMin = Vector2.zero;
                diffLabel.rectTransform.anchorMax = Vector2.one;
                diffLabel.rectTransform.offsetMin = Vector2.zero;
                diffLabel.rectTransform.offsetMax = Vector2.zero;

                var progressContainer = new GameObject("ProgressContainer");
                progressContainer.transform.SetParent(textContainer.transform, false);
                var progressRt = progressContainer.AddComponent<RectTransform>();
                progressRt.sizeDelta = new Vector2(0, 12);

                var progressBarBgGo = new GameObject("ProgressBarBg");
                progressBarBgGo.transform.SetParent(progressContainer.transform, false);
                var progressBgRt = progressBarBgGo.AddComponent<RectTransform>();
                progressBgRt.anchorMin = Vector2.zero;
                progressBgRt.anchorMax = Vector2.one;
                progressBgRt.offsetMin = Vector2.zero;
                progressBgRt.offsetMax = Vector2.zero;
                progressBarBg = progressBarBgGo.AddComponent<Image>();
                progressBarBg.color = new Color(0.1f, 0.1f, 0.15f, 0.8f);

                var progressBarFillGo = new GameObject("Fill");
                progressBarFillGo.transform.SetParent(progressBarBgGo.transform, false);
                var fillRt = progressBarFillGo.AddComponent<RectTransform>();
                fillRt.anchorMin = Vector2.zero;
                fillRt.anchorMax = Vector2.one;
                fillRt.offsetMax = new Vector2(0, 0);
                fillRt.offsetMin = new Vector2(0, 0);
                progressBarFill = progressBarFillGo.AddComponent<Image>();
                progressBarFill.color = new Color(0.4f, 0.75f, 0.5f, 1f);

                click = entryGo.AddComponent<RowClickRelay>();
                click.view = this;
            }

            click.backgroundImage = hbg;

            if (iconImg != null) iconImg.sprite = task.Icon;

            Color diffColor = difficultyEasyColor;
            string diffText = "简单";
            switch (task.difficulty)
            {
                case DailyTaskDifficulty.Normal:
                    diffColor = difficultyNormalColor;
                    diffText = "普通";
                    break;
                case DailyTaskDifficulty.Hard:
                    diffColor = difficultyHardColor;
                    diffText = "困难";
                    break;
                case DailyTaskDifficulty.Epic:
                    diffColor = difficultyEpicColor;
                    diffText = "史诗";
                    break;
            }
            if (diffLabel != null)
            {
                diffLabel.text = diffText;
                diffLabel.color = diffColor;
                var diffBg = diffLabel.transform.parent?.GetComponent<Image>();
                if (diffBg != null) diffBg.color = new Color(diffColor.r * 0.3f, diffColor.g * 0.3f, diffColor.b * 0.3f, 0.8f);
            }

            bool accepted = DailyQuestManager.Instance.IsAccepted(task.id);
            bool finished = DailyQuestManager.Instance.IsFinished(task.id);
            if (finished)
            {
                nameText.text = $"{task.title} [已完成]";
                nameText.color = new Color(0.6f, 0.9f, 0.6f, 1f);
            }
            else if (accepted)
            {
                nameText.text = $"{task.title} [已接取]";
                nameText.color = new Color(1f, 1f, 1f, 1f);
            }
            else
            {
                nameText.text = task.title;
                nameText.color = new Color(1f, 1f, 1f, 0.9f);
            }

            float progressRatio = task.requiredAmount > 0 ? (float)task.progress / task.requiredAmount : 0f;
            if (progressBarFill != null)
            {
                var fillRt = progressBarFill.GetComponent<RectTransform>();
                fillRt.anchorMax = new Vector2(Mathf.Clamp01(progressRatio), 1f);
                progressBarFill.color = finished ? new Color(0.4f, 0.8f, 0.4f, 1f) : (accepted ? new Color(0.4f, 0.7f, 0.9f, 1f) : new Color(0.5f, 0.5f, 0.5f, 0.5f));
            }

            float baseH = 80f;
            float computedH = nameText.preferredHeight + 40f;
            int rowH = (int)Mathf.Max(baseH, computedH);
            var entryLE_ = entryGo.GetComponent<LayoutElement>();
            if (entryLE_) entryLE_.preferredHeight = rowH;
            var rt_ = entryGo.GetComponent<RectTransform>();
            if (rt_) rt_.sizeDelta = new Vector2(0, rowH);

            click.index = activeEntries.Count;
            click.taskId = task.id;

            activeEntries.Add(entryGo);
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
            img.color = new Color(1f, 1f, 1f, 0.15f);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 0);
            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            shadow.effectDistance = new Vector2(2, -2);
            
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0.5f, 0.5f, 0.6f, 0.3f);
            outline.effectDistance = new Vector2(1, 1);
            
            var label = Object.Instantiate(GameplayDataSettings.UIStyle.TemplateTextUGUI);
            label.transform.SetParent(go.transform, false);
            label.text = text;
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 22f;
            label.fontStyle = FontStyles.Bold;
            label.rectTransform.anchorMin = new Vector2(0, 0);
            label.rectTransform.anchorMax = new Vector2(1, 1);
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
            
            var colors = btn.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0.15f);
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.25f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.35f);
            colors.selectedColor = new Color(1f, 1f, 1f, 0.25f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            btn.colors = colors;
            
            return btn;
        }

        private GameObject CreateSeparator(GameObject parent)
        {
            var sepGo = new GameObject("Separator");
            sepGo.transform.SetParent(parent.transform, false);
            var sepRt = sepGo.AddComponent<RectTransform>();
            sepRt.sizeDelta = new Vector2(0, 2);
            var sepImg = sepGo.AddComponent<Image>();
            sepImg.color = new Color(0.3f, 0.3f, 0.4f, 0.4f);
            var sepLe = sepGo.AddComponent<LayoutElement>();
            sepLe.preferredHeight = 2f;
            sepLe.minHeight = 2f;
            return sepGo;
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
            var titleText = detailTitle.GetComponent<TextMeshProUGUI>();
            if (titleText != null) titleText.text = $"{task.title} [难度 {diffLabel}]";
            
            var descText = detailDesc.GetComponent<TextMeshProUGUI>();
            if (descText != null) descText.text = task.description;
            
            var targetText = detailTarget.GetComponent<TextMeshProUGUI>();
            if (targetText != null) targetText.text = BuildTargetText(task);
            
            bool accepted = DailyQuestManager.Instance.IsAccepted(task.id);
            bool finished = DailyQuestManager.Instance.IsFinished(task.id);
            
            float progressRatio = task.requiredAmount > 0 ? (float)task.progress / task.requiredAmount : 0f;
            if (detailProgressBarFill != null)
            {
                var fillRt = detailProgressBarFill.GetComponent<RectTransform>();
                fillRt.anchorMax = new Vector2(Mathf.Clamp01(progressRatio), 1f);
                detailProgressBarFill.color = finished ? new Color(0.4f, 0.8f, 0.4f, 1f) : (accepted ? new Color(0.4f, 0.7f, 0.9f, 1f) : new Color(0.5f, 0.5f, 0.5f, 0.5f));
            }
            if (detailProgressText != null) detailProgressText.text = $"进度 {task.progress}/{task.requiredAmount}";
            
            var rewardCashText = detailRewardCashExp.GetComponent<TextMeshProUGUI>();
            if (rewardCashText != null) rewardCashText.text = $"奖励：现金 {task.rewardCashAmount}，经验 {task.rewardExpAmount}";
            
            var rewardItemsText = detailRewardItems.GetComponent<TextMeshProUGUI>();
            if (rewardItemsText != null) rewardItemsText.text = BuildRewardItemsText(task);
            
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
            // 检查新的奖励列表
            if (task.rewardItems != null && task.rewardItems.Count > 0)
            {
                var names = new List<string>();
                for (int i = 0; i < task.rewardItems.Count; i++)
                {
                    var meta = ItemAssetsCollection.GetMetaData(task.rewardItems[i].typeId);
                    names.Add($"{meta.DisplayName} x{task.rewardItems[i].count}");
                }
                return "奖励物品：" + string.Join("，", names);
            }
            
            // 回退处理旧数据
            #pragma warning disable 612, 618
            if (task.rewardItemTypeId > 0 && task.rewardItemCount > 0)
            {
                var meta = ItemAssetsCollection.GetMetaData(task.rewardItemTypeId);
                return $"奖励物品：{meta.DisplayName} x{task.rewardItemCount}";
            }
            #pragma warning restore 612, 618

            return "奖励物品：无";
        }
    }
}
