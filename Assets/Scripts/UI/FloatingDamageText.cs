using UnityEngine;
using TMPro;

public class FloatingDamageText : MonoBehaviour
{
    [Header("Text Configurations")]
    public TMP_Text textComponent;
    public float floatSpeed = 2.5f;
    public float lifetime = 1.0f;

    [Header("Color Palettes")]
    public Color normalHitColor = Color.white;
    public Color criticalHitColor = Color.yellow;
    public Color missColor = Color.gray;
    public Color supportHealColor = new Color(0.2f, 0.85f, 0.3f, 1f); // Green for healing

    // 🩸 NEW SCARLET RED PALETTE FOR HP COSTS
    public Color hpCostColor = new Color(0.85f, 0.05f, 0.05f, 1f);    // Scarlet Red

    public Color manaRestoreColor = new Color(0f, 0.9f, 1f, 1f);
    public Color staminaRestoreColor = new Color(1f, 0.7f, 0f, 1f);
    public Color energyRestoreColor = new Color(0.9f, 0.2f, 1f, 1f);


    private Color targetColor;
    private float elapsed = 0f;

    /// <summary>
    /// Sets up the text context parameters smoothly before beginning movement rendering.
    /// </summary>
    public void InitializeDisplay(string textValue, bool isCritical, bool isMiss)
    {
        if (textComponent == null) textComponent = GetComponentInChildren<TMP_Text>();
        if (textComponent == null) return;

        textComponent.text = textValue;

        // 🛠️ RICH TEXT BYPASS LOOKUP: If text already has custom inner hex colors embedded, don't overwrite it!
        if (textValue.Contains("<color="))
        {
            // Set text alpha fallback to full capacity and exit early to protect embedded colors
            textComponent.color = Color.white;
        }
        else
        {
            // Apply standard baseline palettes for physical raw damage hits and misses
            if (isMiss)
                targetColor = missColor;
            else if (isCritical)
                targetColor = criticalHitColor;
            else
                targetColor = normalHitColor;

            textComponent.color = targetColor;
        }

        if (isCritical) transform.localScale *= 1.4f;

        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        // Smoothly translate upwards in 3D world space coordinate paths
        transform.Translate(Vector3.up * floatSpeed * Time.deltaTime, Space.World);

        // Billboard orientation effect: Forces text to face the battle camera perfectly
        if (Camera.main != null)
        {
            transform.LookAt(transform.position + Camera.main.transform.forward);
        }

        // Smoothly fade transparency out over time channel loops
        elapsed += Time.deltaTime;
        if (textComponent != null)
        {
            float alpha = Mathf.Lerp(1f, 0f, elapsed / lifetime);
            Color fadeColor = textComponent.color;
            fadeColor.a = alpha;
            textComponent.color = fadeColor;
        }
    }
}
