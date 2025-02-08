using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;

namespace TaskManagerWpf
{
    public partial class ProcessManagerWpf : Window
    {
        public ProcessManagerWpf()
        {
            InitializeComponent();
            LoadProcesses();
        }

        private void LoadProcesses()
        {
            try
            {
                var processes = Process.GetProcesses()
                                       .Select(p => new ProcessInfo
                                       {
                                           Id = p.Id,
                                           Name = p.ProcessName,
                                           MemoryUsage = GetProcessMemoryUsage(p)
                                       }).ToList();

                ProcessesDataGrid.ItemsSource = processes;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження процесів: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetProcessMemoryUsage(Process process)
        {
            try
            {
                return $"{process.PrivateMemorySize64 / (1024 * 1024)} MB";
            }
            catch
            {
                return "Н/Д";
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadProcesses();
        }

        private void KillProcessButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProcessesDataGrid.SelectedItem is ProcessInfo selectedProcess)
            {
                try
                {
                    Process.GetProcessById(selectedProcess.Id).Kill();
                    MessageBox.Show($"Процес '{selectedProcess.Name}' завершено.", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadProcesses();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Помилка завершення процесу: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    public class ProcessInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string MemoryUsage { get; set; }
    }
}
