using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// One-click setup for the complete card selection and inspection system
/// Provides Unity Editor menu integration and validation
/// Creates CardInspectorAutoCreate and validates the entire system
/// </summary>
public class CardSelectionSystemSetup : MonoBehaviour
{
    [Header("System Validation")]
    [SerializeField] private bool validateOnStart = true;
    [SerializeField] private bool autoCreateIfMissing = true;
    [SerializeField] private bool showValidationResults = true;
    
    [Header("Setup Results")]
    [SerializeField] private HandManager foundHandManager;
    [SerializeField] private CardInspectorAutoCreate foundAutoCreate;
    [SerializeField] private int cardInspectorCount;
    [SerializeField] private bool systemValid;
    
    void Start()
    {
        if (validateOnStart)
        {
            ValidateAndSetupSystem();
        }
    }
    
    /// <summary>
    /// Validate and setup the complete card selection system
    /// </summary>
    [ContextMenu("Validate and Setup Card Selection System")]
    public void ValidateAndSetupSystem()
    {
        Debug.Log("CardSelectionSystemSetup: Starting system validation and setup...");
        
        bool success = true;
        
        // Check HandManager
        success &= ValidateHandManager();
        
        // Check or create CardInspectorAutoCreate
        success &= ValidateCardInspectorAutoCreate();
        
        // Validate card inspectors
        success &= ValidateCardInspectors();
        
        // Final validation
        systemValid = success;
        
        if (showValidationResults)
        {
            ShowValidationResults();
        }
        
        Debug.Log($"CardSelectionSystemSetup: System validation {(success ? "PASSED" : "FAILED")}");
    }
    
    /// <summary>
    /// Validate HandManager exists and is properly configured
    /// </summary>
    bool ValidateHandManager()
    {
        foundHandManager = FindObjectOfType<HandManager>();
        
        if (foundHandManager == null)
        {
            Debug.LogError("CardSelectionSystemSetup: No HandManager found! Please create a HandManager first.");
            return false;
        }
        
        // Check if HandManager has cards
        int cardCount = foundHandManager.GetAllCards().Count;
        if (cardCount == 0)
        {
            Debug.LogWarning("CardSelectionSystemSetup: HandManager has no cards. Make sure to setup cards first.");
            return false;
        }
        
        Debug.Log($"CardSelectionSystemSetup: HandManager validated - {cardCount} cards found");
        return true;
    }
    
    /// <summary>
    /// Validate or create CardInspectorAutoCreate component
    /// </summary>
    bool ValidateCardInspectorAutoCreate()
    {
        foundAutoCreate = FindObjectOfType<CardInspectorAutoCreate>();
        
        if (foundAutoCreate == null)
        {
            if (autoCreateIfMissing)
            {
                // Create CardInspectorAutoCreate
                GameObject autoCreateGO = new GameObject("CardInspectorAutoCreate");
                foundAutoCreate = autoCreateGO.AddComponent<CardInspectorAutoCreate>();
                
                Debug.Log("CardSelectionSystemSetup: Created CardInspectorAutoCreate component");
            }
            else
            {
                Debug.LogError("CardSelectionSystemSetup: No CardInspectorAutoCreate found and auto-creation is disabled");
                return false;
            }
        }
        
        Debug.Log("CardSelectionSystemSetup: CardInspectorAutoCreate validated");
        return true;
    }
    
    /// <summary>
    /// Validate card inspectors are properly setup
    /// </summary>
    bool ValidateCardInspectors()
    {
        if (foundHandManager == null)
        {
            return false;
        }
        
        CardInspector[] inspectors = foundHandManager.GetComponentsInChildren<CardInspector>();
        cardInspectorCount = inspectors.Length;
        
        int totalCards = foundHandManager.GetAllCards().Count;
        
        if (cardInspectorCount == 0)
        {
            Debug.LogWarning("CardSelectionSystemSetup: No CardInspector components found. They will be created automatically.");
        }
        else if (cardInspectorCount < totalCards)
        {
            Debug.LogWarning($"CardSelectionSystemSetup: Only {cardInspectorCount}/{totalCards} cards have CardInspector components.");
        }
        else
        {
            Debug.Log($"CardSelectionSystemSetup: All {cardInspectorCount} cards have CardInspector components");
        }
        
        return true;
    }
    
    /// <summary>
    /// Show detailed validation results
    /// </summary>
    void ShowValidationResults()
    {
        Debug.Log("=== Card Selection System Validation Results ===");
        Debug.Log($"HandManager: {(foundHandManager != null ? "✓ Found" : "✗ Missing")}");
        Debug.Log($"CardInspectorAutoCreate: {(foundAutoCreate != null ? "✓ Found" : "✗ Missing")}");
        Debug.Log($"Card Inspectors: {cardInspectorCount} components found");
        
        if (foundHandManager != null)
        {
            int totalCards = foundHandManager.GetAllCards().Count;
            int activeCards = foundHandManager.GetActiveCardCount();
            Debug.Log($"Cards: {totalCards} total, {activeCards} active");
        }
        
        Debug.Log($"System Status: {(systemValid ? "✓ VALID" : "✗ INVALID")}");
        Debug.Log("=== End Validation Results ===");
    }
    
    /// <summary>
    /// Setup and test the complete system with sample interaction
    /// </summary>
    [ContextMenu("Setup and Test Complete System")]
    public void SetupAndTestCompleteSystem()
    {
        Debug.Log("CardSelectionSystemSetup: Setting up and testing complete system...");
        
        // First validate and setup
        ValidateAndSetupSystem();
        
        if (!systemValid)
        {
            Debug.LogError("CardSelectionSystemSetup: Cannot test - system validation failed");
            return;
        }
        
        // Trigger the auto-create setup
        if (foundAutoCreate != null)
        {
            foundAutoCreate.SetupCompleteCardInspectionSystem();
        }
        
        // Test card selection
        TestCardSelection();
        
        Debug.Log("CardSelectionSystemSetup: Setup and testing complete!");
    }
    
    /// <summary>
    /// Test card selection functionality
    /// </summary>
    void TestCardSelection()
    {
        if (foundHandManager == null)
        {
            Debug.LogWarning("CardSelectionSystemSetup: Cannot test card selection - no HandManager");
            return;
        }
        
        // Get active cards
        var activeCards = foundHandManager.GetActiveCards();
        if (activeCards.Count == 0)
        {
            Debug.LogWarning("CardSelectionSystemSetup: Cannot test card selection - no active cards");
            return;
        }
        
        // Try to get CardInspector from first active card
        CardInspector inspector = activeCards[0].GetComponent<CardInspector>();
        if (inspector != null)
        {
            // Test selection
            inspector.SelectCard();
            Debug.Log($"CardSelectionSystemSetup: Test selection on '{activeCards[0].gameObject.name}'");
            
            // Deselect after a moment (for testing)
            StartCoroutine(TestDeselectAfterDelay(inspector, 3f));
        }
        else
        {
            Debug.LogWarning("CardSelectionSystemSetup: Cannot test card selection - no CardInspector component found");
        }
    }
    
    /// <summary>
    /// Test deselection after delay
    /// </summary>
    System.Collections.IEnumerator TestDeselectAfterDelay(CardInspector inspector, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (inspector != null && inspector.IsSelected())
        {
            inspector.DeselectCard();
            Debug.Log("CardSelectionSystemSetup: Test deselection completed");
        }
    }
    
    /// <summary>
    /// Check system status and provide recommendations
    /// </summary>
    [ContextMenu("Check System Status")]
    public void CheckSystemStatus()
    {
        Debug.Log("=== Card Selection System Status Check ===");
        
        // Check HandManager
        HandManager hm = FindObjectOfType<HandManager>();
        if (hm == null)
        {
            Debug.LogError("❌ CRITICAL: No HandManager found in scene!");
            Debug.Log("   → Create a HandManager component on your card container");
            return;
        }
        else
        {
            Debug.Log($"✅ HandManager found: {hm.GetTotalCardCount()} total cards, {hm.GetActiveCardCount()} active");
        }
        
        // Check CardInspectorAutoCreate
        CardInspectorAutoCreate autoCreate = FindObjectOfType<CardInspectorAutoCreate>();
        if (autoCreate == null)
        {
            Debug.LogWarning("⚠️  CardInspectorAutoCreate not found");
            Debug.Log("   → Use 'Setup Complete System' to create it automatically");
        }
        else
        {
            Debug.Log("✅ CardInspectorAutoCreate found");
            
            // Check inspector panel
            GameObject panel = autoCreate.GetInspectorPanel();
            if (panel == null)
            {
                Debug.LogWarning("⚠️  Inspector UI panel not created");
                Debug.Log("   → Use 'Setup Complete System' to create UI elements");
            }
            else
            {
                Debug.Log("✅ Inspector UI panel created");
            }
        }
        
        // Check CardInspector components
        CardInspector[] inspectors = FindObjectsOfType<CardInspector>();
        if (inspectors.Length == 0)
        {
            Debug.LogWarning("⚠️  No CardInspector components found");
            Debug.Log("   → Use 'Setup Complete System' to add them automatically");
        }
        else
        {
            Debug.Log($"✅ {inspectors.Length} CardInspector components found");
            
            // Check how many are properly configured
            int configured = 0;
            foreach (CardInspector inspector in inspectors)
            {
                if (inspector.GetComponent<CardFetcher>() != null)
                {
                    configured++;
                }
            }
            Debug.Log($"   → {configured}/{inspectors.Length} are properly attached to cards");
        }
        
        // Check Canvas and EventSystem
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("⚠️  No Canvas found for UI");
            Debug.Log("   → Canvas will be created automatically during setup");
        }
        else
        {
            Debug.Log("✅ Canvas found for UI");
        }
        
        UnityEngine.EventSystems.EventSystem eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem == null)
        {
            Debug.LogWarning("⚠️  No EventSystem found for UI interactions");
            Debug.Log("   → EventSystem will be created automatically during setup");
        }
        else
        {
            Debug.Log("✅ EventSystem found for UI interactions");
        }
        
        Debug.Log("=== End Status Check ===");
    }
    
    #if UNITY_EDITOR
    /// <summary>
    /// Clean up the entire card selection system
    /// </summary>
    [ContextMenu("Clean Up Card Selection System")]
    public void CleanUpCardSelectionSystem()
    {
        if (EditorUtility.DisplayDialog("Clean Up Card Selection System", 
            "This will remove all CardInspector components and the inspector UI. Are you sure?", 
            "Yes", "Cancel"))
        {
            // Remove CardInspector components
            CardInspector[] inspectors = FindObjectsOfType<CardInspector>();
            foreach (CardInspector inspector in inspectors)
            {
                DestroyImmediate(inspector);
            }
            
            // Remove CardInspectorAutoCreate
            CardInspectorAutoCreate autoCreate = FindObjectOfType<CardInspectorAutoCreate>();
            if (autoCreate != null)
            {
                DestroyImmediate(autoCreate.gameObject);
            }
            
            // Remove inspector panel if it exists
            GameObject panel = GameObject.Find("CardInspectorPanel");
            if (panel != null)
            {
                DestroyImmediate(panel);
            }
            
            Debug.Log("CardSelectionSystemSetup: Card selection system cleaned up");
        }
    }
    
    /// <summary>
    /// Create CardSelectionSystemSetup in scene
    /// </summary>
    [MenuItem("Tools/Card System/Setup Card Selection System", priority = 500)]
    public static void CreateCardSelectionSystemSetup()
    {
        // Check if already exists
        CardSelectionSystemSetup existing = FindObjectOfType<CardSelectionSystemSetup>();
        if (existing != null)
        {
            Debug.Log("Card Selection System Setup already exists, selecting it...");
            Selection.activeGameObject = existing.gameObject;
            return;
        }
        
        // Create new setup object
        GameObject setupGO = new GameObject("CardSelectionSystemSetup");
        CardSelectionSystemSetup setup = setupGO.AddComponent<CardSelectionSystemSetup>();
        
        // Select it in hierarchy
        Selection.activeGameObject = setupGO;
        
        // Automatically run setup
        setup.ValidateAndSetupSystem();
        
        Debug.Log("Card Selection System Setup created! Check the inspector for setup options.");
    }
    
    /// <summary>
    /// Quick setup - create everything automatically
    /// </summary>
    [MenuItem("Tools/Card System/Quick Setup Card Selection", priority = 501)]
    public static void QuickSetupCardSelection()
    {
        // Create or find setup component
        CardSelectionSystemSetup setup = FindObjectOfType<CardSelectionSystemSetup>();
        if (setup == null)
        {
            GameObject setupGO = new GameObject("CardSelectionSystemSetup");
            setup = setupGO.AddComponent<CardSelectionSystemSetup>();
        }
        
        // Run complete setup
        setup.SetupAndTestCompleteSystem();
        
        Debug.Log("Quick Card Selection Setup completed!");
    }
    
    /// <summary>
    /// Validate existing setup
    /// </summary>
    [MenuItem("Tools/Card System/Validate Card Selection System", priority = 502)]
    public static void ValidateCardSelectionSystem()
    {
        CardSelectionSystemSetup setup = FindObjectOfType<CardSelectionSystemSetup>();
        if (setup == null)
        {
            Debug.LogWarning("No CardSelectionSystemSetup found. Create one first using 'Setup Card Selection System'");
            return;
        }
        
        setup.CheckSystemStatus();
    }
    #endif
}