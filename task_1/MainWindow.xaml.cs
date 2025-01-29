using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace task_1
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnSelectFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Multiselect = true 
            };

            if (openFileDialog.ShowDialog() == true)
            {
                FileList.Items.Clear();
                foreach (string fileName in openFileDialog.FileNames)
                {
                    FileList.Items.Add(fileName);
                }
            }
        }

        private void TxtKey_GotFocus(object sender, RoutedEventArgs e)
        {
            if (TxtKey.Text == "Enter Key")
            {
                TxtKey.Text = "";
                TxtKey.Foreground = Brushes.Black;
            }
        }

        private void TxtKey_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtKey.Text))
            {
                TxtKey.Text = "Enter Key";
                TxtKey.Foreground = Brushes.Gray;
            }
        }

        private async void BtnEncrypt_Click(object sender, RoutedEventArgs e) => await ProcessFilesAsync(true);

        private async void BtnDecrypt_Click(object sender, RoutedEventArgs e) => await ProcessFilesAsync(false);

        private async Task ProcessFilesAsync(bool isEncryption)
        {
            if (!ValidateInput()) return;

            ProgressBar.Value = 0;
            var progress = new Progress<int>(value => Dispatcher.Invoke(() => ProgressBar.Value = value));
            var processedFiles = new StringBuilder();
            DateTime startTime = DateTime.Now;

            try
            {
                foreach (string filePath in FileList.Items)
                {
                    string processedFile = await ProcessFileAsync(filePath, TxtKey.Text, isEncryption, progress);
                    processedFiles.AppendLine($"{(isEncryption ? "Encrypted" : "Decrypted")}: {processedFile}");
                }

                TimeSpan elapsedTime = DateTime.Now - startTime;
                MessageBox.Show($"File {(isEncryption ? "encrypted" : "decrypted")} successfully!\n\n" +
                                $"{processedFiles}Time elapsed: {elapsedTime}",
                                "Operation Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during {(isEncryption ? "encryption" : "decryption")}: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateInput()
        {
            if (FileList.Items.Count == 0 || string.IsNullOrEmpty(TxtKey.Text) || TxtKey.Text == "Enter Key")
            {
                MessageBox.Show("Please select files and enter a key.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true;
        }

        private async Task<byte[]> SetupIVAsync(bool isEncryption, FileStream inputFileStream, FileStream outputFileStream, Aes aes)
        {
            byte[] iv = new byte[16];

            if (isEncryption)
            {
                new RNGCryptoServiceProvider().GetBytes(iv);
                aes.IV = iv;
                await outputFileStream.WriteAsync(iv, 0, iv.Length);
            }
            else
            {
                await inputFileStream.ReadAsync(iv, 0, iv.Length);
                aes.IV = iv;
            }

            return iv;
        }

        private async Task EncryptOrDecryptStreamAsync(FileStream inputFileStream, FileStream outputFileStream, Aes aes, bool isEncryption, IProgress<int> progress)
        {
            ICryptoTransform transform = isEncryption ? aes.CreateEncryptor() : aes.CreateDecryptor();
            using (CryptoStream cryptoStream = new CryptoStream(outputFileStream, transform, CryptoStreamMode.Write))
            {
                byte[] buffer = new byte[4096];
                int bytesRead;
                long totalBytesRead = 0;
                long totalBytes = inputFileStream.Length - (isEncryption ? 0 : 16);

                while ((bytesRead = await inputFileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await cryptoStream.WriteAsync(buffer, 0, bytesRead);
                    totalBytesRead += bytesRead;
                    progress.Report((int)((double)totalBytesRead / totalBytes * 100));
                    await Task.Yield();
                }
            }
        }

        private async Task<string> ProcessFileAsync(string filePath, string key, bool isEncryption, IProgress<int> progress)
        {
            string outputFile = isEncryption ? filePath + ".enc" : filePath.Replace(".enc", "");
            byte[] keyBytes = Encoding.UTF8.GetBytes(key.PadRight(16).Substring(0, 16));

            using (FileStream inputFileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (FileStream outputFileStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                byte[] iv = await SetupIVAsync(isEncryption, inputFileStream, outputFileStream, aes);
                await EncryptOrDecryptStreamAsync(inputFileStream, outputFileStream, aes, isEncryption, progress);
            }

            return outputFile;
        }
    }
}
