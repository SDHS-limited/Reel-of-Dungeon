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

    public void Initialize(Canvas canvas, TMP_FontAsset textFont)
    {
        if (initialized) return;
        initialized = true;
        Stage = RoomSelectionState.SelectedRoom;
        stageLabel.text = "ROOM " + Stage;
        settings.SetActive(false);
        nextButton.gameObject.SetActive(false);
        settingsButton.onClick.AddListener(() => settings.SetActive(!settings.activeSelf));
        nextButton.onClick.AddListener(() => {
            NextStageRequested = true;
            nextButton.interactable = false;
            nextButton.gameObject.SetActive(false);
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
        RoomSelectionState.Unlock(Stage + 1);
        stageLabel.text = "ROOM " + Stage + " CLEAR";
        NextStageRequested = false;
        nextButton.gameObject.SetActive(true);
        nextButton.interactable = true;
    }
    public void ShowDefeat()
    {
        stageLabel.text = "DEFEAT";
        nextButton.interactable = false;
        nextButton.gameObject.SetActive(false);
    }
    public void AdvanceStage()
    {
        Stage++;
        RoomSelectionState.Select(Stage);
        NextStageRequested = false;
        stageLabel.text = "ROOM " + Stage;
        nextButton.interactable = false;
        nextButton.gameObject.SetActive(false);
    }
}
