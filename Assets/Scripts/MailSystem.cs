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

    public static MailSystem Instance { get; private set; }

    public MushroomQuest CurrentQuest => currentQuest;


    public event Action<MushroomQuest> OnNewQuestReceived;
    public event Action<MushroomQuest> OnQuestCompleted;

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
        // Start with a simple test quest
        GenerateTestQuest();
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
        FindObjectOfType<MushroomListUI>().Refresh();

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
