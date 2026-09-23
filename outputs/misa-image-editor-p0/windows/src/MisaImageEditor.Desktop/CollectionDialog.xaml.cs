using System.Windows;

namespace MisaImageEditor.Desktop;

public partial class CollectionDialog : Window
{
    private readonly LocalizationService _i18n;

    public CollectionDialog(LocalizationService i18n, string suggestedName)
    {
        _i18n = i18n;
        InitializeComponent();
        Title = _i18n["CollectionDialogTitle"];
        CollectionNameLabel.Text = _i18n["CollectionName"];
        IncludeSelectedBox.Content = _i18n["IncludeSelected"];
        SetTargetBox.Content = _i18n["SetTarget"];
        CreateButton.Content = _i18n["Create"];
        CancelButton.Content = _i18n["Cancel"];
        NameBox.Text = suggestedName;
        NameBox.SelectAll();
        NameBox.Focus();
    }

    public string CollectionName => NameBox.Text.Trim();
    public bool IncludeSelected => IncludeSelectedBox.IsChecked == true;
    public bool SetAsTarget => SetTargetBox.IsChecked == true;

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        if (CollectionName.Length == 0)
        {
            System.Windows.MessageBox.Show(this, _i18n["MissingNameMessage"], _i18n["MissingName"], MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
