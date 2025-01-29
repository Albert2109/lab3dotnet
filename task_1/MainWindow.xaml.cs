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

        private async void BtnEncrypt_Click(object sender, RoutedEventArgs e)
        {
            if (FileList.Items.Count == 0 || string.IsNullOrEmpty(TxtKey.Text) || TxtKey.Text == "Enter Key")
            {
                MessageBox.Show("Please select files and enter a key.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ProgressBar.Value = 0;
            var progress = new Progress<int>(value => Dispatcher.Invoke(() => ProgressBar.Value = value));

            try
            {
                foreach (string filePath in FileList.Items)
                {
                    await EncryptFileAsync(filePath, TxtKey.Text, progress);
                }

                MessageBox.Show("All files encrypted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during encryption: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnDecrypt_Click(object sender, RoutedEventArgs e)
        {
            if (FileList.Items.Count == 0 || string.IsNullOrEmpty(TxtKey.Text) || TxtKey.Text == "Enter Key")
            {
                MessageBox.Show("Please select files and enter a key.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ProgressBar.Value = 0;
            var progress = new Progress<int>(value => Dispatcher.Invoke(() => ProgressBar.Value = value));

            try
            {
                foreach (string filePath in FileList.Items)
                {
                    await DecryptFileAsync(filePath, TxtKey.Text, progress);
                }

                MessageBox.Show("All files decrypted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during decryption: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task EncryptFileAsync(string filePath, string key, IProgress<int> progress)
        {
            DateTime startTime = DateTime.Now;
            string encryptedFile = await Task.Run(() => ProcessFileAsync(filePath, key, true, progress));
            TimeSpan elapsedTime = DateTime.Now - startTime;

            MessageBox.Show($"File encrypted successfully!\n\nFile: {encryptedFile}\nTime: {elapsedTime}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task DecryptFileAsync(string filePath, string key, IProgress<int> progress)
        {
            DateTime startTime = DateTime.Now;
            string decryptedFile = await Task.Run(() => ProcessFileAsync(filePath, key, false, progress));
            TimeSpan elapsedTime = DateTime.Now - startTime;

            MessageBox.Show($"File decrypted successfully!\n\nFile: {decryptedFile}\nTime: {elapsedTime}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task<string> ProcessFileAsync(string filePath, string key, bool isEncryption, IProgress<int> progress)
        {
            string outputFile = isEncryption ? filePath + ".enc" : filePath.Replace(".enc", "");
            byte[] keyBytes = Encoding.UTF8.GetBytes(key.PadRight(16).Substring(0, 16));

            byte[] iv = new byte[16];
            if (isEncryption)
            {
                using (var rng = new RNGCryptoServiceProvider())
                {
                    rng.GetBytes(iv);
                }
            }

            using (FileStream inputFileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (FileStream outputFileStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;

                if (isEncryption)
                {
                    aes.IV = iv;
                    await outputFileStream.WriteAsync(iv, 0, iv.Length); 
                }
                else
                {
                    await inputFileStream.ReadAsync(iv, 0, iv.Length); 
                    aes.IV = iv;
                }

                ICryptoTransform transform = isEncryption ? aes.CreateEncryptor() : aes.CreateDecryptor();
                using (CryptoStream cryptoStream = new CryptoStream(outputFileStream, transform, CryptoStreamMode.Write))
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead;
                    long totalBytesRead = 0;
                    long totalBytes = isEncryption
                        ? inputFileStream.Length
                        : inputFileStream.Length - iv.Length; 

                    while ((bytesRead = await inputFileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await cryptoStream.WriteAsync(buffer, 0, bytesRead);
                        totalBytesRead += bytesRead;

                      
                        int progressPercentage = (int)((double)totalBytesRead / totalBytes * 100);
                        progress.Report(progressPercentage);

                       
                        await Task.Yield();
                    }
                }
            }

            return outputFile;
        }


    }
}
