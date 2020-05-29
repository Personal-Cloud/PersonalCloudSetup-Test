using System.Windows;

public partial class ChooseLanguageView : Window
{
    public ChooseLanguageView()
    {
        InitializeComponent();
    }

    void OK_Click(object sender, RoutedEventArgs e)
    {
        this.DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        this.DialogResult = false;
        Close();
    }

    private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        this.DragMove();
    }
}
