using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Canvas에 배치한 결과창과 카드 템플릿을 사용합니다.
// CanvasGroup은 사용하지 않는다.
public class SpellResultHand : MonoBehaviour
{
    public bool Finished { get; private set; }

    public bool EnemyDefeated => enemyHP <= 0 && (secondEnemy == null || secondEnemyHP <= 0);
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
    private SpriteRenderer secondEnemy;
    [SerializeField] private TMP_Text secondEnemyLabel;
    [SerializeField] private Image secondEnemyHPFill;

    [SerializeField] private Image playerHPFill;
    [SerializeField] private Image enemyHPFill;


    [SerializeField] private BattleHUD battleHUD;
    [SerializeField] private SpellResultCard cardTemplate;
    [SerializeField] private Button endTurnButton;
    [SerializeField, Min(0f)] private float healthLabelOffset = 100f;

    private Canvas canvas;
    [SerializeField] private RectTransform hand;

    [SerializeField] private TMP_Text status;
    [SerializeField] private TMP_Text playerLabel;
    [SerializeField] private TMP_Text enemyLabel;

    private SpriteRenderer player;
    private SpriteRenderer enemy;

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


        // =====================================================
        // 캐릭터 찾기
        // =====================================================

        player =
            GameObject.Find("Player")
            ?.GetComponent<SpriteRenderer>();

        enemy =
            GameObject.Find("Enemy")
            ?.GetComponent<SpriteRenderer>();
        // Unity 복제 기본 이름을 우선 사용하고 이전 이름도 지원한다.
        secondEnemy = GameObject.Find("Enemy (1)")?.GetComponent<SpriteRenderer>();
        if (secondEnemy == null)
            secondEnemy = GameObject.Find("Enemy 2")?.GetComponent<SpriteRenderer>();


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

        ConfigureEnemy(
            enemy,
            settings != null ? settings.enemy : null,
            fallbackHealth,
            fallbackAttack,
            out enemyMaxHP,
            out enemyHP,
            out enemyAttackDamage
        );

        ConfigureEnemy(
            secondEnemy,
            settings != null ? settings.enemy2 : null,
            fallbackHealth,
            fallbackAttack,
            out secondEnemyMaxHP,
            out secondEnemyHP,
            out secondEnemyAttackDamage
        );
    }


    private static void ConfigureEnemy(
        SpriteRenderer renderer,
        RoomEnemySettings settings,
        int fallbackHealth,
        int fallbackAttack,
        out int maxHealth,
        out int health,
        out int attackDamage)
    {
        bool placed = renderer != null && (settings == null || settings.enabled);

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
            "Attack: ENEMY / Heal & Shield: PLAYER";


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
                if (target == secondEnemy)
                    secondEnemyHP = Mathf.Max(0, secondEnemyHP - value);
                else
                    enemyHP = Mathf.Max(0, enemyHP - value);

                break;


            case SymbolType.Shield:

                Shield += value;

                break;


            case SymbolType.Heal:

                PlayerHP =
                    Mathf.Min(
                        100,
                        PlayerHP + value
                    );

                break;


            case SymbolType.Gold:

                Gold += value;

                break;


            case SymbolType.Skull:

                PlayerHP =
                    Mathf.Max(
                        0,
                        PlayerHP - value
                    );

                break;
        }


        cards.Remove(card);

        Destroy(
            card.gameObject
        );


        RefreshStats();


        status.text =
            result.symbol.DisplayName +
            "  " +
            value;


        StartCoroutine(
            Flash(target)
        );


        onCast?.Invoke(result);


        if (
            cards.Count == 0 ||
            EnemyDefeated ||
            PlayerDefeated)
        {
            Finished = true;
            accepting = false;


            if (EnemyDefeated)
                status.text = "VICTORY!";


            if (PlayerDefeated)
                status.text = "DEFEAT";
        }


        return true;
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
    // Hit Effect
    // =========================================================

    private IEnumerator Flash(
        SpriteRenderer target)
    {
        TMP_Text pulse = Label(
            canvas.transform,
            "*",
            80
        );


        pulse.color =
            new Color(
                1f,
                0.8f,
                0.3f
            );


        PositionLabel(
            pulse,
            target,
            0f
        );


        yield return
            new WaitForSecondsRealtime(
                0.3f
            );


        if (pulse != null)
        {
            Destroy(
                pulse.gameObject
            );
        }
    }


    // =========================================================
    // 상태 UI 갱신
    // =========================================================

    private SpriteRenderer FindAttackTarget(Vector2 position)
    {
        if (enemy != null && enemyHP > 0 && HitsTarget(enemy, position)) return enemy;
        if (secondEnemy != null && secondEnemyHP > 0 && HitsTarget(secondEnemy, position)) return secondEnemy;
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
        if (enemy != null) enemy.enabled = firstEnemyAlive;
        if (enemyLabel != null) enemyLabel.gameObject.SetActive(firstEnemyAlive);
        if (secondEnemy != null) secondEnemy.enabled = secondEnemyAlive;
        if (secondEnemyLabel != null)
        {
            secondEnemyLabel.text = $"ENEMY 2  {secondEnemyHP}/{secondEnemyMaxHP}";
            secondEnemyLabel.gameObject.SetActive(secondEnemyAlive);
        }
        if (secondEnemyHPFill != null)
            secondEnemyHPFill.rectTransform.anchorMax = new Vector2((float)secondEnemyHP / secondEnemyMaxHP, 1f);


        if (playerHPFill != null)
        {
            playerHPFill.rectTransform.anchorMax =
                new Vector2(
                    PlayerHP / 100f,
                    1f
                );
        }


        if (enemyHPFill != null)
        {
            enemyHPFill.rectTransform.anchorMax =
                new Vector2(
                    (float)enemyHP /
                    enemyMaxHP,
                    1f
                );
        }
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
    }

    private IEnumerator PerformEnemyAttack(int damage)
    {
        damage = Mathf.Max(0, damage);
        int absorbed = Mathf.Min(Shield, damage);
        Shield -= absorbed;
        PlayerHP = Mathf.Max(0, PlayerHP - (damage - absorbed));
        RefreshStats();

        if (player != null)
            yield return Flash(player);

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
            enemy != null ? enemyMaxHP : 0;
        enemyAttackDamage = 10 + (HUD.Stage - 1) * 2;
        secondEnemyMaxHP = enemyMaxHP;
        secondEnemyHP = secondEnemy != null ? secondEnemyMaxHP : 0;
        secondEnemyAttackDamage = enemyAttackDamage;


        RefreshStats();
    }


    // =========================================================
    // 캐릭터 위 UI 위치
    // =========================================================

    private void LateUpdate()
    {
        if (canvas == null)
            return;
        if (secondEnemyLabel != null)
            PositionLabel(secondEnemyLabel, secondEnemy, healthLabelOffset);


        PositionLabel(
            playerLabel,
            player,
            healthLabelOffset
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
