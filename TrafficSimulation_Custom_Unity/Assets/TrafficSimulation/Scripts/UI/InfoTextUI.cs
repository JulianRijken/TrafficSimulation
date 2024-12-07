using TMPro;
using UnityEngine;

public class InfoTextUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _typeText;
    [SerializeField] private TextMeshProUGUI _infoText;

    public void SetTypeText(string text)
    {
        _typeText.text = text;
    }

    public void SetInfoText(string text)
    {
        _infoText.text = text;
    }
}