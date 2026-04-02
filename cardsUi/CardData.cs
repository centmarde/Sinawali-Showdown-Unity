using UnityEngine;
using System;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// ScriptableObject definition for card game cards with all essential properties
/// Supports extensible types and effects for future expansion
/// </summary>
[CreateAssetMenu(fileName = "New Card", menuName = "Card Game/Card Data", order = 1)]
[Serializable]
public class CardData : ScriptableObject
{
    [Header("Basic Information")]
    [SerializeField] private string cardTitle;
    [TextArea(3, 5)]
    [SerializeField] private string description;
    [SerializeField] private CharacterClass characterClass = CharacterClass.Any;

    [Header("Card Properties")]
    [SerializeField] private CardType type = CardType.Attack;
    [Range(1, 100)]
    [SerializeField] private int damage = 1;
    [SerializeField] private List<CardEffect> effects = new List<CardEffect>();
    [Range(1, 100)]
    [SerializeField] private int manaDeduction = 1;

    [Header("Visuals")]
    [SerializeField] private string imageUrl;
    [SerializeField] private Sprite cardSprite; // Optional: direct sprite reference

    [Header("Advanced Properties (Optional)")]
    [SerializeField] private Rarity rarity = Rarity.Common;
    [SerializeField] private bool isLegendary = false;
    
    [Header("Obtainment Status")]
    [SerializeField] private bool isObtained = true; // Default true for existing cards

    // Public properties for easy access
    public string Title => cardTitle;
    public string Description => description;
    public CardType Type => type;
    public CharacterClass CharacterClass => characterClass;
    public int Damage => damage;
    public List<CardEffect> Effects => new List<CardEffect>(effects); // Return copy to prevent external modification
    public int ManaDeduction => manaDeduction;
    public string ImageUrl => imageUrl;
    public Sprite CardSprite => cardSprite;
    public Rarity Rarity => rarity;
    public bool IsLegendary => isLegendary;
    public bool IsObtained => isObtained;

    /// <summary>
    /// Validates the card data and returns any issues
    /// </summary>
    public List<string> ValidateCard()
    {
        List<string> issues = new List<string>();

        if (string.IsNullOrEmpty(cardTitle))
            issues.Add("Card title cannot be empty");

        if (string.IsNullOrEmpty(description))
            issues.Add("Card description cannot be empty");

        if (damage < 1 || damage > 100)
            issues.Add("Damage must be between 1 and 100");

        if (manaDeduction < 1 || manaDeduction > 100)
            issues.Add("Mana deduction must be between 1 and 100");

        if (string.IsNullOrEmpty(imageUrl) && cardSprite == null)
            issues.Add("Either image URL or card sprite must be provided");
            
        // Note: isObtained can be false for cards that haven't been unlocked yet

        return issues;
    }

    /// <summary>
    /// Returns a formatted display string for the card
    /// </summary>
    public string GetDisplayText()
    {
        return $"{cardTitle}\n{description}\nDamage: {damage} | Mana: {manaDeduction}\nType: {type}";
    }

    /// <summary>
    /// Checks if the card has a specific effect
    /// </summary>
    public bool HasEffect(CardEffect effect)
    {
        return effects.Contains(effect);
    }

    /// <summary>
    /// Adds an effect to the card if it doesn't already exist
    /// </summary>
    public void AddEffect(CardEffect effect)
    {
        if (!effects.Contains(effect))
        {
            effects.Add(effect);
        }
    }

    /// <summary>
    /// Removes an effect from the card
    /// </summary>
    public void RemoveEffect(CardEffect effect)
    {
        effects.Remove(effect);
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor-only: Updates the asset name to match the card title
    /// </summary>
    private void OnValidate()
    {
        if (!string.IsNullOrEmpty(cardTitle) && name != cardTitle)
        {
            // Only rename if we're not in play mode and the asset exists
            if (!Application.isPlaying && AssetDatabase.Contains(this))
            {
                string assetPath = AssetDatabase.GetAssetPath(this);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    AssetDatabase.RenameAsset(assetPath, cardTitle);
                }
            }
        }
    }
#endif
}

/// <summary>
/// Enum for different card types - easily extensible
/// </summary>
[Serializable]
public enum CardType
{
    Attack,
    Buff,
    Debuff,
    Heal,
    Shield,
    Special,
    // Add new types here in the future
}

/// <summary>
/// Enum for card effects - easily extensible
/// </summary>
[Serializable]
public enum CardEffect
{
    None,
    Poison,
    Burn,
    Freeze,
    Stun,
    LifeSteal,
    Regeneration,
    DoubleStrike,
    Piercing,
    // Add new effects here in the future
}

/// <summary>
/// Enum for card rarity levels
/// </summary>
[Serializable]
public enum Rarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary,
    Mythic
}

/// <summary>
/// Enum for character classes that can use specific cards
/// </summary>
[Serializable]
public enum CharacterClass
{
    Any,     // Card can be used by any character
    Warrior,
    Mage
}