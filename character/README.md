# Character System for Unity

A comprehensive character management system featuring ScriptableObject-based character data, automatic UI generation, and status effect management.

## 📋 Table of Contents

- [Quick Start](#quick-start)
- [System Overview](#system-overview)
- [Step-by-Step Setup Guide](#step-by-step-setup-guide)
- [Component Reference](#component-reference)
- [Creating Character Data](#creating-character-data)
- [Using the Character Component](#using-the-character-component)
- [Status Effects System](#status-effects-system)
- [UI System](#ui-system)
- [Demo and Testing](#demo-and-testing)
- [Troubleshooting](#troubleshooting)

## 🚀 Quick Start

### Option 1: Auto Setup (Recommended)
1. Go to `Tools → Character System → Setup Complete Character System`
2. This creates sample characters, UI, and sets up the demo
3. Press Play and use keyboard controls to test

### Option 2: Manual Setup
1. Create character data: `Tools → Character System → Create Sample Characters`
2. Create UI: `Tools → Character System → Create Character UI`
3. Add Character component to a GameObject
4. Assign character data and test

## 🏗️ System Overview

The character system consists of 6 main components:

1. **CharacterData.cs** - ScriptableObject for character stats
2. **StatusEffect.cs** - Status effect system (buffs/debuffs)
3. **Character.cs** - Runtime component for GameObjects
4. **CharacterUIAutoCreate.cs** - Automatic UI generation
5. **CharacterAssetGenerator.cs** - Editor utilities
6. **CharacterSystemDemo.cs** - Testing and demo script

## 📖 Step-by-Step Setup Guide

### Step 1: Create Character Data

#### Method A: Using the Asset Menu
1. Right-click in Project window
2. Choose `Create → Character System → Character Data`
3. Name your character (e.g., "Hero")
4. Configure stats in Inspector

#### Method B: Using the Tools Menu
1. Go to `Tools → Character System → Create Sample Characters`
2. This creates 5 pre-made characters:
   - Warrior (High HP, Low Mana)
   - Mage (Low HP, High Mana)
   - Rogue (Balanced, High Gold)
   - Cleric (High HP, High Mana)
   - Archer (Medium stats)

#### Configuring Character Stats
```
Basic Stats:
- Max HP: 1-1000 (default: 100)
- Gold: 0-10000 (default: 50)
- Max Mana: 0-200 (default: 100)

Starting Values:
- Starting HP: Cannot exceed Max HP
- Starting Mana: Cannot exceed Max Mana

Character Info:
- Character Name: Auto-renames the asset
- Character Portrait: Sprite for UI display
```

### Step 2: Set Up Status Effects

#### Creating Status Effects
```csharp
// Example: Create a healing potion effect
var healingPotion = new StatusEffect
{
    name = "Healing Potion",
    description = "Restores 10 HP per turn",
    type = StatusEffectType.Temporary,
    duration = 3,
    hpModifier = 10,
    manaModifier = 0,
    goldModifier = 0
};
```

#### Status Effect Types
- **Temporary**: Lasts for specified duration, then expires
- **Permanent**: Never expires automatically
- **Passive**: Applies once when added, then removes itself

### Step 3: Add Character Component to GameObject

1. Create or select a GameObject
2. Add Component → `Character`
3. Assign your CharacterData asset to the `characterData` field
4. The character will initialize with data from the ScriptableObject

### Step 4: Set Up UI (Automatic)

#### Option A: Auto-Create UI
1. Go to `Tools → Character System → Create Character UI`
2. UI appears in top-left corner automatically
3. Connects to all Character components in scene

#### Option B: Manual UI Setup
1. Add `CharacterUIAutoCreate` component to any GameObject
2. Set `createUIOnStart = true`
3. Configure `uiAnchor` position (default: TopLeft)
4. Adjust `spacing` between UI elements

### Step 5: Testing the System

#### Using the Demo Script
1. Add `CharacterSystemDemo` component to any GameObject
2. Press Play
3. Use keyboard controls:
   - **Space**: Deal 10 damage
   - **H**: Heal 15 HP
   - **M**: Spend 10 mana
   - **G**: Add 25 gold
   - **B**: Add random buff
   - **D**: Add random debuff
   - **T**: Process all status effects

#### Manual Testing
```csharp
// Get character component
Character character = GetComponent<Character>();

// Modify stats
character.TakeDamage(25);
character.Heal(15);
character.SpendMana(10);
character.AddGold(50);

// Add status effects
character.AddStatusEffect(myStatusEffect);
character.ProcessStatusEffects(); // Call each turn
```

## 📚 Component Reference

### CharacterData (ScriptableObject)

**Purpose**: Stores base character statistics and configuration

**Key Properties**:
```csharp
public int maxHP;           // Maximum health points
public int gold;            // Starting gold amount
public int maxMana;         // Maximum mana points
public string characterName; // Character display name
public Sprite characterPortrait; // UI portrait image
public List<StatusEffect> debuffs; // Starting debuffs
public List<StatusEffect> buffs;   // Starting buffs
```

**Key Methods**:
```csharp
bool ValidateCharacterData();    // Checks data integrity
void ResetToDefaults();          // Context menu: reset values
```

### Character (MonoBehaviour)

**Purpose**: Runtime character management and stat tracking

**Key Properties**:
```csharp
public CharacterData characterData; // Reference to data asset
public int currentHP;              // Current health
public int currentMana;            // Current mana
public int currentGold;            // Current gold
```

**Key Methods**:
```csharp
void TakeDamage(int amount);       // Reduces HP
void Heal(int amount);             // Increases HP (max capped)
void SpendMana(int amount);        // Reduces mana
void AddGold(int amount);          // Increases gold
void AddStatusEffect(StatusEffect effect); // Adds buff/debuff
void ProcessStatusEffects();       // Processes turn-based effects
```

**Events**:
```csharp
System.Action OnStatsChanged;      // Fired when any stat changes
```

### StatusEffect (Serializable Class)

**Purpose**: Represents temporary or permanent character modifications

**Properties**:
```csharp
public string name;                // Effect display name
public string description;         // Effect description
public StatusEffectType type;      // Temporary/Permanent/Passive
public int duration;               // Turns remaining (Temporary only)
public int hpModifier;            // HP change per turn
public int manaModifier;          // Mana change per turn
public int goldModifier;          // Gold change per turn
```

**Methods**:
```csharp
bool IsExpired();                 // Check if effect should be removed
StatusEffect Clone();             // Create independent copy
```

### CharacterUIAutoCreate (MonoBehaviour)

**Purpose**: Automatically generates and manages character UI

**Configuration**:
```csharp
public bool createUIOnStart = true;        // Auto-create UI on Start()
public UIAnchor uiAnchor = UIAnchor.TopLeft; // UI position
public float spacing = 10f;                // Space between UI elements
```

**UI Elements Created**:
- HP Slider (Red) with current/max text
- Mana Slider (Blue) with current/max text
- Gold Text display
- Status Effects list with icons

## 💡 Advanced Usage

### Creating Custom Status Effects

```csharp
// Poison effect - deals damage over time
var poison = new StatusEffect
{
    name = "Poison",
    description = "Deals 5 damage per turn for 4 turns",
    type = StatusEffectType.Temporary,
    duration = 4,
    hpModifier = -5
};

// Mana regeneration - restores mana over time
var manaRegen = new StatusEffect
{
    name = "Mana Spring",
    description = "Restores 8 mana per turn",
    type = StatusEffectType.Permanent,
    manaModifier = 8
};

// Gold blessing - one-time gold bonus
var goldBlessing = new StatusEffect
{
    name = "Treasure Find",
    description = "Instantly grants 100 gold",
    type = StatusEffectType.Passive,
    goldModifier = 100
};
```

### Listening to Character Changes

```csharp
public class CharacterObserver : MonoBehaviour
{
    void Start()
    {
        Character character = GetComponent<Character>();
        character.OnStatsChanged += HandleStatsChanged;
    }
    
    void HandleStatsChanged()
    {
        Debug.Log("Character stats have changed!");
        // Update UI, save game, trigger animations, etc.
    }
}
```

### Custom UI Integration

```csharp
public class CustomCharacterUI : MonoBehaviour
{
    public Character character;
    public Slider hpSlider;
    public Text goldText;
    
    void Start()
    {
        character.OnStatsChanged += UpdateUI;
        UpdateUI(); // Initial update
    }
    
    void UpdateUI()
    {
        hpSlider.value = (float)character.currentHP / character.characterData.maxHP;
        goldText.text = $"Gold: {character.currentGold}";
    }
}
```

## 🐛 Troubleshooting

### Common Issues

**Q: UI doesn't appear**
- A: Ensure Canvas exists in scene or use `Tools → Character System → Create Character UI`
- Check Console for missing UI Toolkit references

**Q: Character stats don't update**
- A: Verify CharacterData asset is assigned to Character component
- Check if `OnStatsChanged` event is being triggered

**Q: Status effects not working**
- A: Call `ProcessStatusEffects()` each turn/frame as needed
- Ensure effects have proper modifiers set

**Q: Sample characters not created**
- A: Check Assets/CardData/ folder exists
- Verify write permissions for project folder

**Q: Editor tools missing from menu**
- A: Ensure CharacterAssetGenerator.cs is in Editor folder or has `#if UNITY_EDITOR` wrapper

### Performance Tips

1. **Call ProcessStatusEffects() strategically** - not every frame
2. **Use OnStatsChanged event** instead of constant UI polling
3. **Pool status effect objects** for frequently applied effects
4. **Cache UI references** instead of repeated FindObjectOfType calls

### Validation Checklist

Before using the system, verify:
- [ ] CharacterData asset created and configured
- [ ] Character component added to GameObject
- [ ] CharacterData assigned to Character component
- [ ] Canvas exists in scene for UI
- [ ] No validation warnings in Console

## 📜 License

This character system is provided as-is for educational and commercial use. Feel free to modify and extend according to your project needs.

---

**Created with Unity 2022.3+ | Tested on Unity 2023.1**

*For issues or feature requests, check the Console for debug messages and validation warnings.*