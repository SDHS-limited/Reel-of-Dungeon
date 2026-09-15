using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CircularSceneTransition : MonoBehaviour
{
    private static CircularSceneTransition active;
    private CircularFadeGraphic curtain;

    public static void Begin(string sceneName, float closeDuration, float revealDuration)
    {
        if (active != null) return;
        if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"씬 '{sceneName}'을 불러올 수 없습니다. Build Profiles의 Scene List에 등록하세요.");
            return;
        }

        var root = new GameObject("Circular Scene Transition", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        DontDestroyOnLoad(root);
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;
        active = root.AddComponent<CircularSceneTransition>();
        var overlay = new GameObject("Circular Fade", typeof(RectTransform), typeof(CanvasRenderer), typeof(CircularFadeGraphic));
        overlay.transform.SetParent(root.transform, false);
        RectTransform rect = (RectTransform)overlay.transform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        active.curtain = overlay.GetComponent<CircularFadeGraphic>();
        active.curtain.color = Color.black;
        active.curtain.raycastTarget = true;
        active.curtain.Openness = 1f;
        active.StartCoroutine(active.Transition(sceneName, closeDuration, revealDuration));
    }

    private IEnumerator Transition(string sceneName, float closeDuration, float revealDuration)
    {
        yield return Animate(1f, 0f, closeDuration);
        AsyncOperation operation = null;
        try
        {
            operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
        if (operation != null)
        {
            while (!operation.isDone) yield return null;
            // 새 씬의 Canvas와 카메라가 준비된 뒤 열린다.
            yield return null;
        }
        yield return Animate(0f, 1f, revealDuration);
        Destroy(gameObject);
    }

    private IEnumerator Animate(float from, float to, float duration)
    {
        duration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            curtain.Openness = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / duration));
            yield return null;
        }
        curtain.Openness = to;
    }

    private void OnDestroy()
    {
        if (active == this) active = null;
    }
}
