using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.InputManagerEntry;

public class PlayerSpawner : Assets.Scripts.SpawnerBase
{
    public PlayerController player; // Játékos karakter prefab

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Activate()
    {
        var spawnedPlayer = PlacePlayer(); // Elmentjük az instanciált játékost egy változóba.

        CinemachineVirtualCamera vcam = FindObjectOfType<CinemachineVirtualCamera>(); // Megkeressük a scene-ben a kamerát.
        vcam.LookAt = spawnedPlayer.transform; // Ráállítjuk a kamera követését
        vcam.Follow = spawnedPlayer.transform; // a játékos transformjára.

        Destroy(gameObject); // A játékos lespawnolása után a spawnerre nincs szükség, töröljük.
    }

    /// <summary>
    /// Elhelyezi a játékos karaktert a pályán.
    /// Az ősosztályból származó metódussal ellentétben van visszatérési érték.
    /// Ez az érték maga az instanciált karakter, amihez hozzáköthetjük a kamerát.
    /// </summary>
    /// <returns></returns>
    public PlayerController PlacePlayer()
    {
        base.Place();

        // Instantiate the enemy at the calculated position
        var spawnedPlayer = Instantiate(player, spawnPosition, Quaternion.identity);

        return spawnedPlayer;
    }

    /// <summary>
    /// Az editorban való könnyű szerkesztéshez egy kört helyezünk el a
    /// spawnoló területre.
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, spawnRadius);
    }

}
