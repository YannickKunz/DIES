// EnemyManager.cs
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    private static EnemyManager _instance;
    public static EnemyManager Instance => _instance;
    
    [SerializeField] private float enemySpacing = 3f;
    [SerializeField] private bool limitActiveEnemies = true;
    [SerializeField] private int maxActiveEnemies = 5;
    [SerializeField] private float activationRange = 20f;
    
    private List<BaseEnemy> allEnemies = new List<BaseEnemy>();
    private Transform playerTransform;
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
    }
    
    private void Start()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        // Find all enemies in scene
        BaseEnemy[] foundEnemies = FindObjectsByType<BaseEnemy>(FindObjectsSortMode.None);
        allEnemies.AddRange(foundEnemies);
        
        // Initialize them
        foreach (var enemy in allEnemies)
        {
            enemy.gameObject.SetActive(false);
        }
    }
    
    private void Update()
    {
        if (playerTransform == null)
            return;
            
        UpdateEnemyActivation();
    }
    
    private void UpdateEnemyActivation()
    {
        int activeCount = 0;
        
        // Sort enemies by distance to player
        allEnemies.Sort((a, b) => {
            if (a == null || b == null) return 0;
            float distA = Vector2.Distance(a.transform.position, playerTransform.position);
            float distB = Vector2.Distance(b.transform.position, playerTransform.position);
            return distA.CompareTo(distB);
        });
        
        // Activate/deactivate enemies based on distance and maximum count
        foreach (var enemy in allEnemies)
        {
            if (enemy == null) continue;
            
            float distance = Vector2.Distance(enemy.transform.position, playerTransform.position);
            
            if (distance <= activationRange && (!limitActiveEnemies || activeCount < maxActiveEnemies))
            {
                // Only activate if not too close to another active enemy
                if (!IsTooCloseToOtherEnemy(enemy.transform))
                {
                    enemy.gameObject.SetActive(true);
                    activeCount++;
                }
                else
                {
                    enemy.gameObject.SetActive(false);
                }
            }
            else
            {
                enemy.gameObject.SetActive(false);
            }
        }
    }
    
    private bool IsTooCloseToOtherEnemy(Transform enemyTransform)
    {
        foreach (var other in allEnemies)
        {
            if (other == null || !other.gameObject.activeInHierarchy) continue;
            if (other.transform == enemyTransform) continue;
            
            float distance = Vector2.Distance(enemyTransform.position, other.transform.position);
            if (distance < enemySpacing)
            {
                return true;
            }
        }
        return false;
    }
    
    public void RegisterEnemy(BaseEnemy enemy)
    {
        if (!allEnemies.Contains(enemy))
        {
            allEnemies.Add(enemy);
        }
    }
    
    public void UnregisterEnemy(BaseEnemy enemy)
    {
        allEnemies.Remove(enemy);
    }
}