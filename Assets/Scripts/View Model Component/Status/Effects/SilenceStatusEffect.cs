using UnityEngine;
using System.Collections;

public class SilenceStatusEffect : StatusEffect {
    void OnEnable() {
        this.AddObserver(OnCanPerformCheck, Ability.CanPerformCheck);
    }

    void OnDisable() {
        this.RemoveObserver(OnCanPerformCheck, Ability.CanPerformCheck);
    }

    void OnCanPerformCheck(object sender, object args) {
        // If the ability is magical, disable it.
        Ability a = sender as Ability;
        MagicalAbilityPower m = a.GetComponentInParent<MagicalAbilityPower>();
        if (m != null) {
            BaseAdjustment adj = (BaseAdjustment)args;
            adj.FlipToggle();
        }
    }
}