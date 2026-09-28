using UnityEngine;
using UnityEngine.UI;

public class TitleManager : MonoBehaviour
{
    [Header("Scene Transition")]
    [SerializeField] private string nextSceneName = "InGame";
    [SerializeField, Min(0.01f)] private float closeDuration = 0.4f;
    [SerializeField, Min(0.01f)] private float revealDuration = 0.7f;
    private bool starting;
    private Button fullScreenTapButton;

    private void Awake()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
            return;

        GameObject tapArea = new GameObject(
            "Full Screen Start Tap",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button)
        );
        tapArea.layer = canvas.gameObject.layer;

        RectTransform rect = tapArea.GetComponent<RectTransform>();
        rect.SetParent(canvas.transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.SetAsLastSibling();

        Image image = tapArea.GetComponent<Image>();
        image.color = Color.clear;
        image.raycastTarget = true;

        fullScreenTapButton = tapArea.GetComponent<Button>();
        fullScreenTapButton.targetGraphic = image;
        fullScreenTapButton.transition = Selectable.Transition.None;
        fullScreenTapButton.onClick.AddListener(GameStart);
    }

    public void GameStart()
    {
        if (starting)
            return;

        starting = true;
        if (fullScreenTapButton != null)
            fullScreenTapButton.interactable = false;
        ScreenManager.RequestLandscape();
        CircularSceneTransition.Begin(nextSceneName, closeDuration, revealDuration);
    }

    public void Setting()
    {
    }

    public void Quit()
    {
        Application.Quit();
    }
}
