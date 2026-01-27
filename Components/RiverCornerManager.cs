using PharloomsGlory.Modifiers;
using UnityEngine;

namespace PharloomsGlory.Components;

public class RiverCornerManager : MonoBehaviour
{
    public string spriteSuffix;

    private void LateUpdate()
    {
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
            SpriteRendererModifier.instance.ModifySpriteRenderer(sr, GlobalEnums.MapZone.CORAL_CAVERNS, $"{Util.GetSpriteName(sr.sprite)}-{spriteSuffix}");
    }
}
