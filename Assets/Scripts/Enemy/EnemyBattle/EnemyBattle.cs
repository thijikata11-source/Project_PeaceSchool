using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBattle : MonoBehaviour
{
    [Header("Enemy Squad Configuration")]
    [Tooltip("Drag your enemy prefabs here. Maximum allowed is 10.")]
    public List<GameObject> enemyPrefabs = new List<GameObject>();

    [Tooltip("Check this if this encounter is a Boss Fight to utilize the unique boss anchor position.")]
    public bool isBossEncounter = false;

    [Header("Fade Clip Settings")]
    public Animator fadeAnimator;
    public string fadeInTrigger = "FadeIn";
    public string fadeOutTrigger = "FadeOut";

    [Tooltip("Time it takes for the FadeIn animation to complete")]
    public float fadeInDuration = 1.0f;

    [Tooltip("Time it takes for the FadeOut animation to complete")]
    public float fadeOutDuration = 1.0f;

    public BattleFieldMode BFM;

    private void OnValidate()
    {
        // Enforce the inspector restriction cleanly up to 10 elements
        if (enemyPrefabs.Count > 10)
        {
            Debug.LogWarning("EnemyBattle: You can only assign a maximum of 10 enemies per encounter.", gameObject);
            enemyPrefabs.RemoveRange(10, enemyPrefabs.Count - 10);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            DisableAllOverworldMovementAndFollowers();
            StartCoroutine(FullBattleTransitionRoutine(other.gameObject));
        }
    }

    private void DisableAllOverworldMovementAndFollowers()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            Animator animator = player.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.SetFloat("Speed", 0f);
                animator.SetFloat("MoveX", 0f);
                animator.SetFloat("MoveZ", 0f);
                animator.SetBool("isSprinting", false);
            }

            PlayerMovement3D movement = player.GetComponent<PlayerMovement3D>();
            if (movement != null)
            {
                movement.ResetMovementStates();
                movement.enabled = false;
            }

            MonoBehaviour npcFollow = player.GetComponent("NPCFollowPlayer") as MonoBehaviour;
            if (npcFollow != null) npcFollow.enabled = false;

            MonoBehaviour accurateFollow = player.GetComponent("ObjectAccurateFollower") as MonoBehaviour;
            if (accurateFollow != null) accurateFollow.enabled = false;

            MonoBehaviour ikLook = player.GetComponent("PlayerIKLook") as MonoBehaviour;
            if (ikLook != null) ikLook.enabled = false;

            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null) rb.velocity = Vector3.zero;
        }
    }

    private IEnumerator FullBattleTransitionRoutine(GameObject triggeringUnit)
    {
        if (fadeAnimator != null) fadeAnimator.SetTrigger(fadeInTrigger);

        yield return new WaitForSeconds(fadeInDuration);

        if (BFM != null)
        {
            // Pushes target reference AND passing operational squad data forward securely
            BFM.InitiateBattleData(triggeringUnit, enemyPrefabs, isBossEncounter);
            BFM.BattleMode = true;
        }

        if (fadeAnimator != null) fadeAnimator.SetTrigger(fadeOutTrigger);

        yield return new WaitForSeconds(fadeOutDuration);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (BFM != null) BFM.BattleMode = false;
        }
    }
}
