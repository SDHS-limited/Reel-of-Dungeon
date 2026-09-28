using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


// ============================================================
// 시너지 등급
// ============================================================

public enum SynergyTier
{
    None,       // 1~2개
    Combo,      // 3개
    BigCombo,   // 4개
    Mega        // 5개
}


// ============================================================
// 슬롯 결과 데이터
// ============================================================

[System.Serializable]
public class SymbolResolution
{
    public ReelSymbolData symbol;

    // 같은 심볼이 몇 개 나왔는지
    public int matchCount;

    // 배율 적용 전 값
    public int baseTotal;

    // 시너지로 증가한 값
    public int synergyBonus;

    // 최종 적용 값
    public int totalValue;

    // 시너지 배율
    public float multiplier;

    // 시너지 등급
    public SynergyTier synergyTier;


    // ========================================================
    // 3개 이상 매치 전용 추가 효과
    // ========================================================

    // 방어 심볼 3개 이상
    // 획득 방어막이 다음 턴까지 유지
    public bool shieldPersists;

    // 회복 심볼 3개 이상
    // 초과 회복량을 방어막으로 전환
    public bool overhealToShield;
}


// ============================================================
// 방별 몬스터 설정
// ============================================================

[System.Serializable]
public class RoomEnemySettings
{
    [Tooltip("이 방에서 해당 몬스터를 배치합니다.")]
    public bool enabled = true;

    [Tooltip("비어 있으면 씬 오브젝트의 기존 스프라이트를 사용합니다.")]
    public Sprite sprite;

    [Tooltip("체크하면 아래 위치를 월드 좌표로 적용합니다.")]
    public bool useCustomPosition;

    public Vector3 position;

    [Tooltip("체크하면 아래 크기를 적용합니다.")]
    public bool useCustomScale;

    public Vector3 scale = Vector3.one;

    [Min(1)] public int maxHealth = 100;
    [Min(0)] public int attackDamage = 10;
}

[System.Serializable]
public class RoomBattleSettings
{
    [Min(1)] public int roomNumber = 1;

    [Range(1, 3)]
    [Tooltip("이 방에 배치할 적의 수입니다. 1~3명을 지원합니다.")]
    public int enemyCount = 2;

    public RoomEnemySettings enemy = new RoomEnemySettings();
    public RoomEnemySettings enemy2 = new RoomEnemySettings();
    public RoomEnemySettings enemy3 = new RoomEnemySettings();
}


// ============================================================
// Slot Machine Controller
// ============================================================

public class SlotMachineController : MonoBehaviour
{
    public event System.Action<IReadOnlyList<SymbolResolution>> SpinResolved;

    public event System.Action<ReelSymbolData> JackpotAnticipationStarted;


    // ========================================================
    // Reels
    // ========================================================

    [Header("Reels")]
    [SerializeField]
    private SlotReel[] reels;


    // ========================================================
    // Symbols
    // ========================================================

    [Header("Symbols")]
    [SerializeField]
    private List<ReelSymbolData> symbolList;


    // ========================================================
    // Room Battle Settings
    // ========================================================

    [Header("Room Battle Settings")]
    [Tooltip("방 번호마다 적의 수, 배치, 체력, 공격력을 설정합니다.")]
    [SerializeField]
    private List<RoomBattleSettings> roomBattleSettings =
        new List<RoomBattleSettings>
        {
            new RoomBattleSettings { roomNumber = 1 },
            new RoomBattleSettings { roomNumber = 2 },
            new RoomBattleSettings { roomNumber = 3 }
        };


    // ========================================================
    // Spin Settings
    // ========================================================

    [Header("Spin Settings")]

    [Tooltip("SPIN 이후 STOP을 누를 수 있기까지의 최소 시간")]
    [SerializeField]
    private float baseSpinDuration = 1f;

    [Tooltip("마지막 릴 잭팟 기대 연출 시간")]
    [SerializeField]
    private float jackpotAnticipationDelay = 1.2f;

    [Tooltip("STOP 입력 후 다음 릴을 정지하기까지의 간격")]
    [SerializeField, Min(0f)]
    private float automaticStopInterval = 0.12f;

    [Header("Reel Stop Camera Shake")]
    [SerializeField, Min(0.01f)]
    private float reelStopShakeDuration = 0.08f;

    [SerializeField, Min(0f)]
    private float reelStopShakeMagnitude = 0.055f;


    // ========================================================
    // UI
    // ========================================================

    [Header("UI")]

    [SerializeField]
    private Button spinButton;

    [SerializeField, Min(1f)]
    private float spinButtonBorderWidth = 6f;

    [SerializeField]
    private Color spinButtonBorderColor = new Color(1f, 0.82f, 0.12f, 1f);

    [Header("Spin Button Idle Animation")]
    [SerializeField, Min(0f)]
    private float spinButtonIdleDistance = 10f;

    [SerializeField, Min(0.1f)]
    private float spinButtonIdleCyclesPerSecond = 1.4f;

    private RectTransform spinButtonRect;
    private Vector2 spinButtonBasePosition;
    private Animator spinButtonAnimator;
    private Coroutine spinButtonIdleRoutine;

    [Tooltip("릴과 SPIN 버튼을 포함하는 Slot UI")]
    [SerializeField]
    private RectTransform slotUIRoot;

    private bool slotUIVisible;


    // ========================================================
    // Turn UI
    // ========================================================

    [Header("Turn UI")]

    [SerializeField]
    private TMP_Text currentTurnText;

    [SerializeField, HideInInspector]
    private TMP_Text playerTurnCountText;

    [SerializeField, HideInInspector]
    private TMP_Text enemyTurnCountText;

    [SerializeField, Min(0f)]
    private float enemyTurnDisplayDuration = 1f;


    // ========================================================
    // Turn State
    // ========================================================

    public int PlayerTurnCount { get; private set; }

    public int EnemyTurnCount { get; private set; }

    public bool IsPlayerTurn { get; private set; }


    // ========================================================
    // Slot State
    // ========================================================

    private bool isBusy = false;

    // 현재 슬롯 전체가 회전 중
    private bool isSpinning = false;

    // STOP 입력 가능 여부
    private bool canStopReels = false;

    // 릴 하나가 현재 정지 애니메이션 중
    private bool isStoppingReel = false;

    // STOP을 한 번 누른 후 남은 릴을 순차적으로 정지하는 중
    private bool isAutoStoppingReels = false;

    // 현재까지 정지한 릴 개수
    private int stoppedReelCount = 0;

    // 다음에 정지할 릴
    private int nextReelToStop = 0;

    // 이번 SPIN에서 결정된 결과
    private List<ReelSymbolData> currentSpinResults;


    // ========================================================
    // Battle UI
    // ========================================================

    private TMP_Text turnBannerText;

    private SpellResultHand spellHand;


    // ========================================================
    // Awake
    // ========================================================

    private void Awake()
    {
        if (slotUIRoot == null && spinButton != null)
        {
            slotUIRoot =
                spinButton.transform.parent as RectTransform;
        }

        EnsureSpinButtonBorder();

        if (spinButton != null)
        {
            spinButtonRect = spinButton.transform as RectTransform;
            spinButtonAnimator = spinButton.GetComponent<Animator>();
            if (spinButtonRect != null)
                spinButtonBasePosition = spinButtonRect.anchoredPosition;
        }

        if (slotUIRoot != null)
        {
            SetSlotUIVisible(false);
        }
    }


    // ========================================================
    // Start
    // ========================================================

    private IEnumerator Start()
    {
        if (spinButton != null)
        {
            spinButton.interactable = false;
        }


        if (!ValidateSetup())
        {
            yield break;
        }


        // ====================================================
        // Canvas 설정
        // ====================================================

        Canvas uiCanvas =
            spinButton != null
                ? spinButton.GetComponentInParent<Canvas>(true)
                : null;


        CanvasScaler scaler =
            uiCanvas != null
                ? uiCanvas.GetComponent<CanvasScaler>()
                : null;


        if (scaler != null)
        {
            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;

            scaler.referenceResolution =
                new Vector2(1920f, 1080f);

            scaler.screenMatchMode =
                CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

            scaler.matchWidthOrHeight = 0.5f;
        }


        // ====================================================
        // Turn UI
        // ====================================================

        EnsureTurnUI();


        Canvas handCanvas =
            currentTurnText != null
                ? currentTurnText.GetComponentInParent<Canvas>()
                : null;


        if (handCanvas != null)
        {
            spellHand =
                handCanvas.GetComponentInChildren<SpellResultHand>(true);

            if (spellHand == null)
            {
                Debug.LogError("Canvas에 SpellResultHand UI를 연결해주세요.", this);
                yield break;
            }
            spellHand.Initialize(
                handCanvas,
                currentTurnText.font,
                GetRoomBattleSettings(RoomSelectionState.SelectedRoom)
            );
        }


        if (currentTurnText != null)
        {
            currentTurnText.text = string.Empty;
        }


        // ====================================================
        // Reel 초기화
        // ====================================================

        foreach (SlotReel reel in reels)
        {
            reel.Init(symbolList);
        }


        // ====================================================
        // SPIN / STOP 버튼
        // ====================================================

        if (spinButton != null)
        {
            spinButton.onClick.AddListener(
                OnSpinButtonClicked
            );
        }


        SetSpinButtonText("SPIN");


        // ====================================================
        // 인트로 대화 대기
        // ====================================================

        dialogueManager intro =
            FindFirstObjectByType<dialogueManager>();


        if (intro != null && intro.isActiveAndEnabled)
        {
            yield return new WaitUntil(
                () => intro == null || intro.HasCompleted
            );
        }


        // ====================================================
        // 첫 플레이어 턴
        // ====================================================

        BeginPlayerTurn();

        yield return ShowTurnBanner();

        SetSlotUIVisible(true);

        SetSpinButtonText("SPIN");
    }


    private RoomBattleSettings GetRoomBattleSettings(int roomNumber)
    {
        if (roomBattleSettings == null)
            return null;

        return roomBattleSettings.Find(
            settings => settings != null && settings.roomNumber == roomNumber
        );
    }


    // ========================================================
    // SPIN / STOP 버튼 클릭
    // ========================================================

    public void OnSpinButtonClicked()
    {
        if (!slotUIVisible)
            return;

        if (!IsPlayerTurn)
            return;

        if (!isActiveAndEnabled)
            return;

        // ====================================================
        // 현재 슬롯이 회전 중이면 STOP 역할
        // ====================================================

        if (isSpinning)
        {
            if (!canStopReels)
                return;

            if (isStoppingReel || isAutoStoppingReels)
                return;

            if (nextReelToStop >= reels.Length)
                return;

            // STOP 입력이 승인되는 순간 버튼 흔들림을 멈춘다.
            SetSpinButtonIdleAnimation(false);

            StartCoroutine(
                StopAllReelsSequentially()
            );

            return;
        }


        // ====================================================
        // 현재 슬롯이 정지 상태면 SPIN 역할
        // ====================================================

        if (isBusy)
            return;


        if (!ValidateSetup())
            return;


        StartCoroutine(
            ExecuteSlotRoutine()
        );
    }


    // ========================================================
    // 메인 슬롯 루틴
    // ========================================================

    private IEnumerator ExecuteSlotRoutine()
    {
        // ====================================================
        // SPIN 시작
        // ====================================================

        isBusy = true;

        isSpinning = true;

        canStopReels = false;

        isStoppingReel = false;

        isAutoStoppingReels = false;

        stoppedReelCount = 0;

        nextReelToStop = 0;

        // SPIN을 누른 뒤 STOP을 누를 때까지 버튼 애니메이션을 유지한다.
        SetSpinButtonIdleAnimation(true);


        // 버튼을 STOP으로 변경
        SetSpinButtonText("STOP");


        // SPIN 직후 바로 정지하는 것 방지
        if (spinButton != null)
        {
            spinButton.interactable = false;
        }


        // ====================================================
        // 이번 턴 결과 미리 결정
        // ====================================================

        currentSpinResults =
            new List<ReelSymbolData>(
                reels.Length
            );


        for (int i = 0; i < reels.Length; i++)
        {
            ReelSymbolData result =
                PickSymbolByWeight();

            currentSpinResults.Add(result);
        }


        // ====================================================
        // 모든 릴 회전 시작
        // ====================================================

        for (int i = 0; i < reels.Length; i++)
        {
            if (
                reels[i] != null &&
                reels[i].isActiveAndEnabled
            )
            {
                reels[i].StartSpin();
            }
        }


        // ====================================================
        // 최소 회전시간
        // ====================================================

        yield return new WaitForSeconds(
            baseSpinDuration
        );


        // ====================================================
        // STOP 가능
        // ====================================================

        canStopReels = true;


        if (spinButton != null)
        {
            spinButton.interactable = true;
        }


        // ====================================================
        // 플레이어가 전부 멈출 때까지 대기
        // ====================================================

        yield return new WaitUntil(
            () => stoppedReelCount >= reels.Length
        );


        // ====================================================
        // 전부 정지 완료
        // ====================================================

        isSpinning = false;

        canStopReels = false;

        isStoppingReel = false;


        SetSpinButtonText("SPIN");


        if (spinButton != null)
        {
            spinButton.interactable = false;
        }


        // ====================================================
        // 결과 계산
        // ====================================================

        List<SymbolResolution> prepared =
            EvaluateTurnResults(
                currentSpinResults
            );


        // ====================================================
        // SpellResultHand 확인
        // ====================================================

        if (spellHand == null)
        {
            Debug.LogError(
                "마법 결과 UI를 표시할 Canvas가 없습니다.",
                this
            );

            ResetSlotState();

            SetSlotUIVisible(true);

            yield break;
        }


        // ====================================================
        // 슬롯 UI 숨기기
        // ====================================================

        SetSlotUIVisible(false);


        // ====================================================
        // 카드 표시
        // ====================================================

        spellHand.Present(
            prepared,
            ApplyAction
        );


        yield return new WaitUntil(
            () => spellHand.Finished
        );


        // ====================================================
        // 플레이어 패배
        // ====================================================

        if (spellHand.PlayerDefeated)
        {
            IsPlayerTurn = false;

            spellHand.Hide();

            spellHand.HUD.ShowDefeat();


            if (currentTurnText != null)
            {
                currentTurnText.text =
                    "DEFEAT";
            }


            isBusy = false;

            yield break;
        }


        // ====================================================
        // 적 처치
        // ====================================================

        if (spellHand.EnemyDefeated)
        {
            IsPlayerTurn = false;


            if (currentTurnText != null)
            {
                currentTurnText.text =
                    "VICTORY!";
            }


            spellHand.Hide();

            spellHand.HUD.ShowVictory();


            yield return new WaitUntil(
                () =>
                    spellHand.HUD.NextStageRequested
            );

            int nextRoom = spellHand.HUD.Stage + 1;
            RoomBattleSettings nextRoomSettings =
                GetRoomBattleSettings(nextRoom);

            if (nextRoomSettings != null)
            {
                RoomSelectionState.Unlock(nextRoom);
                RoomSelectionState.Select(nextRoom);
                CircularSceneTransition.Begin(
                    gameObject.scene.name,
                    0.4f,
                    0.7f
                );
            }
            else
            {
                CircularSceneTransition.Begin(
                    "Selete",
                    0.4f,
                    0.7f
                );
            }

            yield break;
        }


        // ====================================================
        // 카드 UI 숨기기
        // ====================================================

        spellHand.Hide();


        // ====================================================
        // 적 턴
        // ====================================================

        IsPlayerTurn = false;

        EnemyTurnCount++;


        RefreshTurnUI();


        yield return ShowTurnBanner();


        // ====================================================
        // 적 반격
        // ====================================================

        yield return spellHand.Counterattack();


        yield return new WaitForSeconds(
            Mathf.Max(
                0f,
                enemyTurnDisplayDuration
            )
        );


        // ====================================================
        // 적 공격 후 플레이어 사망
        // ====================================================

        if (spellHand.PlayerDefeated)
        {
            spellHand.HUD.ShowDefeat();


            if (currentTurnText != null)
            {
                currentTurnText.text =
                    "DEFEAT";
            }


            isBusy = false;

            yield break;
        }


        // ====================================================
        // 다시 플레이어 턴
        // ====================================================

        BeginPlayerTurn();

        yield return ShowTurnBanner();


        // ====================================================
        // 다음 SPIN 준비
        // ====================================================

        ResetSlotState();

        SetSlotUIVisible(true);
    }


    // ========================================================
    // STOP 클릭 → 남은 릴 순차 정지
    // ========================================================

    private IEnumerator StopAllReelsSequentially()
    {
        if (isAutoStoppingReels)
            yield break;

        isAutoStoppingReels = true;

        if (spinButton != null)
            spinButton.interactable = false;

        while (
            isSpinning &&
            nextReelToStop < reels.Length)
        {
            canStopReels = true;
            yield return StopNextReel();

            if (
                nextReelToStop < reels.Length &&
                automaticStopInterval > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    automaticStopInterval
                );
            }
        }

        isAutoStoppingReels = false;
    }


    // ========================================================
    // 다음 릴 하나 정지
    // ========================================================

    private IEnumerator StopNextReel()
    {
        if (!isSpinning)
            yield break;

        if (!canStopReels)
            yield break;

        if (isStoppingReel)
            yield break;

        if (nextReelToStop >= reels.Length)
            yield break;


        int index =
            nextReelToStop;


        isStoppingReel = true;

        canStopReels = false;


        // 연타 방지
        if (spinButton != null)
        {
            spinButton.interactable = false;
        }


        // ====================================================
        // 마지막 릴 잭팟 기대 연출
        // ====================================================

        if (index == reels.Length - 1)
        {
            if (
                TryGetJackpotCandidate(
                    currentSpinResults,
                    index,
                    out ReelSymbolData candidate
                )
            )
            {
                Debug.Log(
                    $"🎰 {candidate.DisplayName} " +
                    $"메가 잭팟까지 하나 남았습니다!"
                );


                JackpotAnticipationStarted?.Invoke(
                    candidate
                );


                yield return new WaitForSeconds(
                    jackpotAnticipationDelay
                );
            }
        }


        // ====================================================
        // 현재 릴 정지
        // ====================================================

        yield return reels[index].StopSpin(
            currentSpinResults[index]
        );

        PlayReelStopCameraShake();


        stoppedReelCount++;

        nextReelToStop++;

        isStoppingReel = false;


        // ====================================================
        // 아직 릴이 남아있음
        // ====================================================

        if (stoppedReelCount < reels.Length)
        {
            canStopReels = true;


            if (spinButton != null && !isAutoStoppingReels)
            {
                spinButton.interactable = true;
            }
        }

        // ====================================================
        // 전부 정지
        // ====================================================

        else
        {
            canStopReels = false;


            if (spinButton != null)
            {
                spinButton.interactable = false;
            }
        }
    }


    // ========================================================
    // 슬롯 상태 초기화
    // ========================================================

    private void ResetSlotState()
    {
        isBusy = false;

        isSpinning = false;

        canStopReels = false;

        isStoppingReel = false;

        isAutoStoppingReels = false;

        stoppedReelCount = 0;

        nextReelToStop = 0;


        SetSpinButtonText(
            "SPIN"
        );

        RefreshSpinButtonIdleAnimation();
    }


    // ========================================================
    // 버튼 글자 변경
    // ========================================================

    private void SetSpinButtonText(string text)
    {
        if (spinButton == null)
            return;


        // TextMeshPro
        TMP_Text tmp =
            spinButton.GetComponentInChildren<TMP_Text>(
                true
            );


        if (tmp != null)
        {
            tmp.text = text;

            return;
        }


        // Legacy Text
        Text legacy =
            spinButton.GetComponentInChildren<Text>(
                true
            );


        if (legacy != null)
        {
            legacy.text = text;
        }
    }


    private void EnsureSpinButtonBorder()
    {
        if (spinButton == null)
            return;

        RectTransform buttonRect = spinButton.transform as RectTransform;
        if (buttonRect == null || buttonRect.Find("Yellow Border Top") != null)
            return;

        CreateSpinButtonBorderEdge(
            buttonRect,
            "Yellow Border Top",
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, -spinButtonBorderWidth * 0.5f),
            new Vector2(0f, spinButtonBorderWidth)
        );
        CreateSpinButtonBorderEdge(
            buttonRect,
            "Yellow Border Bottom",
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, spinButtonBorderWidth * 0.5f),
            new Vector2(0f, spinButtonBorderWidth)
        );
        CreateSpinButtonBorderEdge(
            buttonRect,
            "Yellow Border Left",
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(spinButtonBorderWidth * 0.5f, 0f),
            new Vector2(spinButtonBorderWidth, 0f)
        );
        CreateSpinButtonBorderEdge(
            buttonRect,
            "Yellow Border Right",
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(-spinButtonBorderWidth * 0.5f, 0f),
            new Vector2(spinButtonBorderWidth, 0f)
        );
    }


    private void CreateSpinButtonBorderEdge(
        RectTransform parent,
        string edgeName,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        GameObject edgeObject = new GameObject(
            edgeName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        edgeObject.layer = parent.gameObject.layer;

        RectTransform edge = edgeObject.GetComponent<RectTransform>();
        edge.SetParent(parent, false);
        edge.anchorMin = anchorMin;
        edge.anchorMax = anchorMax;
        edge.anchoredPosition = anchoredPosition;
        edge.sizeDelta = sizeDelta;
        edge.SetAsLastSibling();

        Image image = edgeObject.GetComponent<Image>();
        image.color = spinButtonBorderColor;
        image.raycastTarget = false;
    }


    private void RefreshSpinButtonIdleAnimation()
    {
        bool shouldPlay =
            spinButton != null &&
            spinButton.gameObject.activeInHierarchy &&
            spinButton.interactable &&
            slotUIVisible &&
            IsPlayerTurn &&
            !isBusy &&
            !isSpinning;

        SetSpinButtonIdleAnimation(shouldPlay);
    }


    private void SetSpinButtonIdleAnimation(bool play)
    {
        if (spinButtonRect == null)
            return;

        if (
            spinButtonAnimator != null &&
            spinButtonAnimator.runtimeAnimatorController != null)
        {
            if (spinButtonAnimator.enabled == play)
                return;

            spinButtonAnimator.enabled = play;
            if (play)
            {
                spinButtonAnimator.Rebind();
                spinButtonAnimator.Update(0f);
            }
            else
            {
                spinButtonRect.anchoredPosition = spinButtonBasePosition;
            }
            return;
        }

        if (play)
        {
            if (spinButtonIdleRoutine == null)
                spinButtonIdleRoutine = StartCoroutine(AnimateSpinButtonIdle());
            return;
        }

        if (spinButtonIdleRoutine != null)
        {
            StopCoroutine(spinButtonIdleRoutine);
            spinButtonIdleRoutine = null;
        }
        spinButtonRect.anchoredPosition = spinButtonBasePosition;
    }


    private IEnumerator AnimateSpinButtonIdle()
    {
        float elapsed = 0f;
        while (true)
        {
            elapsed += Time.unscaledDeltaTime;
            float offset = Mathf.Sin(
                elapsed * spinButtonIdleCyclesPerSecond * Mathf.PI * 2f
            ) * spinButtonIdleDistance;
            spinButtonRect.anchoredPosition =
                spinButtonBasePosition + Vector2.up * offset;
            yield return null;
        }
    }


    private void PlayReelStopCameraShake()
    {
        CameraShake shake = CameraShake.Instance;

        if (shake == null && Camera.main != null)
        {
            shake = Camera.main.GetComponent<CameraShake>();
            if (shake == null)
                shake = Camera.main.gameObject.AddComponent<CameraShake>();
        }

        if (shake != null)
            shake.Shake(reelStopShakeDuration, reelStopShakeMagnitude);
    }


    // ========================================================
    // UI 표시 / 숨기기
    // ========================================================

    private void SetSlotUIVisible(bool visible)
    {
        slotUIVisible = visible;


        if (slotUIRoot != null)
        {
            slotUIRoot.gameObject.SetActive(
                visible
            );
        }


        if (spinButton != null)
        {
            spinButton.interactable =
                visible;
        }

        RefreshSpinButtonIdleAnimation();
    }


    // ========================================================
    // UI Order
    // ========================================================

    private void LateUpdate()
    {
        if (slotUIVisible && slotUIRoot != null)
        {
            UIOverlayOrder.BringToFront(
                slotUIRoot
            );
        }


        if (currentTurnText != null)
        {
            UIOverlayOrder.BringToFront(
                currentTurnText.transform
            );
        }


        if (turnBannerText != null)
        {
            UIOverlayOrder.BringToFront(
                turnBannerText.transform
            );
        }
    }


    // ========================================================
    // 심볼 가중치 추첨
    // ========================================================

    private ReelSymbolData PickSymbolByWeight()
    {
        int totalWeight = 0;


        foreach (ReelSymbolData symbol in symbolList)
        {
            if (symbol != null)
            {
                totalWeight +=
                    Mathf.Max(
                        0,
                        symbol.weight
                    );
            }
        }


        if (totalWeight <= 0)
        {
            return null;
        }


        int random =
            Random.Range(
                0,
                totalWeight
            );


        int sum = 0;


        foreach (ReelSymbolData symbol in symbolList)
        {
            if (symbol == null)
                continue;


            sum +=
                Mathf.Max(
                    0,
                    symbol.weight
                );


            if (random < sum)
            {
                return symbol;
            }
        }


        return null;
    }


    // ========================================================
    // 마지막 릴 잭팟 후보 확인
    // ========================================================

    private bool TryGetJackpotCandidate(
        IReadOnlyList<ReelSymbolData> results,
        int unrevealedIndex,
        out ReelSymbolData candidate
    )
    {
        candidate = null;


        Dictionary<string, int> counts =
            new Dictionary<string, int>();


        Dictionary<string, ReelSymbolData> symbols =
            new Dictionary<string, ReelSymbolData>();


        for (int i = 0; i < unrevealedIndex; i++)
        {
            ReelSymbolData symbol =
                results[i];


            if (symbol == null)
                continue;


            string key =
                GetSymbolMatchKey(
                    symbol
                );


            if (!counts.ContainsKey(key))
            {
                counts[key] = 0;

                symbols[key] = symbol;
            }


            counts[key]++;
        }


        foreach (var pair in counts)
        {
            // 마지막 릴을 제외한 모든 릴이 동일
            if (pair.Value != reels.Length - 1)
                continue;


            candidate =
                symbols[pair.Key];


            return true;
        }


        return false;
    }


    // ========================================================
    // 결과 전체 계산
    // ========================================================

    private List<SymbolResolution> EvaluateTurnResults(
        List<ReelSymbolData> results
    )
    {
        // ====================================================
        // 동일 심볼 개수
        // ====================================================

        Dictionary<string, int> counts =
            new Dictionary<string, int>();


        Dictionary<string, ReelSymbolData> symbolsByKey =
            new Dictionary<string, ReelSymbolData>();


        foreach (ReelSymbolData symbol in results)
        {
            if (symbol == null)
                continue;


            string key =
                GetSymbolMatchKey(
                    symbol
                );


            if (!counts.ContainsKey(key))
            {
                counts[key] = 0;

                symbolsByKey[key] =
                    symbol;
            }


            counts[key]++;
        }


        // ====================================================
        // 그룹 결과
        // ====================================================

        List<SymbolResolution> groupedResults =
            new List<SymbolResolution>();


        Debug.Log(
            "========== 슬롯 결과 =========="
        );


        foreach (var pair in counts)
        {
            ReelSymbolData symbol =
                symbolsByKey[pair.Key];


            SymbolResolution result =
                ResolveSymbol(
                    symbol,
                    pair.Value
                );


            groupedResults.Add(
                result
            );


            Debug.Log(
                $"{symbol.DisplayName} x{pair.Value} | " +
                $"{GetTierDisplayName(result.synergyTier)} | " +
                $"기본합 {result.baseTotal} | " +
                $"x{result.multiplier} | " +
                $"최종 {result.totalValue}"
            );
        }


        SpinResolved?.Invoke(
            groupedResults
        );


        // ====================================================
        // 카드를 릴 하나당 한 장씩 생성
        //
        // 예:
        // 공격 3 x 3개 = 기본합 9
        // 3Combo = 9 × 1.5 = 14
        //
        // 카드:
        // 5 + 5 + 4 = 14
        // ====================================================

        Dictionary<string, int> usedCounts =
            new Dictionary<string, int>();


        List<SymbolResolution> prepared =
            new List<SymbolResolution>();


        foreach (ReelSymbolData symbol in results)
        {
            if (symbol == null)
                continue;


            string key =
                GetSymbolMatchKey(
                    symbol
                );


            int matchCount =
                counts[key];


            int currentIndex = 0;


            if (
                usedCounts.TryGetValue(
                    key,
                    out int used
                )
            )
            {
                currentIndex =
                    used;
            }


            usedCounts[key] =
                currentIndex + 1;


            SymbolResolution group =
                ResolveSymbol(
                    symbol,
                    matchCount
                );


            // =================================================
            // 최종 결과를 카드 수만큼 분배
            // =================================================

            int valuePerCard =
                group.totalValue /
                matchCount;


            int remainder =
                group.totalValue %
                matchCount;


            int cardValue =
                valuePerCard;


            if (currentIndex < remainder)
            {
                cardValue++;
            }


            int baseValue =
                symbol.BaseValue;


            int cardBonus =
                cardValue -
                baseValue;


            prepared.Add(
                new SymbolResolution
                {
                    symbol =
                        symbol,

                    matchCount =
                        1,

                    baseTotal =
                        baseValue,

                    synergyBonus =
                        cardBonus,

                    totalValue =
                        cardValue,

                    multiplier =
                        group.multiplier,

                    synergyTier =
                        group.synergyTier,

                    shieldPersists =
                        group.shieldPersists,

                    overhealToShield =
                        group.overhealToShield
                }
            );
        }


        return prepared;
    }


    // ========================================================
    // 실제 시너지 계산
    // ========================================================

    private SymbolResolution ResolveSymbol(
        ReelSymbolData symbol,
        int matchCount
    )
    {
        if (symbol == null)
        {
            return null;
        }


        // ====================================================
        // 시너지 등급
        // ====================================================

        SynergyTier tier =
            GetSynergyTier(
                matchCount
            );


        // ====================================================
        // 시너지 배율
        // ====================================================

        float multiplier =
            GetSynergyMultiplier(
                tier
            );


        // ====================================================
        // 기본 합계
        // ====================================================

        int baseTotal =
            symbol.BaseValue *
            matchCount;


        // ====================================================
        // 최종 수치
        //
        // 기획:
        //
        // 1~2 = ×1
        // 3 = ×1.5
        // 4 = ×2
        // 5 = ×4
        //
        // .5는 올림
        // ====================================================

        float multipliedValue =
            baseTotal *
            multiplier;


        int totalValue =
            Mathf.FloorToInt(
                multipliedValue + 0.5f
            );


        int synergyBonus =
            totalValue -
            baseTotal;


        // ====================================================
        // 부가 효과
        // ====================================================

        bool shieldPersists = false;

        bool overhealToShield = false;


        if (matchCount >= 3)
        {
            switch (symbol.symbolType)
            {
                // ============================================
                // 방어 3개 이상
                // ============================================

                case SymbolType.Shield:

                    shieldPersists =
                        true;

                    break;


                // ============================================
                // 회복 3개 이상
                // ============================================

                case SymbolType.Heal:

                    overhealToShield =
                        true;

                    break;
            }
        }


        return new SymbolResolution
        {
            symbol =
                symbol,

            matchCount =
                matchCount,

            baseTotal =
                baseTotal,

            synergyBonus =
                synergyBonus,

            totalValue =
                totalValue,

            multiplier =
                multiplier,

            synergyTier =
                tier,

            shieldPersists =
                shieldPersists,

            overhealToShield =
                overhealToShield
        };
    }


    // ========================================================
    // 매치 개수 → 등급
    // ========================================================

    private SynergyTier GetSynergyTier(
        int matchCount
    )
    {
        if (matchCount >= 5)
        {
            return SynergyTier.Mega;
        }


        if (matchCount == 4)
        {
            return SynergyTier.BigCombo;
        }


        if (matchCount == 3)
        {
            return SynergyTier.Combo;
        }


        return SynergyTier.None;
    }


    // ========================================================
    // 등급 → 배율
    // ========================================================

    private float GetSynergyMultiplier(
        SynergyTier tier
    )
    {
        switch (tier)
        {
            case SynergyTier.Combo:

                return 1.5f;


            case SynergyTier.BigCombo:

                return 2f;


            case SynergyTier.Mega:

                return 4f;


            default:

                return 1f;
        }
    }


    // ========================================================
    // 등급 표시
    // ========================================================

    private string GetTierDisplayName(
        SynergyTier tier
    )
    {
        switch (tier)
        {
            case SynergyTier.Combo:

                return "COMBO";


            case SynergyTier.BigCombo:

                return "BIG COMBO";


            case SynergyTier.Mega:

                return "MEGA BONUS";


            default:

                return "NORMAL";
        }
    }


    // ========================================================
    // 동일 심볼 판정
    // ========================================================

    private string GetSymbolMatchKey(
        ReelSymbolData symbol
    )
    {
        return
            $"{(int)symbol.symbolType}:{symbol.DisplayName}";
    }


    // ========================================================
    // 카드 사용 시 호출
    // ========================================================

    private void ApplyAction(
        SymbolResolution result
    )
    {
        if (
            result == null ||
            result.symbol == null
        )
        {
            return;
        }


        string synergyText =
            result.synergyTier ==
            SynergyTier.None

                ? string.Empty

                : $" [{GetTierDisplayName(result.synergyTier)} " +
                  $"x{result.multiplier}]";


        switch (result.symbol.symbolType)
        {
            // =================================================
            // 공격
            // =================================================

            case SymbolType.Attack:

                Debug.Log(
                    $"⚔️ {result.symbol.DisplayName}" +
                    $"{synergyText}: " +
                    $"{result.totalValue} 피해"
                );

                break;


            // =================================================
            // 방어
            // =================================================

            case SymbolType.Shield:

                Debug.Log(
                    $"🛡️ {result.symbol.DisplayName}" +
                    $"{synergyText}: " +
                    $"방어막 +{result.totalValue}"
                );


                if (result.shieldPersists)
                {
                    Debug.Log(
                        "✨ Shield Persistence: " +
                        "방어막이 다음 턴까지 유지됩니다."
                    );
                }

                break;


            // =================================================
            // 회복
            // =================================================

            case SymbolType.Heal:

                Debug.Log(
                    $"🧪 {result.symbol.DisplayName}" +
                    $"{synergyText}: " +
                    $"HP +{result.totalValue}"
                );


                if (result.overhealToShield)
                {
                    Debug.Log(
                        "✨ Overheal Shield Conversion: " +
                        "초과 회복량이 방어막으로 전환됩니다."
                    );
                }

                break;


            // =================================================
            // 기존 추가 심볼 호환
            // =================================================

            case SymbolType.Gold:

                Debug.Log(
                    $"💰 {result.symbol.DisplayName}" +
                    $"{synergyText}: " +
                    $"Gold +{result.totalValue}"
                );

                break;


            case SymbolType.Skull:

                Debug.Log(
                    $"💀 {result.symbol.DisplayName}" +
                    $"{synergyText}: " +
                    $"{result.totalValue}"
                );

                break;
        }
    }


    // ========================================================
    // Player Turn
    // ========================================================

    private void BeginPlayerTurn()
    {
        IsPlayerTurn = true;

        PlayerTurnCount++;

        RefreshTurnUI();
    }


    // ========================================================
    // Turn UI
    // ========================================================

    private void RefreshTurnUI()
    {
        if (currentTurnText == null)
            return;


        currentTurnText.text =
            IsPlayerTurn
                ? $"Player Turn: {PlayerTurnCount}"
                : $"Enemy Turn: {EnemyTurnCount}";


        currentTurnText.color =
            IsPlayerTurn

                ? new Color(
                    0.4f,
                    0.8f,
                    1f
                )

                : new Color(
                    1f,
                    0.45f,
                    0.4f
                );
    }


    // ========================================================
    // Turn UI 준비
    // ========================================================

    private void EnsureTurnUI()
    {
        TMP_Text template =
            currentTurnText != null
                ? currentTurnText
                : playerTurnCountText;


        Canvas rootCanvas =
            template != null
                ? template.GetComponentInParent<Canvas>(
                    true
                )
                : null;


        if (rootCanvas == null && spinButton != null)
        {
            rootCanvas =
                spinButton.GetComponentInParent<Canvas>();
        }


        if (rootCanvas != null)
        {
            currentTurnText =
                CreateTurnText(
                    rootCanvas.transform,
                    "Current Turn Status",
                    new Vector2(
                        0f,
                        -35f
                    ),
                    32
                );


            currentTurnText.rectTransform.anchorMin =
                currentTurnText.rectTransform.anchorMax =
                    new Vector2(
                        0.5f,
                        0.905f
                    );


            currentTurnText.rectTransform.anchoredPosition =
                Vector2.zero;


            currentTurnText.rectTransform.sizeDelta =
                new Vector2(
                    720f,
                    70f
                );


            currentTurnText.fontSize =
                54;


            turnBannerText =
                CreateTurnText(
                    rootCanvas.transform,
                    "Turn Announcement",
                    Vector2.zero,
                    64
                );


            if (
                template != null &&
                template.font != null
            )
            {
                currentTurnText.font =
                    template.font;

                turnBannerText.font =
                    template.font;
            }


            turnBannerText.rectTransform.anchorMin =
                turnBannerText.rectTransform.anchorMax =
                    new Vector2(
                        0.5f,
                        0.5f
                    );


            turnBannerText.rectTransform.pivot =
                new Vector2(
                    0.5f,
                    0.5f
                );


            turnBannerText.rectTransform.sizeDelta =
                new Vector2(
                    900f,
                    120f
                );


            turnBannerText.gameObject.SetActive(
                false
            );


            return;
        }


        if (currentTurnText == null)
        {
            currentTurnText =
                playerTurnCountText != null
                    ? playerTurnCountText
                    : enemyTurnCountText;
        }


        if (
            playerTurnCountText != null &&
            playerTurnCountText != currentTurnText
        )
        {
            playerTurnCountText.enabled =
                false;
        }


        if (
            enemyTurnCountText != null &&
            enemyTurnCountText != currentTurnText
        )
        {
            enemyTurnCountText.enabled =
                false;
        }


        if (currentTurnText != null)
        {
            currentTurnText.enabled =
                true;

            return;
        }


        Canvas canvas =
            spinButton != null
                ? spinButton.GetComponentInParent<Canvas>()
                : null;


        if (
            canvas == null &&
            reels.Length > 0
        )
        {
            canvas =
                reels[0].GetComponentInParent<Canvas>();
        }


        if (canvas == null)
        {
            Debug.LogWarning(
                "턴 UI Canvas를 찾을 수 없습니다.",
                this
            );

            return;
        }


        currentTurnText =
            CreateTurnText(
                canvas.transform,
                "Current Turn",
                new Vector2(
                    0f,
                    -35f
                ),
                32
            );
    }


    // ========================================================
    // TMP 생성
    // ========================================================

    private TMP_Text CreateTurnText(
        Transform parent,
        string objectName,
        Vector2 position,
        int fontSize
    )
    {
        GameObject label =
            new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI)
            );


        label.layer =
            parent.gameObject.layer;


        label.transform.SetParent(
            parent,
            false
        );


        TMP_Text text =
            label.GetComponent<TextMeshProUGUI>();


        text.font =
            TMP_Settings.defaultFontAsset;

        text.fontSize =
            fontSize;

        text.alignment =
            TextAlignmentOptions.Center;

        text.color =
            Color.white;

        text.raycastTarget =
            false;


        RectTransform rect =
            text.rectTransform;


        rect.anchorMin =
            rect.anchorMax =
                new Vector2(
                    0.5f,
                    1f
                );


        rect.pivot =
            new Vector2(
                0.5f,
                1f
            );


        rect.sizeDelta =
            new Vector2(
                320f,
                46f
            );


        rect.anchoredPosition =
            position;


        return text;
    }


    // ========================================================
    // PLAYER TURN / ENEMY TURN 표시
    // ========================================================

    private IEnumerator ShowTurnBanner()
    {
        if (turnBannerText == null)
            yield break;


        bool wasBusy =
            isBusy;


        isBusy =
            true;


        turnBannerText.text =
            IsPlayerTurn
                ? "PLAYER TURN"
                : "ENEMY TURN";


        Color color =
            IsPlayerTurn

                ? new Color(
                    0.4f,
                    0.8f,
                    1f
                )

                : new Color(
                    1f,
                    0.45f,
                    0.4f
                );


        turnBannerText.gameObject.SetActive(
            true
        );


        float elapsed =
            0f;


        while (elapsed < 0.9f)
        {
            elapsed +=
                Time.unscaledDeltaTime;


            float appear =
                Mathf.Clamp01(
                    elapsed / 0.2f
                );


            color.a =
                appear *
                Mathf.Clamp01(
                    (0.9f - elapsed) /
                    0.2f
                );


            turnBannerText.color =
                color;


            turnBannerText.rectTransform.localScale =
                Vector3.one *
                Mathf.Lerp(
                    1.25f,
                    1f,
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        appear
                    )
                );


            yield return null;
        }


        turnBannerText.gameObject.SetActive(
            false
        );


        isBusy =
            wasBusy;
    }


    // ========================================================
    // Setup 검사
    // ========================================================

    private bool ValidateSetup()
    {
        if (
            reels == null ||
            reels.Length == 0
        )
        {
            Debug.LogError(
                "SlotMachineController에 릴이 연결되지 않았습니다.",
                this
            );

            return false;
        }


        if (
            symbolList == null ||
            symbolList.Count == 0
        )
        {
            Debug.LogError(
                "SlotMachineController의 심볼 풀이 비어 있습니다.",
                this
            );

            return false;
        }


        foreach (SlotReel reel in reels)
        {
            if (reel == null)
            {
                Debug.LogError(
                    "비어 있는 Reel 참조가 있습니다.",
                    this
                );

                return false;
            }
        }


        int totalWeight =
            0;


        foreach (ReelSymbolData symbol in symbolList)
        {
            if (symbol != null)
            {
                totalWeight +=
                    Mathf.Max(
                        0,
                        symbol.weight
                    );
            }
        }


        if (totalWeight <= 0)
        {
            Debug.LogError(
                "심볼 전체 가중치는 1 이상이어야 합니다.",
                this
            );

            return false;
        }


        return true;
    }
}
