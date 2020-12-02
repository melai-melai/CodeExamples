using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Card : MonoBehaviour, IPointerClickHandler
{
    public CardData data;
    public SpriteRenderer spriteRenderer;
    public Animator anim;

    [SerializeField]
    private CardBody cardBody;

    private void Start()
    {
        
    }

    public void Open()
    {
        if (!Matcher.Instance.isFull)
        {
            anim.SetTrigger("Open");
            Matcher.Instance.AddCard(this);
        }
    }

    public void Close()
    {
        anim.SetTrigger("Close");
    }

    public IEnumerator WaitUntilClose(float time)
    {
        yield return new WaitForSecondsRealtime(time);
        Close();
    }

    public void SetInfoFromData()
    {
        spriteRenderer.sprite = data.sprite;
    }

    /// <summary>
    /// Detect if a click occurs
    /// </summary>
    /// <param name="pointerEventData"></param>
    public void OnPointerClick(PointerEventData pointerEventData)
    {
        if (!Matcher.Instance.IsStopped && !cardBody.IsOpen)
        {
            Open();
        }
    }
}
