using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;


public class MindReading : MonoBehaviour
{



    public PartyMembers PMembers;
    public Button RB2;


    [Header("Timer Settings")]
    public float timer = 5f;
    public float timeValue =5f;
    public bool timerStart = false;

    public float coolDown=30f;
    public float coolDownValue = 30f;


    public SkinnedMeshRenderer skinnedMeshRenderer;
    public Material replacementMaterialForSlot2;
    public Material replacementMaterialForSlot3;

    public Material oldMaterialForSlot2;
    public Material oldMaterialForSlot3;

    [SerializeField] private MonoBehaviour[] scriptsToDisable;

    public void SwapSlots()
    {
        if (skinnedMeshRenderer == null) return;

        Material[] currentMaterials = skinnedMeshRenderer.materials;

        if (currentMaterials.Length > 2)
        {

            if (replacementMaterialForSlot2 != null) currentMaterials[1] = replacementMaterialForSlot2;
            if (replacementMaterialForSlot3 != null) currentMaterials[2] = replacementMaterialForSlot3;
            skinnedMeshRenderer.materials = currentMaterials;
        }
    }

    public void RevertSlots()
    {
        if (skinnedMeshRenderer == null) return;

        Material[] currentMaterials = skinnedMeshRenderer.materials;

        if (currentMaterials.Length > 2)
        {

            if (oldMaterialForSlot2 != null) currentMaterials[1] = oldMaterialForSlot2;
            if (oldMaterialForSlot3 != null) currentMaterials[2] = oldMaterialForSlot3;
            skinnedMeshRenderer.materials = currentMaterials;
        }
    }



    public void LB2Down()
    {
        if (PMembers.currentFrontId == 0 && coolDown>=coolDownValue)
        {
                PMembers.currentFrontId = 100;
                RB2.enabled = false;
                RB2.gameObject.SetActive(false);
            timerStart = true;
                coolDown = 0f;
                SwapSlots();
                //EnableScripts();

            /* NPCFollowPlayer[] allNPCs = Object.FindObjectsByType<NPCFollowPlayer>(FindObjectsSortMode.None);

             foreach (NPCFollowPlayer npc in allNPCs)
             {
                 GameObject npcGameObject = npc.gameObject;
                 Debug.Log("Found NPC on GameObject: " + npcGameObject.name);


                 int temporaryIdFront = 0;

                 if (npc.characters == null || npc.characters.Length == 0)
                     return;

                 npc.player = npc.characters[temporaryIdFront].transform;

                 if (npc.player == null)
                     return;
             }
             */

        }
    }


    void Update()
    {
        // Only run if timerStart is true
        if (timerStart)
        {
            if (timer > 0)
            {
                timer -= Time.deltaTime;
            }
            else
            {
                timer = timeValue;
                timerStart = false;
                PMembers.currentFrontId = 0;
                RB2.enabled = true;
                RB2.gameObject.SetActive(true);
                RevertSlots();
                DisableScripts();
            }
        }


        if (coolDown >= 0)
        {
            coolDown += Time.deltaTime;

            if (coolDown >= coolDownValue)
            {
                coolDown = coolDownValue;
            }
        }


    }


    public void DisableScripts()
    {
        foreach (MonoBehaviour script in scriptsToDisable)
        {
            // Check if the slot is not empty to avoid errors
            if (script != null)
            {
                script.enabled = false;
            }
        }
    }

    public void EnableScripts()
    {
        foreach (MonoBehaviour script in scriptsToDisable)
        {
            // Check if the slot is not empty to avoid errors
            if (script != null)
            {
                script.enabled = true;
            }
        }
    }

}
