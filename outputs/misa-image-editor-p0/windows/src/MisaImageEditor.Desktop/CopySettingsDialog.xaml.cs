using System.Windows;

namespace MisaImageEditor.Desktop;

public partial class CopySettingsDialog : Window
{
    private readonly LocalizationService _i18n;

    public CopySettingsDialog(LocalizationService i18n)
    {
        _i18n = i18n;
        InitializeComponent();
        Title = _i18n["CopySettingsTitle"];
        InstructionText.Text = _i18n["CopyInstruction"];
        BasicCheckBox.Content = _i18n["BasicAdjustments"];
        ToneCurveCheckBox.Content = _i18n["ToneCurve"];
        ColorMixerCheckBox.Content = _i18n["ColorMixer"] + " / HSL";
        LensCorrectionCheckBox.Content = _i18n["LensCorrections"];
        CropCheckBox.Content = _i18n["CropTransform"];
        MaskCheckBox.Content = _i18n["MaskLocal"];
        WatermarkCheckBox.Content = _i18n["ExportWatermark"];
        CheckAllButton.Content = _i18n["CheckAll"];
        CheckNoneButton.Content = _i18n["CheckNone"];
        CopyButton.Content = _i18n["Copy"];
        CancelButton.Content = _i18n["Cancel"];
    }

    public bool CopyBasic { get; private set; }
    public bool CopyToneCurve { get; private set; }
    public bool CopyColorMixer { get; private set; }
    public bool CopyLensCorrection { get; private set; }
    public bool CopyCrop { get; private set; }
    public bool CopyMask { get; private set; }
    public bool CopyWatermark { get; private set; }

    private void CheckAll_Click(object sender, RoutedEventArgs e)
    {
        BasicCheckBox.IsChecked = true;
        ToneCurveCheckBox.IsChecked = true;
        ColorMixerCheckBox.IsChecked = true;
        LensCorrectionCheckBox.IsChecked = true;
        CropCheckBox.IsChecked = true;
        MaskCheckBox.IsChecked = true;
        WatermarkCheckBox.IsChecked = true;
    }

    private void CheckNone_Click(object sender, RoutedEventArgs e)
    {
        BasicCheckBox.IsChecked = false;
        ToneCurveCheckBox.IsChecked = false;
        ColorMixerCheckBox.IsChecked = false;
        LensCorrectionCheckBox.IsChecked = false;
        CropCheckBox.IsChecked = false;
        MaskCheckBox.IsChecked = false;
        WatermarkCheckBox.IsChecked = false;
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        CopyBasic = BasicCheckBox.IsChecked == true;
        CopyToneCurve = ToneCurveCheckBox.IsChecked == true;
        CopyColorMixer = ColorMixerCheckBox.IsChecked == true;
        CopyLensCorrection = LensCorrectionCheckBox.IsChecked == true;
        CopyCrop = CropCheckBox.IsChecked == true;
        CopyMask = MaskCheckBox.IsChecked == true;
        CopyWatermark = WatermarkCheckBox.IsChecked == true;
        if (!CopyBasic && !CopyToneCurve && !CopyColorMixer && !CopyLensCorrection && !CopyCrop && !CopyMask && !CopyWatermark)
        {
            System.Windows.MessageBox.Show(this, _i18n["CopyAtLeastOne"], _i18n["CopySettingsTitle"], System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
