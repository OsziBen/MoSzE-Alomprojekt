using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using static GameStateManager;

public class AudioManager : BasePersistentManager<AudioManager>
{
    private AudioSource SoundEffectPlayer; // A hangeffektek lejátszásáért felelős AudioSource.
    private AudioSource BackgroundMusicPlayer; // A háttérzene lejátszásáért felelős AudioSource. Loopol.

    private Dictionary<string, AudioClip> soundEffects; // A hangeffekteket tároló dictionary.
    private Dictionary<string, AudioClip> backgroundMusic; // A háttérzenéket tároló dictionary.

    private GameStateManager gameStateManager; // GameStateManager referencia
    private LevelManager levelManager; // LevelManager referencia.
    private GameSceneManager gameSceneManager; // GameSceneManager referencia

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
    /// Az Initialize feladata lényegében csak annyi, hogy assignolja a megfelelő AudioSource komponenseket a megfelelő változóhoz,
    /// beolvassa a listaként megadott audioclipeket a dictionary-kba, és feliratkozik eventekre.
    /// </summary>
    protected override void Initialize()
    {
        base.Initialize();
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

        gameStateManager = FindObjectOfType<GameStateManager>();
        gameStateManager.OnStateChanged += PlayMenuBGM;
        gameStateManager.OnStateChanged += PlayUpgradeSound;
        gameStateManager.OnStateChanged += PlayVictoryBGM;
        gameStateManager.OnStateChanged += PlayDefeatBGM;
        gameStateManager.OnLevelLoaded += PlayLevel1BGM;
        gameStateManager.OnLevelLoaded += PlayLevel2BGM;
        gameStateManager.OnLevelLoaded += PlayLevel3BGM;
        gameStateManager.OnLevelLoaded += PlayLevel4BGM;
        gameStateManager.OnLevelLoaded += PlayBossBGM;
        gameStateManager.OnCutsceneLoaded += PlayIntroBGM;
        gameStateManager.OnCutsceneLoaded += PlayDreamTrans12Sound;
        gameStateManager.OnCutsceneLoaded += PlayDreamTrans23Sound;
        gameStateManager.OnCutsceneLoaded += PlayDreamTrans34Sound;
        levelManager = FindObjectOfType<LevelManager>();
        levelManager.OnLevelLoaded += PlayBossBGM;
        gameSceneManager = FindObjectOfType<GameSceneManager>();
        gameSceneManager.OnCutsceneLoaded += PlayIntroBGM;
        gameSceneManager.OnCutsceneLoaded += PlayDreamTrans12Sound;
        gameSceneManager.OnCutsceneLoaded += PlayDreamTrans23Sound;
        gameSceneManager.OnCutsceneLoaded += PlayDreamTrans34Sound;
    }

    // async fgv feliratkozás
    // async fgv leiratkozás -- esetleg lvlManager

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


    public void PauseBGM()
    {
        if (BackgroundMusicPlayer.isPlaying)
        {
            BackgroundMusicPlayer.Pause();
        }
        else
        {
            Debug.LogWarning("A háttérzene nem szüneteltethető, mivel nem is megy.");
        }
    }

    public void ResumeBGM()
    {
        if (!BackgroundMusicPlayer.isPlaying)
        {
            BackgroundMusicPlayer.UnPause();
        }
        else
        {
            Debug.LogWarning("A háttérzene nem folytatható, mivel nem is megy.");
        }
    }

    // visszaadja, hogy a háttérzene pillanatnyilag fut-e.
    public bool IsBackgroundMusicPlaying()
    {
        return BackgroundMusicPlayer.isPlaying;
    }

    // Az összes háttérzene lejátszó függvényei
    public void PlayMenuBGM(GameState gameState)
    {

        if (gameState == GameState.MainMenu)
        {
            PlayBGM("menu");            
        }

    }

    public void PlayLevel1BGM(GameLevel gameLevel)
    {
        if (gameLevel == GameLevel.Level1)
        {
            PlayBGM("lvl1");                        
        }
    }

    public void PlayLevel2BGM(GameLevel gamelevel)
    {
        if (gamelevel == GameLevel.Level2)
        {
            PlayBGM("lvl2");
        }

    }

    public void PlayLevel3BGM(GameLevel gamelevel)
    {
        if (gamelevel == GameLevel.Level3)
        {
            PlayBGM("lvl3");
        }

    }

    public void PlayLevel4BGM(GameLevel gamelevel)
    {
        if (gamelevel == GameLevel.Level4)
        {
            PlayBGM("lvl4");
        }

    }

    public void PlayBossBGM(GameLevel gameLevel)
    {
        if (gameLevel == GameLevel.BossBattle)
        {
            PlayBGM("boss");
        }
    }

    public void PlayIntroBGM(string cutsceneName)
    {
        if (cutsceneName == "NewGame")
        {
            PlayBGM("intro");            
        }
    }

    public void PlayVictoryBGM(GameState gameState)
    {
        if (gameState == GameState.Victory)
        {
            PlayBGM("victory");
        }
    }

    public void PlayDefeatBGM(GameState gameState)
    {
        if (gameState == GameState.GameOver)
        {
            PlayBGM("defeat");
        }
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

    public void PlayDreamTrans12Sound(string cutsceneName)
    {
        if (cutsceneName == "LevelTransition12")
        {
            PlaySoundEffect("dreamtrans12");            
        }
    }

    public void PlayDreamTrans23Sound(string cutsceneName)
    {
        if (cutsceneName == "LevelTransition23")
        {
            PlaySoundEffect("dreamtrans23");            
        }
    }

    public void PlayDreamTrans34Sound(string cutsceneName)
    {
        if (cutsceneName == "LevelTransition34")
        {
            PlaySoundEffect("dreamtrans34");            
        }
    }

    public void PlayMouseClickSound()
    {
        PlaySoundEffect("mouseclick");
    }

    public void PlayObjectDestructionSound()
    {
        PlaySoundEffect("objectdestruction");
    }

    public void PlayUpgradeSound(GameState gameState)
    {
        if (gameState == GameState.PlayerUpgrade)
        {
            StopBGM();
            PlaySoundEffect("upgrade");            
        }
    }


    /// <summary>
    /// Leiratkozás az eventekről.
    /// </summary>
    private void OnDestroy()
    {
        if (gameStateManager != null)
        {
            gameStateManager.OnStateChanged -= PlayMenuBGM;
            gameStateManager.OnStateChanged -= PlayUpgradeSound;
            gameStateManager.OnStateChanged -= PlayVictoryBGM;
            gameStateManager.OnStateChanged -= PlayDefeatBGM;
            gameStateManager.OnLevelLoaded -= PlayLevel1BGM;
            gameStateManager.OnLevelLoaded -= PlayLevel2BGM;
            gameStateManager.OnLevelLoaded -= PlayLevel3BGM;
            gameStateManager.OnLevelLoaded -= PlayLevel4BGM;
            gameStateManager.OnLevelLoaded -= PlayBossBGM;
            gameStateManager.OnCutsceneLoaded -= PlayIntroBGM;
            gameStateManager.OnCutsceneLoaded -= PlayDreamTrans12Sound;
            gameStateManager.OnCutsceneLoaded -= PlayDreamTrans23Sound;
            gameStateManager.OnCutsceneLoaded -= PlayDreamTrans34Sound;
        }

        if(levelManager != null)
        {
            levelManager.OnLevelLoaded -= PlayBossBGM;
        }

        if(gameSceneManager != null)
        {
            gameSceneManager.OnCutsceneLoaded -= PlayIntroBGM;
            gameSceneManager.OnCutsceneLoaded -= PlayDreamTrans12Sound;
            gameSceneManager.OnCutsceneLoaded -= PlayDreamTrans23Sound;
            gameSceneManager.OnCutsceneLoaded -= PlayDreamTrans34Sound;
        }
    }
}
