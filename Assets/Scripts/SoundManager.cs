using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager {
	private enum MusicState {NONE, STOP_MUSIC}
	public static int BLEURGH = 0;
	public static int COUNTDOWN = 1;
	public static int COUNTDOWN_GO = 2;
	public static int JUMP = 3;
	public static int LOSE = 4;
	public static int RUN = 5;
	public static int WIN = 6;
	public static int GARBAGE = 7;
	private static string[] SFX_FILENAMES = new string[]{
		/*"bleurgh",*/ "jump", "countdown", "go", "jump", "lose", "run", "win", "wow"
	};
	private static AudioClip[] sfx = new AudioClip[0];
	private AudioSource sfxAudioSource;
	private int currentType;

	public SoundManager(AudioSource audioSource){
		LoadSFX ();
		sfxAudioSource = audioSource;
	}

	public bool isPlayingSFX(){
		return sfxAudioSource.isPlaying;
	}

	public void PlaySFX(int type){
		if (type < 0 && type >= sfx.Length ||
		    type == currentType && sfxAudioSource.isPlaying && type != JUMP ||
			(currentType == GARBAGE || currentType == JUMP && type == RUN) && type != WIN && sfxAudioSource.isPlaying) {
			return;
		}
		StopSFX ();
		currentType = type;
		sfxAudioSource.loop = type == RUN;
		sfxAudioSource.clip = sfx [type];
		sfxAudioSource.Play ();
	}

	public void StopSFX()
    {
        if (currentType != COUNTDOWN && currentType != RUN)
			return;
		if (sfxAudioSource.isPlaying) {
			sfxAudioSource.Stop ();
		}
	}

	private void LoadSFX(){
		sfx = new AudioClip[SFX_FILENAMES.Length];
		for (int s = 0; s < SFX_FILENAMES.Length; s++) {
			sfx[s] = Resources.Load<AudioClip>("Sound/" + SFX_FILENAMES[s]);
		}
	}
}
