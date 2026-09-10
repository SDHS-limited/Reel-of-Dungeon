using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class dialogueManager : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField, TextArea(2, 6)] private string dialogue;
    [Tooltip("초당 표시할 글자 수입니다. 0이면 즉시 전체 표시합니다.")]
    [SerializeField, Min(0f)] private float charactersPerSecond = 30f;
    [Tooltip("모든 글자가 나타난 뒤 대사를 유지하는 시간입니다.")]
    [SerializeField, Min(0f)] private float displayDuration = 2f;

    [Header("Battle")]
    [Tooltip("대화 패널이 닫힌 뒤 실행할 전투 시작 함수를 연결하세요.")]
    [SerializeField] private UnityEvent onBattleStart = new UnityEvent();
    [SerializeField] Animator animator;
    [SerializeField] TMP_Text turn;
    public bool IsPlaying { get; private set; }
    public bool HasCompleted { get; private set; }

    private Coroutine dialogueRoutine;

    public void Start()
    {
        BeginDialogue();

    }

    public void BeginDialogue()
    {
        if (IsPlaying || HasCompleted || !isActiveAndEnabled) return;

        if (dialoguePanel == null || dialogueText == null ||
            transform.IsChildOf(dialoguePanel.transform))
        {
            Debug.LogError("패널과 Text를 연결하고, 매니저는 패널 밖의 활성 오브젝트에 배치하세요.", this);
            return;
        }

        IsPlaying = true;
        dialogueRoutine = StartCoroutine(ShowBeforeBattle());
    }

    private IEnumerator ShowBeforeBattle()
    {
        // 대사를 입력하지 않았다면 UI에 작성한 문구를 그대로 사용합니다.
        if (!string.IsNullOrWhiteSpace(dialogue)) dialogueText.text = dialogue;
        // 전체 문장을 먼저 배치해 타이핑 중 줄바꿈이 움직이지 않도록 합니다.
        dialogueText.maxVisibleCharacters = 0;
        dialoguePanel.SetActive(true);
        dialogueText.ForceMeshUpdate();
        int characterCount = dialogueText.textInfo.characterCount;
        float visibleCharacters = 0f;

        while (visibleCharacters < characterCount && charactersPerSecond > 0f)
        {
            yield return null;
            visibleCharacters += charactersPerSecond * Time.unscaledDeltaTime;
            dialogueText.maxVisibleCharacters = Mathf.Min(characterCount, Mathf.FloorToInt(visibleCharacters));
        }

        dialogueText.maxVisibleCharacters = int.MaxValue;
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, displayDuration));

        IsPlaying = false;
        HasCompleted = true;
        dialogueRoutine = null;
        dialoguePanel.SetActive(false);
        onBattleStart.Invoke();

        // animator.SetTrigger("start");
        // StartCoroutine(d());
        //플레이어 슬롯 돌리기
    }

    private void OnDisable()
    {
        if (dialogueRoutine != null) StopCoroutine(dialogueRoutine);
        dialogueRoutine = null;
        IsPlaying = false;
        if (dialoguePanel != null && !transform.IsChildOf(dialoguePanel.transform))
            dialoguePanel.SetActive(false);
    }

    // IEnumerator d()
    // {
    //     yield return new WaitForSecondsRealtime(0.3f);
    //     turn.gameObject.SetActive(true);
    //     yield return new WaitForSecondsRealtime(3.5f);
    //     turn.gameObject.SetActive(false);
    // }
}

