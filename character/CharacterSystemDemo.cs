using UnityEngine;
using UnityEngine.UI;

public class CharacterSystemDemo : MonoBehaviour
{
    [Header("Demo Controls")]
    public Character targetCharacter;
    public KeyCode damageKey = KeyCode.Space;
    public KeyCode healKey = KeyCode.H;
    public KeyCode addBuffKey = KeyCode.B;
    public KeyCode addDebuffKey = KeyCode.D;
    public KeyCode processEffectsKey = KeyCode.T; // T for "turn"
    public KeyCode spendManaKey = KeyCode.M;
    public KeyCode addGoldKey = KeyCode.G;
    
    [Header("Demo Values")]
    public int damageAmount = 10;
    public int healAmount = 15;
    public int manaSpendAmount = 20;
    public int goldAddAmount = 25;
    
    private void Start()
    {
        // Auto-find character if not assigned
        if (targetCharacter == null)
        {
            targetCharacter = FindObjectOfType<Character>();
        }
        
        // Auto-connect to UI
        CharacterUIAutoCreate uiSystem = FindObjectOfType<CharacterUIAutoCreate>();
        if (uiSystem != null && targetCharacter != null)
        {
            uiSystem.SetTrackedCharacter(targetCharacter);
        }
        
        // Show demo instructions
        ShowDemoInstructions();
    }
    
    private void Update()
    {
        if (targetCharacter == null) return;
        
        HandleInput();
    }
    
    private void HandleInput()
    {
        if (Input.GetKeyDown(damageKey))
        {
            targetCharacter.TakeDamage(damageAmount);
            Debug.Log($"Dealt {damageAmount} damage to {targetCharacter.GetCharacterName()}");
        }
        
        if (Input.GetKeyDown(healKey))
        {
            targetCharacter.Heal(healAmount);
            Debug.Log($"Healed {healAmount} HP to {targetCharacter.GetCharacterName()}");
        }
        
        if (Input.GetKeyDown(addBuffKey))
        {
            AddRandomBuff();
        }
        
        if (Input.GetKeyDown(addDebuffKey))
        {
            AddRandomDebuff();
        }
        
        if (Input.GetKeyDown(processEffectsKey))
        {
            targetCharacter.ProcessStatusEffects();
            Debug.Log("Processed status effects (turn passed)");
        }
        
        if (Input.GetKeyDown(spendManaKey))
        {
            if (targetCharacter.SpendMana(manaSpendAmount))
            {
                Debug.Log($"Spent {manaSpendAmount} mana");
            }
            else
            {
                Debug.Log("Not enough mana!");
            }
        }
        
        if (Input.GetKeyDown(addGoldKey))
        {
            targetCharacter.AddGold(goldAddAmount);
            Debug.Log($"Added {goldAddAmount} gold");
        }
    }
    
    private void AddRandomBuff()
    {
        var buffs = new[]
        {
            new StatusEffect("Regeneration", StatusEffectType.Temporary, 3, 1, "Heals 5 HP per turn")
            {
                hpModifier = 5
            },
            new StatusEffect("Mana Flow", StatusEffectType.Temporary, 5, 1, "Restores 10 mana per turn")
            {
                manaModifier = 10
            },
            new StatusEffect("Lucky", StatusEffectType.Temporary, 2, 1, "Gain 5 gold per turn")
            {
                goldModifier = 5
            },
            new StatusEffect("Iron Skin", StatusEffectType.Temporary, 4, 1, "Reduces damage taken"),
            new StatusEffect("Blessed", StatusEffectType.Permanent, 0, 1, "Divine protection")
        };
        
        var randomBuff = buffs[Random.Range(0, buffs.Length)];
        targetCharacter.AddBuff(randomBuff);
        Debug.Log($"Added buff: {randomBuff.effectName}");
    }
    
    private void AddRandomDebuff()
    {
        var debuffs = new[]
        {
            new StatusEffect("Poison", StatusEffectType.Temporary, 4, 1, "Deals 3 damage per turn")
            {
                hpModifier = -3
            },
            new StatusEffect("Mana Burn", StatusEffectType.Temporary, 3, 1, "Drains 8 mana per turn")
            {
                manaModifier = -8
            },
            new StatusEffect("Cursed", StatusEffectType.Temporary, 2, 1, "Lose 10 gold per turn")
            {
                goldModifier = -10
            },
            new StatusEffect("Weakened", StatusEffectType.Temporary, 5, 1, "Reduced damage output"),
            new StatusEffect("Bleed", StatusEffectType.Temporary, 6, 1, "Continuous bleeding")
            {
                hpModifier = -2
            }
        };
        
        var randomDebuff = debuffs[Random.Range(0, debuffs.Length)];
        targetCharacter.AddDebuff(randomDebuff);
        Debug.Log($"Added debuff: {randomDebuff.effectName}");
    }
    
    private void ShowDemoInstructions()
    {
        Debug.Log("=== CHARACTER SYSTEM DEMO ===");
        Debug.Log("Controls:");
        Debug.Log($"[{damageKey}] - Deal {damageAmount} damage");
        Debug.Log($"[{healKey}] - Heal {healAmount} HP");
        Debug.Log($"[{addBuffKey}] - Add random buff");
        Debug.Log($"[{addDebuffKey}] - Add random debuff");
        Debug.Log($"[{processEffectsKey}] - Process status effects (pass turn)");
        Debug.Log($"[{spendManaKey}] - Spend {manaSpendAmount} mana");
        Debug.Log($"[{addGoldKey}] - Add {goldAddAmount} gold");
        Debug.Log("=============================");
    }
    
    [ContextMenu("Create UI Buttons")]
    public void CreateUIButtons()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("No Canvas found in scene!");
            return;
        }
        
        CreateDemoButtonsPanel(canvas.transform);
    }
    
    private void CreateDemoButtonsPanel(Transform canvasParent)
    {
        // Create button panel in bottom-right corner
        GameObject panel = new GameObject("Demo Buttons Panel");
        panel.transform.SetParent(canvasParent);
        
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(1, 0);
        rect.anchoredPosition = new Vector2(-20, 20);
        rect.sizeDelta = new Vector2(200, 300);
        
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        
        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 5;
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.childForceExpandWidth = true;
        
        // Create buttons
        CreateButton(panel.transform, "Damage", () => {
            targetCharacter?.TakeDamage(damageAmount);
        });
        
        CreateButton(panel.transform, "Heal", () => {
            targetCharacter?.Heal(healAmount);
        });
        
        CreateButton(panel.transform, "Add Buff", AddRandomBuff);
        CreateButton(panel.transform, "Add Debuff", AddRandomDebuff);
        
        CreateButton(panel.transform, "Process Effects", () => {
            targetCharacter?.ProcessStatusEffects();
        });
        
        CreateButton(panel.transform, "Spend Mana", () => {
            targetCharacter?.SpendMana(manaSpendAmount);
        });
        
        CreateButton(panel.transform, "Add Gold", () => {
            targetCharacter?.AddGold(goldAddAmount);
        });
        
        Debug.Log("Demo buttons created in bottom-right corner!");
    }
    
    private void CreateButton(Transform parent, string text, System.Action onClick)
    {
        GameObject buttonObj = new GameObject($"Button - {text}");
        buttonObj.transform.SetParent(parent);
        
        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 30);
        
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        
        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(() => onClick?.Invoke());
        
        // Button text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.sizeDelta = Vector2.zero;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        Text buttonText = textObj.AddComponent<Text>();
        buttonText.text = text;
        buttonText.color = Color.white;
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }
}