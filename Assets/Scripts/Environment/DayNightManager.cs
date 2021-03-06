﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public enum Seasons
{
    SPRING,
    SUMMER,
    AUTUMN,
    WINTER
}

public enum TimesOfDay
{
    DAY,
    NIGHT
}

[RequireComponent(typeof(AudioSource))]
public class DayNightManager : MonoBehaviour
{
    public delegate void SeasonsHandler(Seasons season);
    public static event SeasonsHandler OnUpdateSeason;

    private DayNightCycle dayNightCycle;
    AudioSource audioSource;
    [SerializeField] List<Sound> daySounds = new List<Sound>();
    [SerializeField] List<Sound> nightSounds = new List<Sound>();
    [SerializeField] float[] songTimes;
    [SerializeField] AudioMixerGroup musicGroup;
    [SerializeField] AudioMixerGroup soundGroup;

    int currentHour;
    Seasons currentSeason;

    private void Awake()
    {
        // Initialize all sounds
        foreach (Sound s in daySounds)
            s.Initialize(gameObject.AddComponent<AudioSource>());
        foreach (Sound s in nightSounds)
            s.Initialize(gameObject.AddComponent<AudioSource>());


        DayNightCycle.OnDaytime += DayNightCycle_OnDaytime;
        DayNightCycle.OnNightfall += DayNightCycle_OnNightfall;
    }

    private void OnDestroy()
    {
        DayNightCycle.OnDaytime -= DayNightCycle_OnDaytime;
        DayNightCycle.OnNightfall -= DayNightCycle_OnNightfall;
    }

    // Start is called before the first frame update
    void Start()
    {
        dayNightCycle = GetComponentInChildren<DayNightCycle>();
        audioSource = GetComponent<AudioSource>();

        SetCurrentSeason();

        // Initialize all sounds
        //foreach (Sound s in daySounds)
        //    s.Initialize(gameObject.AddComponent<AudioSource>());
        //foreach (Sound s in nightSounds)
        //    s.Initialize(gameObject.AddComponent<AudioSource>());
    }

    // Update is called once per frame
    void Update()
    {
        currentHour = dayNightCycle.GetCurrentHour();
        
        SoundManager();
    }

    #region Sound Management

    void SoundManager()
    {
        // Updates each sounds' source volume based on sound.volume
        foreach (Sound s in daySounds)
            s.Update();

        foreach (Sound s in nightSounds)
            s.Update();
    }

    // Event for daytime
    private void DayNightCycle_OnDaytime()
    {
        //print("It's daytime!");

        // Stop all nighttime sounds
        Stop(TimesOfDay.NIGHT, "NaturalSound");
        Stop(TimesOfDay.NIGHT, "MusicMix");

        // Play all daytime sounds
        Play(TimesOfDay.DAY, "Wind", UnityEngine.Random.Range(0f, 60f));
    }

    // Event for nightfall
    private void DayNightCycle_OnNightfall()
    {
        //print("Nightfall has started.");

        // Stop all daytime sounds
        Stop(TimesOfDay.DAY, "Wind");

        // Play all nighttime sounds
        Play(TimesOfDay.NIGHT, "NaturalSound", UnityEngine.Random.Range(0f, 30f));
        Play(TimesOfDay.NIGHT, "MusicMix", songTimes[UnityEngine.Random.Range(0, songTimes.Length)]);
    }

    // Play a specific sound from the array
    void Play(TimesOfDay time, string name, float playbackTime = 0f)
    {
        // Find specified Sound
        Sound sound = GetSound(time, name);

        // Check if Sound is null
        if (sound == null)
            return;

        // Play the Sound
        if (sound.playCoroutine != null)
            StartCoroutine(RestartSound(sound, playbackTime));
        else
            sound.playCoroutine = StartCoroutine(PlaySound(sound, playbackTime));
    }

    void Stop(TimesOfDay time, string name)
    {
        // Find specified Sound
        Sound sound = GetSound(time, name);

        // Check if Sound is null
        if (sound == null)
            return;

        // Stop the Sound
        StopCoroutine(StopSound(sound));
        StartCoroutine(StopSound(sound));
    }

    #region Sound Helper Functions
    
    private IEnumerator PlaySound(Sound sound, float playbackTime)
    {
        if (sound == null)
            yield break;

        // Stop the source if it's playing something already
        sound.source.Stop();

        // Set the AudioSource.playbackTime to playbackTime
        SetPlaybackTime(sound.source, playbackTime);

        // Set the target volume to baseVolume
        sound.volume = sound.baseVolume;
        
        // Wait for the delay
        yield return new WaitForSeconds(sound.delay / DayNightCycle.instance.GetRate());

        // Play the Sound
        sound.source.Play();
    }

    private IEnumerator StopSound(Sound sound)
    {
        if (sound == null)
            yield break;

        // Set the target volume to 0
        sound.volume = 0;

        // Wait for the delay
        yield return new WaitForSeconds(sound.fadeDuration / DayNightCycle.instance.GetRate());

        // Stop the sound
        sound.playCoroutine = null;
        sound.stopCoroutine = null;
    }

    private IEnumerator RestartSound(Sound sound, float playbackTime)
    {
        if (sound == null)
            yield break;

        yield return StopSound(sound);
        sound.playCoroutine = StartCoroutine(PlaySound(sound, playbackTime));
    }

    private void SetPlaybackTime(AudioSource source, float time)
    {
        source.time = time;
    }

    private Sound GetSound(TimesOfDay time, string name)
    {
        Sound sound = (time == TimesOfDay.DAY) ? daySounds.Find(s => s.name == name) : nightSounds.Find(s => s.name == name);

        if (sound == null)
        {
            Debug.LogError("Sound: [" + name + "] was not found.");
            return null;
        }
        else
            return sound;
    }

    #endregion

    #endregion

    private void SetCurrentSeason()
    {
        switch (DateTime.Now.Month)
        {
            case 3:
            case 4:
            case 5:
                currentSeason = Seasons.SPRING;
                break;
            case 6:
            case 7:
            case 8:
                currentSeason = Seasons.SUMMER;
                break;
            case 9:
            case 10:
            case 11:
                currentSeason = Seasons.AUTUMN;
                break;
            case 12:
            case 1:
            case 2:
                currentSeason = Seasons.WINTER;
                break;
            default:
                Debug.LogError("Invalid month [" + DateTime.Now.Month + "]");
                break;
        }

        OnUpdateSeason?.Invoke(currentSeason);
    }

    public Seasons GetCurrentSeason()
    {
        return currentSeason;
    }
}
