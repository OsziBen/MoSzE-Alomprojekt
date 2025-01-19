using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class AudioManager : BasePersistentManager<AudioManager>
{
    private AudioSource SoundEffectPlayer; // A hangeffektek lejátszásáért felelős AudioSource.
    private AudioSource BackgroundMusicPlayer; // A háttérzene lejátszásáért felelős AudioSource. Loopol.

    private Dictionary<string, AudioClip> soundEffects; // A hangeffekteket tároló dictionary.
    private Dictionary<string, AudioClip> backgroundMusic; // A háttérzenéket tároló dictionary.

    // Mivel a Unity editorban nem lehet dictionary-t szerkeszteni, először custom NamedAudio structokból felépülő listákba vesszük fel a hangeffekteket.
    [Serializable]
    public struct NamedAudio
    {
        public string name;
        public AudioClip audioclip;
    }
    public NamedAudio[] soundEffectInput;
    public NamedAudio[] backgroundMusicInput;

    /// <summary>
    /// A start feladata lényegében csak annyi, hogy assignolja a megfelelő AudioSource komponenseket a megfelelő változóhoz.
    /// Illetve beolvassa a listaként megadott audioclipeket a dictionary-kba.
    /// </summary>
    public void Start()
    {
        AudioSource[] audioSources = gameObject.GetComponents<AudioSource>();
        SoundEffectPlayer = audioSources[0];
        BackgroundMusicPlayer = audioSources[1];

        soundEffects = soundEffectInput.ToDictionary(
            item => item.name,
            item => item.audioclip
        );
        backgroundMusic = backgroundMusicInput.ToDictionary(
            item => item.name,
            item => item.audioclip
        );
    }

    /// <summary>
    /// Lejátssza a paraméterben megadott nevű hangeffektet.
    /// </summary>
    /// <param name="clipKey">Az editorban a hangeffektnek megadott név.</param>
    public void PlaySoundEffect(string clipKey)
    {
        if (soundEffects.TryGetValue(clipKey, out AudioClip clip))
        {
            SoundEffectPlayer.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning($"'{clipKey}' nevű hangeffekt nem található!");
        }
    }

    /// <summary>
    /// Lejátssza a paraméterben megadott nevű háttérzenét.
    /// </summary>
    /// <param name="clipKey">Az editorban a háttérzenének megadott név.</param>
    public void PlayBGM(string clipKey)
    {
        if (backgroundMusic.TryGetValue(clipKey, out AudioClip clip))
        {
            BackgroundMusicPlayer.clip = clip;
            BackgroundMusicPlayer.Play();
        }
        else
        {
            Debug.LogWarning($"'{clipKey}' nevű háttérzene nem található!");
        }
    }

    /// <summary>
    /// Megállítja a háttérzenét, amennyiben az lejátszás alatt van.
    /// </summary>
    public void StopBGM()
    {
        if(BackgroundMusicPlayer.isPlaying)
        {
            BackgroundMusicPlayer.Stop();
        }
        else
        {
            Debug.LogWarning("A háttérzene nem állítható meg, mivel nem is megy.");
        }
    }

    // Az összes háttérzene lejátszó függvényei
    public void PlayMenuBGM()
    {
        PlayBGM("menu");
    }

    public void PlayLevel1BGM()
    {
        PlayBGM("lvl1");
    }

    public void PlayLevel2BGM()
    {
        PlayBGM("lvl2");
    }

    public void PlayLevel3BGM()
    {
        PlayBGM("lvl3");
    }

    public void PlayLevel4BGM()
    {
        PlayBGM("lvl4");
    }

    public void PlayBossBGM()
    {
        PlayBGM("boss");
    }

    // Az összes hangeffekt lejátszó függvényei.
    public void PlayEnemyDeathSound()
    {
        PlaySoundEffect("enemydeath");
    }

    public void PlayEnemyShootSound()
    {
        PlaySoundEffect("enemyshoot");
    }

    public void PlayPlayerDeathSound()
    {
        PlaySoundEffect("playerdeath");
    }

    public void PlayPlayerShootSound()
    {
        PlaySoundEffect("playershoot");
    }

    public void PlayCriticalHitSound()
    {
        PlaySoundEffect("criticalhit");
    }

    public void PlayGiantKillerSound()
    {
        PlaySoundEffect("giantkiller");
    }

    public void PlayPlayerDamageSound()
    {
        PlaySoundEffect("playerdamage");
    }

    public void PlayBossAppearanceSound()
    {
        PlaySoundEffect("bossappearance");
    }

    public void PlayBossDeathSound()
    {
        PlaySoundEffect("bossdeath");
    }

    public void PlayBossLaserSound()
    {
        PlaySoundEffect("bosslaser");
    }

    public void PlayCommonDamageSound()
    {
        PlaySoundEffect("commondamage");
    }

    public void PlayDreamTrans12Sound()
    {
        PlaySoundEffect("dreamtrans12");
    }

    public void PlayDreamTrans23Sound()
    {
        PlaySoundEffect("dreamtrans23");
    }

    public void PlayDreamTrans34Sound()
    {
        PlaySoundEffect("dreamtrans34");
    }

    public void PlayMouseClickSound()
    {
        PlaySoundEffect("mouseclick");
    }

    public void PlayObjectDestructionSound()
    {
        PlaySoundEffect("objectdestruction");
    }

    public void PlayUpgradeSound()
    {
        PlaySoundEffect("upgrade");
    }
}
