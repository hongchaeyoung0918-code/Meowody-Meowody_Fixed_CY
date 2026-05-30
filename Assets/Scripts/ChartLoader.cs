using UnityEngine;
using System.IO;

public class ChartLoader : MonoBehaviour
{
    /*    public static ChartData LoadChart(string filePath)
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
        }*/
    public static ChartData LoadChart(string jsonString)
    {
        if (string.IsNullOrEmpty(jsonString))
        {
            Debug.LogError("ChartLoader: Received empty JSON string.");
            return null;
        }

        try
        {
            // 전달받은 문자열을 바로 파싱
            ChartData loadedData = JsonUtility.FromJson<ChartData>(jsonString);

            // ChartData가 null이거나 notes가 null이면 파싱 실패로 간주
            if (loadedData == null || loadedData.notes == null)
            {
                Debug.LogError("ChartLoader: Failed to parse ChartData. Check JSON format.");
                return null;
            }

            Debug.Log($"Chart loaded successfully. Total notes: {loadedData.notes.Count}");
            return loadedData;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading chart: {e.Message}");
            return null;
        }
    }
}
