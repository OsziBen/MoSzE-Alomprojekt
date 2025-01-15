using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    public List<StaticObstacleController> obstacles; // a spawnolandó objektum megadása

    /// <summary>
    /// A SpawnerBase osztály Place() függvényének kiegészítése
    /// a spawnerhez specifikus gameobject instanciálásával.
    /// </summary>
    public void Place()
    {
        bool isHeads = UnityEngine.Random.Range(0, 2) == 0; // Random bool generálás.
        // Random bool alapján a 2 obstacle típus közül az egyiket lehelyezzük.
        if(isHeads)
        {
            Instantiate(obstacles[0], transform.position, Quaternion.identity);
        } else
        {
            Instantiate(obstacles[1], transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// Az editorban való könnyű szerkesztéshez egy kört helyezünk el a
    /// spawnoló területre.
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(transform.position, 5);
    }
}
