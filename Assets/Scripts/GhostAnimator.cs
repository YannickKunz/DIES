using UnityEngine;

public class GhostAnimator : EnemyAnimator
{
    private Animator animator;
    
    public override void Initialize(EnemyData enemyData)
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("No Animator component found on " + gameObject.name);
            return;
        }
        
        // Log available parameters for debugging
        Debug.Log("Ghost: Found animator parameters:");
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            Debug.Log("- " + param.name + " (" + param.type + ")");
        }
    }
    
    // Override to use MoveTrigger instead of isWalking
    public override void SetWalking(bool isWalking)
    {
        if (animator != null)
        {
            if (isWalking)
            {
                animator.SetTrigger("MoveTrigger");
            }
            // No need to reset since we're using triggers, not booleans
        }
    }
    
    // Override to use AttackTrigger
    public override void PlayAttackAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("AttackTrigger");
        }
    }
    
    // Override to use StunedTrigger instead of Hit
    public override void PlayHitAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("StunedTrigger");
        }
    }
    
    // Override to use DeathTrigger instead of Die
    public override void PlayDeathAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("DeathTrigger");
        }
    }
    
    // Additional ghost-specific animations
    public void PlayClimbAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("ClimbTrigger");
        }
    }
    
    public void PlayJumpAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("JumpTrigger");
        }
    }
    
    public void PlayTalkAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("TalkTrigger");
        }
    }
    
    public void PlaySpecialAttackAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("SpecialATrigger");
        }
    }
    
    // Helper method to check if animator has parameter before setting
    public bool HasParameter(string paramName)
    {
        if (animator == null)
            return false;
            
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName)
                return true;
        }
        
        return false;
    }
    
    // Additional utility methods to reset triggers when needed
    public void ResetAllTriggers()
    {
        if (animator == null) return;
        
        animator.ResetTrigger("MoveTrigger");
        animator.ResetTrigger("TalkTrigger");
        animator.ResetTrigger("ClimbTrigger");
        animator.ResetTrigger("JumpTrigger");
        animator.ResetTrigger("AttackTrigger");
        animator.ResetTrigger("StunedTrigger");
        animator.ResetTrigger("DeathTrigger");
        animator.ResetTrigger("SpecialATrigger");
    }
}