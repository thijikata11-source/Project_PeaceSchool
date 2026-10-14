using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class TurnbaseAnimationsTrigger : MonoBehaviour
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
    public CustomActionClip evadeClip;

    [Header("Character Skill Set Matrix (Max 8)")]
    [Tooltip("Drag and drop your SkillScriptable files here. Enforced maximum ceiling allocation limit of 8 items.")]
    public List<SkillScriptable> characterSkillSet = new List<SkillScriptable>();

    [Header("Dynamic Raw Skill Animation Assets")]
    [Tooltip("Drag your raw custom .anim files directly into this array list box.")]
    public List<AnimationClip> customSkillClips = new List<AnimationClip>();

    [Tooltip("Match the indexes above. Check the box if that clip stands still, uncheck if it runs up to the enemy.")]
    public List<bool> customClipsRangeToggle = new List<bool>();

    private Animator characterAnimator;
    private PlayableGraph playableGraph;
    private AnimationPlayableOutput playableOutput;

    private void Awake()
    {
        characterAnimator = GetComponentInChildren<Animator>();
        if (characterAnimator == null)
        {
            Debug.LogError("[TurnbaseAnimationsTrigger] No Animator found on " + gameObject.name + " or its children!");
        }
    }

    private void OnValidate()
    {
        if (characterSkillSet.Count > 8)
        {
            Debug.LogWarning("[TurnbaseAnimationsTrigger] Skill slots capped at an absolute limit of 8 items on " + gameObject.name, gameObject);
            characterSkillSet.RemoveRange(8, characterSkillSet.Count - 8);
        }

        while (customClipsRangeToggle.Count < customSkillClips.Count) customClipsRangeToggle.Add(false);
        while (customClipsRangeToggle.Count > customSkillClips.Count) customClipsRangeToggle.RemoveAt(customClipsRangeToggle.Count - 1);
    }

    private void OnDestroy()
    {
        CleanPlayableGraph();
    }

    public void PlayDynamicClip(AnimationClip clip)
    {
        if (clip == null || characterAnimator == null) return;

        CleanPlayableGraph();

        playableGraph = PlayableGraph.Create(gameObject.name + "_DynamicGraph");
        playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        playableOutput = AnimationPlayableOutput.Create(playableGraph, "Animation", characterAnimator);

        AnimationClipPlayable clipPlayable = AnimationClipPlayable.Create(playableGraph, clip);
        playableOutput.SetSourcePlayable(clipPlayable);

        playableGraph.Play();

        float safeCleanupTime = Mathf.Max(0.05f, clip.length - 0.03f);
        Invoke(nameof(CleanPlayableGraph), safeCleanupTime);
    }

    public bool TriggerDynamicSkillExecution(string skillAnimName)
    {
        int matchedIndex = customSkillClips.FindIndex(x => x != null && x.name == skillAnimName);

        if (matchedIndex != -1 && customSkillClips[matchedIndex] != null)
        {
            PlayDynamicClip(customSkillClips[matchedIndex]);
            return customClipsRangeToggle[matchedIndex];
        }

        Debug.LogWarning("[TurnbaseAnimationsTrigger] No raw .anim file found named '" + skillAnimName + "' inside the inspector list array variables!");
        return true;
    }

    public float GetSkillAnimationClipLength(string skillAnimName)
    {
        int matchedIndex = customSkillClips.FindIndex(x => x != null && x.name == skillAnimName);
        if (matchedIndex != -1 && customSkillClips[matchedIndex] != null)
        {
            return customSkillClips[matchedIndex].length;
        }
        return 1.2f;
    }

    private void CleanPlayableGraph()
    {
        if (playableGraph.IsValid())
        {
            playableGraph.Stop();
            playableGraph.Destroy();
        }
    }

    public bool TriggerAttackAnimation() { if (attackClip.clip != null) PlayDynamicClip(attackClip.clip); return attackClip.isRangedAction; }
    public bool TriggerDefendAnimation() { if (defendClip.clip != null) PlayDynamicClip(defendClip.clip); return defendClip.isRangedAction; }
    public bool TriggerItemAnimation() { if (itemClip.clip != null) PlayDynamicClip(itemClip.clip); return itemClip.isRangedAction; }
    public bool TriggerWeaponAnimation() { if (weaponClip.clip != null) PlayDynamicClip(weaponClip.clip); return weaponClip.isRangedAction; }
    public bool TriggerRunAnimation() { if (runClip.clip != null) PlayDynamicClip(runClip.clip); return runClip.isRangedAction; }
    public bool TriggerEvadeAnimation() { if (evadeClip != null && evadeClip.clip != null) PlayDynamicClip(evadeClip.clip); return evadeClip != null && evadeClip.isRangedAction; }
    public bool TriggerHurtAnimation() { if (hurtClip != null && hurtClip.clip != null) PlayDynamicClip(hurtClip.clip); return hurtClip != null && hurtClip.isRangedAction; }
}
