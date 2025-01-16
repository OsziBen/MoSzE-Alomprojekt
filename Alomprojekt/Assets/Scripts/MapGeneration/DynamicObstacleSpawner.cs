using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using UnityEngine.UIElements;

public class DynamicObstacleSpawner : MonoBehaviour
{
    /*
    // Teszteléshez
    public Transform objectToSpawn; // A spawnolt aszteroida
    public Transform playerCharacter;        // A játékos karakter.
    public float launchSpeed = 8f;  // Az aszteroida sebessége
    */

    /// <summary>
    /// A pálya oldalain random elhelyez egy meteort, melyet a player irányába indít el.
    /// </summary>
    public void SpawnMeteor(Transform meteor, Transform player, float launchSpeed)
    {
        int side = Random.Range(0, 4); // Eldönti, hogy a pálya melyik oldalán spawnolja le az objektumot.
        Vector2 spawnPosition = Vector2.zero; // inicializáljuk az aszteroida pozícióját.
        SpriteRenderer spriteRenderer = gameObject.GetComponent<SpriteRenderer>(); // Lekérjük a háttér spriterendererjét.
        Vector2 spriteSize = spriteRenderer.bounds.size; // Lekérjük a background spriterendererjének boundját,
        Vector2 position = spriteRenderer.transform.position; // és pozícióját.
        // A oldalak meghatározásához szükséges adatok kinyerése
        Vector2 topLeft = new Vector2(position.x - spriteSize.x / 2, position.y + spriteSize.y / 2);
        Vector2 bottomRight = new Vector2(position.x + spriteSize.x / 2, position.y - spriteSize.y / 2);
        Vector2 boundingBoxMin = new Vector2(topLeft.x + 0.5f, bottomRight.y + 0.5f);
        Vector2 boundingBoxMax = new Vector2(bottomRight.x - 0.5f, topLeft.y - 0.5f); ;

        // 
        switch (side)
        {
            case 0: // Pálya felső oldala
                spawnPosition = new Vector2(Random.Range(boundingBoxMin.x, boundingBoxMax.x), boundingBoxMax.y);
                break;
            case 1: // Pálya alsó oldala
                spawnPosition = new Vector2(Random.Range(boundingBoxMin.x, boundingBoxMax.x), boundingBoxMin.y);
                break;
            case 2: // Pálya bal oldala
                spawnPosition = new Vector2(boundingBoxMin.x, Random.Range(boundingBoxMin.y, boundingBoxMax.y));
                break;
            case 3: // Pálya jobb oldala
                spawnPosition = new Vector2(boundingBoxMax.x, Random.Range(boundingBoxMin.y, boundingBoxMax.y));
                break;
        }

        // Az aszteroida lehelyezése a kiválasztott pozíción
        Transform spawnedObject = Instantiate(meteor, spawnPosition, Quaternion.identity);

        // Az player aszteroidától mért irányának kiszámítása
        Vector2 direction = (player.position - spawnedObject.position).normalized;

        // Velocity vekotr beállítása a player irányába launchSpeed sebességgel
        Rigidbody2D rb = spawnedObject.GetComponent<Rigidbody2D>();
        rb.velocity = direction * launchSpeed;
    }


    /*
    // Teszteléshez
    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            SpawnMeteor(objectToSpawn, playerCharacter, launchSpeed);
            yield return new WaitForSeconds(2f);
        }
    }

    private void Start()
    {
        StartCoroutine(SpawnRoutine());
    }
    */

}
