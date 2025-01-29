using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

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
                    .Select(p => new
                    {
                        Name = p.ProcessName,
                        WindowTitle = p.MainWindowTitle,
                        Memory = p.WorkingSet64 / 1024 / 1024, 
                        StartTime = SafeGetStartTime(p),
                        Priority = p.BasePriority,
                        Threads = p.Threads.Count,
                        Id = p.Id
                    }).ToList());

                ProcessGrid.ItemsSource = processes; 
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private DateTime? SafeGetStartTime(Process process)
        {
            try
            {
                return process.StartTime;
            }
            catch
            {
                return null; 
            }
        }

        private void KillProcess_Click(object sender, RoutedEventArgs e)
        {
            if (ProcessGrid.SelectedItem != null)
            {
                try
                {
                    var selectedProcess = (dynamic)ProcessGrid.SelectedItem;
                    var process = Process.GetProcessById((int)selectedProcess.Id);

                  
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
            if (ProcessGrid.SelectedItem != null)
            {
                try
                {
                    var selectedProcess = (dynamic)ProcessGrid.SelectedItem;
                    var process = Process.GetProcessById((int)selectedProcess.Id);

                    
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
            var button = sender as Button;
            string appName = button?.Tag.ToString();

            if (!string.IsNullOrEmpty(appName))
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
