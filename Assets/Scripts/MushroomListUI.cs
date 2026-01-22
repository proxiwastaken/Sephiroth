using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class MushroomListUI : MonoBehaviour
{
    public GameObject mushroomEntryPrefab;
    public Transform listContainer;       
    public TextMeshProUGUI questTitleText;


    private List<GameObject> spawnedEntries = new List<GameObject>();

    void OnEnable()
    {
        if (MailSystem.Instance != null)
        {
            MailSystem.Instance.OnNewQuestReceived += UpdateList;
            MailSystem.Instance.OnQuestCompleted += UpdateList;
            UpdateList(MailSystem.Instance.CurrentQuest);
        }
    }

    void OnDisable()
    {
        if (MailSystem.Instance != null)
        {
            MailSystem.Instance.OnNewQuestReceived -= UpdateList;
            MailSystem.Instance.OnQuestCompleted -= UpdateList;
        }
    }

    public void UpdateList(MushroomQuest quest)
    {
        // Update quest title
        if (questTitleText != null)
            questTitleText.text = quest != null ? quest.questTitle : "";

        // Clear old entries
        foreach (var entry in spawnedEntries)
            Destroy(entry);
        spawnedEntries.Clear();

        if (quest == null) return;

        foreach (var req in quest.requestedMushrooms)
        {
            GameObject entry = Instantiate(mushroomEntryPrefab, listContainer);
            var text = entry.GetComponent<TextMeshProUGUI>();
            if (text != null)
                text.text = $"{req.mushroomType}: {req.collectedQuantity}/{req.quantity}";
            spawnedEntries.Add(entry);
        }
    }

    public void Refresh()
    {
        UpdateList(MailSystem.Instance.CurrentQuest);
    }
}
