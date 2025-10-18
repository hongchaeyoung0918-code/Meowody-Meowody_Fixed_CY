using UnityEngine;
using System.IO;

public class ChartLoader : MonoBehaviour
{
    public static ChartData LoadChart(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"Chart file not found at path: {filePath}");
            return null;
        }

        try
        {
            string jsonString = File.ReadAllText(filePath);

            ChartData loadedData = JsonUtility.FromJson<ChartData>(jsonString);

            Debug.Log($"Chart loaded successfully fromTotal notes: {loadedData.notes.Count}");
            return loadedData;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading chart from path: {filePath}\n{e.Message}");
            return null;
        }
    }
}
