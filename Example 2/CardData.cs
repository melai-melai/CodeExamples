using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Card Data", menuName = "Card Data")]
public class CardData : ScriptableObject
{
    public string nameCard;
    public Sprite sprite;
}
