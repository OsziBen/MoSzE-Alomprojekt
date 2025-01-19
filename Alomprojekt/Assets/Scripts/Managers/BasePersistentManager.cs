using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Az osztály egy generikus típus, amely csak olyan típusokat enged, amelyek az
// BasePersistentManager<T> osztályt öröklik (azaz önmagukat vagy leszármazottaikat).
// Ez lehetővé teszi, hogy több különböző menedzsert használjunk ugyanebben a rendszerben.
public abstract class BasePersistentManager<T> : MonoBehaviour where T : BasePersistentManager<T>
{
    // A statikus példányt tárolja, amely mindig ugyanazt az objektumot képviseli,
    // ha a menedzsert más részekből is el akarjuk érni.
    public static T Instance { get; private set; }

    // Itt a cél, hogy biztosítsuk, hogy csak egy példány létezik a menedzserből,
    // és hogy az objektum ne tűnjön el, amikor új jelenetet töltünk be.
    protected virtual void Awake()
    {
        // Ha az Instance még nincs beállítva (még nem létezett példány),
        // akkor beállítjuk azt az aktuális példányra, és meghívjuk az Initialize metódust.
        // Emellett a DontDestroyOnLoad(gameObject) biztosítja, hogy az objektum
        // megmaradjon a jelenetek között.
        if (Instance == null)
        {
            Instance = (T)this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        // Ha már létezik egy példány, töröljük az aktuális objektumot,
        // hogy ne legyen több példányunk a játékban.
        else
        {
            Destroy(gameObject);
        }
    }

    // Az Initialize metódus inicializálási feladatokat végez el, és virtuális,
    // tehát felülírható a származtatott osztályokban.
    // A "Hello, I am" üzenet a Debug konzolba írja ki az aktuális objektum nevét.
    protected virtual async void Initialize()
    {
        Debug.Log("Hello, I am " + this.name);
    }

}
