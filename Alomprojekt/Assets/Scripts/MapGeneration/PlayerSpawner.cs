using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    public PlayerController player; // Játékos karakter prefab

    /// <summary>
    /// Meghívjuk a PlacePlayer() függvényt, majd hozzákötjük a pálya kamerájához az visszaadott játékos karaktert.
    /// </summary>
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
    /// A visszatérési érték maga az instanciált karakter, amihez hozzáköthetjük a kamerát.
    /// </summary>
    /// <returns></returns>
    public PlayerController PlacePlayer()
    {
        var spawnedPlayer = Instantiate(player, transform.position, Quaternion.identity);

        return spawnedPlayer;
    }

    /// <summary>
    /// Az editorban való könnyű szerkesztéshez egy kört helyezünk el a
    /// spawnoló területre.
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, 2);
    }

}
