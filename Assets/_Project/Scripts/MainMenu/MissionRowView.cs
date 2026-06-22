using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MissionRowView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image background;
    public Image accentBar;
    public TMP_Text titleText;
    public TMP_Text chevron;

    public Color rowNormal = new Color(0f, 0f, 0f, 0f);
    public Color rowHover = new Color(1f, 1f, 1f, 0.06f);
    public Color rowSelected = new Color(1f, 1f, 1f, 0.10f);
    public Color rowLocked = new Color(0f, 0f, 0f, 0f);

    public Color accentHidden = new Color(1f, 0.85f, 0.20f, 0f);
    public Color accentHover = new Color(1f, 0.85f, 0.20f, 0.55f);
    public Color accentSelected = new Color(1f, 0.85f, 0.20f, 1f);

    public Color titleNormal = new Color(0.92f, 0.94f, 0.97f, 1f);
    public Color titleHover = new Color(1f, 0.85f, 0.20f, 1f);
    public Color titleSelected = new Color(1f, 0.85f, 0.20f, 1f);
    public Color titleLocked = new Color(0.45f, 0.48f, 0.55f, 1f);

    public Color chevronIdle = new Color(1f, 1f, 1f, 0.20f);
    public Color chevronActive = new Color(1f, 0.85f, 0.20f, 1f);
    public Color chevronLocked = new Color(1f, 1f, 1f, 0.08f);

    private bool _hovering;
    private bool _selected;
    private bool _locked;

    public bool IsSelected => _selected;

    public void SetLocked(bool locked)
    {
        _locked = locked;
        Refresh();
    }

    public void SetSelected(bool selected)
    {
        _selected = selected;
        Refresh();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _hovering = true;
        Refresh();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hovering = false;
        Refresh();
    }

    private void OnEnable() => Refresh();

    private void Refresh()
    {
        if (background != null)
        {
            if (_selected) background.color = rowSelected;
            else if (_locked) background.color = rowLocked;
            else if (_hovering) background.color = rowHover;
            else background.color = rowNormal;
        }

        if (accentBar != null)
        {
            if (_selected) accentBar.color = accentSelected;
            else if (_locked) accentBar.color = accentHidden;
            else if (_hovering) accentBar.color = accentHover;
            else accentBar.color = accentHidden;
        }

        if (titleText != null)
        {
            if (_locked) titleText.color = titleLocked;
            else if (_selected) titleText.color = titleSelected;
            else if (_hovering) titleText.color = titleHover;
            else titleText.color = titleNormal;
        }

        if (chevron != null)
        {
            if (_selected || (!_locked && _hovering)) chevron.color = chevronActive;
            else if (_locked) chevron.color = chevronLocked;
            else chevron.color = chevronIdle;
        }
    }
}
