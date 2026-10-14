using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class EnemyAnimationsTrigger : MonoBehaviour
{
    [System.Serializable]
    public class CustomActionClip
    {
        public AnimationClip clip;
        [Tooltip("Check this box if this specific animation is a ranged action (stands still). Leave unchecked for melee (runs up).")]
        public bool isRangedAction = false;
    }
    [Header("Core Action Animation Clips")]
    public CustomActionClip attackClip;
    public CustomActionClip defendClip;
    public CustomActionClip itemClip;
    public CustomActionClip weaponClip;
    public CustomActionClip runClip;
    public CustomActionClip hurtClip;
    // 💨 NEW RAW DODGE & DEFEAT FILE SLOTS
    public CustomActionClip evadeClip;
    public CustomActionClip downClip;


    [Header("Enemy Special Skill Set Matrix")]
    public List<SkillScriptable> enemySkillSet = new List<SkillScriptable>();

    [Header("Dynamic Raw Skill & Special Attack Animation Assets")]
    public List<AnimationClip> customSkillClips = new List<AnimationClip>();
    public List<bool> customClipsRangeToggle = new List<bool>();

    private Animator enemyAnimator;
    private PlayableGraph playableGraph;
    private AnimationPlayableOutput playableOutput;

    private void Awake()
    {
        enemyAnimator = GetComponentInChildren<Animator>();
    }

    private void OnValidate()
    {
        while (customClipsRangeToggle.Count < customSkillClips.Count) customClipsRangeToggle.Add(false);
        while (customClipsRangeToggle.Count > customSkillClips.Count) customClipsRangeToggle.RemoveAt(customClipsRangeToggle.Count - 1);
    }

    private void OnDestroy()
    {
        CleanPlayableGraph();
    }

    public void PlayDynamicClip(AnimationClip clip)
    {
        if (clip == null || enemyAnimator == null) return;

        CleanPlayableGraph();

        playableGraph = PlayableGraph.Create(gameObject.name + "_EnemyGraph");
        playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        playableOutput = AnimationPlayableOutput.Create(playableGraph, "Animation", enemyAnimator);

        AnimationClipPlayable clipPlayable = AnimationClipPlayable.Create(playableGraph, clip);
        playableOutput.SetSourcePlayable(clipPlayable);

        playableGraph.Play();

        // Safe buffer subtraction to prevent end-of-clip loop hitching
        float safeCleanupTime = Mathf.Max(0.05f, clip.length - 0.03f);
        Invoke(nameof(CleanPlayableGraph), safeCleanupTime);
    }

    public bool TriggerAttackAnimation() { if (attackClip.clip != null) PlayDynamicClip(attackClip.clip); return attackClip.isRangedAction; }

    private void CleanPlayableGraph()
    {
        if (playableGraph.IsValid())
        {
            playableGraph.Stop();
            playableGraph.Destroy();
        }
    }
    /// <summary>
    /// Plays the dedicated raw hurt/damage animation asset instantly.
    /// </summary>
    public bool TriggerHurtAnimation()
    {
        if (hurtClip != null && hurtClip.clip != null)
        {
            PlayDynamicClip(hurtClip.clip);
        }
        return hurtClip != null && hurtClip.isRangedAction;
    }


    // 💨 NEW DYNAMIC EVADE & DOWN HOOKS
    public bool TriggerEvadeAnimation() { if (evadeClip != null && evadeClip.clip != null) PlayDynamicClip(evadeClip.clip); return evadeClip != null && evadeClip.isRangedAction; }
    public bool TriggerDownAnimation() { if (downClip != null && downClip.clip != null) PlayDynamicClip(downClip.clip); return downClip != null && downClip.isRangedAction; }

}
