using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace task1
{
    public partial class Form1 : Form
    {
        private readonly FileEncryptor encryptor = new FileEncryptor();
        private readonly BackgroundWorker worker = new BackgroundWorker();
        private Stopwatch stopwatch;

        // Поля для контролів
        private System.Windows.Forms.TextBox txtKey;
        private System.Windows.Forms.TextBox txtResult;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.ListBox listBoxFiles; // Поле для вибору файлів
        private System.Windows.Forms.ListBox listBoxProcessFiles; // Поле для вибору файлів для обробки

        public Form1()
        {
            InitializeComponent();
            this.Text = "Шифрування файлів";
            this.Width = 400;
            this.Height = 400;

            // Поле для ключа
            Label lblKey = new Label
            {
                Text = "Ключ:",
                Left = 10,
                Top = 20,
                Width = 50
            };
            this.Controls.Add(lblKey);

            txtKey = new System.Windows.Forms.TextBox
            {
                Left = 70,
                Top = 20,
                Width = 300
            };
            this.Controls.Add(txtKey);

            // Кнопка "Додати файли"
            Button btnAddFiles = new Button
            {
                Text = "Додати файли",
                Left = 10,
                Top = 60,
                Width = 120
            };
            btnAddFiles.Click += BtnAddFiles_Click;
            this.Controls.Add(btnAddFiles);

            // Кнопка Шифрувати
            Button btnEncrypt = new Button
            {
                Text = "Шифрувати",
                Left = 150,
                Top = 60,
                Width = 120
            };
            btnEncrypt.Click += BtnEncrypt_Click;
            this.Controls.Add(btnEncrypt);

            // Кнопка Розшифрувати
            Button btnDecrypt = new Button
            {
                Text = "Розшифрувати",
                Left = 150,
                Top = 100,
                Width = 120
            };
            btnDecrypt.Click += BtnDecrypt_Click;
            this.Controls.Add(btnDecrypt);

            // Прогрес-бар
            progressBar = new System.Windows.Forms.ProgressBar
            {
                Left = 10,
                Top = 140,
                Width = 360,
                Value = 0
            };
            this.Controls.Add(progressBar);

            // Поле для результату
            txtResult = new System.Windows.Forms.TextBox
            {
                Left = 10,
                Top = 180,
                Width = 360,
                Height = 100,
                Multiline = true,
                ReadOnly = true
            };
            this.Controls.Add(txtResult);

            // ListBox для відображення вибраних файлів для обробки
            listBoxProcessFiles = new ListBox
            {
                Left = 10,
                Top = 220,
                Width = 360,
                Height = 60
            };
            this.Controls.Add(listBoxProcessFiles);

            // Налаштування BackgroundWorker
            worker.DoWork += Worker_DoWork;
            worker.ProgressChanged += Worker_ProgressChanged;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            worker.WorkerReportsProgress = true;
        }

        private void BtnAddFiles_Click(object sender, EventArgs e)
        {
            // Діалог вибору файлів
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Multiselect = true
            };
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // Очищаємо ListBox перед додаванням нових файлів
                listBoxProcessFiles.Items.Clear();

                // Додаємо вибрані файли в ListBox
                foreach (var file in openFileDialog.FileNames)
                {
                    listBoxProcessFiles.Items.Add(file); // Додати шлях до файлу
                }
            }
        }

        private void BtnEncrypt_Click(object sender, EventArgs e)
        {
            if (listBoxProcessFiles.Items.Count == 0)
            {
                MessageBox.Show("Будь ласка, додайте файли для шифрування.", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Шифрування вибраних файлів
            string outputFolder = Path.GetDirectoryName(listBoxProcessFiles.Items[0].ToString()); // Зберігаємо файли в тій самій папці, де знаходяться

            foreach (var file in listBoxProcessFiles.Items)
            {
                string outputFile = Path.Combine(outputFolder, Path.GetFileName(file.ToString()) + ".enc");
                stopwatch = Stopwatch.StartNew();
                worker.RunWorkerAsync(new
                {
                    Input = file.ToString(),
                    Output = outputFile,
                    Key = txtKey.Text,
                    IsEncrypt = true
                });
            }
        }

        private void BtnDecrypt_Click(object sender, EventArgs e)
        {
            if (listBoxProcessFiles.Items.Count == 0)
            {
                MessageBox.Show("Будь ласка, додайте файли для розшифрування.", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Розшифрування вибраних файлів
            string outputFolder = Path.GetDirectoryName(listBoxProcessFiles.Items[0].ToString()); // Зберігаємо файли в тій самій папці, де знаходяться

            foreach (var file in listBoxProcessFiles.Items)
            {
                // Видаляємо розширення .enc для розшифрування
                string outputFile = Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(file.ToString()) + ".dec");
                stopwatch = Stopwatch.StartNew();
                worker.RunWorkerAsync(new
                {
                    Input = file.ToString(),
                    Output = outputFile,
                    Key = txtKey.Text,
                    IsEncrypt = false
                });
            }
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            dynamic args = e.Argument;
            string input = args.Input;
            string output = args.Output;
            string key = args.Key;
            bool isEncrypt = args.IsEncrypt;

            try
            {
                if (isEncrypt)
                    encryptor.EncryptFile(input, output, key);
                else
                    encryptor.DecryptFile(input, output, key);

                e.Result = new { Success = true, FileName = output };
            }
            catch (Exception ex)
            {
                e.Result = new { Success = false, Message = ex.Message };
            }
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            stopwatch.Stop();

            // Перевірка на null для e.Result
            if (e.Result != null)
            {
                dynamic result = e.Result;
                if (result.Success)
                {
                    txtResult.Text = $"Файл збережено: {result.FileName}\nЧас: {stopwatch.Elapsed}";
                    // Оновлюємо список файлів після операції
                    listBoxProcessFiles.Items.Add(result.FileName);
                }
                else
                {
                    txtResult.Text = $"Помилка: {result.Message}";
                }
            }
            else
            {
                // Якщо e.Result рівний null, виводимо повідомлення про помилку
                txtResult.Text = "Невідомий результат або помилка при виконанні.";
            }
        }
    }

}
