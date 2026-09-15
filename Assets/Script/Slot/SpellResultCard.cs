using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SpellResultCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text spellName;
    [SerializeField] private TMP_Text spellValue;
    public Image Icon => icon;
    private SpellResultHand owner;
    private SymbolResolution result;
    private Canvas canvas;
    private RectTransform rect;
    private Transform home;
    private Vector2 min, max, size, position;
    private int sibling, pointer;
    private bool dragging;

    public void Initialize(SpellResultHand hand, SymbolResolution spell, Canvas root)
    {
        spellName.text = spell.symbol.DisplayName;
        spellValue.text = spell.totalValue.ToString();
        owner = hand;
        result = spell;
        canvas = root;
        rect = (RectTransform)transform;
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
