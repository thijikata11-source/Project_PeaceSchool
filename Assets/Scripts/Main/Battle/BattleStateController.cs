using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BattleState
{
    CharacterSelection,
    CommandMenu,
    SkillsMenu,
    EnemyTargeting,
    MultiTargetingSelection, // 🎯 Added: Holds inputs for choosing consecutive manual targets
    AnimationPlaying
}


