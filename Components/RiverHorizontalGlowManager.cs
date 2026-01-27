using PharloomsGlory.Modifiers;
using UnityEngine;

namespace PharloomsGlory.Components;

public class RiverHorizontalGlowManager : MonoBehaviour
{
    private void LateUpdate()
    {
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
        {
            SpriteRendererModifier.instance.ModifySpriteRenderer(sr);
            sr.color = Color.white;
        }
    }
}
