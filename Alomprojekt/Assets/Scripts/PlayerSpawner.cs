using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.InputManagerEntry;

public class PlayerSpawner : Assets.Scripts.SpawnerBase
{
    public PlayerController player;

    // Start is called before the first frame update
    void Start()
    {
        Place();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override void Place()
    {
        base.Place();

        // Instantiate the enemy at the calculated position
        var spawnedPlayer = Instantiate(player, spawnPosition, Quaternion.identity);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, spawnRadius);
    }

}
