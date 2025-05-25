using UnityEngine;

public class DemonAnimator : EnemyAnimator
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
        Debug.Log($"{gameObject.name} Demon Animator: Found {animator.parameters.Length} animator parameters:");
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            Debug.Log($"  - {param.name} ({param.type})");
        }
        
        // Check for common demon animation parameters
        CheckAnimationParameters();
    }
    
    private void CheckAnimationParameters()
    {
        // Use demon-specific parameter names
        string[] commonAttackParams = { "AttackTrigger", "AttackDemon", "Attack" };
        string[] commonMoveParams = { "MoveTrigger", "MoveDemon", "Move" };
        string[] commonHitParams = { "StunedTrigger", "StunedDemon", "Hit" };
        string[] commonDeathParams = { "DeathTrigger", "DeathDemon", "Death" };
        string[] commonSpecialParams = { "SpecialATrigger", "SpecialAttackDemon" };
        
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
        
        foreach (string param in commonSpecialParams)
        {
            if (HasParameter(param))
            {
                Debug.Log($"  ✅ Found SPECIAL ATTACK parameter: {param}");
            }
        }
    }
    
    // Override to use demon-specific attack animation detection
    public override void PlayAttackAnimation()
    {
        if (animator == null) return;
        
        // Try demon-specific parameter names first
        string[] attackParams = { "AttackTrigger", "AttackDemon", "Attack" };
        
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
    
    // Override to use demon-specific movement animation detection
    public override void SetWalking(bool isWalking)
    {
        if (animator == null) return;
        
        // Try demon-specific parameter names first
        string[] moveParams = { "MoveTrigger", "MoveDemon", "Move" };
        
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
    
    // Override to use demon-specific hit animation detection
    public override void PlayHitAnimation()
    {
        Debug.Log($"🎭 {gameObject.name}: DemonAnimator PlayHitAnimation called");
        
        if (animator == null) 
        {
            Debug.LogError($"❌ {gameObject.name}: Animator is null in PlayHitAnimation!");
            return;
        }
        
        // Try demon-specific parameter names first
        string[] hitParams = { "StunedTrigger", "StunedDemon", "Hit", "Damage" };
        
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
    
    // Override to use demon-specific death animation detection
    public override void PlayDeathAnimation()
    {
        if (animator == null) return;
        
        // Try demon-specific parameter names first
        string[] deathParams = { "DeathTrigger", "DeathDemon", "Death", "Die" };
        
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
    
    // Demon-specific animations
    public void PlayClimbAnimation()
    {
        if (animator != null)
        {
            if (HasParameter("ClimbTrigger"))
                animator.SetTrigger("ClimbTrigger");
            else if (HasParameter("ClimbDemon"))
                animator.SetTrigger("ClimbDemon");
        }
    }
    
    public void PlayJumpAnimation()
    {
        if (animator != null)
        {
            if (HasParameter("JumpTrigger"))
                animator.SetTrigger("JumpTrigger");
            else if (HasParameter("JumpDemon"))
                animator.SetTrigger("JumpDemon");
        }
    }
    
    public void PlayTalkAnimation()
    {
        if (animator != null)
        {
            if (HasParameter("TalkTrigger"))
                animator.SetTrigger("TalkTrigger");
            else if (HasParameter("TalkDemon"))
                animator.SetTrigger("TalkDemon");
        }
    }
    
    public void PlaySpecialAttackAnimation()
    {
        if (animator != null)
        {
            if (HasParameter("SpecialATrigger"))
            {
                Debug.Log($"{gameObject.name}: Triggering special attack with SpecialATrigger");
                animator.SetTrigger("SpecialATrigger");
            }
            else if (HasParameter("SpecialAttackDemon"))
            {
                Debug.Log($"{gameObject.name}: Triggering special attack with SpecialAttackDemon");
                animator.SetTrigger("SpecialAttackDemon");
            }
            else
            {
                Debug.LogWarning($"{gameObject.name}: No special attack animation parameter found!");
            }
        }
    }

    // Method to allow DemonAI to set triggers generically
    public void SetTrigger(string triggerName)
    {
        if (animator != null && HasParameter(triggerName))
        {
            animator.SetTrigger(triggerName);
        }
        else if (animator == null)
        {
            Debug.LogError("DemonAnimator: Animator component is null. Cannot set trigger: " + triggerName);
        }
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
        
        return false;
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
    
    [ContextMenu("Test Special Attack Animation")]
    public void TestSpecialAttackAnimation()
    {
        Debug.Log($"{gameObject.name}: Manually testing special attack animation");
        PlaySpecialAttackAnimation();
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
        
        Debug.Log($"{gameObject.name}: 🔍 Validating Demon Animation Setup...");
        Debug.Log($"Controller: {animator.runtimeAnimatorController.name}");
        
        // Check for required parameters
        string[] requiredParams = { "AttackTrigger", "StunedTrigger", "DeathTrigger", "MoveTrigger", "SpecialATrigger" };
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