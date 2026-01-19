using Duckov.UI;
using Duckov;
using Duckov.Utilities;
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
            // 等待一帧确保UI管理器已就绪
            StartCoroutine(EnsureIconRoutine());
        }

        private void OnDisable()
        {
            if (iconGo != null)
            {
                Destroy(iconGo);
                iconGo = null!;
                rt = null!;
            }
        }

        private System.Collections.IEnumerator EnsureIconRoutine()
        {
            yield return null;
            EnsureIcon();
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
                UpdateIconPosition();
                return;
            }

            iconGo = new GameObject("DailyQuestMenuIcon");
            iconGo.transform.SetParent(parent, false);
            rt = iconGo.AddComponent<RectTransform>();
            UpdateIconPosition();

            var btn = iconGo.AddComponent<Button>();
            var bg = iconGo.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);
            
            var shadow = iconGo.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            shadow.effectDistance = new Vector2(2, -2);
            
            var outline = iconGo.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.7f, 0.3f, 0.6f);
            outline.effectDistance = new Vector2(1, 1);

            var colors = btn.colors;
            colors.normalColor = new Color(0.15f, 0.15f, 0.2f, 0.9f);
            colors.highlightedColor = new Color(0.25f, 0.25f, 0.35f, 0.95f);
            colors.pressedColor = new Color(0.35f, 0.35f, 0.45f, 1f);
            colors.selectedColor = new Color(0.25f, 0.25f, 0.35f, 0.95f);
            btn.colors = colors;

            var labelTemplate = GameplayDataSettings.UIStyle.TemplateTextUGUI;
            var label = Instantiate(labelTemplate, iconGo.transform, false);
            label.name = "Label";
            label.text = "任";
            label.color = new Color(1f, 0.85f, 0.5f, 1f);
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 24f;
            label.fontStyle = FontStyles.Bold;
            label.enableWordWrapping = false;
            
            var labelRt = label.rectTransform;
            labelRt.anchorMin = new Vector2(0.5f, 0.5f);
            labelRt.anchorMax = new Vector2(0.5f, 0.5f);
            labelRt.pivot = new Vector2(0.5f, 0.5f);
            labelRt.sizeDelta = new Vector2(28, 28);
            labelRt.anchoredPosition = Vector2.zero;

            btn.onClick.AddListener(OpenView);
        }

        private void UpdateIconPosition()
        {
            if (rt == null) return;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(40, 40);
            float x = PlayerPrefs.GetFloat(KeyX, 8f);
            float y = PlayerPrefs.GetFloat(KeyY, -8f);
            rt.anchoredPosition = new Vector2(x, y);
        }

        private void OpenView()
        {
            if (DailyQuestView.Instance != null)
            {
                DailyQuestView.Instance.Open();
                return;
            }

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
            
            // 限制在屏幕边界内（简单检查）
            float x = Mathf.Clamp(rt.anchoredPosition.x, -Screen.width / 2f, Screen.width / 2f);
            float y = Mathf.Clamp(rt.anchoredPosition.y, -Screen.height / 2f, Screen.height / 2f);
            rt.anchoredPosition = new Vector2(x, y);
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
