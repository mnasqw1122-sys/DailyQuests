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
                // Prefer Singleton instance
                if (DailyQuestView.Instance != null)
                {
                    view = DailyQuestView.Instance;
                }

                if (view == null)
                {
                    // Create new if missing
                    var go = new GameObject("DailyQuestView");
                    
                    // Fix: Ensure parent is valid UI root, fallback to creating a temporary Canvas if needed
                    Transform parent = null!;
                    if (GameplayUIManager.Instance != null)
                    {
                        parent = GameplayUIManager.Instance.transform;
                    }
                    else
                    {
                        // Fallback: Try find any Canvas
                        var canvas = FindObjectOfType<Canvas>();
                        if (canvas != null) parent = canvas.transform;
                        else
                        {
                            // Last resort: create a canvas
                            var cGo = new GameObject("TempCanvas");
                            var c = cGo.AddComponent<Canvas>();
                            c.renderMode = RenderMode.ScreenSpaceOverlay;
                            parent = cGo.transform;
                        }
                    }
                    
                    go.transform.SetParent(parent, false);
                    view = go.AddComponent<DailyQuestView>();
                }
                
                view.Open();
                
                // Fix: Must stop interact to release player control lock
                StopInteract();
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
