using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SeleteManager : MonoBehaviour
{
    [Serializable]
    public class RoomEntry
    {
        public Button button;
        public int roomNumber = 1;
        public string title = "ROOM 1";
        [TextArea(2, 5)] public string description = "Choose your spells and defeat the enemy.";
        public string sceneName = "Ingame";
        public bool unlockedByDefault;
    }

    [SerializeField] private RectTransform roomContainer;
    [Tooltip("방 사이 빨간 줄을 묶은 load 오브젝트입니다. 비어 있으면 Canvas/load를 찾습니다.")]
    [SerializeField] private Transform loadLineRoot;
    [SerializeField] private Button startButton, leftArrow, rightArrow;
    [SerializeField] private TMP_Text catalog;
    [SerializeField] private RoomEntry[] rooms;
    [SerializeField, Min(0.01f)] private float moveDuration = 0.35f;
    [SerializeField] private Color unlockedColor = new Color(0.15f, 1f, 0.15f);
    [SerializeField] private Color lockedColor = new Color(1f, 0.12f, 0.12f);
    private int selected;
    private bool moving, starting;
    private Coroutine movement;
    private Canvas canvas;
    private readonly Dictionary<Button, Vector3> originalScales = new Dictionary<Button, Vector3>();

    private void Start()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null) { Debug.LogError("SeleteManager는 Canvas 아래에 배치하세요.", this); return; }
        if (roomContainer == null) roomContainer = canvas.transform.Find("Room") as RectTransform;
        if (startButton == null) startButton = canvas.transform.Find("Button (Legacy)")?.GetComponent<Button>();
        if (startButton != null)
        {
            if (leftArrow == null) leftArrow = startButton.transform.Find("Image")?.GetComponent<Button>();
            if (rightArrow == null) rightArrow = startButton.transform.Find("Image (1)")?.GetComponent<Button>();
            if (catalog == null) catalog = startButton.transform.Find("Catelog")?.GetComponent<TMP_Text>();
        }
        if (roomContainer == null || startButton == null || catalog == null)
        {
            Debug.LogError("Room, Start, Catelog UI 참조를 확인하세요.", this);
            return;
        }
        if (loadLineRoot == null) loadLineRoot = canvas.transform.Find("load");
        if (loadLineRoot != null && loadLineRoot != roomContainer && !roomContainer.IsChildOf(loadLineRoot))
        {
            // 화면상 위치를 유지한 채 방 이동 그룹에 넣어 같은 애니메이션을 적용한다.
            loadLineRoot.SetParent(roomContainer, true);
            loadLineRoot.SetAsFirstSibling();
            foreach (Graphic line in loadLineRoot.GetComponentsInChildren<Graphic>(true))
                line.raycastTarget = false;
        }
        if (rooms == null || rooms.Length == 0)
        {
            var entries = new List<RoomEntry>();
            foreach (Transform child in roomContainer)
            {
                Button button = child.GetComponent<Button>();
                if (button == null || !int.TryParse(child.name, out int number)) continue;
                entries.Add(new RoomEntry
                {
                    button = button, roomNumber = number, title = "ROOM " + number,
                    description = number == 1 ? "Learn to combine spells and defeat your first enemy."
                        : number == 2 ? "A tougher enemy awaits. Use shields and healing wisely."
                        : number == 3 ? "A powerful enemy guards this room. Build strong spell combos."
                        : number == 4 ? "A mysterious room lies ahead. Prepare for the unknown."
                        : "Unknown room.",
                    unlockedByDefault = number == 1
                });
            }
            entries.Sort((a, b) => a.roomNumber.CompareTo(b.roomNumber));
            rooms = entries.ToArray();
        }
        if (rooms.Length == 0) { startButton.interactable = false; return; }
        for (int i = 0; i < rooms.Length; i++)
        {
            int index = i;
            if (rooms[i].button == null) { Debug.LogError("방 버튼 참조가 비어 있습니다.", this); startButton.interactable = false; return; }
            originalScales[rooms[i].button] = rooms[i].button.transform.localScale;
            rooms[i].button.onClick.AddListener(() => SelectRoom(index));
        }
        startButton.onClick.AddListener(GameStart);
        if (leftArrow != null) leftArrow.onClick.AddListener(PreviousRoom);
        if (rightArrow != null) rightArrow.onClick.AddListener(NextRoom);
        catalog.rectTransform.anchoredPosition = new Vector2(-70f, -110f);
        catalog.rectTransform.sizeDelta = new Vector2(1000f, 160f);
        catalog.fontSize = 50;
        catalog.enableAutoSizing = false;
        catalog.alignment = TextAlignmentOptions.Center;
        catalog.raycastTarget = false;
        selected = Mathf.Max(0, Array.FindIndex(rooms, room => room.roomNumber == RoomSelectionState.SelectedRoom));
        CenterImmediately();
        Refresh();
    }

    private bool Unlocked(RoomEntry room) => room.unlockedByDefault || RoomSelectionState.IsUnlocked(room.roomNumber);

    public void SelectRoom(int index)
    {
        if (starting || rooms == null || index < 0 || index >= rooms.Length) return;
        selected = index;
        if (movement != null) StopCoroutine(movement);
        movement = StartCoroutine(CenterSelection());
    }

    public void PreviousRoom() => SelectRoom(selected - 1);
    public void NextRoom() => SelectRoom(selected + 1);

    [ContextMenu("Reset Room Progress")]
    public void ResetRoomProgress()
    {
        if (starting) return;
        RoomSelectionState.ResetProgress();
        if (Application.isPlaying && canvas != null && rooms != null && rooms.Length > 0)
        {
            int firstRoom = Array.FindIndex(rooms, room => room.roomNumber == 1);
            SelectRoom(Mathf.Max(0, firstRoom));
        }
        Debug.Log("방 진행 초기화 완료: ROOM 1부터 시작합니다.", this);
    }

    private Vector2 CenterPosition()
    {
        // 선택한 버튼의 중심을 Canvas 중앙으로 옮기고 Y 위치는 유지한다.
        RectTransform parent = roomContainer.parent as RectTransform;
        Vector3 buttonCenter = ((RectTransform)rooms[selected].button.transform).TransformPoint(((RectTransform)rooms[selected].button.transform).rect.center);
        Vector3 canvasCenter = ((RectTransform)canvas.transform).TransformPoint(((RectTransform)canvas.transform).rect.center);
        float delta = parent.InverseTransformPoint(canvasCenter).x - parent.InverseTransformPoint(buttonCenter).x;
        return roomContainer.anchoredPosition + new Vector2(delta, 0f);
    }

    private void CenterImmediately() => roomContainer.anchoredPosition = CenterPosition();

    private IEnumerator CenterSelection()
    {
        moving = true;
        Refresh();
        Vector2 from = roomContainer.anchoredPosition, to = CenterPosition();
        float duration = Mathf.Max(0.01f, moveDuration), elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            roomContainer.anchoredPosition = Vector2.Lerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / duration));
            yield return null;
        }
        roomContainer.anchoredPosition = to;
        moving = false;
        movement = null;
        Refresh();
    }

    private void Refresh()
    {
        for (int i = 0; i < rooms.Length; i++)
        {
            Button button = rooms[i].button;
            Color tint = Unlocked(rooms[i]) ? unlockedColor : lockedColor;
            button.targetGraphic.color = tint;
            button.transition = Selectable.Transition.None;
            button.transform.localScale = originalScales[button] * (i == selected ? 1.12f : 1f);
        }
        RoomEntry room = rooms[selected];
        catalog.text = room.title + (Unlocked(room) ? "\n" : "  [LOCKED]\n") + room.description;
        catalog.color = Unlocked(room) ? Color.white : new Color(1f, 0.65f, 0.65f);
        startButton.interactable = !moving && !starting && Unlocked(room);
        if (leftArrow != null) leftArrow.interactable = !starting && selected > 0;
        if (rightArrow != null) rightArrow.interactable = !starting && selected < rooms.Length - 1;
    }

    public void GameStart()
    {
        if (starting || moving || rooms == null || rooms.Length == 0 || !Unlocked(rooms[selected])) return;
        RoomEntry room = rooms[selected];
        if (!Application.CanStreamedLevelBeLoaded(room.sceneName))
        {
            catalog.text = "Scene unavailable: " + room.sceneName;
            return;
        }
        RoomSelectionState.Select(room.roomNumber);
        starting = true;
        Refresh();
        CircularSceneTransition.Begin(room.sceneName, 0.4f, 0.7f);
    }
}
