using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
[ExecuteInEditMode]
#endif
public class GhostParamTuner : MonoBehaviour
{
    [SerializeField] private GhostEnemy ghost;
    [SerializeField] private KeyCode toggleKey = KeyCode.BackQuote; // ` key
    private bool showWindow = false;
    private Rect windowRect = new Rect(20, 20, 300, 500);
    
    private void OnEnable()
    {
        // Auto-assign if on the same GameObject
        if (ghost == null)
            ghost = GetComponent<GhostEnemy>();
            
        // Look for nearby ghost if not on this GameObject
        if (ghost == null)
            ghost = GetComponentInChildren<GhostEnemy>(true);
            
        // Last resort - find first ghost in scene
        if (ghost == null)
            ghost = FindFirstObjectByType<GhostEnemy>();
    }
    
    private void Update()
    {
        // Toggle tuner window
        if (Input.GetKeyDown(toggleKey))
        {
            showWindow = !showWindow;
        }
    }
    
    private void OnGUI()
    {
        if (showWindow && ghost != null)
        {
            windowRect = GUILayout.Window(1234, windowRect, DrawWindow, "Ghost Parameter Tuner");
        }
    }
    
    private void DrawWindow(int windowID)
    {
        GUILayout.Label("Ghost Entity Settings", EditorStyles.boldLabel);
        
        // Get references to all components
        GhostMovement movement = ghost.GetComponent<GhostMovement>();
        GhostAI ai = ghost.GetComponent<GhostAI>();
        GhostAttack attack = ghost.GetComponent<GhostAttack>();
        
        // Draw sections for different component types
        if (movement != null)
            DrawMovementSettings(movement);
            
        if (ai != null)
            DrawAISettings(ai);
            
        if (attack != null)
            DrawAttackSettings(attack);
            
        GUI.DragWindow(new Rect(0, 0, windowRect.width, 20));
    }
    
    private void DrawMovementSettings(GhostMovement movement)
    {
        GUILayout.Space(10);
        GUILayout.Label("Movement Settings", EditorStyles.boldLabel);
        
        // Get field using reflection
        var hoverSmoothingField = movement.GetType().GetField("hoverSmoothing", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
        if (hoverSmoothingField != null)
        {
            float currentValue = (float)hoverSmoothingField.GetValue(movement);
            float newValue = GUILayout.HorizontalSlider(currentValue, 0.05f, 0.5f);
            GUILayout.Label($"Hover Smoothing: {newValue:F2}");
            
            if (newValue != currentValue)
                hoverSmoothingField.SetValue(movement, newValue);
        }
        
        var bobAmpField = movement.GetType().GetField("hoverBobAmplitude", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
        if (bobAmpField != null)
        {
            float currentValue = (float)bobAmpField.GetValue(movement);
            float newValue = GUILayout.HorizontalSlider(currentValue, 0.01f, 0.3f);
            GUILayout.Label($"Bob Amplitude: {newValue:F2}");
            
            if (newValue != currentValue)
                bobAmpField.SetValue(movement, newValue);
        }
        
        var bobSpeedField = movement.GetType().GetField("hoverBobSpeed", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
        if (bobSpeedField != null)
        {
            float currentValue = (float)bobSpeedField.GetValue(movement);
            float newValue = GUILayout.HorizontalSlider(currentValue, 0.5f, 3f);
            GUILayout.Label($"Bob Speed: {newValue:F2}");
            
            if (newValue != currentValue)
                bobSpeedField.SetValue(movement, newValue);
        }
        
        var jumpForceField = movement.GetType().GetField("jumpForce", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
        if (jumpForceField != null)
        {
            float currentValue = (float)jumpForceField.GetValue(movement);
            float newValue = GUILayout.HorizontalSlider(currentValue, 5f, 15f);
            GUILayout.Label($"Jump Force: {newValue:F2}");
            
            if (newValue != currentValue)
                jumpForceField.SetValue(movement, newValue);
        }
    }
    
    private void DrawAISettings(GhostAI ai)
    {
        GUILayout.Space(10);
        GUILayout.Label("AI Settings", EditorStyles.boldLabel);
        
        var detectionRangeField = ai.GetType().GetField("instanceDetectionRange", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
        if (detectionRangeField != null)
        {
            float currentValue = (float)detectionRangeField.GetValue(ai);
            float newValue = GUILayout.HorizontalSlider(currentValue, 5f, 20f);
            GUILayout.Label($"Detection Range: {newValue:F2}");
            
            if (newValue != currentValue)
                detectionRangeField.SetValue(ai, newValue);
        }
        
        var attackRangeField = ai.GetType().GetField("instanceAttackRange", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
        if (attackRangeField != null)
        {
            float currentValue = (float)attackRangeField.GetValue(ai);
            float newValue = GUILayout.HorizontalSlider(currentValue, 1f, 15f);
            GUILayout.Label($"Attack Range: {newValue:F2}");
            
            if (newValue != currentValue)
                attackRangeField.SetValue(ai, newValue);
        }
        
        // Toggle debug rays
        var debugRaysField = ai.GetType().GetField("showDebugRays", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
        if (debugRaysField != null)
        {
            bool currentValue = (bool)debugRaysField.GetValue(ai);
            bool newValue = GUILayout.Toggle(currentValue, "Show Debug Rays");
            
            if (newValue != currentValue)
                debugRaysField.SetValue(ai, newValue);
        }
    }
    
    private void DrawAttackSettings(GhostAttack attack)
    {
        GUILayout.Space(10);
        GUILayout.Label("Attack Settings", EditorStyles.boldLabel);
        
        // Get attack radius field using reflection
        var attackRadiusField = attack.GetType().GetField("instanceAttackRadius", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
        if (attackRadiusField != null)
        {
            float currentValue = (float)attackRadiusField.GetValue(attack);
            float newValue = GUILayout.HorizontalSlider(currentValue, 0.5f, 5f);
            GUILayout.Label($"Attack Radius: {newValue:F2}");
            
            if (newValue != currentValue)
                attackRadiusField.SetValue(attack, newValue);
        }
        
        var specialRadiusField = attack.GetType().GetField("specialAttackRadius", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
        if (specialRadiusField != null)
        {
            float currentValue = (float)specialRadiusField.GetValue(attack);
            float newValue = GUILayout.HorizontalSlider(currentValue, 1f, 8f);
            GUILayout.Label($"Special Attack Radius: {newValue:F2}");
            
            if (newValue != currentValue)
                specialRadiusField.SetValue(attack, newValue);
        }
    }
}