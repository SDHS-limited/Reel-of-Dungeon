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
    private bool enemyUsesNormalRestStrongPattern;
    private int enemyStrongAttackDamage = 20;
    private int enemyPatternStep;
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
    [SerializeField] private Color playerDamageFlashColor = new Color(1f, 0.18f, 0.18f, 1f);

    [Header("Enemy Lunge Attack")]
    [Tooltip("플레이어에게 직접 다가가 공격할 적 스프라이트입니다.")]
    [SerializeField] private Sprite[] enemyLungeAttackSprites;
    [SerializeField, Min(0.05f)] private float enemyLungeApproachDuration = 0.3f;
    [SerializeField, Min(0f)] private float enemyLungeContactGap = 0.08f;
    [SerializeField, Min(0f)] private float enemyLungeImpactPause = 0.12f;
    [SerializeField, Min(0.05f)] private float enemyLungeReturnDuration = 0.28f;
    [SerializeField, Min(0f)] private float enemyAttackSequenceDelay = 0.12f;

    [Header("Enemy Rise And Slam Attack")]
    [Tooltip("위로 올라갔다 내려오며 공격할 적 스프라이트입니다.")]
    [SerializeField] private Sprite[] enemyRiseAttackSprites;
    [SerializeField, Min(0.1f)] private float enemyRiseHeight = 1.6f;
    [SerializeField, Min(0.05f)] private float enemyRiseDuration = 0.38f;
    [SerializeField, Min(0f)] private float enemyRiseHoldDuration = 0.12f;
    [SerializeField, Min(0.05f)] private float enemyFallDuration = 0.2f;

    [Header("Character Outlines")]
    [SerializeField, Range(1f, 1.25f)] private float characterOutlineScale = 1.07f;
    [SerializeField] private Color playerOutlineColor = new Color(0.2f, 0.85f, 1f, 1f);
    [SerializeField] private Color enemyOutlineColor = new Color(1f, 0.25f, 0.2f, 1f);

    [Header("Attack Animation")]
    [SerializeField, Min(0.1f)] private float wizardScale = 4.5f;
    [SerializeField, Min(0.1f)] private float wizardIdleScaleMultiplier = 1.2f;
    [SerializeField] private float wizardVerticalOffset = 0.15f;
    [SerializeField, Min(0.1f)] private float attackProjectileScale = 5f;
    [SerializeField, Min(0.01f)] private float attackFrameDuration = 0.055f;
    [SerializeField, Min(0.05f)] private float attackProjectileDuration = 0.28f;

    [Header("Spell Sounds")]
    [SerializeField] private AudioClip healSound;
    [SerializeField] private AudioClip thunderboltSound;
    [SerializeField] private AudioClip magicMissileSound;
    [SerializeField] private AudioClip fireballSound;
    [SerializeField] private AudioClip skeletonProjectileSound;
    [SerializeField, Range(0f, 1f)] private float spellSoundVolume = 0.9f;
    private AudioSource spellAudioSource;

    [Header("Fireball Projectile")]
    [SerializeField] private Sprite[] fireballProjectileFrames;
    [SerializeField, Min(0.05f)] private float fireballProjectileScale = 0.55f;

    [Header("Thunderbolt Effect")]
    [SerializeField] private Sprite[] thunderboltEffectFrames;
    [SerializeField, Min(0.1f)] private float thunderboltEffectScale = 1.45f;
    [SerializeField, Range(-0.5f, 1f)] private float thunderboltVerticalOffset = 0.35f;
    [SerializeField, Min(0.01f)] private float thunderboltFrameDuration = 0.07f;

    [Header("Mana Shield Effect")]
    [SerializeField] private Sprite[] manaShieldEffectFrames;
    [SerializeField, Min(0.1f)] private float manaShieldEffectScale = 1.08f;
    [SerializeField, Range(-0.5f, 0.5f)] private float manaShieldVerticalOffset = -0.08f;
    [SerializeField, Min(0.01f)] private float manaShieldFrameDuration = 0.08f;
    [SerializeField, Min(0f)] private float manaShieldHoldDuration = 0.16f;

    [Header("Healing Light Effect")]
    [SerializeField] private Sprite[] healingLightEffectFrames;
    [SerializeField, Min(0.1f)] private float healingLightEffectScale = 1.05f;
    [SerializeField, Range(-0.5f, 0.5f)] private float healingLightVerticalOffset = -0.05f;
    [SerializeField, Min(0.01f)] private float healingLightFrameDuration = 0.075f;
    [SerializeField, Min(0f)] private float healingLightHoldDuration = 0.1f;

    [Header("Skeleton Attack Animation")]
    [SerializeField] private Sprite skeletonIdleSprite;
    [SerializeField] private Sprite[] skeletonAttackFrames;
    [SerializeField, Min(0.01f)] private float skeletonAttackFrameDuration = 0.0833f;
    [SerializeField, Min(0.1f)] private float skeletonAttackHeightMultiplier = 1.8f;
    [Header("Skeleton Projectile")]
    [SerializeField] private Sprite[] skeletonProjectileFrames;
    [SerializeField, Min(0.05f)] private float skeletonProjectileDuration = 0.45f;
    [SerializeField, Min(0.05f)] private float skeletonProjectileHeight = 0.35f;
    [SerializeField, Range(0f, 1f)] private float skeletonProjectileSpawnHeight = 0.45f;

    private Sprite[] playerAttackFrames;
    private Sprite wizardIdleSprite;
    private Sprite[] attackProjectileFrames;
    private RuntimeAnimatorController playerAttackController;
    private int activePlayerAttackAnimations;
    private int pendingAttackImpacts;
    private Coroutine playerMagicFlashRoutine;
    private Coroutine manaShieldEffectRoutine;
    private GameObject manaShieldEffectObject;
    private Coroutine healingLightEffectRoutine;
    private GameObject healingLightEffectObject;
    private Color playerColorBeforeMagicFlash;

    private Canvas canvas;
    [SerializeField] private RectTransform hand;

    [SerializeField] private TMP_Text status;
    [SerializeField] private TMP_Text playerLabel;
    [SerializeField] private TMP_Text enemyLabel;

    [Header("Player Shield Status")]
    [SerializeField] private Sprite playerShieldIconSprite;
    [SerializeField, Min(12f)] private float playerShieldIconSize = 38f;
    [SerializeField] private float playerShieldRowYOffset = -24f;

    private Image playerShieldIcon;
    private TMP_Text playerShieldValue;

    private SpriteRenderer player;
    private SpriteRenderer enemy;

    private RectTransform playerTapArea;
    private RectTransform enemyTapArea;
    private RectTransform secondEnemyTapArea;
    private RectTransform thirdEnemyTapArea;

    private readonly Dictionary<SpriteRenderer, SpriteRenderer> characterOutlines =
        new Dictionary<SpriteRenderer, SpriteRenderer>();

    private readonly Dictionary<SpriteRenderer, Vector3> fixedEnemyLabelAnchors =
        new Dictionary<SpriteRenderer, Vector3>();

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
        EnsureSpellAudioSource();
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
        CreateCharacterOutlines();
        CreatePlayerShieldStatusUI();


        // =====================================================
        // 마법 결과 창
        // =====================================================

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

        RoomEnemySettings primarySettings =
            settings != null ? settings.enemy : null;
        enemyUsesNormalRestStrongPattern =
            primarySettings != null &&
            primarySettings.useNormalRestStrongPattern;
        enemyStrongAttackDamage =
            primarySettings != null && primarySettings.strongAttackDamage > 0
                ? primarySettings.strongAttackDamage
                : enemyAttackDamage * 2;
        enemyPatternStep = 0;

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


    private void CreateCharacterOutlines()
    {
        CreateCharacterOutline(player, "Player Outline", playerOutlineColor);
        CreateCharacterOutline(enemy, "Enemy Outline", enemyOutlineColor);
        CreateCharacterOutline(secondEnemy, "Enemy 2 Outline", enemyOutlineColor);
        CreateCharacterOutline(thirdEnemy, "Enemy 3 Outline", enemyOutlineColor);
    }


    private void CreateCharacterOutline(
        SpriteRenderer source,
        string outlineName,
        Color outlineColor)
    {
        if (source == null || characterOutlines.ContainsKey(source))
            return;

        GameObject outlineObject = new GameObject(outlineName);
        outlineObject.layer = source.gameObject.layer;
        outlineObject.transform.SetParent(source.transform, false);
        outlineObject.transform.localPosition = Vector3.zero;
        outlineObject.transform.localRotation = Quaternion.identity;
        outlineObject.transform.localScale = Vector3.one * characterOutlineScale;

        SpriteRenderer outline = outlineObject.AddComponent<SpriteRenderer>();
        outline.sprite = source.sprite;
        outline.color = outlineColor;
        outline.sharedMaterial = source.sharedMaterial;
        outline.sortingLayerID = source.sortingLayerID;
        outline.sortingOrder = source.sortingOrder - 1;
        outline.flipX = source.flipX;
        outline.flipY = source.flipY;
        outline.maskInteraction = source.maskInteraction;
        outline.spriteSortPoint = source.spriteSortPoint;

        characterOutlines.Add(source, outline);
    }


    private void SyncCharacterOutlines()
    {
        foreach (KeyValuePair<SpriteRenderer, SpriteRenderer> pair in characterOutlines)
        {
            SpriteRenderer source = pair.Key;
            SpriteRenderer outline = pair.Value;
            if (source == null || outline == null)
                continue;

            outline.enabled = source.enabled;
            outline.sprite = source.sprite;
            outline.sortingLayerID = source.sortingLayerID;
            outline.sortingOrder = source.sortingOrder - 1;
            outline.flipX = source.flipX;
            outline.flipY = source.flipY;
            outline.transform.localScale = Vector3.one * characterOutlineScale;
            outline.color = source == player ? playerOutlineColor : enemyOutlineColor;
        }
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


        int megaBonus = 0;

        foreach (SymbolResolution result in results)
        {
            if (
                result != null &&
                result.synergyTier == SynergyTier.Mega)
            {
                megaBonus += result.synergyBonus;
            }
        }

        status.text =
            megaBonus > 0
                ? $"MEGA BONUS! +{megaBonus}  ->  TAP THE GLOWING CHARACTER!"
                : "1ST CARD  ->  TAP THE GLOWING CHARACTER!";


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
                PlayAttackAnimation(
                    target,
                    value,
                    result.symbol.DisplayName
                );

                break;


            case SymbolType.Shield:

                Shield += value;
                ShowFloatingCombatText(
                    player,
                    $"+{value}",
                    new Color(0.15f, 0.82f, 1f)
                );
                if (value > 0)
                {
                    PlayPlayerMagicFlash(shieldFlashColor);

                    if (IsManaShieldSpell(result.symbol.DisplayName))
                        PlayManaShieldEffect();
                }

                break;


            case SymbolType.Heal:

                PlaySpellSound(healSound);

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

                if (
                    value > 0 &&
                    IsHealingLightSpell(result.symbol.DisplayName)
                )
                {
                    PlayHealingLightEffect();
                }

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
                {
                    PlayPlayerMagicFlash(playerDamageFlashColor);
                    ShakeCamera();
                }

                break;
        }


        cards.Remove(card);
        RefreshFirstCardHighlight();

        Destroy(
            card.gameObject
        );


        RefreshStats();

        HidePlayerAndShowDefeat();


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


    private void PlayAttackAnimation(
        SpriteRenderer target,
        int damage,
        string spellName)
    {
        PlayAttackSpellSound(spellName);

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
        StartCoroutine(
            PlayAttackAnimationRoutine(
                target,
                damage,
                spellName
            )
        );
    }


    private IEnumerator PlayAttackAnimationRoutine(
        SpriteRenderer target,
        int damage,
        string spellName)
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

        bool usesThunderboltEffect =
            IsThunderboltSpell(spellName) &&
            thunderboltEffectFrames != null &&
            thunderboltEffectFrames.Length > 0;

        if (usesThunderboltEffect)
        {
            yield return PlayThunderboltEffectRoutine(
                target,
                damage,
                sortingOrder + 1
            );
            yield break;
        }

        bool usesFireballProjectile =
            IsFireballSpell(spellName) &&
            fireballProjectileFrames != null &&
            fireballProjectileFrames.Length > 0;

        Sprite[] projectileFrames =
            usesFireballProjectile
                ? fireballProjectileFrames
                : attackProjectileFrames;

        if (projectileFrames == null || projectileFrames.Length == 0)
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
        // Firefly 원본은 불꽃 머리가 왼쪽을 향하므로 오른쪽 발사 시 반전한다.
        projectileRenderer.flipX =
            usesFireballProjectile
                ? direction.x > 0f
                : direction.x < 0f;
        projectileObject.transform.localScale = Vector3.one * (
            usesFireballProjectile
                ? fireballProjectileScale
                : attackProjectileScale
        );

        float elapsed = 0f;
        while (elapsed < attackProjectileDuration)
        {
            float progress = Mathf.Clamp01(elapsed / attackProjectileDuration);
            int frameIndex = Mathf.Min(
                projectileFrames.Length - 1,
                Mathf.FloorToInt(progress * projectileFrames.Length)
            );
            projectileRenderer.sprite = projectileFrames[frameIndex];
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
            projectileFrames[projectileFrames.Length - 1];
        ResolveAttackImpact(target, damage);
        yield return new WaitForSecondsRealtime(attackFrameDuration);
        Destroy(projectileObject);
    }


    private void EnsureSpellAudioSource()
    {
        if (spellAudioSource != null)
            return;

        spellAudioSource = gameObject.AddComponent<AudioSource>();
        spellAudioSource.playOnAwake = false;
        spellAudioSource.loop = false;
        spellAudioSource.spatialBlend = 0f;
    }


    private void PlaySpellSound(AudioClip clip)
    {
        if (clip == null)
            return;

        EnsureSpellAudioSource();
        spellAudioSource.PlayOneShot(clip, spellSoundVolume);
    }


    private void PlayAttackSpellSound(string spellName)
    {
        if (IsFireballSpell(spellName))
        {
            PlaySpellSound(fireballSound);
            return;
        }

        if (IsMagicMissileSpell(spellName))
            PlaySpellSound(magicMissileSound);
    }


    private static bool IsMagicMissileSpell(string spellName)
    {
        return string.Equals(
            spellName,
            "Magic Missile",
            StringComparison.OrdinalIgnoreCase
        );
    }


    private static bool IsFireballSpell(string spellName)
    {
        return string.Equals(
            spellName,
            "Fireball",
            StringComparison.OrdinalIgnoreCase
        );
    }


    private static bool IsThunderboltSpell(string spellName)
    {
        return string.Equals(
            spellName,
            "Thunderbolt",
            StringComparison.OrdinalIgnoreCase
        );
    }


    private IEnumerator PlayThunderboltEffectRoutine(
        SpriteRenderer target,
        int damage,
        int sortingOrder,
        bool damagesPlayer = false)
    {
        // 플레이어와 적의 번개 공격 모두 이 이펙트를 통과하므로
        // 여기서 한 번만 재생해 시각 효과와 소리를 맞춘다.
        PlaySpellSound(thunderboltSound);

        Vector3 targetCenter = target.bounds.center;
        float targetHeight = target.bounds.size.y;
        Vector3 effectPosition =
            targetCenter +
            Vector3.up * targetHeight * thunderboltVerticalOffset;

        GameObject effectObject = new GameObject("Thunderbolt Effect");
        SpriteRenderer effectRenderer =
            effectObject.AddComponent<SpriteRenderer>();

        effectRenderer.sortingLayerID = target.sortingLayerID;
        effectRenderer.sortingOrder = sortingOrder;
        effectObject.transform.position = effectPosition;

        float largestFrameHeight = 0f;
        foreach (Sprite frame in thunderboltEffectFrames)
        {
            if (frame != null)
            {
                largestFrameHeight = Mathf.Max(
                    largestFrameHeight,
                    frame.bounds.size.y
                );
            }
        }

        float effectScale =
            largestFrameHeight > 0f
                ? targetHeight * thunderboltEffectScale / largestFrameHeight
                : thunderboltEffectScale;

        effectObject.transform.localScale = Vector3.one * effectScale;

        int impactFrame = Mathf.Min(1, thunderboltEffectFrames.Length - 1);
        bool impactResolved = false;

        for (int i = 0; i < thunderboltEffectFrames.Length; i++)
        {
            Sprite frame = thunderboltEffectFrames[i];
            if (frame == null)
                continue;

            effectRenderer.sprite = frame;

            if (!impactResolved && i >= impactFrame)
            {
                ResolveThunderboltImpact(target, damage, damagesPlayer);
                impactResolved = true;
            }

            yield return new WaitForSecondsRealtime(thunderboltFrameDuration);
        }

        if (!impactResolved)
            ResolveThunderboltImpact(target, damage, damagesPlayer);

        Destroy(effectObject);
    }


    private void ResolveThunderboltImpact(
        SpriteRenderer target,
        int damage,
        bool damagesPlayer)
    {
        if (damagesPlayer)
            ApplyEnemyAttackDamage(damage);
        else
            ResolveAttackImpact(target, damage);
    }


    private static bool IsManaShieldSpell(string spellName)
    {
        return string.Equals(
            spellName,
            "Mana Shield",
            StringComparison.OrdinalIgnoreCase
        );
    }


    private static bool IsHealingLightSpell(string spellName)
    {
        return
            string.Equals(
                spellName,
                "Healing Light",
                StringComparison.OrdinalIgnoreCase
            ) ||
            string.Equals(
                spellName,
                "Healing_Light_c",
                StringComparison.OrdinalIgnoreCase
            ) ||
            string.Equals(
                spellName,
                "Dewdrop",
                StringComparison.OrdinalIgnoreCase
            );
    }


    private void PlayHealingLightEffect()
    {
        if (
            player == null ||
            healingLightEffectFrames == null ||
            healingLightEffectFrames.Length == 0)
        {
            return;
        }

        if (healingLightEffectRoutine != null)
            StopCoroutine(healingLightEffectRoutine);

        if (healingLightEffectObject != null)
            Destroy(healingLightEffectObject);

        healingLightEffectRoutine = StartCoroutine(
            PlayHealingLightEffectRoutine()
        );
    }


    private IEnumerator PlayHealingLightEffectRoutine()
    {
        healingLightEffectObject = new GameObject("Healing Light Effect");
        SpriteRenderer effectRenderer =
            healingLightEffectObject.AddComponent<SpriteRenderer>();

        effectRenderer.sortingLayerID = player.sortingLayerID;
        effectRenderer.sortingOrder = player.sortingOrder + 5;

        Sprite largestFrame =
            healingLightEffectFrames[healingLightEffectFrames.Length - 1];

        float frameHeight =
            largestFrame != null
                ? largestFrame.bounds.size.y
                : 0f;

        float effectScale =
            frameHeight > 0f
                ? player.bounds.size.y * healingLightEffectScale / frameHeight
                : healingLightEffectScale;

        healingLightEffectObject.transform.localScale =
            Vector3.one * effectScale;

        for (int i = 0; i < healingLightEffectFrames.Length; i++)
        {
            if (player == null)
                break;

            Sprite frame = healingLightEffectFrames[i];
            if (frame == null)
                continue;

            effectRenderer.sprite = frame;
            healingLightEffectObject.transform.position =
                player.bounds.center +
                Vector3.up * player.bounds.size.y * healingLightVerticalOffset;

            yield return new WaitForSecondsRealtime(healingLightFrameDuration);
        }

        if (healingLightHoldDuration > 0f)
            yield return new WaitForSecondsRealtime(healingLightHoldDuration);

        if (healingLightEffectObject != null)
            Destroy(healingLightEffectObject);

        healingLightEffectObject = null;
        healingLightEffectRoutine = null;
    }


    private void PlayManaShieldEffect()
    {
        if (
            player == null ||
            manaShieldEffectFrames == null ||
            manaShieldEffectFrames.Length == 0)
        {
            return;
        }

        if (manaShieldEffectRoutine != null)
            StopCoroutine(manaShieldEffectRoutine);

        if (manaShieldEffectObject != null)
            Destroy(manaShieldEffectObject);

        manaShieldEffectRoutine = StartCoroutine(
            PlayManaShieldEffectRoutine()
        );
    }


    private IEnumerator PlayManaShieldEffectRoutine()
    {
        manaShieldEffectObject = new GameObject("Mana Shield Effect");
        SpriteRenderer effectRenderer =
            manaShieldEffectObject.AddComponent<SpriteRenderer>();

        effectRenderer.sortingLayerID = player.sortingLayerID;
        effectRenderer.sortingOrder = player.sortingOrder + 4;

        Sprite largestFrame =
            manaShieldEffectFrames[manaShieldEffectFrames.Length - 1];

        float frameHeight =
            largestFrame != null
                ? largestFrame.bounds.size.y
                : 0f;

        float effectScale =
            frameHeight > 0f
                ? player.bounds.size.y * manaShieldEffectScale / frameHeight
                : manaShieldEffectScale;

        manaShieldEffectObject.transform.localScale =
            Vector3.one * effectScale;

        for (int i = 0; i < manaShieldEffectFrames.Length; i++)
        {
            if (player == null)
                break;

            effectRenderer.sprite = manaShieldEffectFrames[i];
            manaShieldEffectObject.transform.position =
                player.bounds.center +
                Vector3.up * player.bounds.size.y * manaShieldVerticalOffset;

            yield return new WaitForSecondsRealtime(manaShieldFrameDuration);
        }

        if (manaShieldHoldDuration > 0f)
            yield return new WaitForSecondsRealtime(manaShieldHoldDuration);

        if (manaShieldEffectObject != null)
            Destroy(manaShieldEffectObject);

        manaShieldEffectObject = null;
        manaShieldEffectRoutine = null;
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
            $"PLAYER  {PlayerHP}/100\n\n";

        if (playerShieldValue != null)
            playerShieldValue.text = Shield.ToString();


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


    private void CreatePlayerShieldStatusUI()
    {
        if (playerLabel == null)
            return;

        if (playerShieldIcon == null)
        {
            GameObject iconObject = new GameObject(
                "Shield Icon",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );

            iconObject.transform.SetParent(playerLabel.transform, false);
            playerShieldIcon = iconObject.GetComponent<Image>();
            playerShieldIcon.sprite = playerShieldIconSprite;
            playerShieldIcon.preserveAspect = true;
            playerShieldIcon.raycastTarget = false;

            RectTransform iconRect = playerShieldIcon.rectTransform;
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = Vector2.one * playerShieldIconSize;
            iconRect.anchoredPosition = new Vector2(
                -22f,
                playerShieldRowYOffset
            );
        }

        if (playerShieldValue == null)
        {
            GameObject valueObject = new GameObject(
                "Shield Value",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI)
            );

            valueObject.transform.SetParent(playerLabel.transform, false);
            playerShieldValue = valueObject.GetComponent<TextMeshProUGUI>();
            playerShieldValue.font = font != null ? font : playerLabel.font;
            playerShieldValue.fontSize = playerLabel.fontSize * 0.9f;
            playerShieldValue.color = playerLabel.color;
            playerShieldValue.alignment = TextAlignmentOptions.Left;
            playerShieldValue.raycastTarget = false;

            RectTransform valueRect = playerShieldValue.rectTransform;
            valueRect.anchorMin = new Vector2(0.5f, 0.5f);
            valueRect.anchorMax = new Vector2(0.5f, 0.5f);
            valueRect.pivot = new Vector2(0.5f, 0.5f);
            valueRect.sizeDelta = new Vector2(60f, playerShieldIconSize + 8f);
            valueRect.anchoredPosition = new Vector2(
                26f,
                playerShieldRowYOffset
            );
        }
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
            yield return PerformPrimaryEnemyTurn();

        if (!PlayerDefeated && secondEnemy != null && secondEnemyHP > 0)
            yield return PerformEnemyAttack(secondEnemy, secondEnemyAttackDamage);

        if (!PlayerDefeated && thirdEnemy != null && thirdEnemyHP > 0)
            yield return PerformEnemyAttack(thirdEnemy, thirdEnemyAttackDamage);
    }


    private IEnumerator PerformPrimaryEnemyTurn()
    {
        if (!enemyUsesNormalRestStrongPattern)
        {
            yield return PerformEnemyAttack(enemy, enemyAttackDamage);
            yield break;
        }

        switch (enemyPatternStep)
        {
            case 0:
                yield return PerformEnemyAttack(enemy, enemyAttackDamage);
                break;

            case 1:
                ShowFloatingCombatText(
                    enemy,
                    "...",
                    new Color(0.75f, 0.8f, 0.9f),
                    18f
                );
                yield return new WaitForSecondsRealtime(0.35f);
                break;

            default:
                ShowFloatingCombatText(
                    enemy,
                    "!!",
                    new Color(1f, 0.55f, 0.12f),
                    18f
                );
                yield return new WaitForSecondsRealtime(0.2f);
                yield return PerformEnemyAttack(
                    enemy,
                    enemyStrongAttackDamage,
                    true
                );
                break;
        }

        enemyPatternStep = (enemyPatternStep + 1) % 3;
    }

    private IEnumerator PerformEnemyAttack(
        SpriteRenderer attacker,
        int damage,
        bool usesThunderbolt = false)
    {
        damage = Mathf.Max(0, damage);

        if (CanPlaySkeletonAttack(attacker))
        {
            yield return PlaySkeletonAttackAnimation(
                attacker,
                damage,
                usesThunderbolt
            );
            yield break;
        }

        if (usesThunderbolt)
        {
            yield return PlayEnemyThunderboltAttack(attacker, damage);
            yield break;
        }

        if (CanPlayEnemyRiseAttack(attacker))
        {
            yield return PlayEnemyRiseAttack(attacker, damage);
            yield break;
        }

        if (CanPlayEnemyLungeAttack(attacker))
        {
            yield return PlayEnemyLungeAttack(attacker, damage);
            yield break;
        }

        ApplyEnemyAttackDamage(damage);
        yield return new WaitForSecondsRealtime(0.25f);
    }


    private bool CanPlayEnemyRiseAttack(SpriteRenderer attacker)
    {
        if (
            attacker == null ||
            attacker.sprite == null ||
            enemyRiseAttackSprites == null)
        {
            return false;
        }

        foreach (Sprite riseSprite in enemyRiseAttackSprites)
        {
            if (riseSprite == attacker.sprite)
                return true;
        }

        return false;
    }


    private IEnumerator PlayEnemyRiseAttack(
        SpriteRenderer attacker,
        int damage)
    {
        if (attacker == null)
        {
            ApplyEnemyAttackDamage(damage);
            yield break;
        }

        Transform attackerTransform = attacker.transform;
        Vector3 originalPosition = attackerTransform.position;
        Vector3 raisedPosition =
            originalPosition + Vector3.up * Mathf.Max(0.1f, enemyRiseHeight);
        Bounds originalBounds = attacker.bounds;

        fixedEnemyLabelAnchors[attacker] =
            originalBounds.center + Vector3.up * originalBounds.extents.y;

        try
        {
            yield return MoveEnemyForAttack(
                attackerTransform,
                originalPosition,
                raisedPosition,
                enemyRiseDuration
            );

            if (enemyRiseHoldDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    enemyRiseHoldDuration
                );
            }

            yield return MoveEnemyForAttack(
                attackerTransform,
                raisedPosition,
                originalPosition,
                enemyFallDuration
            );

            // 원래 위치로 착지하는 순간 플레이어에게 피해를 준다.
            ApplyEnemyAttackDamage(damage);

            if (enemyLungeImpactPause > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    enemyLungeImpactPause
                );
            }
        }
        finally
        {
            if (attackerTransform != null)
                attackerTransform.position = originalPosition;

            fixedEnemyLabelAnchors.Remove(attacker);
        }

        if (enemyAttackSequenceDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(
                enemyAttackSequenceDelay
            );
        }
    }


    private bool CanPlayEnemyLungeAttack(SpriteRenderer attacker)
    {
        if (
            attacker == null ||
            attacker.sprite == null ||
            enemyLungeAttackSprites == null)
        {
            return false;
        }

        foreach (Sprite lungeSprite in enemyLungeAttackSprites)
        {
            if (lungeSprite == attacker.sprite)
                return true;
        }

        return false;
    }


    private IEnumerator PlayEnemyLungeAttack(
        SpriteRenderer attacker,
        int damage)
    {
        if (attacker == null || player == null)
        {
            ApplyEnemyAttackDamage(damage);
            yield break;
        }

        Transform attackerTransform = attacker.transform;
        Vector3 originalPosition = attackerTransform.position;
        Bounds originalBounds = attacker.bounds;

        fixedEnemyLabelAnchors[attacker] =
            originalBounds.center + Vector3.up * originalBounds.extents.y;

        float attackSide = Mathf.Sign(
            originalBounds.center.x - player.bounds.center.x
        );
        if (Mathf.Approximately(attackSide, 0f))
            attackSide = 1f;

        float contactCenterX =
            player.bounds.center.x +
            attackSide * (
                player.bounds.extents.x +
                originalBounds.extents.x +
                Mathf.Max(0f, enemyLungeContactGap)
            );

        Vector3 contactPosition = originalPosition;
        contactPosition.x += contactCenterX - originalBounds.center.x;

        try
        {
            yield return MoveEnemyForAttack(
                attackerTransform,
                originalPosition,
                contactPosition,
                enemyLungeApproachDuration
            );

            // 플레이어 바로 앞에 도착한 순간 피해를 적용한다.
            ApplyEnemyAttackDamage(damage);

            if (enemyLungeImpactPause > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    enemyLungeImpactPause
                );
            }

            yield return MoveEnemyForAttack(
                attackerTransform,
                contactPosition,
                originalPosition,
                enemyLungeReturnDuration
            );
        }
        finally
        {
            if (attackerTransform != null)
                attackerTransform.position = originalPosition;

            fixedEnemyLabelAnchors.Remove(attacker);
        }

        if (enemyAttackSequenceDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(
                enemyAttackSequenceDelay
            );
        }
    }


    private static IEnumerator MoveEnemyForAttack(
        Transform target,
        Vector3 start,
        Vector3 destination,
        float duration)
    {
        duration = Mathf.Max(0.05f, duration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = progress * progress * (3f - 2f * progress);
            target.position = Vector3.LerpUnclamped(
                start,
                destination,
                easedProgress
            );
            yield return null;
        }

        target.position = destination;
    }


    private bool CanPlaySkeletonAttack(SpriteRenderer attacker)
    {
        return
            attacker != null &&
            attacker.sprite == skeletonIdleSprite &&
            skeletonAttackFrames != null &&
            skeletonAttackFrames.Length > 0;
    }


    private IEnumerator PlaySkeletonAttackAnimation(
        SpriteRenderer attacker,
        int damage,
        bool usesThunderbolt)
    {
        Sprite originalSprite = attacker.sprite;
        Vector3 originalPosition = attacker.transform.position;
        Vector3 originalScale = attacker.transform.localScale;
        Bounds originalBounds = attacker.bounds;

        fixedEnemyLabelAnchors[attacker] =
            originalBounds.center + Vector3.up * originalBounds.extents.y;

        float largestFrameHeight = 0.001f;
        foreach (Sprite frame in skeletonAttackFrames)
        {
            if (frame != null)
                largestFrameHeight = Mathf.Max(largestFrameHeight, frame.bounds.size.y);
        }

        float originalHeight = Mathf.Max(
            0.001f,
            originalSprite.bounds.size.y
        );
        float frameScale =
            originalHeight * skeletonAttackHeightMultiplier / largestFrameHeight;

        for (int i = 0; i < skeletonAttackFrames.Length; i++)
        {
            Sprite frame = skeletonAttackFrames[i];
            if (frame == null)
                continue;

            attacker.sprite = frame;

            attacker.transform.localScale = new Vector3(
                originalScale.x * frameScale,
                originalScale.y * frameScale,
                originalScale.z
            );
            attacker.transform.position = originalPosition;

            Bounds frameBounds = attacker.bounds;
            attacker.transform.position += new Vector3(
                originalBounds.center.x - frameBounds.center.x,
                originalBounds.min.y - frameBounds.min.y,
                0f
            );
            SnapTransformToSpritePixels(attacker.transform, frame);


            yield return new WaitForSecondsRealtime(
                skeletonAttackFrameDuration
            );
        }

        attacker.sprite = originalSprite;
        attacker.transform.localScale = originalScale;
        attacker.transform.position = originalPosition;
        fixedEnemyLabelAnchors.Remove(attacker);

        if (usesThunderbolt)
            yield return PlayEnemyThunderboltAttack(attacker, damage);
        else
            yield return PlaySkeletonProjectile(attacker, damage);
    }


    private IEnumerator PlayEnemyThunderboltAttack(
        SpriteRenderer attacker,
        int damage)
    {
        if (player == null)
            yield break;

        if (
            thunderboltEffectFrames == null ||
            thunderboltEffectFrames.Length == 0)
        {
            ApplyEnemyAttackDamage(damage);
            yield break;
        }

        int sortingOrder =
            Mathf.Max(
                attacker != null ? attacker.sortingOrder : 0,
                player.sortingOrder
            ) + 6;

        yield return PlayThunderboltEffectRoutine(
            player,
            damage,
            sortingOrder,
            true
        );
    }


    private IEnumerator PlaySkeletonProjectile(SpriteRenderer attacker, int damage)
    {
        if (player == null) yield break;
        if (skeletonProjectileFrames == null || skeletonProjectileFrames.Length == 0)
        {
            ApplyEnemyAttackDamage(damage);
            yield break;
        }

        PlaySpellSound(skeletonProjectileSound);

        GameObject projectile = new GameObject("Skeleton Projectile");
        SpriteRenderer visual = projectile.AddComponent<SpriteRenderer>();
        visual.sortingLayerID = attacker.sortingLayerID;
        visual.sortingOrder = Mathf.Max(attacker.sortingOrder, player.sortingOrder) + 6;
        Vector3 destination = player.bounds.center;
        Vector3 attackerCenter = attacker.bounds.center;
        Vector3 towardPlayer = (destination - attackerCenter).normalized;
        Vector3 start =
            attackerCenter +
            Vector3.up * attacker.bounds.size.y * skeletonProjectileSpawnHeight +
            towardPlayer * attacker.bounds.size.x * 0.2f;
        float largestHeight = 0.001f;
        foreach (Sprite frame in skeletonProjectileFrames)
            if (frame != null) largestHeight = Mathf.Max(largestHeight, frame.bounds.size.y);
        projectile.transform.localScale = Vector3.one *
            (attacker.bounds.size.y * skeletonProjectileHeight / largestHeight);
        float duration = Mathf.Max(0.05f, skeletonProjectileDuration);
        float elapsed = 0f;
        try
        {
            while (elapsed < duration && player != null)
            {
                float progress = Mathf.Clamp01(elapsed / duration);
                destination = player.bounds.center;
                Vector3 direction = destination - start;
                float directionAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                // 원본 스프라이트의 뾰족한 끝(왼쪽 아래)이 플레이어를 향하게 한다.
                projectile.transform.rotation = Quaternion.Euler(0f, 0f, directionAngle + 135f);
                int index = Mathf.Min(skeletonProjectileFrames.Length - 1,
                    Mathf.FloorToInt(progress * skeletonProjectileFrames.Length));
                visual.sprite = skeletonProjectileFrames[index];
                projectile.transform.position = Vector3.Lerp(start, destination, progress);
                SnapTransformToSpritePixels(projectile.transform, visual.sprite);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            if (player != null)
            {
                projectile.transform.position = player.bounds.center;
                ApplyEnemyAttackDamage(damage);
            }
        }
        finally
        {
            if (projectile != null) Destroy(projectile);
        }
        yield return new WaitForSecondsRealtime(0.1f);
    }


    private static void SnapTransformToSpritePixels(
        Transform targetTransform,
        Sprite sprite)
    {
        if (targetTransform == null || sprite == null || sprite.pixelsPerUnit <= 0f)
            return;

        Vector3 position = targetTransform.position;
        Vector3 scale = targetTransform.lossyScale;
        float pixelSizeX = Mathf.Abs(scale.x) / sprite.pixelsPerUnit;
        float pixelSizeY = Mathf.Abs(scale.y) / sprite.pixelsPerUnit;

        if (pixelSizeX > 0.00001f)
            position.x = Mathf.Round(position.x / pixelSizeX) * pixelSizeX;
        if (pixelSizeY > 0.00001f)
            position.y = Mathf.Round(position.y / pixelSizeY) * pixelSizeY;

        targetTransform.position = position;
    }


    private void ApplyEnemyAttackDamage(int damage)
    {
        int healthBeforeAttack = PlayerHP;
        int absorbed = Mathf.Min(Shield, damage);
        Shield -= absorbed;
        PlayerHP = Mathf.Max(0, PlayerHP - (damage - absorbed));
        int healthDamage = healthBeforeAttack - PlayerHP;
        RefreshStats();
        HidePlayerAndShowDefeat();

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
            PlayPlayerMagicFlash(playerDamageFlashColor);
            ShowFloatingCombatText(
                player,
                $"-{healthDamage}",
                new Color(1f, 0.25f, 0.18f),
                -18f
            );
        }

    }

    private void HidePlayerAndShowDefeat()
    {
        if (!PlayerDefeated)
            return;

        if (player != null)
            player.gameObject.SetActive(false);
        if (playerLabel != null)
            playerLabel.gameObject.SetActive(false);
        if (playerHPFill != null)
            playerHPFill.gameObject.SetActive(false);
        if (playerTapArea != null)
            playerTapArea.gameObject.SetActive(false);

        HUD?.ShowDefeat();
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
        SyncCharacterOutlines();

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


        Vector3 worldAnchor =
            target.bounds.center +
            Vector3.up *
            target.bounds.extents.y;

        if (fixedEnemyLabelAnchors.TryGetValue(target, out Vector3 fixedAnchor))
            worldAnchor = fixedAnchor;

        Vector3 point =
            Camera.main.WorldToScreenPoint(worldAnchor);


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
