using UnityEngine;

[System.Serializable]
public class StatusEffect
{
    public string effectName = "New Effect";
    public StatusEffectType effectType = StatusEffectType.Temporary;
    public int duration = 1; // In turns or seconds
    public int potency = 1; // Effect strength
    public string description = "Effect description";
    public Sprite effectIcon;
    
    [Header("Effect Values")]
    public int hpModifier = 0; // Damage over time or healing over time
    public int manaModifier = 0; // Mana drain or mana regen
    public int goldModifier = 0; // Gold loss or gain
    
    public StatusEffect()
    {
        // Default constructor
    }
    
    public StatusEffect(string name, StatusEffectType type, int dur, int pot, string desc = "")
    {
        effectName = name;
        effectType = type;
        duration = dur;
        potency = pot;
        description = string.IsNullOrEmpty(desc) ? name : desc;
    }
    
    public bool IsExpired()
    {
        return effectType == StatusEffectType.Temporary && duration <= 0;
    }
    
    public void ProcessTurn()
    {
        if (effectType == StatusEffectType.Temporary)
        {
            duration = Mathf.Max(0, duration - 1);
        }
    }
    
    public StatusEffect Clone()
    {
        return new StatusEffect(effectName, effectType, duration, potency, description)
        {
            effectIcon = effectIcon,
            hpModifier = hpModifier,
            manaModifier = manaModifier,
            goldModifier = goldModifier
        };
    }
}

public enum StatusEffectType
{
    Temporary,  // Has duration, expires
    Permanent,  // Lasts until removed
    Passive     // Always active while equipped/present
}