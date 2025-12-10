using Duckov.UI;
using UnityEngine;

namespace DailyQuests
{
    public class DailyQuestInteractable : InteractableBase
    {
    private DailyQuestView view = null!;

        protected override void Awake()
        {
            base.Awake();
            InteractName = "UI_DailyQuest";
        }

        protected override void OnInteractStart(CharacterMainControl interactCharacter)
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

        protected override void OnInteractStop()
        {
        }
    }
}
