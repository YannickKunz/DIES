using UnityEngine;
using UnityEngine.UI;                // for UI.Text
using TMPro;                     // if you use TextMeshPro

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;
    public bool DialogueActive { get; private set; }
    
    [Header("UI References")]
    public GameObject dialogPanel;      // the Panel GameObject
    // public Text dialogText;             // UI.Text component
    public TMP_Text dialogText;      // if using TextMeshPro
    public string continueKey = "f";    // key to advance

    private string[] lines;             // current dialog lines
    private int currentLineIndex;


    void Awake()
    {
        // singleton for easy access
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        dialogPanel.SetActive(false);

    }

    void Update()
    {
        // while dialog open, listen for continue key
        if (dialogPanel.activeSelf && Input.GetKeyDown(continueKey))
        {
            NextLine();
        }
    }

    /// <summary>
    /// Start a new conversation given an array of lines.
    /// </summary>
    public void StartDialogue(string[] dialogueLines)
{
    lines = dialogueLines;
    currentLineIndex = 0;
    dialogPanel.SetActive(true);

    // disable player
    //FindFirstObjectByType<PlayerController>().enabled = false;
    //FindFirstObjectByType<PlayerMovement>().enabled = false;
    //FindFirstObjectByType<PlayerMovement>().runSpeed = 0f;
    //FindFirstObjectByType<PlayerController>().runSpeed = 0f;
    //FindFirstObjectByType<AudioSource>().enabled = false;
    //FindFirstObjectByType<Animator>().enabled = false;
    // Freeze x and y axis of the player
    //FindFirstObjectByType<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
    ShowLine();
}

    /// <summary>
    /// Display the current line.
    /// </summary>
    private void ShowLine()
    {
        if (currentLineIndex < lines.Length)
            dialogText.text = lines[currentLineIndex];
    }

    /// <summary>
    /// Advance to the next line or end.
    /// </summary>
    private void NextLine()
    {
        currentLineIndex++;
        if (currentLineIndex >= lines.Length)
        {
            EndDialogue();
        }
        else
        {
            ShowLine();
        }
    }

    private void EndDialogue()
    {
        dialogPanel.SetActive(false);

        //FindFirstObjectByType<PlayerController>().enabled = true;
        //FindFirstObjectByType<PlayerMovement>().enabled = true;
        //FindFirstObjectByType<PlayerController>().runSpeed = 10f;
        //FindFirstObjectByType<PlayerMovement>().runSpeed = 10f;
        FindFirstObjectByType<AudioSource>().enabled = true;
        FindFirstObjectByType<Animator>().enabled = true;
        // Unfreeze x and y axis of the player
        FindFirstObjectByType<Rigidbody2D>().constraints = RigidbodyConstraints2D.None;
        FindFirstObjectByType<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeRotation;



}
}
