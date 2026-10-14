using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerEyesAnim : MonoBehaviour
{
    [Header("Blink Materials")]
    public Renderer faceRenderer;     // Drag your character's Renderer here
    public Material openEyesMaterial; // Drag the "open eyes" material
    public Material closeEyesMaterial; // Drag the "closed eyes" material
    public PlayerBlinkMaterial playerBlinkscript;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void SetEyetoSleep()
    {
        faceRenderer.material = closeEyesMaterial;
        playerBlinkscript.enabled = false;
    }
    public void SetEyetoAwake()
    {
        faceRenderer.material = openEyesMaterial;
        playerBlinkscript.enabled = false;
    }
}
