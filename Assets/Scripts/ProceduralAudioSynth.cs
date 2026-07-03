using UnityEngine;


public static class ProceduralAudioSynth
{
    private const int SampleRate = 44100;

    
    public static AudioClip CreateBeep(float frequency = 880f, float duration = 0.05f)
    {
        int sampleCount = (int)(SampleRate * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SampleRate;
            float envelope = 1f - (t / duration); 
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.15f;
        }

        AudioClip clip = AudioClip.Create("SynthBeep", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

  
    public static AudioClip CreateSlide(float startFreq = 440f, float endFreq = 220f, float duration = 0.12f)
    {
        int sampleCount = (int)(SampleRate * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SampleRate;
            float envelope = 1f - (t / duration);
            
            float phase = 2f * Mathf.PI * (startFreq * t + (endFreq - startFreq) * t * t / (2f * duration));
            samples[i] = Mathf.Sin(phase) * envelope * 0.2f;
        }

        AudioClip clip = AudioClip.Create("SynthSlide", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

   
    public static AudioClip CreateChime()
    {
        float duration = 0.35f;
        int sampleCount = (int)(SampleRate * duration);
        float[] samples = new float[sampleCount];
        
        float[] notes = { 523.25f, 659.25f, 783.99f, 1046.50f }; // C5, E5, G5, C6
        float noteDuration = duration / notes.Length;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SampleRate;
            
           
            int noteIndex = Mathf.Clamp((int)(t / noteDuration), 0, notes.Length - 1);
            float freq = notes[noteIndex];
            
            float noteT = t - (noteIndex * noteDuration);
            float noteEnvelope = 1f - (noteT / noteDuration);
            float overallEnvelope = 1f - (t / duration);

            samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * noteEnvelope * overallEnvelope * 0.15f;
        }

        AudioClip clip = AudioClip.Create("SynthChime", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

   
    public static AudioClip CreateRumble()
    {
        float duration = 0.45f;
        int sampleCount = (int)(SampleRate * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SampleRate;
            float envelope = Mathf.Sin(t / duration * Mathf.PI);
            
            float phase = 2f * Mathf.PI * (150f * t + (70f - 150f) * t * t / (2f * duration));
            
            samples[i] = (Mathf.Sin(phase) + 0.35f * Mathf.Sin(phase * 2f)) * envelope * 0.25f;
        }

        AudioClip clip = AudioClip.Create("SynthRumble", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    
    public static AudioClip CreateFanfare()
    {
        float duration = 1.2f;
        int sampleCount = (int)(SampleRate * duration);
        float[] samples = new float[sampleCount];

        float[] startTimes = { 0.0f, 0.12f, 0.24f, 0.36f, 0.48f, 0.6f };
        float[] freqs = { 261.63f, 329.63f, 392.00f, 523.25f, 659.25f, 1046.50f }; // C4, E4, G4, C5, E5, C6

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SampleRate;
            float sampleVal = 0f;
            int activeNotes = 0;

            for (int n = 0; n < freqs.Length; n++)
            {
                if (t >= startTimes[n])
                {
                    float noteT = t - startTimes[n];
                    float noteDuration = duration - startTimes[n];
                    float env = Mathf.Max(0f, 1f - (noteT / noteDuration));
                    sampleVal += Mathf.Sin(2f * Mathf.PI * freqs[n] * noteT) * env * 0.15f;
                    activeNotes++;
                }
            }
            samples[i] = sampleVal / Mathf.Max(1, activeNotes) * 0.35f;
        }

        AudioClip clip = AudioClip.Create("SynthFanfare", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    
    public static AudioClip CreateSadTune()
    {
        float duration = 1.4f;
        int sampleCount = (int)(SampleRate * duration);
        float[] samples = new float[sampleCount];

        float[] startTimes = { 0.0f, 0.25f, 0.5f };
        float[] freqs = { 261.63f, 311.13f, 196.00f }; // C4, Eb4, G3

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SampleRate;
            float sampleVal = 0f;
            int activeNotes = 0;

            for (int n = 0; n < freqs.Length; n++)
            {
                if (t >= startTimes[n])
                {
                    float noteT = t - startTimes[n];
                    float noteDuration = duration - startTimes[n];
                    float env = Mathf.Max(0f, 1f - (noteT / noteDuration));
                    sampleVal += Mathf.Sin(2f * Mathf.PI * freqs[n] * noteT) * env * 0.15f;
                    activeNotes++;
                }
            }
            samples[i] = sampleVal / Mathf.Max(1, activeNotes) * 0.35f;
        }

        AudioClip clip = AudioClip.Create("SynthSadTune", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
