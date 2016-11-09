using System;
using System.IO;
using System.Threading;
using System.Windows;

namespace ForegroundLogger_Managed
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ViewModel _vm;

        public MainWindow()
        {
            InitializeComponent();
            _vm = new ViewModel(this);
        }

        public void OnStartStopClick(object sender, RoutedEventArgs e)
        {
            _vm.OnStartStopClick(sender, e);
        }
    }
}
