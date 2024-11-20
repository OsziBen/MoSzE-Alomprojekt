using UnityEngine;
using UnityEngine.InputSystem;


public class Projectile : MonoBehaviour
{
    /// <summary>
    /// V�ltoz�k
    /// </summary>
    public float deleteDistance = 25.0f;    // A l�ved�k maxim�lisan megtehet� t�vols�ga, amely ut�n t�rl�sre ker�l
    public int force = 3;                   // Az alkalmazott er�, amely meghat�rozza a l�ved�k ind�t�s�nak intenzit�s�t



    /// <summary>
    /// Komponenesek
    /// </summary>
    Rigidbody2D rigidbody2d;    // L�ved�khez kapcsol�d� Rigidbody2D komponens


    /// <summary>
    /// Getterek �s Setterek
    /// </summary>


    /// <summary>
    /// Esem�nyek
    /// </summary>
    /// </summary>


    /// <summary>
    /// Inicializ�lja a Rigidbody2D komponenst a GameObject-hez tartoz� fizikai interakci�khoz.
    /// </summary>
    void Awake()
    {
        rigidbody2d = GetComponent<Rigidbody2D>();
    }


    /// <summary>
    /// Minden egyes friss�t�skor ellen�rzi, hogy a GameObject t�vols�ga meghaladja-e a t�rl�shez
    /// sz�ks�ges hat�r�rt�ket. Ha igen, akkor t�rli a GameObject-et.
    /// </summary>
    void Update()
    {
        if (transform.position.magnitude > deleteDistance)
        {
            Destroy(gameObject);
        }
    }


    /// <summary>
    /// Elind�tja a GameObject-et egy adott ir�nyba �s er�vel a Rigidbody2D komponens seg�ts�g�vel.
    /// </summary>
    /// <param name="direction">Az ir�ny, amelybe a GameObject-et el kell ind�tani</param>
    /// <param name="force">Az er�, amellyel a GameObject-nek mozognia kell</param>
    public void Launch(Vector2 direction, float force)
    {
        rigidbody2d.AddForce(direction * force);
    }


    /// <summary>
    /// Esem�nykezel�, amely akkor h�v�dik meg, amikor a GameObject egy m�sik objektummal �tk�zik.
    /// A koll�zi�r�l debug �zenetet �r ki a konzolra, majd t�rli a GameObject-et.
    /// </summary>
    /// <param name="collision">Az �tk�z� objektum, amellyel a GameObject kollid�lt</param>
    void OnCollisionEnter2D(Collision2D collision)
    {

        Debug.Log("Projectile collision with " + collision.gameObject);
        Destroy(gameObject);
    }
}
