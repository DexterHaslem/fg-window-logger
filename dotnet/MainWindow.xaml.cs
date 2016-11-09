using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

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
            DataContext = _vm;
        }

        public void OnStartStopClick(object sender, RoutedEventArgs e)
        {
            _vm.OnStartStopClick(sender, e);
        }

        private void OnDeleteLogClick(object sender, RoutedEventArgs e)
        {
            _vm.OnDeleteLogClick();
        }

        private void OnExportLogClick(object sender, RoutedEventArgs e)
        {
            _vm.OnExportLogClick();
        }

        private void OnLogSelectionChanged(object sender, RoutedEventArgs e)
        {
            _vm.OnLogSelectionChanged();
        }
    }
}
