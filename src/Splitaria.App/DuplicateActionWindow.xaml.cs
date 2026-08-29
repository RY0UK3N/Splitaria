using Splitaria.Core;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Splitaria.App;

public partial class DuplicateActionWindow : Window
{
    public DuplicateAction? SelectedAction { get; private set; }

    public DuplicateActionWindow(int conflictCount)
    {
        InitializeComponent();
        DescriptionText.Text = conflictCount == 1
            ? "1 arquivo selecionado já existe ou possui o mesmo nome no destino."
            : $"{conflictCount} arquivos selecionados já existem ou possuem o mesmo nome no destino.";
    }

    private void Choice_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton { Tag: string tag } || !Enum.TryParse<DuplicateAction>(tag, out var action)) return;
        SelectedAction = action;
        if (ConfirmButton is not null) ConfirmButton.IsEnabled = true;
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedAction is null) return;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) DragMove();
    }
}
