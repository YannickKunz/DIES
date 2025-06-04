using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class EnemyGroupInfo
{
    public string enemyTag;
    public string groupName;
    public int expectedCount;
}

public class EnemySetupValidator : MonoBehaviour
{
    [Header("Enemy Groups to Validate")]
    [SerializeField] private EnemyGroupInfo[] enemyGroups = new EnemyGroupInfo[]
    {
        new EnemyGroupInfo { enemyTag = "Ghost", groupName = "Ghosts", expectedCount = 2 },
        new EnemyGroupInfo { enemyTag = "Enemy", groupName = "Skeletons", expectedCount = 3 }
    };
    
    [Header("Validation Results")]
    [SerializeField] private bool showDetailedResults = true;
    
    [ContextMenu("🔍 Validate All Enemy Groups")]
    public void ValidateAllEnemyGroups()
    {
        Debug.Log("=== ENEMY SETUP VALIDATION START ===");
        
        bool allValid = true;
        
        foreach (var group in enemyGroups)
        {
            bool groupValid = ValidateEnemyGroup(group);
            if (!groupValid) allValid = false;
        }
        
        if (allValid)
        {
            Debug.Log("✅ ALL ENEMY GROUPS ARE PROPERLY CONFIGURED!");
        }
        else
        {
            Debug.LogWarning("⚠️ Some enemy groups need attention. Check logs above.");
        }
        
        Debug.Log("=== ENEMY SETUP VALIDATION END ===");
    }
    
    private bool ValidateEnemyGroup(EnemyGroupInfo groupInfo)
    {
        Debug.Log($"\n🔍 Validating {groupInfo.groupName} (Tag: '{groupInfo.enemyTag}')...");
        
        // Find all objects with the tag
        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(groupInfo.enemyTag);
        
        if (taggedObjects.Length == 0)
        {
            Debug.LogError($"❌ No objects found with tag '{groupInfo.enemyTag}'!");
            return false;
        }
        
        Debug.Log($"📊 Found {taggedObjects.Length} objects with tag '{groupInfo.enemyTag}' (Expected: {groupInfo.expectedCount})");
        
        if (taggedObjects.Length != groupInfo.expectedCount)
        {
            Debug.LogWarning($"⚠️ Count mismatch! Expected {groupInfo.expectedCount}, found {taggedObjects.Length}");
        }
        
        // Validate each object
        int validEnemies = 0;
        List<string> invalidEnemies = new List<string>();
        
        foreach (GameObject enemy in taggedObjects)
        {
            bool isValid = ValidateSingleEnemy(enemy, groupInfo.enemyTag);
            if (isValid)
            {
                validEnemies++;
            }
            else
            {
                invalidEnemies.Add(enemy.name);
            }
        }
        
        // Summary for this group
        if (validEnemies == taggedObjects.Length)
        {
            Debug.Log($"✅ {groupInfo.groupName}: All {validEnemies} enemies are properly configured!");
            return true;
        }
        else
        {
            Debug.LogError($"❌ {groupInfo.groupName}: Only {validEnemies}/{taggedObjects.Length} enemies are properly configured!");
            Debug.LogError($"Invalid enemies: {string.Join(", ", invalidEnemies)}");
            return false;
        }
    }
    
    private bool ValidateSingleEnemy(GameObject enemy, string expectedTag)
    {
        bool isValid = true;
        List<string> issues = new List<string>();
        
        // Check tag
        if (!enemy.CompareTag(expectedTag))
        {
            issues.Add($"Wrong tag (has '{enemy.tag}', expected '{expectedTag}')");
            isValid = false;
        }
        
        // Check EnemyHealth component
        EnemyHealth health = enemy.GetComponent<EnemyHealth>();
        if (health == null)
        {
            issues.Add("Missing EnemyHealth component");
            isValid = false;
        }
        
        // Check other important components based on enemy type
        if (expectedTag == "Ghost")
        {
            if (enemy.GetComponent<GhostAI>() == null)
                issues.Add("Missing GhostAI component");
            if (enemy.GetComponent<GhostInitializer>() == null)
                issues.Add("Missing GhostInitializer component");
        }
        else if (expectedTag == "Demon")
        {
            if (enemy.GetComponent<DemonAI>() == null)
                issues.Add("Missing DemonAI component");
        }
        
        // Check collider
        Collider2D col = enemy.GetComponent<Collider2D>();
        if (col == null)
        {
            issues.Add("Missing Collider2D component");
            isValid = false;
        }
        
        if (showDetailedResults)
        {
            if (isValid)
            {
                Debug.Log($"✅ {enemy.name}: Valid {expectedTag}");
            }
            else
            {
                Debug.LogError($"❌ {enemy.name}: Issues found - {string.Join(", ", issues)}");
            }
        }
        
        return isValid;
    }
    
    [ContextMenu("📋 List All Tagged Objects")]
    public void ListAllTaggedObjects()
    {
        Debug.Log("=== ALL TAGGED OBJECTS ===");
        
        foreach (var group in enemyGroups)
        {
            GameObject[] objects = GameObject.FindGameObjectsWithTag(group.enemyTag);
            Debug.Log($"\n{group.enemyTag} ({objects.Length} objects):");
            
            foreach (GameObject obj in objects)
            {
                Debug.Log($"  • {obj.name} at {obj.transform.position}");
            }
        }
    }
    
    [ContextMenu("🏷️ Show Available Tags")]
    public void ShowAvailableTags()
    {
        Debug.Log("=== AVAILABLE TAGS ===");
        Debug.Log("Common enemy tags to use:");
        Debug.Log("• Ghost - for ghost enemies");
        Debug.Log("• Enemy - for skeleton/basic enemies");  
        Debug.Log("• Demon - for demon enemies");
        Debug.Log("• Player - for the player");
        
        Debug.Log("\nMake sure your enemies have the correct tags assigned in the Inspector!");
    }
} 