using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class SaveLoadManager : BaseTransientManager<SaveLoadManager>
{
    public async Task<bool> SaveStateAsync(int level)
    {
        try
        {
            await Task.Delay(1000);  // Szimuláljuk a betöltési időt
            Debug.Log($"The data of Level {level} has been saved.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.Log($"Error saving data: {ex.Message}");
            return false;
        }
    }
}
