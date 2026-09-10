using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum SynergyTier
{
    None,
    Combo,
    Mega
}

[System.Serializable]
public class SymbolResolution
{
    public ReelSymbolData symbol;
    public int matchCount;
    public int baseTotal;
    public int synergyBonus;
    public int totalValue;
    public SynergyTier synergyTier;
}

public class SlotMachineController : MonoBehaviour
{
    public event System.Action<IReadOnlyList<SymbolResolution>> SpinResolved;
    public event System.Action<ReelSymbolData> JackpotAnticipationStarted;

    [Header("Reels")]
    [SerializeField] private SlotReel[] reels;

    [Header("Symbols")]
    [SerializeField] private List<ReelSymbolData> symbolList;

    [Header("Spin Settings")]
    [SerializeField] private float baseSpinDuration = 1.0f; // 첫 번째 릴 멈출 때까지의 시간
    [SerializeField] private float reelInterval = 0.35f;     // 각 릴 간의 정지 간격
    [SerializeField] private float jackpotAnticipationDelay = 1.2f;
    [SerializeField] private float jackpotAnticipationDelay2 = 0.5f;

    [Header("UI")]
    [SerializeField] private Button spinButton;

    [Header("Turn UI")]
    [SerializeField] private TMP_Text currentTurnText;
    // 기존 씬에 연결된 개별 카운터는 공용 표시로 이전한 뒤 숨긴다.
    [SerializeField, HideInInspector] private TMP_Text playerTurnCountText;
    [SerializeField, HideInInspector] private TMP_Text enemyTurnCountText;
    [SerializeField, Min(0f)] private float enemyTurnDisplayDuration = 1f;

    public int PlayerTurnCount { get; private set; }
    public int EnemyTurnCount { get; private set; }
    public bool IsPlayerTurn { get; private set; }

    private bool isBusy = false;

    private void Start()
    {
        if (!ValidateSetup()) return;

        EnsureTurnUI();
        BeginPlayerTurn();

        foreach (var reel in reels)
        {
            reel.Init(symbolList);
        }

        if (spinButton != null)
        {
            spinButton.onClick.AddListener(OnSpinButtonClicked);
        }
    }

    public void OnSpinButtonClicked()
    {
        if (isBusy || !IsPlayerTurn || !isActiveAndEnabled || !ValidateSetup()) return;
        StartCoroutine(ExecuteSlotRoutine());
    }

    private IEnumerator ExecuteSlotRoutine()
    {
        isBusy = true;
        if (spinButton != null)
            spinButton.interactable = false;

        // 결과를 먼저 확정해 모든 심볼을 한 턴에 동시에 판정한다.
        List<ReelSymbolData> finalResults = new List<ReelSymbolData>(reels.Length);
        for (int i = 0; i < reels.Length; i++)
            finalResults.Add(PickSymbolByWeight());

        // 1. 모든 릴 회전 시작
        for (int i = 0; i < reels.Length; i++)
        {
            reels[i].StartSpin();
        }

        yield return new WaitForSeconds(baseSpinDuration);


        // 2. 가중치 기반으로 결과 미리 추첨 및 순차 정지
        for (int i = 0; i < reels.Length; i++)
        {
            if (i == reels.Length - 1 && TryGetJackpotCandidate(finalResults, i, out ReelSymbolData candidate))
            {
                Debug.Log($"🎰 {candidate.DisplayName} 메가 보너스까지 하나 남았습니다! 마지막 릴 공개를 늦춥니다.");
                JackpotAnticipationStarted?.Invoke(candidate);
                yield return new WaitForSeconds(jackpotAnticipationDelay);
            }
            
            if(i == reels.Length - 2 )
            {
                yield return new WaitForSeconds(jackpotAnticipationDelay);
            }
            if(i == reels.Length - 1)
            {
                yield return new WaitForSeconds(jackpotAnticipationDelay2);
            }


            yield return reels[i].StopSpin(finalResults[i]);

            if (i < reels.Length - 1)
                yield return new WaitForSeconds(reelInterval);
        }

        // 3. 결과 판정 및 스킬 발동
        EvaluateTurnResults(finalResults);

        IsPlayerTurn = false;
        EnemyTurnCount++;
        RefreshTurnUI();
        // 적 행동 시스템을 연결하기 전까지 턴 표시 시간을 확보한다.
        yield return new WaitForSeconds(Mathf.Max(0f, enemyTurnDisplayDuration));
        BeginPlayerTurn();

        isBusy = false;
        if (spinButton != null)
            spinButton.interactable = true;
    }

    private void BeginPlayerTurn()
    {
        IsPlayerTurn = true;
        PlayerTurnCount++;
        RefreshTurnUI();
    }

    private void RefreshTurnUI()
    {
        if (currentTurnText != null)
        {
            currentTurnText.text = IsPlayerTurn
                ? $"Player Turn: {PlayerTurnCount}"
                : $"Enemy Turn: {EnemyTurnCount}";
            currentTurnText.color = IsPlayerTurn
                ? new Color(0.4f, 0.8f, 1f)
                : new Color(1f, 0.45f, 0.4f);
        }

    }

    private void EnsureTurnUI()
    {
        if (currentTurnText == null)
            currentTurnText = playerTurnCountText != null ? playerTurnCountText : enemyTurnCountText;
        if (playerTurnCountText != null && playerTurnCountText != currentTurnText)
            playerTurnCountText.enabled = false;
        if (enemyTurnCountText != null && enemyTurnCountText != currentTurnText)
            enemyTurnCountText.enabled = false;
        if (currentTurnText != null)
        {
            currentTurnText.enabled = true;
            return;
        }

        Canvas canvas = spinButton != null ? spinButton.GetComponentInParent<Canvas>() : null;
        if (canvas == null && reels.Length > 0)
            canvas = reels[0].GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("턴 텍스트를 표시할 Canvas를 찾지 못했습니다. Turn UI 참조를 연결하세요.", this);
            return;
        }

        if (currentTurnText == null)
            currentTurnText = CreateTurnText(canvas.transform, "Current Turn", new Vector2(0f, -35f), 32);
    }

    private TMP_Text CreateTurnText(Transform parent, string objectName, Vector2 position, int fontSize)
    {
        GameObject label = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        label.layer = parent.gameObject.layer;
        label.transform.SetParent(parent, false);
        TMP_Text text = label.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        RectTransform rect = text.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(320f, 46f);
        rect.anchoredPosition = position;
        return text;
    }

    private ReelSymbolData PickSymbolByWeight()
    {
        int totalWeight = 0;
        foreach (var s in symbolList)
        {
            if (s != null)
                totalWeight += Mathf.Max(0, s.weight);
        }

        if (totalWeight <= 0)
            return null;

        int randomVal = Random.Range(0, totalWeight);
        int currentSum = 0;

        foreach (var s in symbolList)
        {
            if (s == null) continue;

            currentSum += Mathf.Max(0, s.weight);
            if (randomVal < currentSum) return s;
        }

        return null;
    }

    private bool TryGetJackpotCandidate(
        IReadOnlyList<ReelSymbolData> results,
        int unrevealedIndex,
        out ReelSymbolData candidate)
    {
        candidate = null;
        Dictionary<string, int> revealedCounts = new Dictionary<string, int>();
        Dictionary<string, ReelSymbolData> symbolsByKey = new Dictionary<string, ReelSymbolData>();

        for (int i = 0; i < unrevealedIndex; i++)
        {
            ReelSymbolData symbol = results[i];
            if (symbol == null) continue;

            string key = GetSymbolMatchKey(symbol);
            if (!revealedCounts.ContainsKey(key))
            {
                revealedCounts[key] = 0;
                symbolsByKey[key] = symbol;
            }

            revealedCounts[key]++;
        }

        foreach (var pair in revealedCounts)
        {
            if (pair.Value != 4) continue;

            candidate = symbolsByKey[pair.Key];
            return true;
        }

        return false;
    }

    private void EvaluateTurnResults(List<ReelSymbolData> results)
    {
        // 같은 타입이 아니라 같은 마법 이름이 나온 횟수를 센다.
        // 상점에서 같은 마법을 여러 개 보유해 별도 항목으로 저장해도 하나의 조합으로 묶인다.
        Dictionary<string, int> counts = new Dictionary<string, int>();
        Dictionary<string, ReelSymbolData> symbolsByKey = new Dictionary<string, ReelSymbolData>();
        foreach (var r in results)
        {
            if (r == null) continue;

            string key = GetSymbolMatchKey(r);
            if (!counts.ContainsKey(key))
            {
                counts[key] = 0;
                symbolsByKey[key] = r;
            }

            counts[key]++;
        }

        List<SymbolResolution> resolutions = new List<SymbolResolution>(counts.Count);
        Debug.Log("--- [동시 결과 판정] ---");
        foreach (var pair in counts)
        {
            SymbolResolution resolution = ResolveSymbol(symbolsByKey[pair.Key], pair.Value);
            resolutions.Add(resolution);
            ApplyAction(resolution);
        }

        SpinResolved?.Invoke(resolutions);
    }

    private string GetSymbolMatchKey(ReelSymbolData symbol)
    {
        return $"{(int)symbol.symbolType}:{symbol.DisplayName}";
    }

    private SymbolResolution ResolveSymbol(ReelSymbolData symbol, int matchCount)
    {
        SynergyTier tier = matchCount >= 5
            ? SynergyTier.Mega
            : matchCount >= 3 ? SynergyTier.Combo : SynergyTier.None;

        int baseTotal = symbol.BaseValue * matchCount;
        int bonus = tier == SynergyTier.Mega
            ? symbol.MegaBonus
            : tier == SynergyTier.Combo ? symbol.ComboBonus : 0;

        return new SymbolResolution
        {
            symbol = symbol,
            matchCount = matchCount,
            baseTotal = baseTotal,
            synergyBonus = bonus,
            totalValue = baseTotal + bonus,
            synergyTier = tier
        };
    }

    private void ApplyAction(SymbolResolution result)
    {
        string synergyText = result.synergyTier == SynergyTier.None
            ? string.Empty
            : $" / {result.synergyTier} +{result.synergyBonus}";

        switch (result.symbol.symbolType)
        {
            case SymbolType.Attack:
                Debug.Log($"⚔️ {result.symbol.DisplayName}: {result.totalValue} 데미지 ({result.matchCount}개{synergyText})");
                break;

            case SymbolType.Shield:
                Debug.Log($"🛡️ {result.symbol.DisplayName}: 보호막 {result.totalValue} ({result.matchCount}개{synergyText})");
                break;

            case SymbolType.Heal:
                Debug.Log($"❤️ {result.symbol.DisplayName}: HP {result.totalValue} 회복 ({result.matchCount}개{synergyText})");
                break;

            case SymbolType.Gold:
                Debug.Log($"💰 {result.symbol.DisplayName}: Gold +{result.totalValue} ({result.matchCount}개{synergyText})");
                break;

            case SymbolType.Skull:
                Debug.Log($"💀 {result.symbol.DisplayName}: 패널티 {result.matchCount}회");
                break;
        }
    }

    private bool ValidateSetup()
    {
        if (reels == null || reels.Length == 0)
        {
            Debug.LogError("SlotMachineController에 릴이 연결되지 않았습니다.", this);
            return false;
        }

        if (symbolList == null || symbolList.Count == 0)
        {
            Debug.LogError("SlotMachineController의 심볼 풀이 비어 있습니다.", this);
            return false;
        }

        foreach (var reel in reels)
        {
            if (reel == null)
            {
                Debug.LogError("SlotMachineController에 비어 있는 릴 참조가 있습니다.", this);
                return false;
            }
        }

        int totalWeight = 0;
        foreach (var symbol in symbolList)
        {
            if (symbol != null)
                totalWeight += Mathf.Max(0, symbol.weight);
        }

        if (totalWeight <= 0)
        {
            Debug.LogError("심볼 풀의 전체 가중치는 1 이상이어야 합니다.", this);
            return false;
        }

        return true;
    }
}
