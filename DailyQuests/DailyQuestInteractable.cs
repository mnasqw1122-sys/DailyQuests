using Duckov.UI;
using UnityEngine;

namespace DailyQuests
{
    public class DailyQuestInteractable : InteractableBase
    {
    private DailyQuestView view = null!;

        protected override void Awake()
        {
            // Initialize private field otherInterablesInGroup in base class to avoid NRE in base.Awake()
            try 
            {
                var field = typeof(InteractableBase).GetField("otherInterablesInGroup", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (field != null && field.GetValue(this) == null)
                {
                    field.SetValue(this, new System.Collections.Generic.List<InteractableBase>());
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DailyQuests] Failed to initialize otherInterablesInGroup: {ex}");
            }

            base.Awake();
            InteractName = "UI_DailyQuest";
        }

        protected override void OnInteractStart(CharacterMainControl interactCharacter)
        {
            try
            {
                if (DailyQuestView.Instance != null)
                {
                    view = DailyQuestView.Instance;
                }

                if (view == null)
                {
                    var go = new GameObject("DailyQuestView");
                    var parent = GameplayUIManager.Instance ? GameplayUIManager.Instance.transform : this.transform;
                    go.transform.SetParent(parent, false);
                    view = go.AddComponent<DailyQuestView>();
                }
                view.Open();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DailyQuests] Failed to open DailyQuestView: {ex}");
                interactCharacter.PopText("无法打开每日任务界面，请检查日志");
                StopInteract();
            }
        }

        protected override void OnInteractStop()
        {
        }
    }
}
