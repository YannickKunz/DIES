// EnemyAnimator.cs
using UnityEngine;

public class EnemyAnimator : MonoBehaviour
{
    private Animator animator;
    
    public void Initialize(EnemyData enemyData)
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("No Animator component found on " + gameObject.name);
        }
        
        // Log available parameters for debugging
        Debug.Log("Found animator parameters:");
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            Debug.Log("- " + param.name + " (" + param.type + ")");
        }
    }
    
    public void SetWalking(bool isWalking)
    {
        if (animator != null && HasParameter("isWalking"))
        {
            animator.SetBool("isWalking", isWalking);
        }
    }
    
    public void PlayAttackAnimation()
    {
        if (animator != null && HasParameter("Attack"))
        {
            animator.SetTrigger("Attack");
        }
    }
    
    public void PlayHitAnimation()
    {
        if (animator != null && HasParameter("Hit"))
        {
            animator.SetTrigger("Hit");
        }
    }
    
    public void PlayDeathAnimation()
    {
        if (animator != null && HasParameter("Die"))
        {
            animator.SetTrigger("Die");
        }
    }
    
    private bool HasParameter(string paramName)
    {
        if (animator == null)
            return false;
            
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName)
                return true;
        }
        
        Debug.LogWarning(gameObject.name + ": Animator parameter '" + paramName + "' not found!");
        return false;
    }
}