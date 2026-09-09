using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class SlotReel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image displayImage;

    [Header("Settings")]
    [Tooltip("심볼 하나가 오른쪽에서 중앙으로 들어오는 시간입니다.")]
    [SerializeField] private float spinSpeed = 0.12f;

    [FormerlySerializedAs("dropDistanceMultiplier")]
    [Tooltip("릴 너비에 대한 심볼 이동 거리 배율입니다.")]
    [SerializeField] private float slideDistanceMultiplier = 1f;

    private List<ReelSymbolData> symbolPool;
    private Image incomingImage;
    private RectTransform displayRect;
    private RectTransform incomingRect;
    private Vector2 centerPosition;
    private bool isSpinning;
    private bool stopRequested;
    private ReelSymbolData stopTarget;
    private ReelSymbolData currentResult;
    private Coroutine spinCoroutine;

    public bool IsSpinning => isSpinning;
    public ReelSymbolData CurrentResult => currentResult;

    public void Init(List<ReelSymbolData> pool)
    {
        symbolPool = pool;
        EnsureReelVisuals();

        ReelSymbolData initialSymbol = PickRandomSymbol();
        if (initialSymbol == null) return;

        currentResult = initialSymbol;
        displayImage.sprite = initialSymbol.symbolSprite;
    }

    public void StartSpin()
    {
        if (isSpinning || !EnsureReelVisuals()) return;

        stopRequested = false;
        stopTarget = null;
        isSpinning = true;
        spinCoroutine = StartCoroutine(SpinRoutine());
    }

    private IEnumerator SpinRoutine()
    {
        while (isSpinning)
        {
            ReelSymbolData nextSymbol = stopRequested
                ? stopTarget
                : PickRandomSymbol();

            if (nextSymbol == null)
            {
                isSpinning = false;
                break;
            }

            yield return AnimateSymbolSlide(nextSymbol);
            currentResult = nextSymbol;

            if (stopRequested)
            {
                stopRequested = false;
                isSpinning = false;
            }
        }

        spinCoroutine = null;
    }

    public IEnumerator StopSpin(ReelSymbolData targetSymbol)
    {
        if (targetSymbol == null) yield break;

        if (!isSpinning)
        {
            if (!EnsureReelVisuals()) yield break;

            yield return AnimateSymbolSlide(targetSymbol);
            currentResult = targetSymbol;
            yield break;
        }

        stopTarget = targetSymbol;
        stopRequested = true;

        while (isSpinning)
            yield return null;
    }

    private IEnumerator AnimateSymbolSlide(ReelSymbolData nextSymbol)
    {
        float distance = GetSlideDistance();
        float duration = Mathf.Max(0.01f, spinSpeed);
        Vector2 entryPosition = centerPosition + Vector2.right * distance;
        Vector2 exitPosition = centerPosition + Vector2.left * distance;

        displayRect.anchoredPosition = centerPosition;
        incomingRect.anchoredPosition = entryPosition;
        incomingImage.sprite = nextSymbol.symbolSprite;
        incomingImage.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);

            displayRect.anchoredPosition = Vector2.Lerp(centerPosition, exitPosition, progress);
            incomingRect.anchoredPosition = Vector2.Lerp(entryPosition, centerPosition, progress);
            yield return null;
        }

        displayImage.sprite = nextSymbol.symbolSprite;
        displayRect.anchoredPosition = centerPosition;
        incomingRect.anchoredPosition = entryPosition;
        incomingImage.gameObject.SetActive(false);
    }

    private ReelSymbolData PickRandomSymbol()
    {
        if (symbolPool == null || symbolPool.Count == 0) return null;

        int totalWeight = 0;
        foreach (var symbol in symbolPool)
        {
            if (symbol != null)
                totalWeight += Mathf.Max(0, symbol.weight);
        }

        if (totalWeight <= 0) return null;

        int roll = Random.Range(0, totalWeight);
        foreach (var symbol in symbolPool)
        {
            if (symbol == null) continue;

            roll -= Mathf.Max(0, symbol.weight);
            if (roll < 0) return symbol;
        }

        return null;
    }

    private bool EnsureReelVisuals()
    {
        if (displayImage == null)
        {
            Debug.LogError("SlotReel에 Display Image가 연결되지 않았습니다.", this);
            return false;
        }

        if (incomingImage != null) return true;

        displayRect = displayImage.rectTransform;
        centerPosition = displayRect.anchoredPosition;

        RectTransform viewport = displayRect.parent as RectTransform;
        if (viewport != null && viewport.GetComponent<RectMask2D>() == null)
            viewport.gameObject.AddComponent<RectMask2D>();

        incomingImage = Instantiate(displayImage, displayRect.parent);
        incomingImage.name = $"{displayImage.name} (Incoming)";
        incomingImage.raycastTarget = false;
        incomingImage.transform.SetSiblingIndex(displayImage.transform.GetSiblingIndex() + 1);
        incomingRect = incomingImage.rectTransform;
        incomingRect.anchoredPosition = centerPosition + Vector2.right * GetSlideDistance();
        incomingImage.gameObject.SetActive(false);

        return true;
    }

    private float GetSlideDistance()
    {
        RectTransform viewport = displayRect != null
            ? displayRect.parent as RectTransform
            : null;

        float width = viewport != null ? viewport.rect.width : 0f;
        if (width <= 0f && displayRect != null)
            width = displayRect.rect.width;

        return Mathf.Max(1f, width * Mathf.Max(0.1f, slideDistanceMultiplier));
    }

    private void OnDisable()
    {
        if (spinCoroutine != null)
            StopCoroutine(spinCoroutine);

        spinCoroutine = null;
        isSpinning = false;
        stopRequested = false;

        if (displayRect != null)
            displayRect.anchoredPosition = centerPosition;

        if (incomingImage != null)
            incomingImage.gameObject.SetActive(false);
    }
}
