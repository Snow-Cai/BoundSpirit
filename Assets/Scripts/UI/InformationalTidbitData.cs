using UnityEngine;

[CreateAssetMenu(fileName = "InformationalTidbit", menuName = "Bound Spirit/UI/Informational Tidbit")]
public class InformationalTidbitData : ScriptableObject
{
    [SerializeField] private string title = "INFO TIDBIT";
    [SerializeField] [TextArea(3, 10)] private string body;

    public string Title => title;
    public string Body => body;

    public bool HasContent()
    {
        return !string.IsNullOrWhiteSpace(title) || !string.IsNullOrWhiteSpace(body);
    }

    public string FormatForPopup()
    {
        string trimmedTitle = string.IsNullOrWhiteSpace(title) ? string.Empty : title.Trim();
        string trimmedBody = string.IsNullOrWhiteSpace(body) ? string.Empty : body.Trim();

        if (string.IsNullOrEmpty(trimmedTitle))
            return trimmedBody;

        if (string.IsNullOrEmpty(trimmedBody))
            return trimmedTitle;

        return trimmedTitle + ":\n\n" + trimmedBody;
    }
}
