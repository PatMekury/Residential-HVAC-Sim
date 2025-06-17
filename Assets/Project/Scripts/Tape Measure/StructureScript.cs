// StructureScript.cs
using UnityEngine;
using TMPro;

public class StructureScript : MonoBehaviour
{
    [Header("Measured Values")]
    public float measuredLength = 0f;
    public float measuredBreadth = 0f;
    public float calculatedSqFt = 0f;

    [Header("Status")]
    public bool isLengthMeasured = false;
    public bool isBreadthMeasured = false;

    [Header("UI (Optional)")]
    public TextMeshProUGUI sqFtDisplay;

    public void RecordMeasurement(EdgeDimensionType dimensionType, float value)
    {
        if (dimensionType == EdgeDimensionType.Length)
        {
            measuredLength = value;
            isLengthMeasured = true;
            Debug.Log($"{gameObject.name}: Length measured: {FormatMeasurement(value)}");
        }
        else if (dimensionType == EdgeDimensionType.Breadth)
        {
            measuredBreadth = value;
            isBreadthMeasured = true;
            Debug.Log($"{gameObject.name}: Breadth measured: {FormatMeasurement(value)}");
        }

        TryCalculateSqFt();
    }

    void TryCalculateSqFt()
    {
        if (isLengthMeasured && isBreadthMeasured)
        {
            calculatedSqFt = measuredLength * measuredBreadth;
            Debug.Log($"{gameObject.name}: Area calculated: {calculatedSqFt:F2} sq ft");
            if (sqFtDisplay != null)
            {
                sqFtDisplay.text = $"Area: {calculatedSqFt:F2} sq ft";
            }
        }
    }

    public static string FormatMeasurement(float measurementInFeet)
    {
        int feet = Mathf.FloorToInt(measurementInFeet);
        float fractionalFeet = measurementInFeet - feet;
        int inches = Mathf.RoundToInt(fractionalFeet * 12f);

        if (inches == 12)
        {
            feet++;
            inches = 0;
        }
        return $"{feet}' {inches}\"";
    }

    public void ResetMeasurements()
    {
        measuredLength = 0f;
        measuredBreadth = 0f;
        calculatedSqFt = 0f;
        isLengthMeasured = false;
        isBreadthMeasured = false;
        if (sqFtDisplay != null)
        {
            sqFtDisplay.text = "Area: N/A";
        }
        Debug.Log($"{gameObject.name}: Measurements reset.");
    }
}