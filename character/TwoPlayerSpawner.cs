using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class TwoPlayerSpawner : MonoBehaviour
{
    [System.Serializable]
    public class CharacterPrefabEntry
    {
        [Tooltip("The CharacterData asset (e.g., Alon, Kidlat).")]
        public CharacterData characterData;

        [Tooltip("Prefab to spawn when this CharacterData is assigned to a player.")]
        public GameObject prefab;
    }

    [Header("Spawn Points")]
    public Transform player1Spawn;
    public Transform player2Spawn;

    [Header("Prefabs (Optional)")]
    [Tooltip("If set, this prefab will be instantiated for Player1. Must include a Character component (or one will be added).")]
    public GameObject player1Prefab;

    [Tooltip("If set, this prefab will be instantiated for Player2. Must include a Character component (or one will be added).")]
    public GameObject player2Prefab;

    [Header("Character Prefab Mapping (Recommended)")]
    [Tooltip("If provided, this mapping overrides player1Prefab/player2Prefab so the spawned prefab matches the chosen character.")]
    public List<CharacterPrefabEntry> characterPrefabs = new List<CharacterPrefabEntry>();

    [Header("Fallback Character Data (Optional)")]
    [Tooltip("Used if GameManager does not provide Player2 override data.")]
    public CharacterData player2FallbackCharacterData;

    [Header("Options")]
    public bool spawnOnStart = true;

    [Tooltip("If true, after spawning Player1 and Player2 will rotate to face each other (Y axis only).")]
    public bool faceEachOtherOnSpawn = true;

    [Tooltip("If true, destroys previously spawned players when spawning again.")]
    public bool respawnIfAlreadySpawned = true;

    [Tooltip("Optional: if set, this binder will be assigned spawned Player1/Player2 objects automatically.")]
    public HPTrackerBinder hpTrackerBinder;

    public GameObject SpawnedPlayer1 { get; private set; }
    public GameObject SpawnedPlayer2 { get; private set; }

    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnFromGameState();
        }
    }

    [ContextMenu("Spawn From Game State")]
    public void SpawnFromGameState()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogWarning("TwoPlayerSpawner: GameManager.Instance not found. Cannot spawn from selection.");
            return;
        }

        CharacterData player1Data = gm.selectedCharacterData;
        if (player1Data == null)
        {
            Debug.LogWarning("TwoPlayerSpawner: GameManager has no selectedCharacterData. Cannot spawn Player1.");
            return;
        }

        CharacterData player2Data = null;
        if (gm.overridePlayer2FromSelection && gm.player2CharacterDataOverride != null)
        {
            player2Data = gm.player2CharacterDataOverride;
        }
        else
        {
            player2Data = player2FallbackCharacterData;
        }

        if (player2Data == null)
        {
            // Best-effort: try to load the "other" character by name (Alon/Kidlat) from Resources.
            player2Data = TryResolveOtherByName(player1Data.characterName);
        }

        if (player2Data == null)
        {
            Debug.LogWarning("TwoPlayerSpawner: No Player2 character data available. Assign player2FallbackCharacterData, or ensure CharacterSelection sets the Player2 override.");
            return;
        }

        SpawnPlayers(player1Data, player2Data);

        // Keep GameManager active reference aligned with Player1.
        if (SpawnedPlayer1 != null)
        {
            gm.activeCharacterObject = SpawnedPlayer1;
        }

        AssignBinderIfPresent();
    }

    public void SpawnPlayers(CharacterData player1Data, CharacterData player2Data)
    {
        if (player1Spawn == null || player2Spawn == null)
        {
            Debug.LogWarning("TwoPlayerSpawner: Assign both player1Spawn and player2Spawn.");
            return;
        }

        if (!respawnIfAlreadySpawned)
        {
            if (SpawnedPlayer1 != null || SpawnedPlayer2 != null)
            {
                return;
            }
        }

        DestroySpawnedIfAny();

        GameObject p1Prefab = ResolvePrefabForCharacter(player1Data, player1Prefab);
        GameObject p2Prefab = ResolvePrefabForCharacter(player2Data, player2Prefab);

        SpawnedPlayer1 = SpawnOne(p1Prefab, player1Spawn, $"Player1 - {player1Data.characterName}", player1Data);
        SpawnedPlayer2 = SpawnOne(p2Prefab, player2Spawn, $"Player2 - {player2Data.characterName}", player2Data);

        if (faceEachOtherOnSpawn)
        {
            // Apply immediately, then again next frame (common fix if other scripts reset rotation in Start()).
            FaceOff(SpawnedPlayer1, SpawnedPlayer2);
            StartCoroutine(ApplyFaceOffNextFrame());
        }
    }

    private IEnumerator ApplyFaceOffNextFrame()
    {
        // Wait one frame so all spawned components run Start() before we set the final facing.
        yield return null;

        if (SpawnedPlayer1 == null || SpawnedPlayer2 == null) yield break;
        FaceOff(SpawnedPlayer1, SpawnedPlayer2);
    }

    private void DestroySpawnedIfAny()
    {
        if (SpawnedPlayer1 != null)
        {
            Destroy(SpawnedPlayer1);
            SpawnedPlayer1 = null;
        }

        if (SpawnedPlayer2 != null)
        {
            Destroy(SpawnedPlayer2);
            SpawnedPlayer2 = null;
        }
    }

    private static GameObject SpawnOne(GameObject prefab, Transform spawn, string fallbackName, CharacterData data)
    {
        GameObject obj;
        if (prefab != null)
        {
            obj = Instantiate(prefab, spawn.position, spawn.rotation);
            obj.name = fallbackName;
        }
        else
        {
            obj = new GameObject(fallbackName);
            obj.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
        }

        Character character = obj.GetComponent<Character>();
        if (character == null)
        {
            character = obj.AddComponent<Character>();
        }

        character.SetCharacterData(data);
        return obj;
    }

    private static void FaceOff(GameObject player1, GameObject player2)
    {
        if (player1 == null || player2 == null) return;

        Vector3 p1 = player1.transform.position;
        Vector3 p2 = player2.transform.position;

        Vector3 p1ToP2 = p2 - p1;
        p1ToP2.y = 0f;
        Vector3 p2ToP1 = p1 - p2;
        p2ToP1.y = 0f;

        if (p1ToP2.sqrMagnitude > 0.0001f)
        {
            player1.transform.rotation = LookRotationNegativeX(p1ToP2.normalized);
        }

        if (p2ToP1.sqrMagnitude > 0.0001f)
        {
            player2.transform.rotation = LookRotationNegativeX(p2ToP1.normalized);
        }
    }

    private static Quaternion LookRotationNegativeX(Vector3 direction)
    {
        // Unity's LookRotation assumes +Z forward.
        // This project uses prefabs whose "forward" points along Negative X.
        // To make Negative X face the target direction, apply a +90° yaw offset.
        Quaternion baseRot = Quaternion.LookRotation(direction, Vector3.up);
        return baseRot * Quaternion.Euler(0f, 60f, 0f);
    }

    private GameObject ResolvePrefabForCharacter(CharacterData data, GameObject fallback)
    {
        if (data == null) return fallback;

        if (characterPrefabs != null)
        {
            // First try exact asset reference match.
            foreach (var entry in characterPrefabs)
            {
                if (entry == null) continue;
                if (entry.characterData == data && entry.prefab != null)
                {
                    return entry.prefab;
                }
            }

            // Then try characterName match (useful if assets were duplicated).
            foreach (var entry in characterPrefabs)
            {
                if (entry == null || entry.characterData == null || entry.prefab == null) continue;
                if (entry.characterData.characterName.Equals(data.characterName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return entry.prefab;
                }
            }
        }

        return fallback;
    }

    private void AssignBinderIfPresent()
    {
        HPTrackerBinder binder = hpTrackerBinder;
        if (binder == null)
        {
            binder = FindObjectOfType<HPTrackerBinder>();
        }

        if (binder == null) return;

        if (SpawnedPlayer1 != null) binder.player1Object = SpawnedPlayer1;
        if (SpawnedPlayer2 != null) binder.player2Object = SpawnedPlayer2;
        binder.RefreshBindings();
    }

    private static CharacterData TryResolveOtherByName(string selectedName)
    {
        if (string.IsNullOrEmpty(selectedName)) return null;

        string otherName;
        if (selectedName.Equals("Alon", System.StringComparison.OrdinalIgnoreCase))
            otherName = "Kidlat";
        else if (selectedName.Equals("Kidlat", System.StringComparison.OrdinalIgnoreCase))
            otherName = "Alon";
        else
            return null;

        // Common pattern in this project: Resources/CharacterData/
        CharacterData[] fromFolder = Resources.LoadAll<CharacterData>("CharacterData");
        if (fromFolder != null)
        {
            foreach (var cd in fromFolder)
            {
                if (cd != null && cd.characterName.Equals(otherName, System.StringComparison.OrdinalIgnoreCase))
                    return cd;
            }
        }

        // Fallback: load all CharacterData in Resources.
        CharacterData[] fromRoot = Resources.LoadAll<CharacterData>(string.Empty);
        if (fromRoot != null)
        {
            foreach (var cd in fromRoot)
            {
                if (cd != null && cd.characterName.Equals(otherName, System.StringComparison.OrdinalIgnoreCase))
                    return cd;
            }
        }

        return null;
     }
 }
