using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerProfile : MonoBehaviour
{
    public string PlayerName;
    public int PlayerGolds;
    public int PlayerLumbers;
    public string PlayerHealthStatus = "Normal"; // Normal, Fatigue, Sick, NearDeath
    public int PlayerStamina;

    public string PlayerGender; // Boy or Girl

    void Start() { }
    void Update() { }
}
