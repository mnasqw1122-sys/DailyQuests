using Duckov.UI;
using UnityEngine;

namespace DailyQuests
{
    public class DailyQuestInteractable : InteractableBase
    {
    private DailyQuestView view = null!;

        protected override void Awake()
        {
            // 初始化基类中的私有字段 otherInterablesInGroup 以避免在 base.Awake() 中出现空引用异常
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
                    // 优先使用单例实例
                    if (DailyQuestView.Instance != null)
                    {
                        view = DailyQuestView.Instance;
                    }

                    if (view == null)
                    {
                        // 如果缺失则创建新的
                        var go = new GameObject("DailyQuestView");
                        
                        // 修复：确保父级是有效的UI根节点，必要时回退创建临时Canvas
                        Transform parent = null!;
                        if (GameplayUIManager.Instance != null)
                        {
                            parent = GameplayUIManager.Instance.transform;
                        }
                        else
                        {
                            // 回退方案：尝试查找任何Canvas
                            var canvas = FindObjectOfType<Canvas>();
                            if (canvas != null) parent = canvas.transform;
                            else
                            {
                                // 最后手段：创建一个canvas
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
                    
                    // 修复：必须停止交互以释放玩家控制锁定
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
