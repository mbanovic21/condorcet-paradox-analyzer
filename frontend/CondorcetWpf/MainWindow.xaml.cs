using System.Windows;
using CondorcetWpf.ViewModels;

namespace CondorcetWpf;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}
