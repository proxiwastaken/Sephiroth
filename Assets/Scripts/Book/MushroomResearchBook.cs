using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class MushroomResearchEntry
{
    public string mushroomType;
    public string displayName;
    public string scientificName;
    [TextArea(3, 5)]
    public string description;
    [TextArea(2, 3)]
    public string habitat;
    [TextArea(2, 3)]
    public string cookingNotes;
    public Sprite illustration;
    public bool isDiscovered = false;
    public int timesCollected = 0;

    // Progressive unlock thresholds
    public int nameUnlockCount = 1;
    public int habitatUnlockCount = 3;
    public int cookingUnlockCount = 5;
}

public class MushroomResearchBook : MonoBehaviour
{
    [Header("3D Book Model")]
    public GameObject bookModel; // The 3D book mesh
    public Transform bookClosedPosition; // Where book sits in world
    public Transform bookOpenPosition; // Where book moves when opened (in front of camera)

    [Header("UI Overlay (Screen Space)")]
    public Canvas bookUICanvas; // Screen space overlay canvas
    public GameObject bookUIPanel;

    [Header("Book UI Elements")]
    public TextMeshProUGUI leftPageTitle;
    public TextMeshProUGUI leftPageContent;
    public Image leftPageImage;
    public TextMeshProUGUI rightPageTitle;
    public TextMeshProUGUI rightPageContent;
    public Image rightPageImage;

    [Header("Navigation")]
    public Button nextPageButton;
    public Button previousPageButton;
    public Button closeBookButton;
    public TextMeshProUGUI pageNumberText;

    [Header("Pickup Interaction")]
    public float pickupRange = 2f;
    public KeyCode interactKey = KeyCode.E;
    public GameObject interactionPrompt; // Small world space "Press E" text
    private Book3DInteraction bookInteraction;

    [Header("Research Data")]
    public MushroomResearchEntry[] mushroomEntries;

    // Runtime state
    private List<MushroomResearchEntry> discoveredMushrooms = new List<MushroomResearchEntry>();
    private bool isBookOpen = false;
    private bool isPlayerInRange = false;
    private int currentPagePair = 0; // Which pair of pages we're viewing
    private Transform player;
    private Camera playerCamera;

    // Page content
    private List<BookPagePair> bookPages = new List<BookPagePair>();

    [System.Serializable]
    public class BookPagePair
    {
        public string leftTitle;
        public string leftContent;
        public Sprite leftImage;
        public string rightTitle;
        public string rightContent;
        public Sprite rightImage;
    }

    void Start()
    {
        bookInteraction = GetComponent<Book3DInteraction>();


        InitializeBook();
        SetupEventListeners();
        LoadProgress();

        // Subscribe to mushroom collection events
        if (MailSystem.Instance != null)
        {
            MailSystem.Instance.OnMushroomCollected += OnMushroomCollected;
        }

        // Find player and camera
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerCamera = Camera.main;
        }
    }

    void InitializeBook()
    {
        // Start with book closed and UI hidden
        if (bookModel != null && bookClosedPosition != null)
            bookModel.transform.position = bookClosedPosition.position;

        if (bookUICanvas != null)
            bookUICanvas.gameObject.SetActive(false);

        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);

        GenerateBookPages();
    }

    void SetupEventListeners()
    {
        if (nextPageButton != null)
            nextPageButton.onClick.AddListener(NextPage);

        if (previousPageButton != null)
            previousPageButton.onClick.AddListener(PreviousPage);

        if (closeBookButton != null)
            closeBookButton.onClick.AddListener(CloseBook);
    }

    void Update()
    {
        CheckPlayerProximity();
        HandleInput();
    }

    void CheckPlayerProximity()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        bool wasInRange = isPlayerInRange;
        isPlayerInRange = distance <= pickupRange && !isBookOpen;

        if (isPlayerInRange != wasInRange)
        {
            if (interactionPrompt != null)
                interactionPrompt.SetActive(isPlayerInRange);
        }
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(interactKey))
        {
            if (isPlayerInRange && !isBookOpen)
            {
                OpenBook();
            }
            else if (isBookOpen)
            {
                CloseBook();
            }
        }

        // Keyboard navigation when book is open
        if (isBookOpen)
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
                PreviousPage();
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
                NextPage();
        }
    }

    public void OpenBook()
    {
        if (isBookOpen) return;

        isBookOpen = true;

        // Notify interaction script
        if (bookInteraction != null)
            bookInteraction.OnBookStateChanged(true);


        // Hide interaction prompt
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);

        // Move 3D book to open position (in front of camera)
        if (bookModel != null && bookOpenPosition != null)
        {
            StartCoroutine(AnimateBookToPosition(bookOpenPosition.position, bookOpenPosition.rotation, 0.8f));
        }

        // Show UI overlay
        if (bookUICanvas != null)
        {
            bookUICanvas.gameObject.SetActive(true);
        }

        // Start from first page
        currentPagePair = 0;
        UpdatePageDisplay();

        // Lock player movement (optional)
        var playerController = player?.GetComponent<OverheadController>();
        if (playerController != null)
            playerController.enabled = false;

        Debug.Log("📖 Research book opened!");
    }

    public void CloseBook()
    {
        if (!isBookOpen) return;

        isBookOpen = false;

        // Notify interaction script
        if (bookInteraction != null)
            bookInteraction.OnBookStateChanged(false);


        // Hide UI
        if (bookUICanvas != null)
            bookUICanvas.gameObject.SetActive(false);

        // Move 3D book back to world position
        if (bookModel != null && bookClosedPosition != null)
        {
            StartCoroutine(AnimateBookToPosition(bookClosedPosition.position, bookClosedPosition.rotation, 0.8f));
        }

        // Unlock player movement
        var playerController = player?.GetComponent<OverheadController>();
        if (playerController != null)
            playerController.enabled = true;

        Debug.Log("📖 Research book closed!");
    }

    System.Collections.IEnumerator AnimateBookToPosition(Vector3 targetPos, Quaternion targetRot, float duration)
    {
        if (bookModel == null) yield break;

        Vector3 startPos = bookModel.transform.position;
        Quaternion startRot = bookModel.transform.rotation;
        float elapsedTime = 0;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            t = Mathf.SmoothStep(0, 1, t); // Smooth easing

            bookModel.transform.position = Vector3.Lerp(startPos, targetPos, t);
            bookModel.transform.rotation = Quaternion.Lerp(startRot, targetRot, t);

            yield return null;
        }

        bookModel.transform.position = targetPos;
        bookModel.transform.rotation = targetRot;
    }

    public void OnMushroomCollected(string mushroomType)
    {
        var entry = mushroomEntries.FirstOrDefault(e => e.mushroomType == mushroomType);
        if (entry != null)
        {
            entry.timesCollected++;

            bool wasDiscovered = entry.isDiscovered;
            entry.isDiscovered = true;

            if (!wasDiscovered)
            {
                discoveredMushrooms.Add(entry);
                ShowDiscoveryNotification(entry);
            }

            GenerateBookPages();
            SaveProgress();
        }
    }

    void GenerateBookPages()
    {
        bookPages.Clear();

        // Cover page
        bookPages.Add(new BookPagePair
        {
            leftTitle = "Mushroom Research Journal",
            leftContent = "A Field Guide to Fungal Discoveries\n\nBy: Frog Naturalist\n\nDiscovered Species: " + discoveredMushrooms.Count,
            leftImage = null,
            rightTitle = "Table of Contents",
            rightContent = GenerateTableOfContents(),
            rightImage = null
        });

        // Mushroom pages (one page pair per mushroom)
        foreach (var entry in discoveredMushrooms.OrderBy(e => e.displayName))
        {
            bookPages.Add(CreateMushroomPagePair(entry));
        }
    }

    [ContextMenu("Add Test Mushrooms")]
    void AddTestMushrooms()
    {
        // For testing - manually add some mushrooms as discovered
        foreach (var entry in mushroomEntries)
        {
            if (entry.isDiscovered && !discoveredMushrooms.Contains(entry))
            {
                entry.timesCollected = Mathf.Max(1, entry.timesCollected);
                discoveredMushrooms.Add(entry);
            }
        }

        GenerateBookPages();

        if (isBookOpen)
            UpdatePageDisplay();

        Debug.Log($"Test: Added {discoveredMushrooms.Count} mushrooms to book");
    }

    string GenerateTableOfContents()
    {
        if (discoveredMushrooms.Count == 0)
            return "No species discovered yet.\n\nExplore the world to find mushrooms!";

        string toc = "";
        int pageNum = 2; // Start after cover

        foreach (var entry in discoveredMushrooms.OrderBy(e => e.displayName))
        {
            toc += $"• {entry.displayName} ........ {pageNum}\n";
            if (entry.timesCollected > 1)
                toc += $"  (Collected ×{entry.timesCollected})\n";
            pageNum++;
        }

        return toc;
    }

    BookPagePair CreateMushroomPagePair(MushroomResearchEntry entry)
    {
        // Left page - Basic info and illustration
        string leftContent = "";
        if (entry.timesCollected >= entry.nameUnlockCount)
        {
            leftContent += $"<b>Scientific Name:</b>\n<i>{entry.scientificName}</i>\n\n";
            leftContent += $"<b>Specimens Collected:</b> {entry.timesCollected}\n\n";
            leftContent += $"<b>Description:</b>\n{entry.description}";
        }
        else
        {
            leftContent = "Collect this mushroom to unlock research data.";
        }

        // Right page - Habitat and cooking info
        string rightContent = "";
        if (entry.timesCollected >= entry.habitatUnlockCount)
        {
            rightContent += $"<b>Habitat:</b>\n{entry.habitat}\n\n";
        }
        else if (entry.timesCollected >= entry.nameUnlockCount)
        {
            rightContent += "<b>Habitat:</b>\n<i>[Collect more specimens]</i>\n\n";
        }

        if (entry.timesCollected >= entry.cookingUnlockCount)
        {
            rightContent += $"<b>Culinary Notes:</b>\n{entry.cookingNotes}";
        }
        else if (entry.timesCollected >= entry.nameUnlockCount)
        {
            rightContent += "<b>Culinary Notes:</b>\n<i>[Collect more specimens]</i>";
        }

        if (rightContent == "")
        {
            rightContent = "Additional research data will be unlocked as you collect more specimens of this species.";
        }

        return new BookPagePair
        {
            leftTitle = entry.isDiscovered ? entry.displayName : "Unknown Species",
            leftContent = leftContent,
            leftImage = entry.isDiscovered ? entry.illustration : null,
            rightTitle = "Research Notes",
            rightContent = rightContent,
            rightImage = null
        };
    }

    public void NextPage()
    {
        if (currentPagePair < bookPages.Count - 1)
        {
            currentPagePair++;
            UpdatePageDisplay();
        }
    }

    public void PreviousPage()
    {
        if (currentPagePair > 0)
        {
            currentPagePair--;
            UpdatePageDisplay();
        }
    }

    void UpdatePageDisplay()
    {
        if (currentPagePair >= bookPages.Count) return;

        var pagePair = bookPages[currentPagePair];

        // Left page
        if (leftPageTitle != null)
            leftPageTitle.text = pagePair.leftTitle;
        if (leftPageContent != null)
            leftPageContent.text = pagePair.leftContent;
        if (leftPageImage != null)
        {
            leftPageImage.sprite = pagePair.leftImage;
            leftPageImage.gameObject.SetActive(pagePair.leftImage != null);
        }

        // Right page
        if (rightPageTitle != null)
            rightPageTitle.text = pagePair.rightTitle;
        if (rightPageContent != null)
            rightPageContent.text = pagePair.rightContent;
        if (rightPageImage != null)
        {
            rightPageImage.sprite = pagePair.rightImage;
            rightPageImage.gameObject.SetActive(pagePair.rightImage != null);
        }

        // Page number
        if (pageNumberText != null)
            pageNumberText.text = $"Page {(currentPagePair * 2) + 1}-{(currentPagePair * 2) + 2}";

        // Update navigation buttons
        if (nextPageButton != null)
            nextPageButton.interactable = currentPagePair < bookPages.Count - 1;
        if (previousPageButton != null)
            previousPageButton.interactable = currentPagePair > 0;
    }

    void ShowDiscoveryNotification(MushroomResearchEntry entry)
    {
        Debug.Log($"📚 New species discovered: {entry.displayName}");
        // You could add a popup notification here
    }

    void SaveProgress()
    {
        foreach (var entry in mushroomEntries)
        {
            PlayerPrefs.SetInt($"Mushroom_{entry.mushroomType}_Discovered", entry.isDiscovered ? 1 : 0);
            PlayerPrefs.SetInt($"Mushroom_{entry.mushroomType}_Count", entry.timesCollected);
        }
        PlayerPrefs.Save();
    }

    void LoadProgress()
    {
        discoveredMushrooms.Clear();

        foreach (var entry in mushroomEntries)
        {
            // Load from PlayerPrefs
            bool savedDiscovered = PlayerPrefs.GetInt($"Mushroom_{entry.mushroomType}_Discovered", 0) == 1;
            int savedCount = PlayerPrefs.GetInt($"Mushroom_{entry.mushroomType}_Count", 0);

            // Use inspector values as fallback/override for testing
            if (!savedDiscovered && entry.isDiscovered)
            {
                // Inspector override - mushroom is marked as discovered for testing
                Debug.Log($"Using inspector override for {entry.displayName}");
                // Don't overwrite inspector values
            }
            else
            {
                // Use saved values
                entry.isDiscovered = savedDiscovered;
                entry.timesCollected = savedCount;
            }

            // Add to discovered list if marked as discovered (either saved or inspector)
            if (entry.isDiscovered)
                discoveredMushrooms.Add(entry);
        }

        GenerateBookPages();

        Debug.Log($"Loaded {discoveredMushrooms.Count} discovered mushrooms");
        foreach (var mushroom in discoveredMushrooms)
        {
            Debug.Log($"- {mushroom.displayName} (×{mushroom.timesCollected})");
        }
    }


    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }

    void OnDestroy()
    {
        if (MailSystem.Instance != null)
            MailSystem.Instance.OnMushroomCollected -= OnMushroomCollected;
    }
}
