namespace CouiSharp.Theme;

/// <summary>
/// COUI geometry tokens, in dp. Values come from <c>docs/coui-retheme-spec.md</c>: card 12dp,
/// buttons are capsules (height / 2), text fields 10dp, popups 16dp, sheets 28dp, dialogs 33dp.
/// </summary>
public static class CouiShape
{
    public const float CardRadius = 12f;
    public const float CardMargin = 16f;
    public const float CardInset = 16f;

    /// <summary>List row height; the first and last row of a card grow by 2dp.</summary>
    public const float RowHeight = 48f;

    public const float ButtonHeight = 44f;
    public const float ButtonPadding = 20f;

    public const float InputRadius = 10f;
    public const float InputHeight = 44f;
    public const float InputPadding = 14f;

    public const float PopupRadius = 16f;
    public const float SheetRadius = 28f;
    public const float DialogRadius = 33f;
    public const float TabRadius = 8f;
    public const float ChipHeight = 32f;

    public const float SwitchWidth = 36f;
    public const float SwitchHeight = 22f;
    public const float SwitchThumb = 18f;

    public const float SliderTrack = 4f;
    public const float SliderMinHeight = 20f;
    public const float SliderThumbRatio = 0.72f;

    public const float Divider = 1f;
    public const float FocusRing = 2f;

    /// <summary>Stroke width for outlines, in dp (hairline at 1x).</summary>
    public const float Stroke = 1f;
}
