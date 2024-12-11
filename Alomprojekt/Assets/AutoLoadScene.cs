using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

[InitializeOnLoad] // Ez az attrib�tum biztos�tja, hogy a szkript automatikusan futni kezd az Editor bet�lt�sekor.
public class AutoLoadScene
{
    static AutoLoadScene()
    {
        // Be�ll�tjuk a k�v�nt scene el�r�si �tj�t
        string scenePath = "Assets/Scenes/ManagersTestScene.unity"; // Cser�ld ki a helyes el�r�si �tra

        // Ellen�rizz�k, hogy a scene m�g nincs megnyitva
        if (EditorSceneManager.GetActiveScene().path != scenePath)
        {
            // Bet�ltj�k a k�v�nt scene-t
            EditorSceneManager.OpenScene(scenePath);
        }
    }
}
