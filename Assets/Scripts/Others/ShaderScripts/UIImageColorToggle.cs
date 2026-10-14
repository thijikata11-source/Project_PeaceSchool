using UnityEngine;
using UnityEngine.UI;

public class UIImageColorToggle : MonoBehaviour
{
    [Header("UI Image References")]
    [SerializeField] private Image imageA;
    [SerializeField] private Image imageB;

    [Header("Colors")]
    [SerializeField] private Color colorA = Color.white;
    [SerializeField] private Color colorB = Color.white;

    public void SetColors(Color newColorA, Color newColorB)
    {
        colorA = newColorA;
        colorB = newColorB;
        Debug.Log($"[UIImageColorToggle] SetColors called: A={colorA}, B={colorB}");
    }

    public void ApplyColors()
    {
        if (imageA != null)
        {
            imageA.color = colorA;
            Debug.Log($"[UIImageColorToggle] Applied colorA to imageA: {colorA}");
        }
        else
        {
            Debug.LogWarning("[UIImageColorToggle] imageA is not assigned.");
        }

        if (imageB != null)
        {
            imageB.color = colorB;
            Debug.Log($"[UIImageColorToggle] Applied colorB to imageB: {colorB}");
        }
        else
        {
            Debug.LogWarning("[UIImageColorToggle] imageB is not assigned.");
        }
    }
}
