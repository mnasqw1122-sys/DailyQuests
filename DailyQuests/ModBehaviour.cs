using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Duckov.Quests;
using Duckov.UI;
using Duckov.Utilities;
using UnityEngine;

namespace DailyQuests
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        private DailyQuestInteractable interactable = null!;
        private QuestGiver jeff = null!;

        private void Awake()
        {
            Debug.Log("DailyQuests V0.95 Loaded");
            SodaCraft.Localizations.LocalizationManager.SetOverrideText("UI_DailyQuest", "每日任务");
        }

        private void OnEnable()
        {
            LevelManager.OnLevelInitialized += OnLevelInitialized;
            Quest.onQuestCompleted += OnQuestCompleted;
        }

        private void OnDisable()
        {
            LevelManager.OnLevelInitialized -= OnLevelInitialized;
            Quest.onQuestCompleted -= OnQuestCompleted;
        }

        private void OnLevelInitialized()
        {
            TryInject().Forget();
        }

        private async UniTask TryInject()
        {
            await UniTask.Yield();
            EnsureTopIcon();
            EnsureMerchantHook();
            jeff = UnityEngine.Object.FindObjectsOfType<QuestGiver>(true).FirstOrDefault(e => e != null && e.ID == QuestGiverID.Jeff);
            if (jeff == null)
            {
                DailyQuestManager.Instance.Initialize();
                
                return;
            }

            interactable = jeff.GetComponent<DailyQuestInteractable>();
            if (interactable == null)
            {
                interactable = jeff.gameObject.AddComponent<DailyQuestInteractable>();
            }

            interactable.interactMarkerOffset = jeff.interactMarkerOffset + new Vector3(0f, -0.6f, 0f);
            interactable.MarkerActive = false;
            interactable.InteractName = "UI_DailyQuest";

            var fi = typeof(InteractableBase).GetField("otherInterablesInGroup", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var list = fi.GetValue(jeff) as System.Collections.IList;
            if (list != null && !list.Contains(interactable))
            {
                list.Add(interactable);
            }
            jeff.interactableGroup = true;

            DailyQuestManager.Instance.Initialize();
            DailyQuestManager.Instance.AttachJeff(jeff);
            
        }

        private void EnsureTopIcon()
        {
            if (GameplayUIManager.Instance == null) return;
            var topIcon = GameplayUIManager.Instance.GetComponent<DailyQuestTopIcon>();
            if (topIcon == null)
            {
                GameplayUIManager.Instance.gameObject.AddComponent<DailyQuestTopIcon>();
            }
        }

        private void EnsureMerchantHook()
        {
            if (GameplayUIManager.Instance == null) return;
            var hook = GameplayUIManager.Instance.GetComponent<MerchantPurchaseHooker>();
            if (hook == null)
            {
                GameplayUIManager.Instance.gameObject.AddComponent<MerchantPurchaseHooker>();
            }
        }

        

        private void OnQuestCompleted(Quest quest)
        {
            DailyQuestManager.Instance.OnQuestCompleted(quest);
        }
    }
}
