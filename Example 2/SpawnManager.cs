using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class SpawnManager : MonoBehaviour
{
    public Image lastCardImage;

    public List<CardData> cardData;
    private List<Card> cards;
    public CardData DataOfLastCard { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        cards = gameObject.GetComponentsInChildren<Card>(true).ToList();
        SpawnCards();

        DataOfLastCard = cards[Random.Range(0, cards.Count - 1)].data;
        SetImageForLastCard();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// Sort and set cardData for pair of card
    /// </summary>
    private void SpawnCards()
    {
        RandomSort(cards); 
        for (int i = 0, y = 0; i < cards.Count && y < cardData.Count; i += 2, y += 1)
        {
            var card = cards[i];
            var card2 = cards[i + 1];

            card.data = cardData[y];
            card2.data = cardData[y];

            card.SetInfoFromData();
            card2.SetInfoFromData();

            if (y == cardData.Count - 1)
            {
                y = -1; // after step y will be y=0
            }
        }
    }

    /// <summary>
    /// Sort cards (Fisher-Yates Shuffle)
    /// </summary>
    private void RandomSort<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n -= 1;
            int i = Random.Range(0, n);
            var temp = list[i];
            list[i] = list[n];
            list[n] = temp;
        }
    }

    private void SetImageForLastCard()
    {
        lastCardImage.sprite = DataOfLastCard.sprite;
    }
}
