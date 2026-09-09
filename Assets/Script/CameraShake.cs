using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [Header("UI 흔들림 설정")]
    [Tooltip("Canvas 아래의 UIShakeRoot를 연결하세요.")]
    [SerializeField] private RectTransform uiShakeRoot;

    [Tooltip("UI 흔들림 배율입니다. UI는 픽셀 단위이므로 카메라보다 크게 설정합니다.")]
    [SerializeField] private float uiShakeMultiplier = 20f;

    private Vector3 originalCameraLocalPos;
    private Vector2 originalUIAnchoredPos;

    private Coroutine shakeCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        originalCameraLocalPos = transform.localPosition;

        if (uiShakeRoot != null)
            originalUIAnchoredPos = uiShakeRoot.anchoredPosition;
    }

    /// <summary>
    /// 카메라와 Overlay UI를 함께 흔듭니다.
    /// </summary>
    public void Shake(float duration, float magnitude)
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            ResetPositions();
        }

        shakeCoroutine = StartCoroutine(
            ShakeRoutine(duration, magnitude)
        );
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // 시간이 지날수록 흔들림 감소
            float damper = 1f - (elapsed / duration);

            Vector2 randomDirection = Random.insideUnitCircle;

            float cameraOffsetX =
                randomDirection.x * magnitude * damper;

            float cameraOffsetY =
                randomDirection.y * magnitude * damper;

            // 3D 카메라 흔들림
            transform.localPosition =
                originalCameraLocalPos +
                new Vector3(cameraOffsetX, cameraOffsetY, 0f);

            // Overlay UI 흔들림
            if (uiShakeRoot != null)
            {
                Vector2 uiOffset =
                    randomDirection *
                    magnitude *
                    uiShakeMultiplier *
                    damper;

                uiShakeRoot.anchoredPosition =
                    originalUIAnchoredPos + uiOffset;
            }

            // 일시정지 상태에서도 흔들리게 하려면
            // unscaledDeltaTime 사용
            elapsed += Time.unscaledDeltaTime;

            yield return null;
        }

        ResetPositions();
        shakeCoroutine = null;
    }

    private void ResetPositions()
    {
        transform.localPosition = originalCameraLocalPos;

        if (uiShakeRoot != null)
            uiShakeRoot.anchoredPosition = originalUIAnchoredPos;
    }

    private void OnDisable()
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            shakeCoroutine = null;
        }

        ResetPositions();
    }
}