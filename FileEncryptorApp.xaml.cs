using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows;
using Microsoft.Win32;

namespace FileEncryptorApp
{
    public partial class MainWindow : Window
    {
        private BackgroundWorker worker;
        private Stopwatch stopwatch;
        private string selectedFilePath;
        private bool isEncryptionMode;

        public MainWindow()
        {
            InitializeComponent();
            InitializeBackgroundWorker();
        }

        private void InitializeBackgroundWorker()
        {
            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += Worker_DoWork;
            worker.ProgressChanged += Worker_ProgressChanged;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            stopwatch = new Stopwatch();
        }

        private void SelectFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                selectedFilePath = openFileDialog.FileName;
                statusTextBlock.Text = $"Selected File: {selectedFilePath}";
            }
        }

        private void Encrypt_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateInput())
            {
                isEncryptionMode = true;
                StartWorker();
            }
        }

        private void Decrypt_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateInput())
            {
                isEncryptionMode = false;
                StartWorker();
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrEmpty(selectedFilePath))
            {
                MessageBox.Show("Please select a file.");
                return false;
            }

            if (string.IsNullOrEmpty(keyTextBox.Text))
            {
                MessageBox.Show("Please enter an encryption key.");
                return false;
            }

            return true;
        }

        private void StartWorker()
        {
            stopwatch.Restart();
            worker.RunWorkerAsync();
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            string key = string.Empty;
            keyTextBox.Dispatcher.Invoke(() =>
            {
                key = keyTextBox.Text;
            });

            string tempFilePath = Path.GetTempFileName(); // Створюємо тимчасовий файл

            using (FileStream inputStream = new FileStream(selectedFilePath, FileMode.Open, FileAccess.Read))
            using (FileStream outputStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write)) // Запис у тимчасовий файл
            using (Aes aes = Aes.Create())
            {
                // Генерація ключа та IV
                byte[] keyBytes = Encoding.UTF8.GetBytes(key.PadRight(32));
                aes.Key = keyBytes;
                aes.IV = keyBytes.Take(16).ToArray();

                // Створюємо трансформатор (шифрування або дешифрування)
                ICryptoTransform cryptoTransform = isEncryptionMode ? aes.CreateEncryptor() : aes.CreateDecryptor();
                using (CryptoStream cryptoStream = new CryptoStream(outputStream, cryptoTransform, CryptoStreamMode.Write))
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead;
                    long totalBytes = inputStream.Length;
                    long totalRead = 0;

                    while ((bytesRead = inputStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        cryptoStream.Write(buffer, 0, bytesRead);
                        totalRead += bytesRead;

                        int progress = (int)((double)totalRead / totalBytes * 100);
                        worker.ReportProgress(progress);
                    }
                }
            }

            // Переміщуємо тимчасовий файл на місце оригінального
            File.Copy(tempFilePath, selectedFilePath, overwrite: true);
            File.Delete(tempFilePath); // Видаляємо тимчасовий файл

            e.Result = selectedFilePath; // Передаємо шлях результату
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Dispatcher.Invoke(() =>
            {
                progressBar.Value = e.ProgressPercentage;
            });

        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            stopwatch.Stop();
            progressBar.Value = 100;

            // Оновлюємо статус через Dispatcher
            statusTextBlock.Dispatcher.Invoke(() =>
            {
                statusTextBlock.Text = $"Operation completed: {e.Result}";
            });

            timeTextBlock.Dispatcher.Invoke(() =>
            {
                timeTextBlock.Text = $"Time: {stopwatch.Elapsed.TotalSeconds:F2} seconds";
            });

            // Повідомлення про успіх
            MessageBox.Show($"Operation completed successfully. File saved at the same path: {e.Result}");

            // Очищення форми після завершення
            ResetForm();
        }

        private void ResetForm()
        {
            // Скидаємо всі елементи до початкового стану
            selectedFilePath = string.Empty;
            keyTextBox.Text = string.Empty;

            statusTextBlock.Text = "Ready to start.";
            timeTextBlock.Text = string.Empty;
            progressBar.Value = 0;
        }
    }
}
