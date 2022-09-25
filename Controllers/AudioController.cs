using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class AudioController : MonoBehaviour {
    [Header("Sources")]
    public AudioMixer mixer;
    public AudioSource audioSound;
    public AudioSource audioMusic;
    public GameObject audioPitchedSound;
    public GameObject audioLoopedSound;
    AudioSource[] audioPitchedSources;
    AudioSource[] audioLoopedSources;

    [Header("Music")]
    public AudioClip MusicStart;
    public AudioClip MusicLoop;

    [Header("Player")]
    public AudioClip Shoot;
    public AudioClip LassoExtend;
    public AudioClip LassoAttach;
    public AudioClip LassoSnap;
    public AudioClip Puncture0;
    public AudioClip Puncture1;
    public AudioClip Puncture2;
    public AudioClip Death;
    public AudioClip RevolverSpin;
    public AudioClip RevolverNext;
    public AudioClip RevolverLoad;

    [Header("Obstacles")]
    public AudioClip CactusSpike;

    [Header("Cow")]
    public AudioClip CowMoo;
    public AudioClip CowCollect;

    [Header("Menu")]
    public AudioClip LevelSelect;
    public AudioClip LevelHover;
    public AudioClip DialogueText;
    public AudioClip DialogueShow;
    public AudioClip DialogueHide;

    public static AudioClip shoot;
    public static AudioClip lassoExtend;
    public static AudioClip lassoAttach;
    public static AudioClip lassoSnap;
    public static AudioClip death;
    public static AudioClip puncture0;
    public static AudioClip puncture1;
    public static AudioClip puncture2;
    public static AudioClip revolverSpin;
    public static AudioClip revolverNext;
    public static AudioClip revolverLoad;
    public static AudioClip cactusSpike;
    public static AudioClip cowMoo;
    public static AudioClip cowCollect;
    public static AudioClip levelSelect;
    public static AudioClip levelHover;
    public static AudioClip dialogueText;
    public static AudioClip dialogueShow;
    public static AudioClip dialogueHide;

    public void Init() {
        shoot        = Shoot;
        lassoExtend  = LassoExtend;
        lassoAttach  = LassoAttach;
        lassoSnap    = LassoSnap;
        death        = Death;
        puncture0    = Puncture0;
        puncture1    = Puncture1;
        puncture2    = Puncture2;
        revolverSpin = RevolverSpin;
        revolverNext = RevolverNext;
        revolverLoad = RevolverLoad;
        cactusSpike  = CactusSpike;
        cowMoo       = CowMoo;
        cowCollect   = CowCollect;
        levelSelect  = LevelSelect;
        levelHover   = LevelHover;
        dialogueText = DialogueText;
        dialogueShow = DialogueShow;
        dialogueHide = DialogueHide;

        for (int i = 0; i < 10; i++) audioPitchedSound.AddComponent<AudioSource>();
        audioPitchedSources = audioPitchedSound.GetComponents<AudioSource>();
        foreach (AudioSource a in audioPitchedSources) {
            a.playOnAwake = false;
            a.outputAudioMixerGroup = audioSound.outputAudioMixerGroup;
        }

        for (int i = 0; i < 3; i++) audioLoopedSound.AddComponent<AudioSource>();
        audioLoopedSources = audioLoopedSound.GetComponents<AudioSource>();
        foreach (AudioSource a in audioLoopedSources) {
            a.playOnAwake = false;
            a.loop = true;
            a.outputAudioMixerGroup = audioSound.outputAudioMixerGroup;
        }

        StartCoroutine(InitMusic());

        IEnumerator InitMusic() {
            audioMusic.Play();

            yield return new WaitWhile(() => audioMusic.isPlaying);

            audioMusic.clip = MusicLoop;
            audioMusic.loop = true;
            audioMusic.Play();
        }
    }

    public void PlayLooped(AudioClip clip, float pitch = 1) {
        if (audioLoopedSources == null)
            return;

        for (int i = 0; i < audioPitchedSources.Length; i++) {
            if (!audioLoopedSources[i].isPlaying) {
                if (pitch > 0) audioLoopedSources[i].pitch = pitch;
                audioLoopedSources[i].clip = clip;
                audioLoopedSources[i].Play();
                break;
            }
        }
    }
    public void StopLooped(AudioClip clip) {
        for (int i = 0; i < audioLoopedSources.Length; i++) {
            if (audioLoopedSources[i].isPlaying && audioLoopedSources[i].clip == clip)
                audioLoopedSources[i].Stop();
        }
    }

    public void PlayPitched(AudioClip clip, float pitch) {
        if (audioPitchedSources == null)
            return;
        
        for (int i = 0; i < audioPitchedSources.Length; i++) {
            if (!audioPitchedSources[i].isPlaying) {
                audioPitchedSources[i].pitch = pitch;
                audioPitchedSources[i].PlayOneShot(clip);
                break;
            }
        }
    }

    public void PlayRandom(AudioClip clip) {
        PlayPitched(clip, GetRandomPitch(1));
    }

    public static float GetRandomPitch(float basePitch = 1) {
        return basePitch + Random.Range(-0.3f, 0.3f);
    }
}
