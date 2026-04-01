using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Character : MonoBehaviour
{
    [Header("Character Data")]
    public CharacterData characterData;
    
    [Header("Current Stats")]
    [SerializeField] private int currentHP;
    [SerializeField] private int currentGold;
    [SerializeField] private int currentMana;
    
    [Header("Active Effects")]
    [SerializeField] private List<StatusEffect> activeDebuffs = new List<StatusEffect>();
    [SerializeField] private List<StatusEffect> activeBuffs = new List<StatusEffect>();
    
    // Events for UI updates
    public System.Action<int, int> OnHPChanged; // currentHP, maxHP
    public System.Action<int> OnGoldChanged;
    public System.Action<int, int> OnManaChanged; // currentMana, maxMana
    public System.Action<List<StatusEffect>> OnDebuffsChanged;
    public System.Action<List<StatusEffect>> OnBuffsChanged;
    public System.Action OnCharacterDied;
    
    private void Start()
    {
        InitializeCharacter();
    }
    
    public void InitializeCharacter()
    {
        if (characterData != null)
        {
            currentHP = characterData.startingHP;
            currentGold = characterData.gold;
            currentMana = characterData.startingMana;
            
            // Copy starting effects
            activeDebuffs = characterData.debuffs.Select(effect => effect.Clone()).ToList();
            activeBuffs = characterData.buffs.Select(effect => effect.Clone()).ToList();
            
            // Trigger UI updates
            RefreshUI();
        }
        else
        {
            Debug.LogWarning($"No CharacterData assigned to {gameObject.name}");
        }
    }
    
    public void SetCharacterData(CharacterData data)
    {
        characterData = data;
        InitializeCharacter();
    }
    
    // HP Management
    public void TakeDamage(int damage)
    {
        if (damage <= 0) return;
        
        currentHP = Mathf.Max(0, currentHP - damage);
        OnHPChanged?.Invoke(currentHP, GetMaxHP());
        
        if (currentHP == 0)
        {
            Die();
        }
    }
    
    public void Heal(int healAmount)
    {
        if (healAmount <= 0) return;
        
        currentHP = Mathf.Min(GetMaxHP(), currentHP + healAmount);
        OnHPChanged?.Invoke(currentHP, GetMaxHP());
    }
    
    // Mana Management
    public bool SpendMana(int amount)
    {
        if (currentMana >= amount)
        {
            currentMana -= amount;
            OnManaChanged?.Invoke(currentMana, GetMaxMana());
            return true;
        }
        return false;
    }
    
    public void RestoreMana(int amount)
    {
        if (amount <= 0) return;
        
        currentMana = Mathf.Min(GetMaxMana(), currentMana + amount);
        OnManaChanged?.Invoke(currentMana, GetMaxMana());
    }
    
    // Gold Management
    public bool SpendGold(int amount)
    {
        if (currentGold >= amount)
        {
            currentGold -= amount;
            OnGoldChanged?.Invoke(currentGold);
            return true;
        }
        return false;
    }
    
    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        
        currentGold += amount;
        OnGoldChanged?.Invoke(currentGold);
    }
    
    // Status Effect Management
    public void AddDebuff(StatusEffect debuff)
    {
        if (debuff == null) return;
        
        activeDebuffs.Add(debuff.Clone());
        OnDebuffsChanged?.Invoke(activeDebuffs);
    }
    
    public void AddBuff(StatusEffect buff)
    {
        if (buff == null) return;
        
        activeBuffs.Add(buff.Clone());
        OnBuffsChanged?.Invoke(activeBuffs);
    }
    
    public void RemoveDebuff(string effectName)
    {
        activeDebuffs.RemoveAll(effect => effect.effectName == effectName);
        OnDebuffsChanged?.Invoke(activeDebuffs);
    }
    
    public void RemoveBuff(string effectName)
    {
        activeBuffs.RemoveAll(effect => effect.effectName == effectName);
        OnBuffsChanged?.Invoke(activeBuffs);
    }
    
    public void ProcessStatusEffects()
    {
        // Process debuffs
        for (int i = activeDebuffs.Count - 1; i >= 0; i--)
        {
            var debuff = activeDebuffs[i];
            ApplyStatusEffectModifiers(debuff);
            debuff.ProcessTurn();
            
            if (debuff.IsExpired())
            {
                activeDebuffs.RemoveAt(i);
            }
        }
        
        // Process buffs
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            var buff = activeBuffs[i];
            ApplyStatusEffectModifiers(buff);
            buff.ProcessTurn();
            
            if (buff.IsExpired())
            {
                activeBuffs.RemoveAt(i);
            }
        }
        
        OnDebuffsChanged?.Invoke(activeDebuffs);
        OnBuffsChanged?.Invoke(activeBuffs);
    }
    
    private void ApplyStatusEffectModifiers(StatusEffect effect)
    {
        if (effect.hpModifier != 0)
        {
            if (effect.hpModifier > 0)
                Heal(effect.hpModifier);
            else
                TakeDamage(-effect.hpModifier);
        }
        
        if (effect.manaModifier != 0)
        {
            if (effect.manaModifier > 0)
                RestoreMana(effect.manaModifier);
            else
                SpendMana(-effect.manaModifier);
        }
        
        if (effect.goldModifier != 0)
        {
            if (effect.goldModifier > 0)
                AddGold(effect.goldModifier);
            else
                SpendGold(-effect.goldModifier);
        }
    }
    
    private void Die()
    {
        Debug.Log($"{characterData.characterName} has died!");
        OnCharacterDied?.Invoke();
    }
    
    // Getters
    public int GetMaxHP() => characterData != null ? characterData.maxHP : 100;
    public int GetMaxMana() => characterData != null ? characterData.maxMana : 100;
    public int GetCurrentHP() => currentHP;
    public int GetCurrentMana() => currentMana;
    public int GetCurrentGold() => currentGold;
    public List<StatusEffect> GetActiveDebuffs() => activeDebuffs;
    public List<StatusEffect> GetActiveBuffs() => activeBuffs;
    public string GetCharacterName() => characterData != null ? characterData.characterName : "Unknown";
    
    private void RefreshUI()
    {
        OnHPChanged?.Invoke(currentHP, GetMaxHP());
        OnManaChanged?.Invoke(currentMana, GetMaxMana());
        OnGoldChanged?.Invoke(currentGold);
        OnDebuffsChanged?.Invoke(activeDebuffs);
        OnBuffsChanged?.Invoke(activeBuffs);
    }
    
    [ContextMenu("Reset Character Stats")]
    public void ResetStats()
    {
        InitializeCharacter();
    }
    
    [ContextMenu("Apply Random Debuff")]
    public void TestAddDebuff()
    {
        var testDebuff = new StatusEffect("Test Poison", StatusEffectType.Temporary, 3, 1, "Deals 5 damage per turn")
        {
            hpModifier = -5
        };
        AddDebuff(testDebuff);
    }
    
    [ContextMenu("Apply Random Buff")]
    public void TestAddBuff()
    {
        var testBuff = new StatusEffect("Test Regeneration", StatusEffectType.Temporary, 3, 1, "Heals 3 HP per turn")
        {
            hpModifier = 3
        };
        AddBuff(testBuff);
    }
}