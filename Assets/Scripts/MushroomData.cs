using UnityEngine;

[CreateAssetMenu(fileName = "New Mushroom Data", menuName = "Mushroom Foraging/Mushroom Data")]
public class MushroomData : ScriptableObject
{
    [Header("Basic Info")]
    public string mushroomType = "Chanterelle";
    public string displayName = "Chanterelle Mushroom";
    public MushroomRarity rarity = MushroomRarity.Common;

    [Header("Detection")]
    public float detectionRange = 3f;

    [Header("Behavior")]
    public GameObject personalityPrefab;
    public float hideSpeed = 2f;
    public float hideDepth = 0.5f;
    public float fleeSpeed = 5f;

    [Header("Visual")]
    public GameObject mushroomPrefab;
    public GameObject collectionEffect;

    [Header("Audio")]
    public AudioClip[] rustleSounds;
    public AudioClip collectionSound;
}

public enum MushroomRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

