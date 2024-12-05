using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleSpawner : Assets.Scripts.SpawnerBase
{
    public GameObject obstacle; // a spawnolandó objektum megadása

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// A SpawnerBase osztály Place() függvényének kiegészítése
    /// a spawnerhez specifikus gameobject instanciálásával.
    /// </summary>
    public override void Place()
    {
        base.Place();

        // Instantiate the enemy at the calculated position
        Instantiate(obstacle, spawnPosition, Quaternion.identity);
    }

    /// <summary>
    /// Az editorban való könnyű szerkesztéshez egy kört helyezünk el a
    /// spawnoló területre.
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(transform.position, spawnRadius);
    }
}
