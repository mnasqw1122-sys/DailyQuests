using Duckov.UI;
using Duckov;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace DailyQuests
{
    public class DailyQuestTopIcon : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private GameObject iconGo = null!;
        private RectTransform rt = null!;
        private const string KeyX = "DailyQuestTopIcon_PosX";
        private const string KeyY = "DailyQuestTopIcon_PosY";

        private void OnEnable()
        {
            EnsureIcon();
        }

        private void OnDisable()
        {
        }

        private void EnsureIcon()
        {
            if (GameplayUIManager.Instance == null) return;
            var parent = GameplayUIManager.Instance.transform;
            var existed = parent.Find("DailyQuestMenuIcon");
            if (existed != null)
            {
                iconGo = existed.gameObject;
                rt = iconGo.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0f, 1f);
                    rt.sizeDelta = new Vector2(40, 40);
                    float px = PlayerPrefs.GetFloat(KeyX, 8f);
                    float py = PlayerPrefs.GetFloat(KeyY, -8f);
                    rt.anchoredPosition = new Vector2(px, py);
                }
                return;
            }

            iconGo = new GameObject("DailyQuestMenuIcon");
            iconGo.transform.SetParent(parent, false);
            rt = iconGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(40, 40);
            float x = PlayerPrefs.GetFloat(KeyX, 8f);
            float y = PlayerPrefs.GetFloat(KeyY, -8f);
            rt.anchoredPosition = new Vector2(x, y);

            var btn = iconGo.AddComponent<Button>();
            var bg = iconGo.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.12f);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(iconGo.transform, false);
            var label = labelGo.AddComponent<TextMeshProUGUI>();
            label.text = "任";
            label.color = new Color(1f, 1f, 1f, 0.95f);
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 22f;
            var labelRt = label.rectTransform;
            labelRt.anchorMin = new Vector2(0.5f, 0.5f);
            labelRt.anchorMax = new Vector2(0.5f, 0.5f);
            labelRt.pivot = new Vector2(0.5f, 0.5f);
            labelRt.sizeDelta = new Vector2(28, 28);
            labelRt.anchoredPosition = Vector2.zero;

            btn.onClick.AddListener(OpenView);
        }

        private void OpenView()
        {
            var parent = GameplayUIManager.Instance ? GameplayUIManager.Instance.transform : this.transform;
            var go = new GameObject("DailyQuestView");
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<DailyQuestView>();
            view.Open();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (rt == null) return;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (rt == null) return;
            if (!(Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))) return;
            rt.anchoredPosition += eventData.delta;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (rt == null) return;
            PlayerPrefs.SetFloat(KeyX, rt.anchoredPosition.x);
            PlayerPrefs.SetFloat(KeyY, rt.anchoredPosition.y);
            PlayerPrefs.Save();
        }
    }
}
