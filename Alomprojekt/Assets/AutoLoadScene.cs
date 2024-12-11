using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

[InitializeOnLoad] // Ez az attribútum biztosítja, hogy a szkript automatikusan futni kezd az Editor betöltésekor.
public class AutoLoadScene
{
    static AutoLoadScene()
    {
        // Beállítjuk a kívánt scene elérési útját
        string scenePath = "Assets/Scenes/ManagersTestScene.unity"; // Cseréld ki a helyes elérési útra

        // Ellenõrizzük, hogy a scene még nincs megnyitva
        if (EditorSceneManager.GetActiveScene().path != scenePath)
        {
            // Betöltjük a kívánt scene-t
            EditorSceneManager.OpenScene(scenePath);
        }
    }
}
