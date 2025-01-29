using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using task2;

namespace ProcessManager
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer _updateTimer;

        public MainWindow()
        {
            InitializeComponent();
            LoadProcessesAsync();

            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2) 
            };
            _updateTimer.Tick += (s, e) => LoadProcessesAsync(); 
            _updateTimer.Start();
        }

        private async Task LoadProcessesAsync()
        {
            try
            {
                var processes = await Task.Run(() => Process.GetProcesses()
                    .Where(p => !string.IsNullOrEmpty(p.MainWindowTitle))
                    .Select(p => new ProcessInfo(p))
                    .ToList());

                ProcessGrid.ItemsSource = processes;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void KillProcess_Click(object sender, RoutedEventArgs e)
        {
            if (ProcessGrid.SelectedItem is ProcessInfo selectedProcess)
            {
                try
                {
                    var process = Process.GetProcessById(selectedProcess.Id);
                    Task.Run(() => process.Kill()).Wait();

                    LoadProcessesAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ChangePriority_Click(object sender, RoutedEventArgs e)
        {
            if (ProcessGrid.SelectedItem is ProcessInfo selectedProcess)
            {
                try
                {
                    var process = Process.GetProcessById(selectedProcess.Id);
                    Task.Run(() => process.PriorityClass = ProcessPriorityClass.High).Wait();

                    LoadProcessesAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void StartApp_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string appName)
            {
                try
                {
                    Process.Start(appName);
                    LoadProcessesAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
