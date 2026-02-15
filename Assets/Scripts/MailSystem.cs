using UnityEngine;
using System.Collections.Generic;
using System;

[Serializable]
public class MushroomQuest
{
    public string questId;
    public string questTitle;
    public List<MushroomRequest> requestedMushrooms;
    public DateTime deadline;
    public bool isCompleted;
}

[Serializable]
public class MushroomRequest
{
    public string mushroomType;
    public int quantity;
    public int collectedQuantity;
}

public class MailSystem : MonoBehaviour
{
    [SerializeField] private List<MushroomQuest> activeQuests = new List<MushroomQuest>();
    [SerializeField] private MushroomQuest currentQuest;

    [Header("UI Settings")]
    public GameObject mailUIPanel; // Add reference to mail UI panel
    public KeyCode toggleMailKey = KeyCode.M; // Use M key for Mail
    private bool isMailOpen = false;

    public static MailSystem Instance { get; private set; }

    public MushroomQuest CurrentQuest => currentQuest;

    public event Action<MushroomQuest> OnNewQuestReceived;
    public event Action<MushroomQuest> OnQuestCompleted;
    public event Action<string> OnMushroomCollected;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Hide mail UI by default
        if (mailUIPanel != null)
            mailUIPanel.SetActive(false);

        // Start with a simple test quest but don't show UI
        GenerateTestQuest();
    }

    void Update()
    {
        // Handle mail UI toggle
        if (Input.GetKeyDown(toggleMailKey))
        {
            ToggleMailUI();
        }
    }

    void ToggleMailUI()
    {
        isMailOpen = !isMailOpen;

        if (mailUIPanel != null)
            mailUIPanel.SetActive(isMailOpen);

        Debug.Log(isMailOpen ? "📬 Mail opened!" : "📬 Mail closed!");
    }

    void GenerateTestQuest()
    {
        var quest = new MushroomQuest
        {
            questId = "QUEST_001",
            questTitle = "Morning Foraging Order",
            requestedMushrooms = new List<MushroomRequest>
            {
                new MushroomRequest { mushroomType = "Chanterelle", quantity = 3, collectedQuantity = 0 },
                new MushroomRequest { mushroomType = "Shiitake", quantity = 2, collectedQuantity = 0 }
            },
            deadline = DateTime.Now.AddMinutes(10),
            isCompleted = false
        };

        ReceiveNewQuest(quest);
    }

    public void ReceiveNewQuest(MushroomQuest quest)
    {
        activeQuests.Add(quest);
        currentQuest = quest;
        OnNewQuestReceived?.Invoke(quest);

        // Don't automatically show UI, just refresh if it's open
        if (isMailOpen)
        {
            FindObjectOfType<MushroomListUI>()?.Refresh();
        }

        Debug.Log($"New quest received: {quest.questTitle}");
    }

    public void UpdateMushroomProgress(string mushroomType, int amount)
    {
        if (currentQuest == null) return;

        var request = currentQuest.requestedMushrooms.Find(r => r.mushroomType == mushroomType);
        if (request != null)
        {
            request.collectedQuantity = Mathf.Min(request.collectedQuantity + amount, request.quantity);
            CheckQuestCompletion();
        }

        // Fire event for research book
        OnMushroomCollected?.Invoke(mushroomType);
    }

    void CheckQuestCompletion()
    {
        if (currentQuest == null) return;

        bool allCompleted = currentQuest.requestedMushrooms.TrueForAll(r => r.collectedQuantity >= r.quantity);

        if (allCompleted && !currentQuest.isCompleted)
        {
            currentQuest.isCompleted = true;
            OnQuestCompleted?.Invoke(currentQuest);
            Debug.Log($"Quest completed: {currentQuest.questTitle}");
        }
    }
}

