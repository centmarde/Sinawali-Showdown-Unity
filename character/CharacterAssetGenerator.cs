using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.IO;

public class CharacterAssetGenerator
{
    [MenuItem("Tools/Character System/Create Sample Characters")]
    public static void CreateSampleCharacters()
    {
        string folderPath = "Assets/CharacterData";
        
        // Create folder if it doesn't exist
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", "CharacterData");
        }
        
        // Sample characters data
        var charactersData = new[]
        {
            new { name = "Warrior", hp = 150, mana = 50, gold = 100, desc = "A strong melee fighter" },
            new { name = "Mage", hp = 80, mana = 200, gold = 75, desc = "A powerful spellcaster" },
            new { name = "Rogue", hp = 100, mana = 100, gold = 150, desc = "A sneaky assassin" },
            new { name = "Cleric", hp = 120, mana = 150, gold = 50, desc = "A divine healer" },
            new { name = "Archer", hp = 90, mana = 80, gold = 125, desc = "A skilled ranged combatant" }
        };
        
        foreach (var charData in charactersData)
        {
            CreateCharacterAsset(charData.name, charData.hp, charData.mana, charData.gold, folderPath);
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"Created {charactersData.Length} sample character assets in {folderPath}");
    }
    
    [MenuItem("Tools/Character System/Create Character Asset")]
    public static void CreateNewCharacterAsset()
    {
        string folderPath = "Assets/CharacterData";
        
        // Create folder if it doesn't exist
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", "CharacterData");
        }
        
        CreateCharacterAsset("New Character", 100, 100, 50, folderPath);
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    
    [MenuItem("Tools/Character System/Create Character UI")]
    public static void CreateCharacterUIFromMenu()
    {
        CharacterUIAutoCreate.CreateCharacterUI();
    }
    
    [MenuItem("Tools/Character System/Setup Complete Character System")]
    public static void SetupCompleteSystem()
    {
        // Create sample characters
        CreateSampleCharacters();
        
        // Create UI
        CharacterUIAutoCreate.CreateCharacterUI();
        
        // Create a test character in scene
        CreateTestCharacterInScene();
        
        Debug.Log("Complete Character System setup finished!");
    }
    
    public static CharacterData CreateCharacterAsset(string characterName, int maxHP, int maxMana, int gold, string folderPath)
    {
        // Create the ScriptableObject
        CharacterData characterData = ScriptableObject.CreateInstance<CharacterData>();
        
        // Set properties
        characterData.characterName = characterName;
        characterData.maxHP = maxHP;
        characterData.startingHP = maxHP;
        characterData.maxMana = maxMana;
        characterData.startingMana = maxMana;
        characterData.gold = gold;
        
        // Add some sample effects based on character type
        AddSampleEffectsBasedOnName(characterData, characterName);
        
        // Create the asset file
        string assetPath = $"{folderPath}/{characterName}.asset";
        AssetDatabase.CreateAsset(characterData, assetPath);
        
        Debug.Log($"Created character asset: {assetPath}");
        return characterData;
    }
    
    private static void AddSampleEffectsBasedOnName(CharacterData character, string name)
    {
        switch (name.ToLower())
        {
            case "warrior":
                character.buffs.Add(new StatusEffect("Armor", StatusEffectType.Passive, 0, 1, "Reduces incoming damage"));
                break;
                
            case "mage":
                character.buffs.Add(new StatusEffect("Mana Shield", StatusEffectType.Passive, 0, 1, "Protects with mana"));
                break;
                
            case "rogue":
                character.buffs.Add(new StatusEffect("Stealth", StatusEffectType.Temporary, 3, 1, "Harder to detect"));
                break;
                
            case "cleric":
                character.buffs.Add(new StatusEffect("Divine Blessing", StatusEffectType.Passive, 0, 1, "Slowly regenerates HP")
                {
                    hpModifier = 2
                });
                break;
                
            case "archer":
                character.buffs.Add(new StatusEffect("Eagle Eye", StatusEffectType.Passive, 0, 1, "Increased accuracy"));
                break;
        }
    }
    
    private static void CreateTestCharacterInScene()
    {
        GameObject characterObj = new GameObject("Test Character");
        Character character = characterObj.AddComponent<Character>();
        
        // Try to load a character asset
        string[] characterPaths = AssetDatabase.FindAssets("t:CharacterData");
        if (characterPaths.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(characterPaths[0]);
            CharacterData characterData = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
            character.SetCharacterData(characterData);
            
            Debug.Log($"Created test character GameObject with {characterData.characterName} data");
        }
        else
        {
            Debug.LogWarning("No character data assets found. Create some first!");
        }
        
        // Auto-connect to UI if it exists
        CharacterUIAutoCreate uiSystem = Object.FindObjectOfType<CharacterUIAutoCreate>();
        if (uiSystem != null)
        {
            uiSystem.SetTrackedCharacter(character);
        }
    }
    
    [MenuItem("Tools/Character System/Validate All Character Assets")]
    public static void ValidateAllCharacterAssets()
    {
        string[] characterPaths = AssetDatabase.FindAssets("t:CharacterData");
        int validCount = 0;
        int invalidCount = 0;
        
        foreach (string guid in characterPaths)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CharacterData character = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
            
            if (character.ValidateCharacterData())
            {
                validCount++;
            }
            else
            {
                invalidCount++;
                Debug.LogWarning($"Invalid character data: {path}");
            }
        }
        
        Debug.Log($"Character validation complete: {validCount} valid, {invalidCount} invalid");
    }
}

// Context menu for CharacterData assets
[CustomEditor(typeof(CharacterData))]
public class CharacterDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        CharacterData character = (CharacterData)target;
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Validate Character Data"))
        {
            if (character.ValidateCharacterData())
            {
                Debug.Log($"Character '{character.characterName}' is valid!");
            }
        }
        
        if (GUILayout.Button("Reset to Defaults"))
        {
            character.ResetToDefaults();
            EditorUtility.SetDirty(character);
        }
        
        if (GUILayout.Button("Create Test Character in Scene"))
        {
            GameObject charObj = new GameObject($"Character - {character.characterName}");
            Character charComponent = charObj.AddComponent<Character>();
            charComponent.SetCharacterData(character);
            
            // Auto-connect to UI
            CharacterUIAutoCreate uiSystem = Object.FindObjectOfType<CharacterUIAutoCreate>();
            if (uiSystem != null)
            {
                uiSystem.SetTrackedCharacter(charComponent);
            }
        }
    }
}
#endif