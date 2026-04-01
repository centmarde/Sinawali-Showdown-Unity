using UnityEngine;

/// <summary>
/// Quick test script to demonstrate the improved Character UI
/// Attach to any GameObject and click the buttons or use keyboard shortcuts
/// </summary>
public class QuickUITest : MonoBehaviour
{
    [Header("Test Settings")]
    public CharacterData testCharacterData;
    public Character character;
    public CharacterUIAutoCreate uiSystem;
    
    [Header("Test Controls")]
    [Tooltip("Space = Damage, H = Heal, M = Add Mana, G = Add Gold")]
    public bool showKeyboardControls = true;
    
    private void Start()
    {
        // Auto-setup if components are missing
        SetupTestEnvironment();
        
        if (showKeyboardControls)
        {
            Debug.Log("UI Test Controls:\n" +
                     "SPACE = Take 10 damage\n" +
                     "H = Heal 15 HP\n" +
                     "M = Add 10 mana\n" +
                     "G = Add 25 gold\n" +
                     "B = Add random buff\n" +
                     "D = Add random debuff\n" +
                     "R = Reset to full stats");
        }
    }
    
    private void SetupTestEnvironment()
    {
        // Find or create Character component
        if (character == null)
        {
            character = FindObjectOfType<Character>();
            if (character == null)
            {
                character = gameObject.AddComponent<Character>();
                Debug.Log("Added Character component to test object");
            }
        }
        
        // Create test character data if needed
        if (testCharacterData == null && character.characterData == null)
        {
            // Create a runtime test character
            testCharacterData = ScriptableObject.CreateInstance<CharacterData>();
            testCharacterData.characterName = "Test Hero";
            testCharacterData.maxHP = 100;
            testCharacterData.startingHP = 100;
            testCharacterData.maxMana = 50;
            testCharacterData.startingMana = 50;
            testCharacterData.gold = 100;
            
            character.characterData = testCharacterData;
            character.InitializeCharacter();
            Debug.Log("Created test character data");
        }
        
        // Find or create UI system
        if (uiSystem == null)
        {
            uiSystem = FindObjectOfType<CharacterUIAutoCreate>();
            if (uiSystem == null)
            {
                CharacterUIAutoCreate.CreateCharacterUI();
                uiSystem = FindObjectOfType<CharacterUIAutoCreate>();
                Debug.Log("Created Character UI System");
            }
        }
        
        // Connect UI to character
        if (uiSystem != null && character != null)
        {
            uiSystem.SetTrackedCharacter(character);
            Debug.Log("Connected UI to character - UI should now display in top-left corner!");
        }
    }
    
    private void Update()
    {
        if (character == null) return;
        
        // Keyboard test controls
        if (Input.GetKeyDown(KeyCode.Space))
        {
            character.TakeDamage(10);
            Debug.Log("Took 10 damage");
        }
        
        if (Input.GetKeyDown(KeyCode.H))
        {
            character.Heal(15);
            Debug.Log("Healed 15 HP");
        }
        
        if (Input.GetKeyDown(KeyCode.M))
        {
            character.RestoreMana(10);
            Debug.Log("Added 10 mana");
        }
        
        if (Input.GetKeyDown(KeyCode.G))
        {
            character.AddGold(25);
            Debug.Log("Added 25 gold");
        }
        
        if (Input.GetKeyDown(KeyCode.B))
        {
            AddTestBuff();
        }
        
        if (Input.GetKeyDown(KeyCode.D))
        {
            AddTestDebuff();
        }
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetStats();
            Debug.Log("Stats reset to full");
        }
    }
    
    [ContextMenu("Test Damage")]
    public void TestDamage()
    {
        if (character != null)
        {
            character.TakeDamage(10);
            Debug.Log("Test: Took 10 damage");
        }
    }
    
    [ContextMenu("Test Heal")]
    public void TestHeal()
    {
        if (character != null)
        {
            character.Heal(15);
            Debug.Log("Test: Healed 15 HP");
        }
    }
    
    [ContextMenu("Add Test Buff")]
    public void AddTestBuff()
    {
        if (character == null) return;
        
        StatusEffect buff = new StatusEffect
        {
            effectName = "Strength",
            effectType = StatusEffectType.Temporary,
            duration = 5,
            hpModifier = 0,
            manaModifier = 10,
            goldModifier = 0,
            description = "Increased strength!"
        };
        
        character.AddBuff(buff);
        Debug.Log("Added Strength buff");
    }
    
    [ContextMenu("Add Test Debuff")]
    public void AddTestDebuff()
    {
        if (character == null) return;
        
        StatusEffect debuff = new StatusEffect
        {
            effectName = "Poison",
            effectType = StatusEffectType.Temporary,
            duration = 3,
            hpModifier = -5,
            manaModifier = 0,
            goldModifier = 0,
            description = "Poisoned! Losing health."
        };
        
        character.AddDebuff(debuff);
        Debug.Log("Added Poison debuff");
    }
    
    [ContextMenu("Reset Stats")]
    public void ResetStats()
    {
        if (character != null && character.characterData != null)
        {
            character.InitializeCharacter();
        }
    }
    
    [ContextMenu("Force UI Update")]
    public void ForceUIUpdate()
    {
        if (uiSystem != null && character != null)
        {
            uiSystem.SetTrackedCharacter(character);
            Debug.Log("Forced UI update");
        }
    }
}