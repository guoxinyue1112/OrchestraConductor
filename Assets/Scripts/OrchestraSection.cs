using System;
using UnityEngine;

[Serializable]
public class OrchestraSection
{
    [SerializeField] private string sectionName;
    [SerializeField] private AudioSource[] sources;
    [SerializeField] private OrchestraSectionVisual visual;
    [SerializeField] private float activeVolume = 1f;

    private bool _isActive;
    private float[] _currentVolumes = Array.Empty<float>();
    private float[] _velocityBySource = Array.Empty<float>();

    public string SectionName => sectionName;
    public AudioSource[] Sources => sources;
    public bool IsActive => _isActive;
    public bool HasSources => sources != null && sources.Length > 0;
    public AudioClip ReferenceClip => HasSources && sources[0] != null ? sources[0].clip : null;

    public void Initialize()
    {
        int length = sources?.Length ?? 0;
        _currentVolumes = new float[length];
        _velocityBySource = new float[length];

        for (int i = 0; i < length; i++)
        {
            if (sources[i] == null)
            {
                continue;
            }

            ConfigureSource(sources[i]);
            _currentVolumes[i] = 0f;
            sources[i].volume = 0f;
        }

        _isActive = false;
        visual?.SetActiveVisual(false);
    }

    public void SchedulePlay(double dspTime)
    {
        if (!HasSources)
        {
            return;
        }

        for (int i = 0; i < sources.Length; i++)
        {
            AudioSource source = sources[i];
            if (source == null || source.clip == null)
            {
                continue;
            }

            source.Stop();
            source.timeSamples = 0;
            source.volume = 0f;
            _currentVolumes[i] = 0f;
            _velocityBySource[i] = 0f;
            source.PlayScheduled(dspTime);
        }
    }

    public void Stop()
    {
        if (!HasSources)
        {
            return;
        }

        for (int i = 0; i < sources.Length; i++)
        {
            AudioSource source = sources[i];
            if (source == null)
            {
                continue;
            }

            source.Stop();
            source.timeSamples = 0;
            source.volume = 0f;
            _currentVolumes[i] = 0f;
            _velocityBySource[i] = 0f;
        }

        _isActive = false;
        visual?.SetActiveVisual(false);
    }

    public void SetActive(bool active)
    {
        if (_isActive == active)
        {
            return;
        }

        _isActive = active;
        visual?.SetActiveVisual(active);
    }

    public void UpdateVolumes(float fadeDuration)
    {
        if (!HasSources)
        {
            return;
        }

        float targetVolume = _isActive ? activeVolume : 0f;

        for (int i = 0; i < sources.Length; i++)
        {
            AudioSource source = sources[i];
            if (source == null)
            {
                continue;
            }

            if (fadeDuration <= Mathf.Epsilon)
            {
                _currentVolumes[i] = targetVolume;
            }
            else
            {
                _currentVolumes[i] = Mathf.SmoothDamp(
                    _currentVolumes[i],
                    targetVolume,
                    ref _velocityBySource[i],
                    fadeDuration,
                    Mathf.Infinity,
                    Time.unscaledDeltaTime);
            }

            source.volume = _currentVolumes[i];
        }
    }

    public void ApplyImmediateSilence()
    {
        _isActive = false;

        if (!HasSources)
        {
            visual?.SetActiveVisual(false);
            return;
        }

        for (int i = 0; i < sources.Length; i++)
        {
            AudioSource source = sources[i];
            if (source == null)
            {
                continue;
            }

            source.volume = 0f;
            _currentVolumes[i] = 0f;
            _velocityBySource[i] = 0f;
        }

        visual?.SetActiveVisual(false);
    }

    private void ConfigureSource(AudioSource source)
    {
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
    }
}
