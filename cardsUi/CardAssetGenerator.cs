using UnityEngine;
using System.Collections.Generic;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Utility for generating and managing card assets
/// Provides easy creation of sample cards and batch generation tools
/// </summary>
public class CardAssetGenerator : MonoBehaviour
{
    [Header("Card Generation Settings")]
    [SerializeField] private string cardDataFolderPath = "Assets/CardData/";
    
    [Header("Sample Card Templates")]
    [SerializeField] private bool generateSampleCards = true;
    [SerializeField] private int numberOfSampleCards = 5;

#if UNITY_EDITOR

    [ContextMenu("Generate Sample Cards")]
    public void GenerateSampleCards()
    {
        if (!generateSampleCards)
        {
            Debug.LogWarning("Sample card generation is disabled. Enable it in the inspector first.");
            return;
        }

        CreateCardDataFolder();
        
        // Create a variety of sample cards showcasing different types and effects
        CreateSampleCard("Fire Strike", "A blazing attack that burns enemies over time.", 
                        CardType.Attack, 45, CardEffect.Burn, 25, 
                        "https://example.com/fire-strike.png", Rarity.Common);

        CreateSampleCard("Ice Shield", "Creates a protective barrier that may freeze attackers.", 
                        CardType.Buff, 0, CardEffect.Freeze, 30, 
                        "https://example.com/ice-shield.png", Rarity.Uncommon);

        CreateSampleCard("Poison Dagger", "Quick strike with lingering poison damage.", 
                        CardType.Attack, 25, CardEffect.Poison, 20, 
                        "https://example.com/poison-dagger.png", Rarity.Common);

        CreateSampleCard("Healing Potion", "Restores health and provides regeneration.", 
                        CardType.Heal, 0, CardEffect.Regeneration, 35, 
                        "https://example.com/healing-potion.png", Rarity.Common);

        CreateSampleCard("Lightning Bolt", "Devastating attack that may stun the target.", 
                        CardType.Attack, 60, CardEffect.Stun, 40, 
                        "https://example.com/lightning-bolt.png", Rarity.Rare);

        CreateSampleCard("Berserker Rage", "Double strike buff with life steal.", 
                        CardType.Buff, 0, CardEffect.DoubleStrike, 50, 
                        "https://example.com/berserker-rage.png", Rarity.Epic);

        CreateSampleCard("Dragon's Breath", "Legendary fire attack with massive damage.", 
                        CardType.Special, 85, CardEffect.Burn, 75, 
                        "https://example.com/dragons-breath.png", Rarity.Legendary, true);

        CreateSampleCard("Frost Nova", "Area effect that freezes all enemies.", 
                        CardType.Special, 30, CardEffect.Freeze, 60, 
                        "https://example.com/frost-nova.png", Rarity.Epic);

        Debug.Log($"Generated 8 sample cards in {cardDataFolderPath}");
        AssetDatabase.Refresh();
    }

    [ContextMenu("Create Card Data Folder")]
    public void CreateCardDataFolder()
    {
        if (!AssetDatabase.IsValidFolder(cardDataFolderPath))
        {
            // Create the folder if it doesn't exist
            string[] folders = cardDataFolderPath.Split('/');
            string currentPath = folders[0]; // "Assets"
            
            for (int i = 1; i < folders.Length; i++)
            {
                if (!string.IsNullOrEmpty(folders[i]))
                {
                    string newPath = currentPath + "/" + folders[i];
                    if (!AssetDatabase.IsValidFolder(newPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, folders[i]);
                    }
                    currentPath = newPath;
                }
            }
            
            Debug.Log($"Created CardData folder at: {cardDataFolderPath}");
        }
        else
        {
            Debug.Log($"CardData folder already exists at: {cardDataFolderPath}");
        }
    }

    /// <summary>
    /// Creates a new card asset with the specified properties
    /// </summary>
    private CardData CreateSampleCard(string title, string description, CardType type, 
                                     int damage, CardEffect effect, int manaCost, 
                                     string imageUrl, Rarity rarity, bool isLegendary = false)
    {
        // Check if card already exists
        string assetPath = cardDataFolderPath + title + ".asset";
        CardData existingCard = AssetDatabase.LoadAssetAtPath<CardData>(assetPath);
        
        if (existingCard != null)
        {
            Debug.Log($"Card '{title}' already exists, skipping creation.");
            return existingCard;
        }

        // Create new card
        CardData newCard = ScriptableObject.CreateInstance<CardData>();
        
        // Use reflection to set private fields since they don't have public setters
        var cardTitleField = typeof(CardData).GetField("cardTitle", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var descriptionField = typeof(CardData).GetField("description", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var typeField = typeof(CardData).GetField("type", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var damageField = typeof(CardData).GetField("damage", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var effectsField = typeof(CardData).GetField("effects", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var manaField = typeof(CardData).GetField("manaDeduction", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var imageUrlField = typeof(CardData).GetField("imageUrl", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var rarityField = typeof(CardData).GetField("rarity", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var legendaryField = typeof(CardData).GetField("isLegendary", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        cardTitleField?.SetValue(newCard, title);
        descriptionField?.SetValue(newCard, description);
        typeField?.SetValue(newCard, type);
        damageField?.SetValue(newCard, damage);
        
        // Set effects list
        List<CardEffect> effectsList = new List<CardEffect>();
        if (effect != CardEffect.None)
        {
            effectsList.Add(effect);
        }
        effectsField?.SetValue(newCard, effectsList);
        
        manaField?.SetValue(newCard, manaCost);
        imageUrlField?.SetValue(newCard, imageUrl);
        rarityField?.SetValue(newCard, rarity);
        legendaryField?.SetValue(newCard, isLegendary);

        // Create the asset
        AssetDatabase.CreateAsset(newCard, assetPath);
        
        Debug.Log($"Created card: {title} ({type}) - Damage: {damage}, Mana: {manaCost}, Effect: {effect}");
        
        return newCard;
    }

    /// <summary>
    /// Creates a new empty card asset for manual editing
    /// </summary>
    [MenuItem("Tools/Card System/Create New Card Asset")]
    public static void CreateNewCardAsset()
    {
        CardData newCard = ScriptableObject.CreateInstance<CardData>();
        
        string path = "Assets/CardData/";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder("Assets", "CardData");
        }
        
        string assetPath = AssetDatabase.GenerateUniqueAssetPath(path + "New Card.asset");
        AssetDatabase.CreateAsset(newCard, assetPath);
        AssetDatabase.Refresh();
        
        // Select the newly created asset
        Selection.activeObject = newCard;
        EditorGUIUtility.PingObject(newCard);
        
        Debug.Log($"Created new empty card asset at: {assetPath}");
    }

    /// <summary>
    /// Loads all card assets from the CardData folder
    /// </summary>
    [MenuItem("Tools/Card System/Load All Cards")]
    public static List<CardData> LoadAllCards()
    {
        List<CardData> allCards = new List<CardData>();
        
        string[] cardGuids = AssetDatabase.FindAssets("t:CardData", new[] { "Assets/CardData" });
        
        foreach (string guid in cardGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CardData card = AssetDatabase.LoadAssetAtPath<CardData>(path);
            
            if (card != null)
            {
                allCards.Add(card);
            }
        }
        
        Debug.Log($"Loaded {allCards.Count} cards from CardData folder.");
        return allCards;
    }

    /// <summary>
    /// Validates all card assets and reports any issues
    /// </summary>
    [MenuItem("Tools/Card System/Validate All Cards")]
    public static void ValidateAllCards()
    {
        List<CardData> allCards = LoadAllCards();
        int issuesFound = 0;
        
        foreach (CardData card in allCards)
        {
            List<string> issues = card.ValidateCard();
            
            if (issues.Count > 0)
            {
                Debug.LogWarning($"Card '{card.Title}' has {issues.Count} issue(s):");
                foreach (string issue in issues)
                {
                    Debug.LogWarning($"  - {issue}");
                }
                issuesFound++;
            }
        }
        
        if (issuesFound == 0)
        {
            Debug.Log($"All {allCards.Count} cards passed validation!");
        }
        else
        {
            Debug.LogWarning($"Found issues in {issuesFound} out of {allCards.Count} cards.");
        }
    }

#endif

    /// <summary>
    /// Runtime method to get a card by name
    /// </summary>
    public static CardData GetCardByName(string cardName)
    {
#if UNITY_EDITOR
        string[] cardGuids = AssetDatabase.FindAssets($"{cardName} t:CardData", new[] { "Assets/CardData" });
        
        if (cardGuids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(cardGuids[0]);
            return AssetDatabase.LoadAssetAtPath<CardData>(path);
        }
#endif
        
        // Runtime loading would require Resources folder or Addressables
        // For now, return null and log warning
        Debug.LogWarning($"Card '{cardName}' not found. Consider using Resources folder for runtime loading.");
        return null;
    }
}