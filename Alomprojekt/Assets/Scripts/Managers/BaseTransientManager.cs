using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Az osztály egy generikus típus, amely csak olyan típusokat enged, amelyek az
// BaseTransientManager<T> osztályt öröklik (azaz önmagukat vagy leszármazottaikat).
// Ez lehetővé teszi, hogy több különböző menedzsert használjunk ugyanebben a rendszerben.
public abstract class BaseTransientManager<T> : MonoBehaviour where T : BaseTransientManager<T>
{
    // A statikus példányt tárolja, amely mindig ugyanazt az objektumot képviseli,
    // ha a menedzsert más részekből is el akarjuk érni.
    public static T Instance { get; private set; }

    // Itt a cél, hogy biztosítsuk, hogy csak egy példány létezik a menedzserből.
    protected virtual void Awake()
    {
        // Ha az Instance még nincs beállítva (még nem létezett példány),
        // akkor beállítjuk azt az aktuális példányra, és meghívjuk az Initialize metódust.
        if (Instance == null)
        {
            Instance = (T)this;
            Initialize();
        }
        // Ha már létezik egy példány, töröljük az aktuális objektumot.
        // Ezzel megakadályozzuk, hogy több példány létezzen egyszerre.
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
