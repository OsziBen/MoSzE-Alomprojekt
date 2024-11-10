using UnityEngine;


public class Projectile : MonoBehaviour
{
    Rigidbody2D rigidbody2d;
    public float deleteDistance = 25.0f;
    public int force = 3;

    void Awake()
    {
        rigidbody2d = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (transform.position.magnitude > deleteDistance)
        {
            Destroy(gameObject);
        }
    }

    public void Launch(Vector2 direction, float force)
    {
        rigidbody2d.AddForce(direction * force);
    }

    void OnCollisionEnter2D(Collision2D other)
    {

        Debug.Log("Projectile collision with " + other.gameObject);
        Destroy(gameObject);
    }
}
