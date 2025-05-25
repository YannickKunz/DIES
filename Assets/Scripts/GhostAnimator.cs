using UnityEngine;

public class GhostAnimator : EnemyAnimator
{
    public override void Initialize(EnemyData enemyData)
    {
        // Call base to set up animator reference
        base.Initialize(enemyData);
        
        // Skip the GetComponent call since it's handled in the base class
        if (animator == null)
        {
            Debug.LogError("No Animator component found on " + gameObject.name);
            return;
        }
        
        // Log available parameters for debugging
        Debug.Log($"{gameObject.name} Ghost Animator: Found {animator.parameters.Length} animator parameters:");
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            Debug.Log($"  - {param.name} ({param.type})");
    }
    
        // Check for common ghost animation parameters
        CheckAnimationParameters();
    }
    
    private void CheckAnimationParameters()
    {
        // Use the user's exact parameter names first
        string[] commonAttackParams = { "AttackTrigger", "AttackGhost", "Attack", "attack" };
        string[] commonMoveParams = { "MoveTrigger", "Move", "isWalking", "Walking" };
        string[] commonHitParams = { "StunedTrigger", "Hit", "Damage", "TakeDamage" };
        string[] commonDeathParams = { "DeathTrigger", "Death", "Die", "Dying" };
        
        Debug.Log($"{gameObject.name}: Checking for animation parameters...");
        
        foreach (string param in commonAttackParams)
        {
            if (HasParameter(param))
            {
                Debug.Log($"  ✅ Found ATTACK parameter: {param}");
            }
        }
        
        foreach (string param in commonMoveParams)
        {
            if (HasParameter(param))
            {
                Debug.Log($"  ✅ Found MOVE parameter: {param}");
            }
        }
        
        foreach (string param in commonHitParams)
        {
            if (HasParameter(param))
            {
                Debug.Log($"  ✅ Found HIT parameter: {param}");
            }
        }
        
        foreach (string param in commonDeathParams)
        {
            if (HasParameter(param))
            {
                Debug.Log($"  ✅ Found DEATH parameter: {param}");
            }
        }
    }
    
    // Override to use flexible attack animation detection
    public override void PlayAttackAnimation()
    {
        if (animator == null) return;
        
        // Try user's exact parameter names first
        string[] attackParams = { "AttackTrigger", "AttackGhost", "Attack", "attack" };
        
        foreach (string param in attackParams)
        {
            if (HasParameter(param))
            {
                Debug.Log($"{gameObject.name}: Triggering attack animation with parameter '{param}'");
                animator.SetTrigger(param);
                return; // Success! Exit after first match
            }
        }
        
        Debug.LogWarning($"{gameObject.name}: No attack animation parameter found! Tried: {string.Join(", ", attackParams)}");
    }
    
    // Override to use flexible movement animation detection
    public override void SetWalking(bool isWalking)
    {
        if (animator == null) return;
        
        // Try user's exact parameter names first
        string[] moveParams = { "MoveTrigger", "Move", "isWalking", "Walking" };
        
        foreach (string param in moveParams)
        {
            if (HasParameter(param))
            {
                AnimatorControllerParameter paramInfo = GetParameterInfo(param);
                if (paramInfo.type == AnimatorControllerParameterType.Trigger && isWalking)
                {
                    Debug.Log($"{gameObject.name}: Triggering movement with trigger '{param}'");
                    animator.SetTrigger(param);
                    return;
                }
                else if (paramInfo.type == AnimatorControllerParameterType.Bool)
                {
                    Debug.Log($"{gameObject.name}: Setting movement bool '{param}' to {isWalking}");
                    animator.SetBool(param, isWalking);
                    return;
                }
            }
        }
        
        if (isWalking)
        {
            Debug.LogWarning($"{gameObject.name}: No movement animation parameter found! Tried: {string.Join(", ", moveParams)}");
        }
    }
    
    // Override to use flexible hit animation detection
    public override void PlayHitAnimation()
    {
        if (animator == null) return;
        
        // Try user's exact parameter names first
        string[] hitParams = { "StunedTrigger", "Hit", "Damage", "TakeDamage", "hurt" };
        
        foreach (string param in hitParams)
        {
            if (HasParameter(param))
            {
                Debug.Log($"{gameObject.name}: Triggering hit animation with parameter '{param}'");
                animator.SetTrigger(param);
                return;
            }
        }
        
        Debug.LogWarning($"{gameObject.name}: No hit animation parameter found! Tried: {string.Join(", ", hitParams)}");
    }
    
    // Override to use flexible death animation detection
    public override void PlayDeathAnimation()
    {
        if (animator == null) return;
        
        // Try user's exact parameter names first
        string[] deathParams = { "DeathTrigger", "Death", "Die", "Dying", "dead" };
        
        foreach (string param in deathParams)
        {
            if (HasParameter(param))
            {
                Debug.Log($"{gameObject.name}: Triggering death animation with parameter '{param}'");
                animator.SetTrigger(param);
                return;
            }
        }
        
        Debug.LogWarning($"{gameObject.name}: No death animation parameter found! Tried: {string.Join(", ", deathParams)}");
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

    // New method to allow GhostAI to set triggers generically
    public void SetTrigger(string triggerName)
    {
        if (animator != null && HasParameter(triggerName)) // Optional: check HasParameter here too
        {
            animator.SetTrigger(triggerName);
        }
        else if (animator == null)
        {
            Debug.LogError("GhostAnimator: Animator component is null. Cannot set trigger: " + triggerName);
        }
        // If HasParameter is false, it's already handled by GhostAI's FireAnimationTrigger or a warning will be logged by it.
    }
    
    // Helper method to check if animator has parameter before setting
    public override bool HasParameter(string paramName)
    {
        if (animator == null)
            return false;
            
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName)
                return true;
        }
        
        // Don't log warnings here since we try multiple parameter names
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
        animator.ResetTrigger("AttackGhost"); // Reset user's animation too
    }
    
    // Helper method to get parameter info
    private AnimatorControllerParameter GetParameterInfo(string paramName)
    {
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName)
                return param;
        }
        return null;
    }
    
    // Manual testing methods (accessible from Inspector)
    [ContextMenu("Test Attack Animation")]
    public void TestAttackAnimation()
    {
        Debug.Log($"{gameObject.name}: Manually testing attack animation");
        PlayAttackAnimation();
    }
    
    [ContextMenu("Test Hit Animation")]
    public void TestHitAnimation()
    {
        Debug.Log($"{gameObject.name}: Manually testing hit animation");
        PlayHitAnimation();
    }
    
    [ContextMenu("Test Death Animation")]
    public void TestDeathAnimation()
    {
        Debug.Log($"{gameObject.name}: Manually testing death animation");
        PlayDeathAnimation();
    }
    
    [ContextMenu("Test Movement Animation")]
    public void TestMovementAnimation()
    {
        Debug.Log($"{gameObject.name}: Manually testing movement animation");
        SetWalking(true);
    }
    
    [ContextMenu("List All Animation Parameters")]
    public void ListAllParameters()
    {
        if (animator == null)
        {
            Debug.LogError($"{gameObject.name}: No animator found!");
            return;
        }
        
        Debug.Log($"{gameObject.name}: Animation Controller Parameters:");
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            Debug.Log($"  • {param.name} ({param.type})");
        }
    }
    
    [ContextMenu("Validate Animation Setup")]
    public void ValidateAnimationSetup()
    {
        if (animator == null)
        {
            Debug.LogError($"{gameObject.name}: ❌ No Animator component found!");
            return;
        }
        
        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogError($"{gameObject.name}: ❌ No Animation Controller assigned to Animator!");
            return;
        }
        
        Debug.Log($"{gameObject.name}: 🔍 Validating Animation Setup...");
        Debug.Log($"Controller: {animator.runtimeAnimatorController.name}");
        
        // Check for required parameters
        string[] requiredParams = { "AttackTrigger", "StunedTrigger", "DeathTrigger", "MoveTrigger" };
        int foundParams = 0;
        
        foreach (string param in requiredParams)
        {
            if (HasParameter(param))
            {
                Debug.Log($"  ✅ {param} - Found!");
                foundParams++;
            }
            else
            {
                Debug.LogWarning($"  ❌ {param} - Missing!");
            }
        }
        
        if (foundParams == requiredParams.Length)
        {
            Debug.Log($"🎉 {gameObject.name}: All required animation parameters found!");
        }
        else
        {
            Debug.LogWarning($"⚠️ {gameObject.name}: {foundParams}/{requiredParams.Length} required parameters found.");
        }
        
        // Additional checks
        Debug.Log($"Total Parameters: {animator.parameterCount}");
        Debug.Log($"Layer Count: {animator.layerCount}");
        
        // Check if we're in a valid state
        if (animator.layerCount > 0)
        {
            AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
            Debug.Log($"Current State: {currentState.fullPathHash} (normalized time: {currentState.normalizedTime:F2})");
        }
    }
}