using UnityEngine;

public class AnimationEventReceiver : MonoBehaviour
{
    private BattleFieldMode battlefieldMode;

    private void Awake()
    {
        battlefieldMode = FindFirstObjectByType<BattleFieldMode>();

        if (battlefieldMode == null)
        {
            Debug.LogWarning($"[AnimationEventReceiver] Could not find an active BattleFieldMode instance in the scene on {gameObject.name}.");
        }
    }

    /// <summary>
    /// Combined Event: Triggers both the visual explosion and subsequent damage/text popups sequentially.
    /// </summary>
    public void OnHitImpactTrigger()
    {
        if (battlefieldMode != null)
        {
            battlefieldMode.ExecuteAnimationHitImpactEvent();
        }
    }
}
