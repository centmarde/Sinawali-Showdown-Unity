using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Character", menuName = "Character System/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Basic Stats")]
    [Range(1, 1000)]
    public int maxHP = 100;
    
    [Range(0, 10000)]
    public int gold = 50;
    
    [Range(0, 200)]
    public int maxMana = 100;

    [Header("Character Info")]
    public string characterName = "New Character";
    public string characterType = "Warrior"; // Character class/type for card filtering (must be specific)
    public Sprite characterPortrait;
    
    [Header("Character Class Details")]
    [TextArea(2, 4)]
    public string characterDescription = "A brave adventurer ready for battle.";
    public bool isUniversalClass = false; // Can use any character-specific cards
    
    [Header("Effects")]
    public List<StatusEffect> debuffs = new List<StatusEffect>();
    public List<StatusEffect> buffs = new List<StatusEffect>();

    [Header("Starting Values")]
    [Range(0, 1000)]
    public int startingHP = 100;
    
    [Range(0, 200)]
    public int startingMana = 100;

    private void OnValidate()
    {
        // Ensure starting values don't exceed max values
        startingHP = Mathf.Min(startingHP, maxHP);
        startingMana = Mathf.Min(startingMana, maxMana);
        
        // Auto-rename the asset to match character name
        if (!string.IsNullOrEmpty(characterName))
        {
            name = characterName;
        }
        
        // Ensure character type is not empty and not "Any"
        if (string.IsNullOrEmpty(characterType) || characterType.Equals("Any", System.StringComparison.OrdinalIgnoreCase))
        {
            characterType = "Warrior"; // Default to a specific type
        }
        
        // Validate the character data
        ValidateCharacterData();
    }

    public bool ValidateCharacterData()
    {
        bool isValid = true;
        
        if (maxHP <= 0)
        {
            Debug.LogWarning($"Character {characterName}: Max HP must be greater than 0");
            isValid = false;
        }
        
        if (maxMana < 0)
        {
            Debug.LogWarning($"Character {characterName}: Max Mana cannot be negative");
            isValid = false;
        }
        
        if (gold < 0)
        {
            Debug.LogWarning($"Character {characterName}: Gold cannot be negative");
            isValid = false;
        }

        if (string.IsNullOrEmpty(characterName))
        {
            Debug.LogWarning("Character name cannot be empty");
            isValid = false;
        }
        
        if (string.IsNullOrEmpty(characterType))
        {
            Debug.LogWarning($"Character {characterName}: Character type cannot be empty");
            isValid = false;
        }
        
        return isValid;
    }

    [ContextMenu("Reset to Default Values")]
    public void ResetToDefaults()
    {
        maxHP = 100;
        gold = 50;
        maxMana = 100;
        startingHP = maxHP;
        startingMana = maxMana;
        characterName = "New Character";
        characterType = "Warrior";
        characterDescription = "A brave adventurer ready for battle.";
        isUniversalClass = false;
        debuffs.Clear();
        buffs.Clear();
    }
}
