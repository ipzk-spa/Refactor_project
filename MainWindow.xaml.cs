using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace TaskManagerWpf
{
    public partial class MainWindow : Window
    {
        private Process selectedProcess;

        public MainWindow()
        {
            InitializeComponent();
            LoadProcesses();
        }

        private async void LoadProcesses()
        {
            processesDataGrid.ItemsSource = null;
            await System.Threading.Tasks.Task.Run(() =>
            {
                var processes = Process.GetProcesses().Select(p =>
                {
                    try
                    {
                        return new
                        {
                            ProcessName = p.ProcessName,
                            Id = p.Id,
                            MemoryUsage = (p.PrivateMemorySize64 / 1024 / 1024) + " MB",
                            Priority = p.PriorityClass.ToString(),
                            StartTime = p.StartTime.ToString("dd.MM.yyyy HH:mm:ss"),
                            ThreadCount = p.Threads.Count.ToString()
                        };
                    }
                    catch
                    {
                        return new
                        {
                            ProcessName = p.ProcessName,
                            Id = p.Id,
                            MemoryUsage = "N/A",
                            Priority = "N/A",
                            StartTime = "N/A",
                            ThreadCount = "N/A"
                        };
                    }
                }).ToList();

                Dispatcher.Invoke(() =>
                {
                    processesDataGrid.ItemsSource = processes;
                });
            });
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadProcesses();
        }

        private void TerminateButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedProcess != null)
            {
                try
                {
                    selectedProcess.Kill();
                    MessageBox.Show($"Процес {selectedProcess.ProcessName} завершено.");
                    LoadProcesses();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Не вдалося завершити процес: " + ex.Message);
                }
                finally
                {
                    selectedProcess = null;
                }
            }
            else
            {
                MessageBox.Show("Будь ласка, виберіть процес для завершення.");
            }
        }

        private void processesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = processesDataGrid.SelectedItem as dynamic;
            if (selectedItem != null)
            {
                try
                {
                    int pid = selectedItem.Id;
                    selectedProcess = Process.GetProcessById(pid);
                    UpdatePriorityComboBox();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Помилка доступу до процесу: " + ex.Message);
                }
            }
            else
            {
                selectedProcess = null;
                priorityComboBox.SelectedIndex = -1;
                priorityComboBox.IsEnabled = false;
            }
        }

        private void UpdatePriorityComboBox()
        {
            if (selectedProcess != null)
            {
                priorityComboBox.IsEnabled = true;

                switch (selectedProcess.PriorityClass)
                {
                    case ProcessPriorityClass.Idle:
                        priorityComboBox.SelectedIndex = 0;
                        break;
                    case ProcessPriorityClass.BelowNormal:
                        priorityComboBox.SelectedIndex = 1;
                        break;
                    case ProcessPriorityClass.Normal:
                        priorityComboBox.SelectedIndex = 2;
                        break;
                    case ProcessPriorityClass.AboveNormal:
                        priorityComboBox.SelectedIndex = 3;
                        break;
                    case ProcessPriorityClass.High:
                        priorityComboBox.SelectedIndex = 4;
                        break;
                    case ProcessPriorityClass.RealTime:
                        priorityComboBox.SelectedIndex = 5;
                        break;
                    default:
                        priorityComboBox.SelectedIndex = -1;
                        break;
                }
            }
        }

        private void priorityComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (selectedProcess == null)
            {
                //MessageBox.Show("Процес не вибрано. Будь ласка, виберіть процес зі списку.");
                return;
            }

            if (priorityComboBox.SelectedItem != null)
            {
                try
                {
                    var selectedPriority = (priorityComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                    ProcessPriorityClass newPriority = selectedProcess.PriorityClass;
                    bool flag = false;

                    switch (selectedPriority)
                    {
                        case "Idle":
                            newPriority = ProcessPriorityClass.Idle;
                            flag = true;
                            break;
                        case "BelowNormal":
                            newPriority = ProcessPriorityClass.BelowNormal;
                            flag = true;
                            break;
                        case "Normal":
                            newPriority = ProcessPriorityClass.Normal;
                            flag = true;
                            break;
                        case "AboveNormal":
                            newPriority = ProcessPriorityClass.AboveNormal;
                            flag = true;
                            break;
                        case "High":
                            newPriority = ProcessPriorityClass.High;
                            flag = true;
                            break;
                        case "RealTime":
                            newPriority = ProcessPriorityClass.RealTime;
                            flag = true;
                            break;
                        default:
                            flag = false;
                            break;
                    }

                    if (flag && selectedProcess.PriorityClass != newPriority)
                    {
                        selectedProcess.PriorityClass = newPriority;

                        var selectedItem = processesDataGrid.SelectedItem as dynamic;
                        if (selectedItem != null)
                        {
                            selectedItem.Priority = newPriority.ToString();
                            processesDataGrid.Items.Refresh();
                        }

                        MessageBox.Show($"Пріоритет процесу {selectedProcess.ProcessName} змінено на {newPriority}.");
                    }
                    else if (!flag)
                    {
                        MessageBox.Show("Вибраний некоректний пункт пріоритету.");
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show("Не вдалося змінити пріоритет. Недостатньо прав доступу.");
                }
                catch (Exception ex)
                {
                    //MessageBox.Show("Сталася помилка при зміні пріоритету: " + ex.Message);
                }
            }
        }





        private void LaunchExcel(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("excel.exe");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не вдалося запустити Excel: " + ex.Message);
            }
        }

        private void LaunchCalculator(object sender, RoutedEventArgs e)
        {
            Process.Start("calc.exe");
        }

        private void LaunchWord(object sender, RoutedEventArgs e)
        {
            Process.Start("winword.exe");
        }

        private void LaunchPyCharm(object sender, RoutedEventArgs e)
        {
            Process.Start("\"D:\\PyCharm 2024.2.1\\bin\\pycharm64.exe\"");
        }

        private void LaunchSteam(object sender, RoutedEventArgs e)
        {
            Process.Start("\"D:\\Steam\\steam.exe\"");
        }

        private void OpenEncryptor_Click(object sender, RoutedEventArgs e)
        {
            FileEncryptorApp.MainWindow encryptorWindow = new FileEncryptorApp.MainWindow();
            encryptorWindow.Show();
        }
    }
}
