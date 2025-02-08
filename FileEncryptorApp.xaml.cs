using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
            worker = new BackgroundWorker { WorkerReportsProgress = true };
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

        private void StartProcess(bool encryptionMode)
        {
            if (ValidateInput())
            {
                isEncryptionMode = encryptionMode;
                StartWorker();
            }
        }

        private void Encrypt_Click(object sender, RoutedEventArgs e) => StartProcess(true);
        private void Decrypt_Click(object sender, RoutedEventArgs e) => StartProcess(false);

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

        private string GetEncryptionKey()
        {
            string key = string.Empty;
            keyTextBox.Dispatcher.Invoke(() => key = keyTextBox.Text);
            return key.PadRight(32);
        }

        private (Aes, ICryptoTransform) PrepareEncryption()
        {
            Aes aes = Aes.Create();
            byte[] keyBytes = Encoding.UTF8.GetBytes(GetEncryptionKey());
            aes.Key = keyBytes;
            aes.IV = keyBytes.Take(16).ToArray();

            ICryptoTransform cryptoTransform = isEncryptionMode ? aes.CreateEncryptor() : aes.CreateDecryptor();
            return (aes, cryptoTransform);
        }

        private void ProcessFile(string tempFilePath, ICryptoTransform cryptoTransform)
        {
            using (FileStream inputStream = new FileStream(selectedFilePath, FileMode.Open, FileAccess.Read))
            using (FileStream outputStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
            using (CryptoStream cryptoStream = new CryptoStream(outputStream, cryptoTransform, CryptoStreamMode.Write))
            {
                byte[] buffer = new byte[1024];
                long totalBytes = inputStream.Length, totalRead = 0;
                int bytesRead;

                while ((bytesRead = inputStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    cryptoStream.Write(buffer, 0, bytesRead);
                    totalRead += bytesRead;
                    worker.ReportProgress((int)((double)totalRead / totalBytes * 100));
                }
            }
        }

        private void ReplaceOriginalFile(string tempFilePath)
        {
            File.Copy(tempFilePath, selectedFilePath, overwrite: true);
            File.Delete(tempFilePath);
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            string tempFilePath = Path.GetTempFileName();
            var (aes, cryptoTransform) = PrepareEncryption();

            try
            {
                ProcessFile(tempFilePath, cryptoTransform);
                ReplaceOriginalFile(tempFilePath);
                e.Result = selectedFilePath;
            }
            finally
            {
                aes.Dispose();
            }
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Dispatcher.Invoke(() => progressBar.Value = e.ProgressPercentage);
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            stopwatch.Stop();
            progressBar.Value = 100;

            statusTextBlock.Dispatcher.Invoke(() => statusTextBlock.Text = $"Operation completed: {e.Result}");
            timeTextBlock.Dispatcher.Invoke(() => timeTextBlock.Text = $"Time: {stopwatch.Elapsed.TotalSeconds:F2} seconds");

            MessageBox.Show($"Operation completed successfully. File saved at the same path: {e.Result}");
            ResetForm();
        }

        private void ResetForm()
        {
            selectedFilePath = string.Empty;
            keyTextBox.Text = string.Empty;
            statusTextBlock.Text = "Ready to start.";
            timeTextBlock.Text = string.Empty;
            progressBar.Value = 0;
        }
    }
}
