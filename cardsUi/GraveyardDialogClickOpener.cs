using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Click handler that opens/toggles a GraveyardDialog.
/// Attach to any UI element or 3D object with a collider.
/// </summary>
public class GraveyardDialogClickOpener : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private GraveyardDialog targetDialog;
    [SerializeField] private bool toggleDialog = true;

    private void Awake()
    {
        if (targetDialog == null)
        {
            targetDialog = FindObjectOfType<GraveyardDialog>();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (targetDialog == null)
        {
            return;
        }

        if (toggleDialog)
        {
            targetDialog.Toggle();
        }
        else
        {
            targetDialog.Show();
        }
    }
}
