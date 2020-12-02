using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Matcher : MonoSingleton<Matcher>
{
    private int cardsForMatch = 2;
    private List<Card> cards = new List<Card>();
    private float timeUntilClose = 3f;
    private float timer = -1.0f;
    private int sceneCardsCount;                         // in the scene

    private float delayNotTheSame = 0.7f;



    public bool isFull
    {
        get
        {
            return cards.Count == cardsForMatch;
        }
    }
    public bool IsStopped { get; set; }

    [Header("Audio")]
    public AudioClip audioWin;
    public AudioClip audioFail;
    private AudioSource audioSource;
    private SpawnManager spawnManager;

    public delegate void OnFinishing(bool isWinner);
    public static event OnFinishing onFinishing;                        // the event that we call when the cards.count == 0

    private void Start()
    {
        audioSource = gameObject.GetComponent<AudioSource>();
        spawnManager = gameObject.GetComponent<SpawnManager>();

        Card[] sceneCards = gameObject.GetComponentsInChildren<Card>(true);
        sceneCardsCount = sceneCards.Length;
    }

    private void Update()
    {
        if (timer > 0)
        {
            timer -= Time.deltaTime;
            if (timer < 0)
            {
                CloseCards(0);
                ResetMatcher();
            }
        }
    }

    /// <summary>
    /// Add card to cards for matching (all cards have the same name)
    /// </summary>
    /// <param name="card"></param>
    public void AddCard(Card card)
    {
        if (cards.Count == 0)
        {
            cards.Add(card);
            if (timer < 0)
            {
                timer = timeUntilClose;
            }
        } else if (cards.Count < cardsForMatch)
        {
            string nameOfLast = cards[cards.Count - 1].data.nameCard;
            string nameOfNew = card.data.nameCard;
 
            if (nameOfLast == nameOfNew) // the same
            {
                cards.Add(card);
                StartCoroutine("Match");
            }
            else // not the same
            {
                StartCoroutine(card.WaitUntilClose(delayNotTheSame));
                CloseCards(delayNotTheSame);
                ResetMatcher();
                StartCoroutine("PlayFailAudioClip");
            }
        }                
    }

    public IEnumerator Match()
    {
        if (isFull)
        {
            CardData dataOfCardFromMatching = cards[0].data;
            sceneCardsCount -= cards.Count;
            timer = -1f; // need for right close the cards

            yield return new WaitForSeconds(1f);
            audioSource.PlayOneShot(audioWin);
            HideMatchedCards();
            ResetMatcher();

            if (sceneCardsCount == 0 && PlayerData.Instance.tutorialDone)
            {
                if (dataOfCardFromMatching.nameCard == spawnManager.DataOfLastCard.nameCard)
                {
                    onFinishing?.Invoke(true);
                }
                onFinishing?.Invoke(false);
            }
        }        
    }

    public void ResetMatcher()
    {
        cards.Clear();
        timer = -1f;
    }

    public void HideMatchedCards()
    {
        foreach (var card in cards)
        {
            card.gameObject.SetActive(false);
        }
    }

    public void CloseCards(float delay = 0.5f)
    {
        foreach (var card in cards)
        {
            StartCoroutine(card.WaitUntilClose(delay));
        }
    }

    private IEnumerator PlayFailAudioClip()
    {
        yield return new WaitForSeconds(1f);
        audioSource.PlayOneShot(audioFail);
    }
}
