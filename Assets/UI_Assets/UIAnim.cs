using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class UIAnim : MonoBehaviour
{
    public void tweenScale(GameObject i, Vector2 baseScale, Vector2 newScale, float t)
    {
        i.transform.localScale = baseScale;
        i.transform.LeanScale(newScale, t);
    }

    public void Hovering(GameObject i)
    {
        tweenScale(i, i.transform.localScale, new Vector2(1.05f, 1.05f), 0.1f);
    }
    public void UnHovering(GameObject i)
    {
        tweenScale(i, i.transform.localScale, new Vector2(1, 1), 0.1f);
    }

    public void UnderlineText(TMPro.TMP_Text component)
    {
        component.fontStyle = TMPro.FontStyles.Underline;
    }
    public void NormalText(TMPro.TMP_Text component)
    {
        component.fontStyle = TMPro.FontStyles.Normal;
    }
}