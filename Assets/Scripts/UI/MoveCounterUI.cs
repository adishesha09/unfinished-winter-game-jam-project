using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class MoveCounterUI : MonoBehaviour
{
    [SerializeField] private SwitchController switchController;
    [SerializeField] private string counterLabel    = "Switches: ";
    [SerializeField] private string unlimitedLabel  = "∞";
    [SerializeField] private Color normalColor      = Color.white;
    [SerializeField] private Color lowMovesColor    = new Color(1f, 0.6f, 0.1f);
    [SerializeField] private Color exhaustedColor   = new Color(1f, 0.2f, 0.2f);
    [SerializeField] private int lowMovesThreshold  = 3;

    private TextMeshProUGUI _text;

    private void Awake()
    {
        _text = GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        if (switchController == null)
            switchController = FindFirstObjectByType<SwitchController>();

        if (switchController == null) return;

        switchController.OnMovesRemainingChanged += Refresh;
        Refresh(switchController.MovesRemaining);
    }

    private void OnDestroy()
    {
        if (switchController != null)
            switchController.OnMovesRemainingChanged -= Refresh;
    }

    private void Refresh(int movesRemaining)
    {
        if (movesRemaining == int.MaxValue)
        {
            _text.text  = counterLabel + unlimitedLabel;
            _text.color = normalColor;
            return;
        }

        _text.text  = counterLabel + movesRemaining.ToString();
        _text.color = movesRemaining == 0                  ? exhaustedColor
                    : movesRemaining <= lowMovesThreshold  ? lowMovesColor
                    : normalColor;
    }
}