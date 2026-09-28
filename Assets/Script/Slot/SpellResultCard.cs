using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SpellResultCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private Image categoryIcon;
    [SerializeField] private TMP_Text spellName;
    [SerializeField] private TMP_Text spellValue;
    [SerializeField] private Sprite attackCategoryIcon;
    [SerializeField] private Sprite shieldCategoryIcon;
    [SerializeField] private Sprite healCategoryIcon;
    public Image Icon => icon;
    private SpellResultHand owner;
    private SymbolResolution result;
    private Canvas canvas;
    private RectTransform rect;
    private Transform home;
    private Vector2 min, max, size, position;
    private int sibling, pointer;
    private bool dragging;
    private Outline firstCardOutline;

    public SymbolResolution Result => result;

    public void SetFirstCardHighlight(bool highlighted)
    {
        if (firstCardOutline == null)
        {
            firstCardOutline = gameObject.AddComponent<Outline>();

            firstCardOutline.effectColor = new Color(1f, 0.82f, 0.12f, 1f);
            firstCardOutline.effectDistance = new Vector2(5f, -5f);
            firstCardOutline.useGraphicAlpha = false;
        }

        firstCardOutline.enabled = highlighted;
    }

    public void Initialize(SpellResultHand hand, SymbolResolution spell, Canvas root)
    {
        // 이름 대신 왼쪽에는 마법 그림, 오른쪽에는 종류 아이콘과 수치를 표시한다.
        if (spellName != null)
            spellName.gameObject.SetActive(false);

        if (spellValue != null)
        {
            spellValue.gameObject.SetActive(true);
            spellValue.text = spell.totalValue.ToString();
        }

        if (icon != null)
        {
            // 크기와 위치는 Ingame Canvas의 Card Template 설정을 그대로 사용한다.
            icon.preserveAspect = true;
            icon.raycastTarget = false;
        }

        if (categoryIcon != null)
        {
            Sprite categorySprite = GetCategoryIcon(spell.symbol.symbolType);
            categoryIcon.sprite = categorySprite;
            categoryIcon.gameObject.SetActive(categorySprite != null);
            categoryIcon.preserveAspect = true;
            categoryIcon.raycastTarget = false;
        }

        owner = hand;
        result = spell;
        canvas = root;
        rect = (RectTransform)transform;
    }

    private Sprite GetCategoryIcon(SymbolType symbolType)
    {
        switch (symbolType)
        {
            case SymbolType.Attack:
                return attackCategoryIcon;
            case SymbolType.Shield:
                return shieldCategoryIcon;
            case SymbolType.Heal:
                return healCategoryIcon;
            default:
                return null;
        }
    }

    public void OnBeginDrag(PointerEventData data)
    {
        if (owner == null || !owner.CanDrag || dragging || data.button != PointerEventData.InputButton.Left) return;
        dragging = true;
        pointer = data.pointerId;
        home = transform.parent;
        sibling = transform.GetSiblingIndex();
        min = rect.anchorMin; max = rect.anchorMax;
        size = rect.sizeDelta; position = rect.anchoredPosition;
        Vector2 dimensions = rect.rect.size;
        rect.SetParent(canvas.transform, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = dimensions;
        rect.SetAsLastSibling();
        GetComponent<Image>().raycastTarget = false;
        OnDrag(data);
    }

    public void OnDrag(PointerEventData data)
    {
        if (!dragging || data.pointerId != pointer) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, data.position,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out Vector2 point);
        rect.anchoredPosition = point;
    }

    public void OnEndDrag(PointerEventData data)
    {
        if (!dragging || data.pointerId != pointer) return;
        dragging = false;
        if (owner.TryCast(this, result, data.position)) return;
        Restore();
    }

    private void Restore()
    {
        if (home == null) return;
        rect.SetParent(home, false);
        rect.SetSiblingIndex(sibling);
        rect.anchorMin = min; rect.anchorMax = max;
        rect.sizeDelta = size; rect.anchoredPosition = position;
        GetComponent<Image>().raycastTarget = true;
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused && dragging) { dragging = false; Restore(); }
    }
}
