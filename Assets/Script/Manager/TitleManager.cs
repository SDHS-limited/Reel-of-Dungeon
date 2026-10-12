using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TitleManager : MonoBehaviour
{
    [Header("Scene Transition")]
    [SerializeField] private string nextSceneName = "InGame";
    [SerializeField, Min(0.01f)] private float closeDuration = 0.4f;
    [SerializeField, Min(0.01f)] private float revealDuration = 0.7f;

    [Header("Tap To Game Blink")]
    [SerializeField] private TMP_Text tapToGameText;
    [Tooltip("알파가 0에서 255까지 변하는 데 걸리는 시간입니다.")]
    [SerializeField, Min(0.1f)] private float blinkDuration = 1f;

    private bool starting;
    private Button fullScreenTapButton;
    private Color tapToGameBaseColor;
    private float blinkElapsed;

    private void Awake()
    {
        if (tapToGameText != null)
        {
            tapToGameBaseColor = tapToGameText.color;
            SetTapToGameAlpha(0f);
        }

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

    private void Update()
    {
        if (tapToGameText == null || starting)
            return;

        blinkElapsed += Time.unscaledDeltaTime;
        float alpha = Mathf.PingPong(blinkElapsed / Mathf.Max(0.1f, blinkDuration), 1f);
        SetTapToGameAlpha(alpha);
    }

    private void SetTapToGameAlpha(float alpha)
    {
        Color color = tapToGameBaseColor;
        color.a = Mathf.Clamp01(alpha);
        tapToGameText.color = color;
    }

    public void GameStart()
    {
        if (starting)
            return;

        starting = true;
        if (tapToGameText != null)
            SetTapToGameAlpha(1f);

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
