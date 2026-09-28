using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

// Canvas에 배치한 결과창과 카드 템플릿을 사용합니다.
// CanvasGroup은 사용하지 않는다.
public class SpellResultHand : MonoBehaviour
{
    public bool Finished { get; private set; }

    public bool EnemyDefeated =>
        enemyHP <= 0 &&
        (secondEnemy == null || secondEnemyHP <= 0) &&
        (thirdEnemy == null || thirdEnemyHP <= 0);
    public bool PlayerDefeated => PlayerHP <= 0;

    public BattleHUD HUD { get; private set; }

    public int PlayerHP { get; private set; } = 100;
    public int Shield { get; private set; }
    public int Gold { get; private set; }

    private int enemyHP = 100;
    private int enemyMaxHP = 100;
    private int enemyAttackDamage = 10;
    private int secondEnemyHP;
    private int secondEnemyMaxHP = 100;
    private int secondEnemyAttackDamage = 10;
    private int thirdEnemyHP;
    private int thirdEnemyMaxHP = 100;
    private int thirdEnemyAttackDamage = 10;
    private int configuredEnemyCount = 2;
    private SpriteRenderer secondEnemy;
    private SpriteRenderer thirdEnemy;
    [SerializeField] private TMP_Text secondEnemyLabel;
    [SerializeField] private Image secondEnemyHPFill;
    private TMP_Text thirdEnemyLabel;
    private Image thirdEnemyHPFill;

    [SerializeField] private Image playerHPFill;
    [SerializeField] private Image enemyHPFill;


    [SerializeField] private BattleHUD battleHUD;
    [SerializeField] private SpellResultCard cardTemplate;
    [SerializeField] private Button endTurnButton;
    [SerializeField, Min(0f)] private float healthLabelOffset = 100f;
    [SerializeField, Min(0f)] private float playerLabelOffset = 35f;
    [SerializeField, Min(0f)] private float characterTapPadding = 30f;

    [Header("Floating Combat Text")]
    [SerializeField, Min(0.1f)] private float floatingTextDuration = 0.9f;
    [SerializeField, Min(0f)] private float floatingTextDistance = 90f;
    [SerializeField, Range(20, 120)] private int floatingTextSize = 56;

    [Header("Camera Shake")]
    [SerializeField, Min(0.01f)] private float hitShakeDuration = 0.12f;
    [SerializeField, Min(0f)] private float hitShakeMagnitude = 0.08f;

    [Header("Enemy Hit Flash")]
    [SerializeField, Min(0.01f)] private float enemyHitFlashDuration = 0.12f;
    [SerializeField] private Color enemyHitFlashColor = new Color(1f, 0.18f, 0.18f, 1f);

    [Header("Player Magic Flash")]
    [SerializeField, Min(0.01f)] private float playerMagicFlashDuration = 0.16f;
    [SerializeField] private Color healFlashColor = new Color(0.45f, 1f, 0.35f, 1f);
    [SerializeField] private Color shieldFlashColor = new Color(0.25f, 0.65f, 1f, 1f);

    [Header("Attack Animation")]
    [SerializeField, Min(0.1f)] private float wizardScale = 4.5f;
    [SerializeField, Min(0.1f)] private float wizardIdleScaleMultiplier = 1.2f;
    [SerializeField] private float wizardVerticalOffset = 0.15f;
    [SerializeField, Min(0.1f)] private float attackProjectileScale = 5f;
    [SerializeField, Min(0.01f)] private float attackFrameDuration = 0.055f;
    [SerializeField, Min(0.05f)] private float attackProjectileDuration = 0.28f;

    private Sprite[] playerAttackFrames;
    private Sprite wizardIdleSprite;
    private Sprite[] attackProjectileFrames;
    private RuntimeAnimatorController playerAttackController;
    private int activePlayerAttackAnimations;
    private int pendingAttackImpacts;
    private Coroutine playerMagicFlashRoutine;
    private Color playerColorBeforeMagicFlash;

    private Canvas canvas;
    [SerializeField] private RectTransform hand;

    [SerializeField] private TMP_Text status;
    [SerializeField] private TMP_Text playerLabel;
    [SerializeField] private TMP_Text enemyLabel;

    private SpriteRenderer player;
    private SpriteRenderer enemy;

    private RectTransform playerTapArea;
    private RectTransform enemyTapArea;
    private RectTransform secondEnemyTapArea;
    private RectTransform thirdEnemyTapArea;

    private readonly List<SpellResultCard> cards =
        new List<SpellResultCard>();

    private readonly List<Sprite> ownedSprites =
        new List<Sprite>();

    private Action<SymbolResolution> onCast;

    private TMP_FontAsset font;

    private bool accepting;


    // =========================================================
    // Initialize
    // =========================================================

    public void Initialize(
        Canvas targetCanvas,
        TMP_FontAsset targetFont,
        RoomBattleSettings roomSettings)
    {
        canvas = targetCanvas;
        font = targetFont;
        Sprite[] wizardIdleFrames = LoadAnimationFrames("AttackAnimations/Wizard");
        wizardIdleSprite =
            wizardIdleFrames.Length > 0 ? wizardIdleFrames[0] : null;
        playerAttackFrames = LoadAnimationFrames("AttackAnimations/WizardAttack");
        attackProjectileFrames = LoadAnimationFrames("AttackAnimations/ball-Sheet");
        playerAttackController = Resources.Load<RuntimeAnimatorController>(
            "AttackAnimations/WizardAttack"
        );


        // =====================================================
        // 캐릭터 찾기
        // =====================================================

        player =
            GameObject.Find("Player")
            ?.GetComponent<SpriteRenderer>();

        if (player != null)
        {
            if (wizardIdleSprite != null)
                player.sprite = wizardIdleSprite;

            float xDirection = Mathf.Sign(player.transform.localScale.x);
            if (Mathf.Approximately(xDirection, 0f))
                xDirection = 1f;
            float idleScale = wizardScale * wizardIdleScaleMultiplier;
            player.transform.localScale = new Vector3(
                idleScale * xDirection,
                idleScale,
                1f
            );
            player.transform.position += Vector3.up * wizardVerticalOffset;

            Animator scenePlayerAnimator = player.GetComponent<Animator>();
            if (scenePlayerAnimator != null)
                scenePlayerAnimator.enabled = false;
        }

        enemy =
            GameObject.Find("Enemy")
            ?.GetComponent<SpriteRenderer>();
        // Unity 복제 기본 이름을 우선 사용하고 이전 이름도 지원한다.
        secondEnemy = GameObject.Find("Enemy (1)")?.GetComponent<SpriteRenderer>();
        if (secondEnemy == null)
            secondEnemy = GameObject.Find("Enemy 2")?.GetComponent<SpriteRenderer>();

        configuredEnemyCount = ResolveEnemyCount(roomSettings);
        EnsureThirdEnemyAndUI();

        CreateCharacterTapAreas();


        // =====================================================
        // Battle HUD
        // ROOM / SETTINGS는 기존 BattleHUD 그대로 사용
        // =====================================================

        HUD =
            battleHUD;

        HUD.Initialize(
            canvas,
            font
        );


        // =====================================================
        // 적 HP
        // =====================================================

        ApplyRoomBattleSettings(roomSettings);


        // =====================================================
        // 마법 결과 창
        // =====================================================

        endTurnButton.onClick.AddListener(Finish);
        cardTemplate.gameObject.SetActive(false);
        hand.gameObject.SetActive(false);
        RefreshStats();
    }


    private void ApplyRoomBattleSettings(RoomBattleSettings settings)
    {
        int fallbackHealth = 100 + (HUD.Stage - 1) * 25;
        int fallbackAttack = 10 + (HUD.Stage - 1) * 2;
        configuredEnemyCount = ResolveEnemyCount(settings);

        ConfigureEnemy(
            enemy,
            settings != null ? settings.enemy : null,
            configuredEnemyCount >= 1,
            fallbackHealth,
            fallbackAttack,
            out enemyMaxHP,
            out enemyHP,
            out enemyAttackDamage
        );

        ConfigureEnemy(
            secondEnemy,
            settings != null ? settings.enemy2 : null,
            configuredEnemyCount >= 2,
            fallbackHealth,
            fallbackAttack,
            out secondEnemyMaxHP,
            out secondEnemyHP,
            out secondEnemyAttackDamage
        );

        ConfigureEnemy(
            thirdEnemy,
            settings != null ? settings.enemy3 : null,
            configuredEnemyCount >= 3,
            fallbackHealth,
            fallbackAttack,
            out thirdEnemyMaxHP,
            out thirdEnemyHP,
            out thirdEnemyAttackDamage
        );
    }


    private static int ResolveEnemyCount(RoomBattleSettings settings)
    {
        return settings == null || settings.enemyCount <= 0
            ? 2
            : Mathf.Clamp(settings.enemyCount, 1, 3);
    }


    private void EnsureThirdEnemyAndUI()
    {
        if (configuredEnemyCount < 3)
            return;

        thirdEnemy = GameObject.Find("Enemy (2)")?.GetComponent<SpriteRenderer>();
        if (thirdEnemy == null)
            thirdEnemy = GameObject.Find("Enemy 3")?.GetComponent<SpriteRenderer>();

        if (thirdEnemy == null && secondEnemy != null)
        {
            GameObject clone = Instantiate(
                secondEnemy.gameObject,
                secondEnemy.transform.parent
            );
            clone.name = "Enemy (2)";
            Vector3 spacing =
                enemy != null
                    ? secondEnemy.transform.position - enemy.transform.position
                    : Vector3.left * 2.7f;
            clone.transform.position = secondEnemy.transform.position + spacing * 0.85f;
            thirdEnemy = clone.GetComponent<SpriteRenderer>();
        }

        if (thirdEnemyLabel == null && secondEnemyLabel != null)
        {
            thirdEnemyLabel = Instantiate(
                secondEnemyLabel,
                secondEnemyLabel.transform.parent
            );
            thirdEnemyLabel.name = "Enemy 3 Info";

            Image[] clonedImages =
                thirdEnemyLabel.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < clonedImages.Length; i++)
            {
                if (
                    secondEnemyHPFill != null &&
                    clonedImages[i].gameObject.name ==
                    secondEnemyHPFill.gameObject.name)
                {
                    thirdEnemyHPFill = clonedImages[i];
                    break;
                }
            }
        }
    }


    private static void ConfigureEnemy(
        SpriteRenderer renderer,
        RoomEnemySettings settings,
        bool enabledByCount,
        int fallbackHealth,
        int fallbackAttack,
        out int maxHealth,
        out int health,
        out int attackDamage)
    {
        bool placed =
            renderer != null &&
            enabledByCount &&
            (settings == null || settings.enabled);

        maxHealth = settings != null
            ? Mathf.Max(1, settings.maxHealth)
            : Mathf.Max(1, fallbackHealth);

        attackDamage = settings != null
            ? Mathf.Max(0, settings.attackDamage)
            : Mathf.Max(0, fallbackAttack);

        health = placed ? maxHealth : 0;

        if (renderer == null)
            return;

        if (settings != null)
        {
            if (settings.sprite != null)
                renderer.sprite = settings.sprite;

            if (settings.useCustomPosition)
                renderer.transform.position = settings.position;

            if (settings.useCustomScale)
                renderer.transform.localScale = settings.scale;
        }

        renderer.enabled = placed;
    }


    // =========================================================
    // 마법 카드 표시
    // =========================================================

    public void Present(
        List<SymbolResolution> results,
        Action<SymbolResolution> cast)
    {
        ClearCards();

        Finished = false;
        accepting = true;

        onCast = cast;


        hand.gameObject.SetActive(true);

        hand.SetAsLastSibling();


        playerLabel.gameObject.SetActive(true);
        RefreshStats();


        status.text =
            "1ST CARD  ->  TAP THE GLOWING CHARACTER!";


        Texture2D atlas =
            Resources.Load<Texture2D>(
                "SpellIcons/spell-atlas"
            );


        // =====================================================
        // 카드 생성
        // =====================================================

        for (int i = 0; i < results.Count; i++)
        {
            SpellResultCard card = Instantiate(cardTemplate, hand);
            card.gameObject.SetActive(true);
            RectTransform slot = (RectTransform)card.transform;
            slot.name = "Spell " + (i + 1);

            float step =
                0.85f /
                Mathf.Max(
                    1,
                    results.Count
                );


            slot.anchorMin =
                new Vector2(
                    0.015f + i * step,
                    0.07f
                );


            slot.anchorMax =
                new Vector2(
                    0.015f +
                    (i + 1) * step -
                    0.012f,

                    0.78f
                );


            slot.offsetMin =
                Vector2.zero;

            slot.offsetMax =
                Vector2.zero;


            SymbolResolution result =
                results[i];


            // =================================================
            // 아이콘
            // =================================================

            Image icon = card.Icon;

            int tile =
                (int)result.symbol.symbolType;


            // 릴 심볼 이미지 사용
            if (result.symbol.symbolSprite != null)
            {
                icon.sprite =
                    result.symbol.symbolSprite;
            }
            else if (
                atlas != null &&
                tile >= 0 &&
                tile < 3)
            {
                int width =
                    atlas.width / 3;


                icon.sprite =
                    Sprite.Create(
                        atlas,

                        new Rect(
                            tile * width,
                            0,
                            width,
                            atlas.height
                        ),

                        new Vector2(
                            0.5f,
                            0.5f
                        )
                    );


                ownedSprites.Add(
                    icon.sprite
                );
            }
            else
            {
                icon.sprite =
                    result.symbol.symbolSprite;
            }


            // =================================================
            // 마법 이름
            // =================================================

            card.Initialize(
                this,
                result,
                canvas
            );


            cards.Add(card);
        }

        RefreshFirstCardHighlight();


        if (results.Count == 0)
        {
            Finish();
        }
    }


    // =========================================================
    // Drag 가능 여부
    // =========================================================

    public bool CanDrag =>
        accepting &&
        !Finished &&
        !PlayerDefeated &&
        pendingAttackImpacts == 0 &&
        isActiveAndEnabled;


    // =========================================================
    // 마법 사용
    // =========================================================

    public bool TryCast(
        SpellResultCard card,
        SymbolResolution result,
        Vector2 screenPosition)
    {
        if (
            !CanDrag ||
            !cards.Contains(card))
        {
            return false;
        }


        bool offensive =
            result.symbol.symbolType ==
            SymbolType.Attack;


        SpriteRenderer target =
            offensive
            ? FindAttackTarget(screenPosition)
            : player;


        if (
            target == null ||
            !HitsTarget(
                target,
                screenPosition
            ))
        {
            status.text =
                offensive
                ? "Drop this spell on ENEMY"
                : "Drop this spell on PLAYER";

            return false;
        }


        int value =
            Mathf.Max(
                0,
                result.totalValue
            );


        switch (
            result.symbol.symbolType)
        {
            case SymbolType.Attack:
                PlayAttackAnimation(target, value);

                break;


            case SymbolType.Shield:

                Shield += value;
                ShowFloatingCombatText(
                    player,
                    $"+{value}",
                    new Color(0.15f, 0.82f, 1f)
                );
                if (value > 0)
                    PlayPlayerMagicFlash(shieldFlashColor);

                break;


            case SymbolType.Heal:

                int previousPlayerHealth = PlayerHP;

                PlayerHP =
                    Mathf.Min(
                        100,
                        PlayerHP + value
                    );

                int appliedHealing = PlayerHP - previousPlayerHealth;

                ShowFloatingCombatText(
                    player,
                    $"+{appliedHealing}",
                    new Color(0.25f, 1f, 0.35f)
                );
                if (appliedHealing > 0)
                    PlayPlayerMagicFlash(healFlashColor);

                break;


            case SymbolType.Gold:

                Gold += value;

                break;


            case SymbolType.Skull:

                int healthBeforeSkull = PlayerHP;

                PlayerHP =
                    Mathf.Max(
                        0,
                        PlayerHP - value
                    );

                ShowFloatingCombatText(
                    player,
                    $"-{healthBeforeSkull - PlayerHP}",
                    new Color(1f, 0.25f, 0.18f)
                );
                if (healthBeforeSkull > PlayerHP)
                    ShakeCamera();

                break;
        }


        cards.Remove(card);
        RefreshFirstCardHighlight();

        Destroy(
            card.gameObject
        );


        RefreshStats();


        status.text =
            result.symbol.DisplayName +
            "  " +
            value;


        onCast?.Invoke(result);


        TryFinishHand();


        return true;
    }


    private void TryFinishHand()
    {
        if (pendingAttackImpacts > 0)
            return;

        if (cards.Count == 0 || EnemyDefeated || PlayerDefeated)
        {
            Finished = true;
            accepting = false;

            if (EnemyDefeated)
                status.text = "VICTORY!";

            if (PlayerDefeated)
                status.text = "DEFEAT";
        }
    }


    // =========================================================
    // 캐릭터 탭으로 첫 번째 카드 사용
    // =========================================================

    private void CreateCharacterTapAreas()
    {
        if (canvas == null)
            return;

        playerTapArea = CreateTapArea(
            "Player Tap Area",
            () => TryCastFirstCardAt(player)
        );

        enemyTapArea = CreateTapArea(
            "Enemy Tap Area",
            () => TryCastFirstCardAt(enemy)
        );

        secondEnemyTapArea = CreateTapArea(
            "Enemy 2 Tap Area",
            () => TryCastFirstCardAt(secondEnemy)
        );

        thirdEnemyTapArea = CreateTapArea(
            "Enemy 3 Tap Area",
            () => TryCastFirstCardAt(thirdEnemy)
        );
    }


    private RectTransform CreateTapArea(
        string objectName,
        UnityAction onClick)
    {
        GameObject tapObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button)
        );

        RectTransform rect = tapObject.GetComponent<RectTransform>();
        rect.SetParent(canvas.transform, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.SetAsLastSibling();

        Image image = tapObject.GetComponent<Image>();
        image.color = Color.clear;
        image.raycastTarget = true;

        CreateBorderEdge(
            rect,
            "Top",
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, -3f),
            new Vector2(0f, 6f)
        );
        CreateBorderEdge(
            rect,
            "Bottom",
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 3f),
            new Vector2(0f, 6f)
        );
        CreateBorderEdge(
            rect,
            "Left",
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(3f, 0f),
            new Vector2(6f, 0f)
        );
        CreateBorderEdge(
            rect,
            "Right",
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(-3f, 0f),
            new Vector2(6f, 0f)
        );

        Button button = tapObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;
        button.onClick.AddListener(onClick);

        tapObject.SetActive(false);
        return rect;
    }


    private static void CreateBorderEdge(
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

        RectTransform edge = edgeObject.GetComponent<RectTransform>();
        edge.SetParent(parent, false);
        edge.anchorMin = anchorMin;
        edge.anchorMax = anchorMax;
        edge.anchoredPosition = anchoredPosition;
        edge.sizeDelta = sizeDelta;

        Image edgeImage = edgeObject.GetComponent<Image>();
        edgeImage.color = new Color(1f, 0.82f, 0.12f, 1f);
        edgeImage.raycastTarget = false;
    }


    private void UpdateCharacterTapAreas()
    {
        bool canTap = CanDrag && cards.Count > 0;
        bool firstCardIsAttack =
            canTap &&
            cards[0] != null &&
            cards[0].Result != null &&
            cards[0].Result.symbol != null &&
            cards[0].Result.symbol.symbolType == SymbolType.Attack;

        UpdateTapArea(
            playerTapArea,
            player,
            canTap && !firstCardIsAttack && PlayerHP > 0
        );

        UpdateTapArea(
            enemyTapArea,
            enemy,
            canTap && firstCardIsAttack && enemyHP > 0
        );

        UpdateTapArea(
            secondEnemyTapArea,
            secondEnemy,
            canTap && firstCardIsAttack && secondEnemyHP > 0
        );

        UpdateTapArea(
            thirdEnemyTapArea,
            thirdEnemy,
            canTap && firstCardIsAttack && thirdEnemyHP > 0
        );
    }


    private void UpdateTapArea(
        RectTransform tapArea,
        SpriteRenderer target,
        bool visible)
    {
        if (tapArea == null)
            return;

        visible =
            visible &&
            target != null &&
            target.enabled &&
            canvas != null &&
            Camera.main != null;

        tapArea.gameObject.SetActive(visible);
        if (!visible)
            return;

        Bounds bounds = target.bounds;
        Vector3 screenMin = Camera.main.WorldToScreenPoint(bounds.min);
        Vector3 screenMax = Camera.main.WorldToScreenPoint(bounds.max);

        if (screenMin.z < 0f || screenMax.z < 0f)
        {
            tapArea.gameObject.SetActive(false);
            return;
        }

        RectTransform canvasRect = (RectTransform)canvas.transform;
        Camera eventCamera =
            canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera != null
                    ? canvas.worldCamera
                    : Camera.main;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenMin,
            eventCamera,
            out Vector2 localMin
        );

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenMax,
            eventCamera,
            out Vector2 localMax
        );

        tapArea.anchoredPosition = (localMin + localMax) * 0.5f;
        tapArea.sizeDelta = new Vector2(
            Mathf.Abs(localMax.x - localMin.x) + characterTapPadding * 1.2f,
            Mathf.Abs(localMax.y - localMin.y) + characterTapPadding * 2f
        );

        float pulse = (Mathf.Sin(Time.unscaledTime * 5f) + 1f) * 0.5f;
        Image image = tapArea.GetComponent<Image>();
        image.color = Color.clear;

        Outline oldOutline = tapArea.GetComponent<Outline>();
        if (oldOutline != null)
            oldOutline.enabled = false;

        Color borderColor = new Color(
            1f,
            0.82f,
            0.12f,
            Mathf.Lerp(0.65f, 1f, pulse)
        );

        for (int i = 0; i < tapArea.childCount; i++)
        {
            Image edge = tapArea.GetChild(i).GetComponent<Image>();
            if (edge != null)
                edge.color = borderColor;
        }

        tapArea.localScale = Vector3.one * Mathf.Lerp(1f, 1.05f, pulse);
        tapArea.SetAsLastSibling();
    }


    private void RefreshFirstCardHighlight()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] != null)
                cards[i].SetFirstCardHighlight(i == 0);
        }
    }


    private void TryCastFirstCardAt(SpriteRenderer target)
    {
        if (!CanDrag || target == null || cards.Count == 0)
            return;

        SpellResultCard firstCard = cards[0];
        if (firstCard == null || firstCard.Result == null || Camera.main == null)
            return;

        Vector2 screenPosition =
            Camera.main.WorldToScreenPoint(target.bounds.center);

        TryCast(firstCard, firstCard.Result, screenPosition);
    }


    // =========================================================
    // 대상 판정
    // =========================================================

    private bool HitsTarget(
        SpriteRenderer target,
        Vector2 point)
    {
        Camera camera =
            Camera.main;


        if (camera == null)
            return false;


        Bounds b =
            target.bounds;


        Vector3 min =
            camera.WorldToScreenPoint(
                b.min
            );


        Vector3 max =
            camera.WorldToScreenPoint(
                b.max
            );


        return Rect.MinMaxRect(
            Mathf.Min(
                min.x,
                max.x
            ) - 20,

            Mathf.Min(
                min.y,
                max.y
            ) - 20,

            Mathf.Max(
                min.x,
                max.x
            ) + 20,

            Mathf.Max(
                min.y,
                max.y
            ) + 20
        ).Contains(point);
    }


    // =========================================================
    // Floating Combat Text
    // =========================================================

    private static Sprite[] LoadAnimationFrames(string resourcePath)
    {
        Sprite[] frames = Resources.LoadAll<Sprite>(resourcePath);
        Array.Sort(frames, (left, right) =>
            GetFrameNumber(left.name).CompareTo(GetFrameNumber(right.name))
        );
        return frames;
    }


    private static int GetFrameNumber(string spriteName)
    {
        int separator = spriteName.LastIndexOf('_');
        if (
            separator >= 0 &&
            int.TryParse(spriteName.Substring(separator + 1), out int frameNumber))
        {
            return frameNumber;
        }

        return 0;
    }


    private void PlayAttackAnimation(SpriteRenderer target, int damage)
    {
        if (
            player == null ||
            target == null ||
            playerAttackFrames == null ||
            playerAttackFrames.Length == 0)
        {
            ApplyAttackDamage(target, damage);
            return;
        }

        pendingAttackImpacts++;
        StartCoroutine(PlayAttackAnimationRoutine(target, damage));
    }


    private IEnumerator PlayAttackAnimationRoutine(
        SpriteRenderer target,
        int damage)
    {
        Vector3 start = player.bounds.center;
        Vector3 destination = target.bounds.center;
        int sortingOrder = Mathf.Max(player.sortingOrder, target.sortingOrder) + 5;

        GameObject attackObject = new GameObject("Player Attack Animation");
        SpriteRenderer attackRenderer = attackObject.AddComponent<SpriteRenderer>();
        attackRenderer.sortingLayerID = player.sortingLayerID;
        attackRenderer.sortingOrder = sortingOrder;
        attackRenderer.flipX = destination.x < start.x;
        attackObject.transform.position = start;
        attackObject.transform.localScale = Vector3.one * wizardScale;

        activePlayerAttackAnimations++;
        player.enabled = false;

        if (playerAttackController != null)
        {
            Animator attackAnimator = attackObject.AddComponent<Animator>();
            attackAnimator.runtimeAnimatorController = playerAttackController;
            attackAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            yield return new WaitForSecondsRealtime(
                attackFrameDuration * playerAttackFrames.Length
            );
        }
        else
        {
            for (int i = 0; i < playerAttackFrames.Length; i++)
            {
                attackRenderer.sprite = playerAttackFrames[i];
                yield return new WaitForSecondsRealtime(attackFrameDuration);
            }
        }

        Destroy(attackObject);
        FinishPlayerAttackAnimation();

        if (attackProjectileFrames == null || attackProjectileFrames.Length == 0)
        {
            ResolveAttackImpact(target, damage);
            yield break;
        }

        Vector3 direction = (destination - start).normalized;
        Vector3 projectileStart = start + direction * 0.65f;
        GameObject projectileObject = new GameObject("Attack Projectile");
        SpriteRenderer projectileRenderer = projectileObject.AddComponent<SpriteRenderer>();
        projectileRenderer.sortingLayerID = player.sortingLayerID;
        projectileRenderer.sortingOrder = sortingOrder + 1;
        projectileRenderer.flipX = direction.x < 0f;
        projectileObject.transform.localScale = Vector3.one * attackProjectileScale;

        float elapsed = 0f;
        while (elapsed < attackProjectileDuration)
        {
            float progress = Mathf.Clamp01(elapsed / attackProjectileDuration);
            int frameIndex = Mathf.Min(
                attackProjectileFrames.Length - 1,
                Mathf.FloorToInt(progress * attackProjectileFrames.Length)
            );
            projectileRenderer.sprite = attackProjectileFrames[frameIndex];
            projectileObject.transform.position = Vector3.Lerp(
                projectileStart,
                destination,
                progress
            );

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        projectileObject.transform.position = destination;
        projectileRenderer.sprite =
            attackProjectileFrames[attackProjectileFrames.Length - 1];
        ResolveAttackImpact(target, damage);
        yield return new WaitForSecondsRealtime(attackFrameDuration);
        Destroy(projectileObject);
    }


    private void ResolveAttackImpact(SpriteRenderer target, int damage)
    {
        ApplyAttackDamage(target, damage);
        pendingAttackImpacts = Mathf.Max(0, pendingAttackImpacts - 1);
        RefreshFirstCardHighlight();
        TryFinishHand();
    }


    private void ApplyAttackDamage(SpriteRenderer target, int damage)
    {
        int previousHealth;
        int currentHealth;

        if (target == thirdEnemy)
        {
            previousHealth = thirdEnemyHP;
            thirdEnemyHP = Mathf.Max(0, thirdEnemyHP - damage);
            currentHealth = thirdEnemyHP;
        }
        else if (target == secondEnemy)
        {
            previousHealth = secondEnemyHP;
            secondEnemyHP = Mathf.Max(0, secondEnemyHP - damage);
            currentHealth = secondEnemyHP;
        }
        else
        {
            previousHealth = enemyHP;
            enemyHP = Mathf.Max(0, enemyHP - damage);
            currentHealth = enemyHP;
        }

        int appliedDamage = previousHealth - currentHealth;
        ShowFloatingCombatText(
            target,
            $"-{appliedDamage}",
            new Color(1f, 0.25f, 0.18f)
        );
        if (appliedDamage > 0)
        {
            StartCoroutine(EnemyHitFlashRoutine(target));
            ShakeCamera();
        }

        RefreshStats();
    }


    private IEnumerator EnemyHitFlashRoutine(SpriteRenderer target)
    {
        if (target == null)
            yield break;

        Color originalColor = target.color;
        Color flashColor = enemyHitFlashColor;
        flashColor.a = originalColor.a;
        target.color = flashColor;

        yield return new WaitForSecondsRealtime(enemyHitFlashDuration);

        if (target != null)
            target.color = originalColor;
    }


    private void PlayPlayerMagicFlash(Color flashColor)
    {
        if (player == null)
            return;

        if (playerMagicFlashRoutine != null)
        {
            StopCoroutine(playerMagicFlashRoutine);
            player.color = playerColorBeforeMagicFlash;
        }

        playerColorBeforeMagicFlash = player.color;
        playerMagicFlashRoutine = StartCoroutine(
            PlayerMagicFlashRoutine(flashColor)
        );
    }


    private IEnumerator PlayerMagicFlashRoutine(Color flashColor)
    {
        flashColor.a = playerColorBeforeMagicFlash.a;
        player.color = flashColor;

        yield return new WaitForSecondsRealtime(playerMagicFlashDuration);

        if (player != null)
            player.color = playerColorBeforeMagicFlash;

        playerMagicFlashRoutine = null;
    }


    private void FinishPlayerAttackAnimation()
    {
        activePlayerAttackAnimations = Mathf.Max(0, activePlayerAttackAnimations - 1);
        if (activePlayerAttackAnimations == 0 && player != null && !PlayerDefeated)
            player.enabled = true;
    }


    private void ShakeCamera()
    {
        CameraShake shake = CameraShake.Instance;

        if (shake == null && Camera.main != null)
        {
            shake = Camera.main.GetComponent<CameraShake>();
            if (shake == null)
                shake = Camera.main.gameObject.AddComponent<CameraShake>();
        }

        if (shake != null)
            shake.Shake(hitShakeDuration, hitShakeMagnitude);
    }


    private void ShowFloatingCombatText(
        SpriteRenderer target,
        string text,
        Color color,
        float verticalOffset = 0f)
    {
        if (target == null || canvas == null)
            return;

        StartCoroutine(
            FloatingCombatTextRoutine(
                target,
                text,
                color,
                verticalOffset
            )
        );
    }


    private IEnumerator FloatingCombatTextRoutine(
        SpriteRenderer target,
        string text,
        Color color,
        float verticalOffset)
    {
        TMP_Text floatingText = Label(
            canvas.transform,
            text,
            floatingTextSize
        );

        floatingText.name = "Floating Combat Text";
        floatingText.color = color;
        floatingText.fontStyle = FontStyles.Bold;
        floatingText.outlineWidth = 0.18f;
        floatingText.outlineColor = Color.black;
        floatingText.rectTransform.sizeDelta = new Vector2(320f, 100f);
        floatingText.transform.SetAsLastSibling();

        PositionLabel(
            floatingText,
            target,
            verticalOffset
        );

        Vector2 startPosition = floatingText.rectTransform.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < floatingTextDuration && floatingText != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / floatingTextDuration);
            float easedProgress = 1f - Mathf.Pow(1f - progress, 2f);

            floatingText.rectTransform.anchoredPosition =
                startPosition + Vector2.up * floatingTextDistance * easedProgress;

            Color fadedColor = color;
            fadedColor.a = 1f - progress;
            floatingText.color = fadedColor;

            yield return null;
        }

        if (floatingText != null)
            Destroy(floatingText.gameObject);
    }


    // =========================================================
    // 상태 UI 갱신
    // =========================================================

    private SpriteRenderer FindAttackTarget(Vector2 position)
    {
        if (enemy != null && enemyHP > 0 && HitsTarget(enemy, position)) return enemy;
        if (secondEnemy != null && secondEnemyHP > 0 && HitsTarget(secondEnemy, position)) return secondEnemy;
        if (thirdEnemy != null && thirdEnemyHP > 0 && HitsTarget(thirdEnemy, position)) return thirdEnemy;
        return null;
    }

    private void RefreshStats()
    {
        playerLabel.text =
            $"PLAYER  {PlayerHP}/100\n" +
            $"SHIELD {Shield}  GOLD {Gold}";


        enemyLabel.text =
            $"ENEMY  {enemyHP}/{enemyMaxHP}";
        bool firstEnemyAlive = enemy != null && enemyHP > 0;
        bool secondEnemyAlive = secondEnemy != null && secondEnemyHP > 0;
        bool thirdEnemyAlive = thirdEnemy != null && thirdEnemyHP > 0;
        if (enemy != null) enemy.enabled = firstEnemyAlive;
        if (enemyLabel != null) enemyLabel.gameObject.SetActive(firstEnemyAlive);
        if (secondEnemy != null) secondEnemy.enabled = secondEnemyAlive;
        if (secondEnemyLabel != null)
        {
            secondEnemyLabel.text = $"ENEMY 2  {secondEnemyHP}/{secondEnemyMaxHP}";
            secondEnemyLabel.gameObject.SetActive(secondEnemyAlive);
        }
        RefreshEnemyHealthBar(
            secondEnemyHPFill,
            secondEnemyHP,
            secondEnemyMaxHP,
            secondEnemyAlive
        );
        if (thirdEnemy != null) thirdEnemy.enabled = thirdEnemyAlive;
        if (thirdEnemyLabel != null)
        {
            thirdEnemyLabel.text = $"ENEMY 3  {thirdEnemyHP}/{thirdEnemyMaxHP}";
            thirdEnemyLabel.gameObject.SetActive(thirdEnemyAlive);
        }
        RefreshEnemyHealthBar(
            thirdEnemyHPFill,
            thirdEnemyHP,
            thirdEnemyMaxHP,
            thirdEnemyAlive
        );


        if (playerHPFill != null)
        {
            playerHPFill.rectTransform.anchorMax =
                new Vector2(
                    PlayerHP / 100f,
                    1f
                );
        }


        RefreshEnemyHealthBar(
            enemyHPFill,
            enemyHP,
            enemyMaxHP,
            firstEnemyAlive
        );
    }


    private static void RefreshEnemyHealthBar(
        Image fill,
        int currentHealth,
        int maxHealth,
        bool visible)
    {
        if (fill == null)
            return;

        GameObject healthBar =
            fill.transform.parent != null
                ? fill.transform.parent.gameObject
                : fill.gameObject;

        healthBar.SetActive(visible);
        if (!visible)
            return;

        fill.rectTransform.anchorMax = new Vector2(
            (float)Mathf.Max(0, currentHealth) / Mathf.Max(1, maxHealth),
            1f
        );
    }


    // =========================================================
    // 체력바
    // =========================================================

    public IEnumerator Counterattack()
    {
        if (EnemyDefeated || PlayerDefeated) yield break;

        if (enemy != null && enemyHP > 0)
            yield return PerformEnemyAttack(enemyAttackDamage);

        if (!PlayerDefeated && secondEnemy != null && secondEnemyHP > 0)
            yield return PerformEnemyAttack(secondEnemyAttackDamage);

        if (!PlayerDefeated && thirdEnemy != null && thirdEnemyHP > 0)
            yield return PerformEnemyAttack(thirdEnemyAttackDamage);
    }

    private IEnumerator PerformEnemyAttack(int damage)
    {
        damage = Mathf.Max(0, damage);
        int healthBeforeAttack = PlayerHP;
        int absorbed = Mathf.Min(Shield, damage);
        Shield -= absorbed;
        PlayerHP = Mathf.Max(0, PlayerHP - (damage - absorbed));
        int healthDamage = healthBeforeAttack - PlayerHP;
        RefreshStats();

        if (damage > 0)
            ShakeCamera();

        if (absorbed > 0)
        {
            ShowFloatingCombatText(
                player,
                $"-{absorbed}",
                new Color(0.15f, 0.82f, 1f),
                28f
            );
        }

        if (healthDamage > 0)
        {
            ShowFloatingCombatText(
                player,
                $"-{healthDamage}",
                new Color(1f, 0.25f, 0.18f),
                -18f
            );
        }

        yield return new WaitForSecondsRealtime(0.25f);
    }
    public void AdvanceStage()
    {
        Hide();

        HUD.AdvanceStage();


        enemyMaxHP =
            100 +
            (HUD.Stage - 1) * 25;


        enemyHP =
            enemy != null && configuredEnemyCount >= 1 ? enemyMaxHP : 0;
        enemyAttackDamage = 10 + (HUD.Stage - 1) * 2;
        secondEnemyMaxHP = enemyMaxHP;
        secondEnemyHP =
            secondEnemy != null && configuredEnemyCount >= 2
                ? secondEnemyMaxHP
                : 0;
        secondEnemyAttackDamage = enemyAttackDamage;
        thirdEnemyMaxHP = enemyMaxHP;
        thirdEnemyHP =
            thirdEnemy != null && configuredEnemyCount >= 3
                ? thirdEnemyMaxHP
                : 0;
        thirdEnemyAttackDamage = enemyAttackDamage;


        RefreshStats();
    }


    // =========================================================
    // 캐릭터 위 UI 위치
    // =========================================================

    private void LateUpdate()
    {
        if (canvas == null)
            return;

        UpdateCharacterTapAreas();

        if (secondEnemyLabel != null)
            PositionLabel(secondEnemyLabel, secondEnemy, healthLabelOffset);

        if (thirdEnemyLabel != null)
            PositionLabel(thirdEnemyLabel, thirdEnemy, healthLabelOffset);


        PositionLabel(
            playerLabel,
            player,
            playerLabelOffset
        );


        PositionLabel(
            enemyLabel,
            enemy,
            healthLabelOffset
        );
    }


    private void PositionLabel(
        TMP_Text label,
        SpriteRenderer target,
        float offset)
    {
        if (
            label == null ||
            target == null ||
            Camera.main == null)
        {
            return;
        }


        Vector3 point =
            Camera.main.WorldToScreenPoint(
                target.bounds.center +
                Vector3.up *
                target.bounds.extents.y
            );


        point.y +=
            offset *
            canvas.scaleFactor;


        RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                (RectTransform)canvas.transform,

                point,

                canvas.renderMode ==
                RenderMode.ScreenSpaceOverlay
                    ? null
                    : canvas.worldCamera,

                out Vector2 local
            );


        label.rectTransform
            .anchoredPosition =
            local;
    }


    private void PositionHealthBar(
        Image fill,
        SpriteRenderer target,
        float offset)
    {
        if (
            fill == null ||
            fill.transform.parent == null ||
            target == null ||
            Camera.main == null)
        {
            return;
        }

        RectTransform track = fill.transform.parent as RectTransform;
        if (track == null)
            return;

        Vector3 point = Camera.main.WorldToScreenPoint(
            target.bounds.center + Vector3.up * target.bounds.extents.y
        );
        point.y += offset * canvas.scaleFactor;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)canvas.transform,
            point,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera,
            out Vector2 local
        );
        track.anchoredPosition = local;
    }


    // =========================================================
    // Finish
    // =========================================================

    public void Finish()
    {
        if (!accepting)
            return;


        accepting = false;
        Finished = true;

        ClearCards();
    }


    // =========================================================
    // Hide
    // =========================================================

    public void Hide()
    {
        accepting = false;

        ClearCards();

        hand.gameObject
            .SetActive(false);
    }


    // =========================================================
    // 카드 제거
    // =========================================================

    private void ClearCards()
    {
        foreach (
            var card in cards)
        {
            if (card != null)
            {
                Destroy(
                    card.gameObject
                );
            }
        }


        cards.Clear();


        foreach (
            var sprite in ownedSprites)
        {
            if (sprite != null)
            {
                Destroy(sprite);
            }
        }


        ownedSprites.Clear();
    }


    // =========================================================
    // Panel 생성
    // =========================================================

    private RectTransform Panel(
        string name,
        Transform parent,
        Color color)
    {
        var go =
            new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );


        go.layer =
            parent.gameObject.layer;


        go.transform.SetParent(
            parent,
            false
        );


        go.GetComponent<Image>()
            .color = color;


        return go
            .GetComponent<RectTransform>();
    }


    // =========================================================
    // Label 생성
    // =========================================================

    private TMP_Text Label(
        Transform parent,
        string text,
        int size)
    {
        var go =
            new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI)
            );


        go.layer =
            parent.gameObject.layer;


        go.transform.SetParent(
            parent,
            false
        );


        TMP_Text label =
            go.GetComponent<TMP_Text>();


        label.font = font;

        label.fontSize = size;

        label.color =
            Color.white;

        label.text =
            text;

        label.alignment =
            TextAlignmentOptions.Center;

        label.raycastTarget =
            false;


        label.rectTransform.anchorMin =
            Vector2.zero;

        label.rectTransform.anchorMax =
            Vector2.one;


        label.rectTransform.offsetMin =
            Vector2.zero;

        label.rectTransform.offsetMax =
            Vector2.zero;


        // Canvas 바로 아래에 만들어지는
        // 플레이어/적 정보 UI
        if (parent == canvas.transform)
        {
            label.rectTransform.anchorMin =
                new Vector2(
                    0.5f,
                    0.5f
                );


            label.rectTransform.anchorMax =
                new Vector2(
                    0.5f,
                    0.5f
                );
        }


        return label;
    }


    // =========================================================
    // Destroy
    // =========================================================

    private void OnDestroy()
    {
        if (Application.isPlaying) ClearCards();
    }
}
