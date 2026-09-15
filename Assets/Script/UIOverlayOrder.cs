using UnityEngine;

public static class UIOverlayOrder
{
    public static void BringToFront(Transform ui)
    {
        if (ui == null || !ui.gameObject.activeInHierarchy) return;
        Canvas canvas = ui.GetComponentInParent<Canvas>();
        if (canvas == null) return;
        // 같은 Canvas의 부모 패널도 함께 앞으로 옮겨 다른 HUD에 가리지 않게 한다.
        while (ui != null && ui != canvas.transform)
        {
            if (ui.GetSiblingIndex() != ui.parent.childCount - 1)
                ui.SetAsLastSibling();
            ui = ui.parent;
        }
    }
}
