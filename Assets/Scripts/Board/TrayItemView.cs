using UnityEngine;

// Attached to an item's view GameObject when the item is placed in the tray.
// Used by raycasting to identify tray items.
public class TrayItemView : MonoBehaviour
{
    public Item Item { get; set; }
}
