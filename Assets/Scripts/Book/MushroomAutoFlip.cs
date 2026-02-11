using UnityEngine;

[RequireComponent(typeof(MushroomResearchBook))]
public class MushroomBookAutoFlip : MonoBehaviour
{
    [Header("Page Turn Settings")]
    public float pageFlipDuration = 0.3f;
    public AudioClip pageFlipSound;

    private MushroomResearchBook researchBook;
    private AudioSource audioSource;
    private bool isFlipping = false;

    void Start()
    {
        researchBook = GetComponent<MushroomResearchBook>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null && pageFlipSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.volume = 0.5f;
        }
    }

    public void FlipToNextPage()
    {
        if (isFlipping || researchBook == null) return;

        StartCoroutine(AnimatePageFlip(() => researchBook.NextPage()));
    }

    public void FlipToPreviousPage()
    {
        if (isFlipping || researchBook == null) return;

        StartCoroutine(AnimatePageFlip(() => researchBook.PreviousPage()));
    }

    System.Collections.IEnumerator AnimatePageFlip(System.Action flipAction)
    {
        isFlipping = true;

        // Play sound effect
        if (audioSource != null && pageFlipSound != null)
        {
            audioSource.PlayOneShot(pageFlipSound);
        }

        // Optional: Add a slight delay for page flip feeling
        yield return new WaitForSeconds(pageFlipDuration * 0.5f);

        // Actually change the page
        flipAction?.Invoke();

        // Complete the flip animation
        yield return new WaitForSeconds(pageFlipDuration * 0.5f);

        isFlipping = false;

        Debug.Log("📄 Page flipped!");
    }

    // Quick access methods that can be called from UI buttons
    public void QuickNextPage()
    {
        if (!isFlipping && researchBook != null)
            researchBook.NextPage();
    }

    public void QuickPreviousPage()
    {
        if (!isFlipping && researchBook != null)
            researchBook.PreviousPage();
    }
}
