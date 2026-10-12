using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class PersistentBackgroundMusic : MonoBehaviour
{
    private static PersistentBackgroundMusic instance;

    [Tooltip("씬마다 다른 배경음으로 바뀔 때 겹쳐서 전환되는 시간입니다.")]
    [SerializeField, Min(0f)] private float crossfadeDuration = 0.6f;

    private AudioSource activeSource;
    private AudioSource inactiveSource;
    private Coroutine crossfadeRoutine;

    private void Awake()
    {
        AudioSource sceneSource = GetComponent<AudioSource>();

        if (instance != null && instance != this)
        {
            AudioClip requestedClip = sceneSource.clip;
            float requestedVolume = sceneSource.volume;
            float requestedPitch = sceneSource.pitch;

            // 새 씬의 AudioSource가 잠깐 중복 재생되는 것을 막는다.
            sceneSource.playOnAwake = false;
            sceneSource.Stop();

            instance.ChangeTrack(
                requestedClip,
                requestedVolume,
                requestedPitch
            );

            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        activeSource = sceneSource;
        activeSource.loop = true;
        activeSource.playOnAwake = false;

        inactiveSource = gameObject.AddComponent<AudioSource>();
        CopySourceSettings(activeSource, inactiveSource);
        inactiveSource.playOnAwake = false;
        inactiveSource.volume = 0f;
        inactiveSource.Stop();

        if (activeSource.clip != null && !activeSource.isPlaying)
            activeSource.Play();
    }

    private void ChangeTrack(
        AudioClip requestedClip,
        float requestedVolume,
        float requestedPitch)
    {
        if (requestedClip == null || activeSource == null)
            return;

        requestedVolume = Mathf.Clamp01(requestedVolume);

        // 같은 음악이 들어오면 재생 시간을 유지한다.
        if (activeSource.clip == requestedClip)
        {
            activeSource.volume = requestedVolume;
            activeSource.pitch = requestedPitch;
            if (!activeSource.isPlaying)
                activeSource.Play();
            return;
        }

        if (crossfadeRoutine != null)
            StopCoroutine(crossfadeRoutine);

        inactiveSource.Stop();
        CopySourceSettings(activeSource, inactiveSource);
        inactiveSource.clip = requestedClip;
        inactiveSource.pitch = requestedPitch;
        inactiveSource.volume = 0f;
        inactiveSource.Play();

        crossfadeRoutine = StartCoroutine(
            CrossfadeToInactive(requestedVolume)
        );
    }

    private IEnumerator CrossfadeToInactive(float requestedVolume)
    {
        float duration = Mathf.Max(0f, crossfadeDuration);
        float outgoingVolume = activeSource.volume;

        if (duration <= 0f)
        {
            activeSource.Stop();
            inactiveSource.volume = requestedVolume;
        }
        else
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                activeSource.volume = Mathf.Lerp(
                    outgoingVolume,
                    0f,
                    progress
                );
                inactiveSource.volume = Mathf.Lerp(
                    0f,
                    requestedVolume,
                    progress
                );
                yield return null;
            }

            activeSource.Stop();
            activeSource.volume = 0f;
            inactiveSource.volume = requestedVolume;
        }

        AudioSource previousSource = activeSource;
        activeSource = inactiveSource;
        inactiveSource = previousSource;
        crossfadeRoutine = null;
    }

    private static void CopySourceSettings(
        AudioSource source,
        AudioSource destination)
    {
        destination.outputAudioMixerGroup = source.outputAudioMixerGroup;
        destination.loop = true;
        destination.mute = source.mute;
        destination.priority = source.priority;
        destination.panStereo = source.panStereo;
        destination.spatialBlend = source.spatialBlend;
        destination.reverbZoneMix = source.reverbZoneMix;
        destination.bypassEffects = source.bypassEffects;
        destination.bypassListenerEffects = source.bypassListenerEffects;
        destination.bypassReverbZones = source.bypassReverbZones;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
