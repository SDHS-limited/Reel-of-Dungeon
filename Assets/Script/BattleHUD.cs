using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Canvas의 Battle HUD 오브젝트에 연결된 UI 동작만 관리합니다.
public class BattleHUD : MonoBehaviour
{
    public int Stage { get; private set; } = 1;
    public bool NextStageRequested { get; private set; }
    [SerializeField] private GameObject settings;
    [SerializeField] private TMP_Text stageLabel;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button audioButton;
    [SerializeField] private Button quitButton;
    private float savedVolume = 1f;
    private bool initialized;
    private bool defeatMode;
    private TMP_Text nextButtonLabel;
    private string nextButtonDefaultText = "NEXT ROOM";

    public void Initialize(Canvas canvas, TMP_FontAsset textFont)
    {
        if (initialized) return;
        initialized = true;
        Stage = RoomSelectionState.SelectedRoom;
        stageLabel.text = "ROOM " + Stage;
        settings.SetActive(false);
        nextButton.gameObject.SetActive(false);
        nextButtonLabel = nextButton.GetComponentInChildren<TMP_Text>(true);
        if (nextButtonLabel != null && !string.IsNullOrWhiteSpace(nextButtonLabel.text))
            nextButtonDefaultText = nextButtonLabel.text;

        settingsButton.onClick.AddListener(() => settings.SetActive(!settings.activeSelf));
        nextButton.onClick.AddListener(() => {
            nextButton.interactable = false;
            nextButton.gameObject.SetActive(false);

            if (defeatMode)
            {
                CircularSceneTransition.Begin("Title", 0.4f, 0.7f);
                return;
            }

            NextStageRequested = true;
        });
        audioButton.onClick.AddListener(() => {
            if (AudioListener.volume > 0f) { savedVolume = AudioListener.volume; AudioListener.volume = 0f; }
            else AudioListener.volume = savedVolume;
            RefreshSound();
        });
        RefreshSound();
        quitButton.onClick.AddListener(() => {
            quitButton.interactable = false;
            CircularSceneTransition.Begin("Selete", 0.4f, 0.7f);
        });
    }

    private void RefreshSound() => audioButton.GetComponentInChildren<TMP_Text>(true).text = AudioListener.volume > 0f ? "SOUND ON" : "SOUND OFF";

    public void ShowVictory()
    {
        defeatMode = false;
        RoomSelectionState.Unlock(Stage + 1);
        stageLabel.text = "ROOM " + Stage + " CLEAR";
        NextStageRequested = false;
        SetNextButtonText(nextButtonDefaultText);
        nextButton.gameObject.SetActive(true);
        nextButton.interactable = true;
    }

    public void ShowDefeat()
    {
        if (defeatMode) return;
        defeatMode = true;
        stageLabel.text = "DEFEAT";
        NextStageRequested = false;
        settings.SetActive(false);
        SetNextButtonText("MAIN TITLE");
        transform.SetAsLastSibling();
        nextButton.transform.SetAsLastSibling();
        nextButton.gameObject.SetActive(true);
        nextButton.interactable = true;
    }

    public void AdvanceStage()
    {
        defeatMode = false;
        Stage++;
        RoomSelectionState.Select(Stage);
        NextStageRequested = false;
        stageLabel.text = "ROOM " + Stage;
        SetNextButtonText(nextButtonDefaultText);
        nextButton.interactable = false;
        nextButton.gameObject.SetActive(false);
    }

    private void SetNextButtonText(string text)
    {
        if (nextButtonLabel == null)
            nextButtonLabel = nextButton.GetComponentInChildren<TMP_Text>(true);
        if (nextButtonLabel != null)
            nextButtonLabel.text = text;
    }
}
