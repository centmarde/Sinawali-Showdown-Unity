using UnityEngine;

/// <summary>
/// Simple button actions for the Card Inspector UI.
/// - Cancel: hides the inspector panel.
/// - Confirm: logs a message.
/// Attach this to the Confirm/Cancel UI Buttons and wire their OnClick to the methods below,
/// or let CardInspectorAutoCreate wire them automatically.
/// </summary>
public class CardInspectorButtonActions : MonoBehaviour
{
    [Header("Optional")]
    [SerializeField] private GameObject inspectorPanelOverride;

    /// <summary>
    /// Called by the Cancel button.
    /// </summary>
    public void Cancel()
    {
        // Preferred: use CardInspector's centralized hide logic (animation + shared panel).
        CardInspector.HideInspectorStatic();

        // Fallback: if the static panel reference isn't set for some reason,
        // try to disable a provided override.
        if (inspectorPanelOverride != null)
        {
            inspectorPanelOverride.SetActive(false);
            return;
        }

        // Last fallback: try to find by common names.
        GameObject panel = GameObject.Find("CardInspectorPanel")
                        ?? GameObject.Find("InspectorPanel")
                        ?? GameObject.Find("CardDetailPanel");

        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    /// <summary>
    /// Called by the Confirm button.
    /// </summary>
    public void Confirm()
    {
        Debug.Log("has been clicked");
    }

    /// <summary>
    /// Used by auto-wiring scripts to inject the panel reference.
    /// </summary>
    public void SetInspectorPanelOverride(GameObject panel)
    {
        inspectorPanelOverride = panel;
    }
}
