using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using SunnysideIsland.UI.Menu;

namespace SunnysideIsland.Editor
{
    public class SaveLoadUIBuilder : EditorWindow
    {
        [MenuItem("Tools/Save System/Generate Save-Load UI")]
        public static void GenerateUI()
        {
            // 1. Canvas 찾거나 생성
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasGO.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            // 2. Main Panel 생성
            GameObject panelGO = new GameObject("SaveLoadPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(SaveLoadPanel));
            panelGO.transform.SetParent(canvas.transform, false);
            
            RectTransform panelRT = panelGO.GetComponent<RectTransform>();
            panelRT.anchorMin = Vector2.zero;
            panelRT.anchorMax = Vector2.one;
            panelRT.sizeDelta = Vector2.zero;

            Image panelImg = panelGO.GetComponent<Image>();
            panelImg.color = new Color(0, 0, 0, 0.8f);

            SaveLoadPanel saveLoadPanel = panelGO.GetComponent<SaveLoadPanel>();

            // 3. Title Text 생성
            GameObject titleGO = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(panelGO.transform, false);
            TextMeshProUGUI titleTMP = titleGO.GetComponent<TextMeshProUGUI>();
            titleTMP.text = "Save/Load Game";
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.fontSize = 48;
            
            RectTransform titleRT = titleGO.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0.5f, 1f);
            titleRT.anchorMax = new Vector2(0.5f, 1f);
            titleRT.pivot = new Vector2(0.5f, 1f);
            titleRT.anchoredPosition = new Vector2(0, -50);
            titleRT.sizeDelta = new Vector2(600, 100);

            // 4. Scroll View 생성
            GameObject scrollGO = new GameObject("Scroll View", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
            scrollGO.transform.SetParent(panelGO.transform, false);
            
            RectTransform scrollRT = scrollGO.GetComponent<RectTransform>();
            scrollRT.anchorMin = new Vector2(0.1f, 0.2f);
            scrollRT.anchorMax = new Vector2(0.9f, 0.8f);
            scrollRT.sizeDelta = Vector2.zero;

            Image scrollImg = scrollGO.GetComponent<Image>();
            scrollImg.color = new Color(1, 1, 1, 0.1f);

            ScrollRect scrollRect = scrollGO.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            // Viewport
            GameObject viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
            viewportGO.transform.SetParent(scrollGO.transform, false);
            RectTransform viewportRT = viewportGO.GetComponent<RectTransform>();
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.sizeDelta = Vector2.zero;
            viewportGO.GetComponent<Mask>().showMaskGraphic = false;
            scrollRect.viewport = viewportRT;

            // Content
            GameObject contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGO.transform.SetParent(viewportGO.transform, false);
            RectTransform contentRT = contentGO.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0.5f, 1);
            contentRT.sizeDelta = new Vector2(0, 0);

            VerticalLayoutGroup vlg = contentGO.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(20, 20, 20, 20);
            vlg.spacing = 10;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;

            ContentSizeFitter csf = contentGO.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRT;

            // 5. Close Button
            GameObject closeButtonGO = new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            closeButtonGO.transform.SetParent(panelGO.transform, false);
            
            RectTransform closeRT = closeButtonGO.GetComponent<RectTransform>();
            closeRT.anchorMin = new Vector2(1, 1);
            closeRT.anchorMax = new Vector2(1, 1);
            closeRT.pivot = new Vector2(1, 1);
            closeRT.anchoredPosition = new Vector2(-20, -20);
            closeRT.sizeDelta = new Vector2(60, 60);
            
            closeButtonGO.GetComponent<Image>().color = Color.red;
            Button closeBtn = closeButtonGO.GetComponent<Button>();
            
            GameObject closeTextGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            closeTextGO.transform.SetParent(closeButtonGO.transform, false);
            TextMeshProUGUI closeTMP = closeTextGO.GetComponent<TextMeshProUGUI>();
            closeTMP.text = "X";
            closeTMP.alignment = TextAlignmentOptions.Center;
            closeTMP.fontSize = 30;
            ((RectTransform)closeTextGO.transform).sizeDelta = Vector2.zero;
            ((RectTransform)closeTextGO.transform).anchorMin = Vector2.zero;
            ((RectTransform)closeTextGO.transform).anchorMax = Vector2.one;

            // 6. 스크립트 연결 (중요!)
            // SaveLoadPanel 필드에 씬 오브젝트 자동 연결
            SerializedObject so = new SerializedObject(saveLoadPanel);
            so.FindProperty("_slotContainer").objectReferenceValue = contentRT;
            so.FindProperty("_titleText").objectReferenceValue = titleTMP;
            so.ApplyModifiedProperties();

            // Close 버튼 이벤트 연결
            closeBtn.onClick.AddListener(saveLoadPanel.Close);

            // 7. 슬롯 프리팹 (임시로 빈 게임오브젝트 생성 안내)
            Debug.Log("[SaveLoadUIBuilder] UI Hierarchy generated! Please create a Slot Prefab and assign it to the SaveLoadPanel in Inspector.");
            
            Selection.activeGameObject = panelGO;
        }

        [MenuItem("Tools/Save System/Generate Save Slot Prefab")]
        public static void GenerateSlotPrefab()
        {
            // 슬롯 UI 구조 생성
            GameObject slotGO = new GameObject("SaveSlot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(SaveSlotUI), typeof(HorizontalLayoutGroup));
            RectTransform slotRT = slotGO.GetComponent<RectTransform>();
            slotRT.sizeDelta = new Vector2(800, 100);
            
            Image slotImg = slotGO.GetComponent<Image>();
            slotImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            HorizontalLayoutGroup hlg = slotGO.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(10, 10, 10, 10);
            hlg.spacing = 20;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childForceExpandWidth = false;

            // Name
            GameObject nameGO = new GameObject("NameText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            nameGO.transform.SetParent(slotGO.transform, false);
            TextMeshProUGUI nameTMP = nameGO.GetComponent<TextMeshProUGUI>();
            nameTMP.text = "Save Name";
            nameTMP.fontSize = 24;

            // Time
            GameObject timeGO = new GameObject("TimeText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            timeGO.transform.SetParent(slotGO.transform, false);
            TextMeshProUGUI timeTMP = timeGO.GetComponent<TextMeshProUGUI>();
            timeTMP.text = "00:00:00";
            timeTMP.fontSize = 20;

            // Load Button
            GameObject loadBtnGO = new GameObject("LoadButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            loadBtnGO.transform.SetParent(slotGO.transform, false);
            loadBtnGO.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 50);
            loadBtnGO.GetComponent<Image>().color = Color.green;
            
            GameObject loadTextGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            loadTextGO.transform.SetParent(loadBtnGO.transform, false);
            TextMeshProUGUI loadTMP = loadTextGO.GetComponent<TextMeshProUGUI>();
            loadTMP.text = "Load";
            loadTMP.alignment = TextAlignmentOptions.Center;
            loadTMP.color = Color.black;
            ((RectTransform)loadTextGO.transform).sizeDelta = Vector2.zero;
            ((RectTransform)loadTextGO.transform).anchorMin = Vector2.zero;
            ((RectTransform)loadTextGO.transform).anchorMax = Vector2.one;

            // Delete Button
            GameObject delBtnGO = new GameObject("DeleteButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            delBtnGO.transform.SetParent(slotGO.transform, false);
            delBtnGO.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 50);
            delBtnGO.GetComponent<Image>().color = Color.red;

            GameObject delTextGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            delTextGO.transform.SetParent(delBtnGO.transform, false);
            TextMeshProUGUI delTMP = delTextGO.GetComponent<TextMeshProUGUI>();
            delTMP.text = "Del";
            delTMP.alignment = TextAlignmentOptions.Center;
            ((RectTransform)delTextGO.transform).sizeDelta = Vector2.zero;
            ((RectTransform)delTextGO.transform).anchorMin = Vector2.zero;
            ((RectTransform)delTextGO.transform).anchorMax = Vector2.one;

            // 스크립트 연결
            SaveSlotUI slotUI = slotGO.GetComponent<SaveSlotUI>();
            SerializedObject so = new SerializedObject(slotUI);
            so.FindProperty("_saveNameText").objectReferenceValue = nameTMP;
            so.FindProperty("_playTimeText").objectReferenceValue = timeTMP;
            so.FindProperty("_loadButton").objectReferenceValue = loadBtnGO.GetComponent<Button>();
            so.FindProperty("_deleteButton").objectReferenceValue = delBtnGO.GetComponent<Button>();
            so.ApplyModifiedProperties();

            Debug.Log("[SaveLoadUIBuilder] Save Slot UI created! Please make this a Prefab and assign it to the SaveLoadPanel.");
            Selection.activeGameObject = slotGO;
        }
    }
}
