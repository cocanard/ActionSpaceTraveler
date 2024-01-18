using UnityEngine;
using UnityEngine.UI;

public class FilteredButton : Button
{
    public override Selectable FindSelectableOnUp()
    {
        Selectable value = base.FindSelectableOnUp();
        bool isValid = value != null && value != UIManagement.UI.transform.Find("AdButton").GetComponent<Button>() && value != UIManagement.UI.transform.Find("QuitButton").GetComponent<Button>();
        return isValid ? base.FindSelectableOnUp() : null;
    }

    public override Selectable FindSelectableOnDown()
    {
        Selectable value = base.FindSelectableOnDown();
        bool isValid = value != null && value != UIManagement.UI.transform.Find("AdButton").GetComponent<Button>() && value != UIManagement.UI.transform.Find("QuitButton").GetComponent<Button>();
        return isValid ? value : null;
    }

    public override Selectable FindSelectableOnLeft()
    {
        Selectable value = base.FindSelectableOnLeft();
        bool isValid = value != null && value != UIManagement.UI.transform.Find("AdButton").GetComponent<Button>() && value != UIManagement.UI.transform.Find("QuitButton").GetComponent<Button>();
        return isValid ? value : null;
    }

    public override Selectable FindSelectableOnRight()
    {
        Selectable value = base.FindSelectableOnRight();
        bool isValid = value != null && value != UIManagement.UI.transform.Find("AdButton").GetComponent<Button>() && value != UIManagement.UI.transform.Find("QuitButton").GetComponent<Button>();
        return isValid ? value : null;
    }
}
