using UnityEngine;

/// <summary>
/// A simple, self-contained Singleton AudioManager that generates and caches
/// procedural audio clips on start, and plays them when requested by game systems.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private AudioSource audioSource;

    private AudioClip selectClip;
    private AudioClip swapClip;
    private AudioClip matchClip;
    private AudioClip gravityClip;
    private AudioClip victoryClip;
    private AudioClip defeatClip;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            audioSource = GetComponent<AudioSource>();

            // Generate and cache SFX clips procedurally
            selectClip = ProceduralAudioSynth.CreateBeep();
            swapClip = ProceduralAudioSynth.CreateSlide();
            matchClip = ProceduralAudioSynth.CreateChime();
            gravityClip = ProceduralAudioSynth.CreateRumble();
            victoryClip = ProceduralAudioSynth.CreateFanfare();
            defeatClip = ProceduralAudioSynth.CreateSadTune();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlaySelect() => Play(selectClip);
    public void PlaySwap() => Play(swapClip);
    public void PlayMatch() => Play(matchClip);
    public void PlayGravityShift() => Play(gravityClip);
    public void PlayVictory() => Play(victoryClip);
    public void PlayDefeat() => Play(defeatClip);

    private void Play(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
