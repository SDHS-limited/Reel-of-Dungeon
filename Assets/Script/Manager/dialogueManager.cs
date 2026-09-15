using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class RoomDialogue
{
    [Tooltip("이 대사를 사용할 방 번호")]
    public int roomNumber = 1;

    [TextArea(2, 6)]
    public string dialogue;
}

public class dialogueManager : MonoBehaviour
{
    // =========================================================
    // Dialogue
    // =========================================================

    [Header("Dialogue")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text dialogueText;

    [Header("Room Dialogues")]
    [SerializeField] private List<RoomDialogue> roomDialogues
        = new List<RoomDialogue>();

    [Tooltip("게임 시작 시 재생할 방 번호")]
    [SerializeField, Min(1)]
    private int startRoomNumber = 1;

    [Tooltip("해당 방의 대사를 찾지 못했을 때 사용할 기본 대사")]
    [SerializeField, TextArea(2, 6)]
    private string defaultDialogue = "전투가 시작됩니다.";

    [Tooltip("초당 표시할 글자 수입니다. 0이면 즉시 전체 표시합니다.")]
    [SerializeField, Min(0f)]
    private float charactersPerSecond = 30f;

    [Tooltip("모든 글자가 나타난 뒤 대사를 유지하는 시간입니다.")]
    [SerializeField, Min(0f)]
    private float displayDuration = 2f;


    // =========================================================
    // Battle
    // =========================================================

    [Header("Battle")]

    [Tooltip("대화와 전투 시작 효과가 끝난 뒤 실행할 전투 시작 함수를 연결하세요.")]
    [SerializeField]
    private UnityEvent onBattleStart = new UnityEvent();

    [SerializeField]
    private Animator animator;

    [SerializeField]
    private TMP_Text turn;

    [SerializeField, Min(0f)]
    private float battleStartDuration = 2.5f;


    // =========================================================
    // State
    // =========================================================

    public bool IsPlaying { get; private set; }

    public bool HasCompleted { get; private set; }

    public int CurrentRoomNumber { get; private set; } = 1;


    private Coroutine dialogueRoutine;

    private bool hasStarted;


    // =========================================================
    // Start
    // =========================================================

    private void Start()
    {
        // 선택 씬에서 고른 방의 대사를 재생한다. Start가 UnityEvent 등에
        // 실수로 연결되어 다시 호출되더라도 인트로를 반복하지 않는다.
        if (hasStarted)
            return;

        hasStarted = true;

        int selectedRoom = RoomSelectionState.SelectedRoom;
        PlayRoomDialogue(selectedRoom > 0 ? selectedRoom : startRoomNumber);
    }


    // =========================================================
    // 특정 방 대사 재생
    // =========================================================

    public void PlayRoomDialogue(int roomNumber)
    {
        if (!isActiveAndEnabled)
            return;

        // 이미 대사가 진행 중이라면 종료
        if (dialogueRoutine != null)
        {
            StopCoroutine(dialogueRoutine);

            dialogueRoutine = null;
        }

        CurrentRoomNumber = roomNumber;

        // 새로운 방이므로 상태 초기화
        IsPlaying = false;
        HasCompleted = false;

        BeginDialogue();
    }


    // =========================================================
    // 다음 방 대사
    // =========================================================

    public void PlayNextRoomDialogue()
    {
        PlayRoomDialogue(
            CurrentRoomNumber + 1
        );
    }


    // =========================================================
    // Dialogue 시작
    // =========================================================

    public void BeginDialogue()
    {
        if (IsPlaying)
            return;

        if (HasCompleted)
            return;

        if (!isActiveAndEnabled)
            return;


        if (
            dialoguePanel == null ||
            dialogueText == null
        )
        {
            Debug.LogError(
                "Dialogue Panel과 Dialogue Text를 연결해주세요.",
                this
            );

            return;
        }


        // 매니저 자체가 패널 안에 있으면
        // 패널을 껐을 때 매니저까지 비활성화되므로 방지
        if (transform.IsChildOf(dialoguePanel.transform))
        {
            Debug.LogError(
                "dialogueManager는 Dialogue Panel의 자식이 아닌 별도의 활성 오브젝트에 배치해주세요.",
                this
            );

            return;
        }


        // =====================================================
        // 현재 방에 맞는 대사 가져오기
        // =====================================================

        string selectedDialogue =
            GetDialogueForRoom(
                CurrentRoomNumber
            );


        IsPlaying = true;


        dialogueRoutine =
            StartCoroutine(
                ShowBeforeBattle(
                    selectedDialogue
                )
            );
    }


    // =========================================================
    // 방 번호에 맞는 Dialogue 찾기
    // =========================================================

    private string GetDialogueForRoom(int roomNumber)
    {
        foreach (RoomDialogue roomDialogue in roomDialogues)
        {
            if (roomDialogue == null)
                continue;


            if (roomDialogue.roomNumber == roomNumber)
            {
                // 대사가 비어있으면 기본 대사
                if (
                    string.IsNullOrWhiteSpace(
                        roomDialogue.dialogue
                    )
                )
                {
                    return defaultDialogue;
                }


                return roomDialogue.dialogue;
            }
        }


        // =====================================================
        // 해당 방 대사를 찾지 못함
        // =====================================================

        Debug.LogWarning(
            $"Room {roomNumber}의 대사가 없습니다. 기본 대사를 사용합니다.",
            this
        );


        return defaultDialogue;
    }


    // =========================================================
    // Dialogue Coroutine
    // =========================================================

    private IEnumerator ShowBeforeBattle(
        string selectedDialogue
    )
    {
        // =====================================================
        // 대사 입력
        // =====================================================

        dialogueText.text =
            selectedDialogue;


        // 전체 문장을 먼저 배치해서
        // 타이핑 도중 줄바꿈 위치가 움직이지 않게 함
        dialogueText.maxVisibleCharacters =
            0;


        dialoguePanel.SetActive(
            true
        );


        dialogueText.ForceMeshUpdate();


        int characterCount =
            dialogueText
                .textInfo
                .characterCount;


        float visibleCharacters =
            0f;


        // =====================================================
        // 타이핑 효과
        // =====================================================

        if (charactersPerSecond > 0f)
        {
            while (
                visibleCharacters <
                characterCount
            )
            {
                yield return null;


                visibleCharacters +=
                    charactersPerSecond *
                    Time.unscaledDeltaTime;


                dialogueText.maxVisibleCharacters =
                    Mathf.Min(
                        characterCount,
                        Mathf.FloorToInt(
                            visibleCharacters
                        )
                    );
            }
        }


        // 전체 글자 표시
        dialogueText.maxVisibleCharacters =
            int.MaxValue;


        // =====================================================
        // 대사 유지
        // =====================================================

        yield return new WaitForSecondsRealtime(
            Mathf.Max(
                0f,
                displayDuration
            )
        );


        // =====================================================
        // 대화창 종료
        // =====================================================

        dialoguePanel.SetActive(
            false
        );


        // =====================================================
        // BATTLE START 표시
        // =====================================================

        if (turn != null)
        {
            turn.text =
                "BATTLE START!";

            turn.color =
                Color.white;

            turn.gameObject.SetActive(
                true
            );
        }


        // =====================================================
        // BATTLE START 애니메이션
        // =====================================================

        if (animator != null)
        {
            animator.gameObject.SetActive(
                true
            );

            animator.updateMode =
                AnimatorUpdateMode.UnscaledTime;

            animator.SetTrigger(
                "start"
            );
        }


        yield return new WaitForSecondsRealtime(
            Mathf.Max(
                0f,
                battleStartDuration
            )
        );


        // =====================================================
        // BATTLE START 종료
        // =====================================================

        if (turn != null)
        {
            turn.gameObject.SetActive(
                false
            );
        }


        if (animator != null)
        {
            animator.gameObject.SetActive(
                false
            );
        }


        // =====================================================
        // 완료
        // =====================================================

        IsPlaying = false;

        HasCompleted = true;

        dialogueRoutine = null;


        Debug.Log(
            $"Room {CurrentRoomNumber} 대화 완료"
        );


        // 전투 시작
        onBattleStart.Invoke();
    }


    // =========================================================
    // 비활성화
    // =========================================================

    private void OnDisable()
    {
        if (dialogueRoutine != null)
        {
            StopCoroutine(
                dialogueRoutine
            );
        }


        dialogueRoutine = null;

        IsPlaying = false;


        if (
            dialoguePanel != null &&
            !transform.IsChildOf(
                dialoguePanel.transform
            )
        )
        {
            dialoguePanel.SetActive(
                false
            );
        }
    }


    // =========================================================
    // UI Order
    // =========================================================

    private void LateUpdate()
    {
        if (!IsPlaying)
            return;


        if (dialoguePanel != null)
        {
            UIOverlayOrder.BringToFront(
                dialoguePanel.transform
            );
        }


        if (animator != null)
        {
            UIOverlayOrder.BringToFront(
                animator.transform
            );
        }
    }
}
