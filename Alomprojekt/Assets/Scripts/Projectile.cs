using UnityEngine;
using UnityEngine.InputSystem;


public class Projectile : MonoBehaviour
{
    /// <summary>
    /// Változók
    /// </summary>
    public float deleteDistance = 25.0f;    // A lövedék maximálisan megtehetõ távolsága, amely után törlésre kerül
    public int force = 3;                   // Az alkalmazott erõ, amely meghatározza a lövedék indításának intenzitását



    /// <summary>
    /// Komponenesek
    /// </summary>
    Rigidbody2D rigidbody2d;    // Lövedékhez kapcsolódó Rigidbody2D komponens


    /// <summary>
    /// Getterek és Setterek
    /// </summary>


    /// <summary>
    /// Események
    /// </summary>
    /// </summary>


    /// <summary>
    /// Inicializálja a Rigidbody2D komponenst a GameObject-hez tartozó fizikai interakciókhoz.
    /// </summary>
    void Awake()
    {
        rigidbody2d = GetComponent<Rigidbody2D>();
    }


    /// <summary>
    /// Minden egyes frissítéskor ellenõrzi, hogy a GameObject távolsága meghaladja-e a törléshez
    /// szükséges határértéket. Ha igen, akkor törli a GameObject-et.
    /// </summary>
    void Update()
    {
        if (transform.position.magnitude > deleteDistance)
        {
            Destroy(gameObject);
        }
    }


    /// <summary>
    /// Elindítja a GameObject-et egy adott irányba és erõvel a Rigidbody2D komponens segítségével.
    /// </summary>
    /// <param name="direction">Az irány, amelybe a GameObject-et el kell indítani</param>
    /// <param name="force">Az erõ, amellyel a GameObject-nek mozognia kell</param>
    public void Launch(Vector2 direction, float force)
    {
        rigidbody2d.AddForce(direction * force);
    }


    /// <summary>
    /// Eseménykezelõ, amely akkor hívódik meg, amikor a GameObject egy másik objektummal ütközik.
    /// A kollízióról debug üzenetet ír ki a konzolra, majd törli a GameObject-et.
    /// </summary>
    /// <param name="collision">Az ütközõ objektum, amellyel a GameObject kollidált</param>
    void OnCollisionEnter2D(Collision2D collision)
    {

        Debug.Log("Projectile collision with " + collision.gameObject);
        Destroy(gameObject);
    }
}
