using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public class FileEncryptor
{
    private readonly string encryptedFilesDir = "EncryptedFiles";

    public FileEncryptor()
    {
        // Створення директорії для зашифрованих файлів, якщо її не існує
        if (!Directory.Exists(encryptedFilesDir))
        {
            Directory.CreateDirectory(encryptedFilesDir);
        }
    }

    public void EncryptFile(string inputFile, string outputFile, string key)
    {
        FileStream inputStream = null;
        FileStream outputStream = null;
        Aes aes = null;

        try
        {
            inputStream = new FileStream(inputFile, FileMode.Open, FileAccess.Read);
            outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
            aes = Aes.Create();

            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            if (keyBytes.Length != aes.Key.Length)
            {
                Array.Resize(ref keyBytes, aes.Key.Length);
            }
            aes.Key = keyBytes;
            aes.GenerateIV();

            // Запис IV у початок вихідного файлу
            outputStream.Write(aes.IV, 0, aes.IV.Length);

            // Створення CryptoStream для шифрування
            CryptoStream cryptoStream = null;
            try
            {
                cryptoStream = new CryptoStream(outputStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
                inputStream.CopyTo(cryptoStream);
            }
            finally
            {
                if (cryptoStream != null)
                    cryptoStream.Dispose();
            }
        }
        finally
        {
            if (aes != null)
                aes.Dispose();
            if (inputStream != null)
                inputStream.Dispose();
            if (outputStream != null)
                outputStream.Dispose();
        }
    }

    public void DecryptFile(string inputFile, string outputFile, string key)
    {
        FileStream inputStream = null;
        FileStream outputStream = null;
        Aes aes = null;

        try
        {
            inputStream = new FileStream(inputFile, FileMode.Open, FileAccess.Read);
            outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
            aes = Aes.Create();

            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            if (keyBytes.Length != aes.Key.Length)
            {
                Array.Resize(ref keyBytes, aes.Key.Length);
            }
            aes.Key = keyBytes;

            // Читання IV із початку зашифрованого файлу
            byte[] iv = new byte[aes.IV.Length];
            inputStream.Read(iv, 0, iv.Length);
            aes.IV = iv;

            // Створення CryptoStream для розшифрування
            CryptoStream cryptoStream = null;
            try
            {
                cryptoStream = new CryptoStream(inputStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
                cryptoStream.CopyTo(outputStream);
            }
            finally
            {
                if (cryptoStream != null)
                    cryptoStream.Dispose();
            }
        }
        finally
        {
            if (aes != null)
                aes.Dispose();
            if (inputStream != null)
                inputStream.Dispose();
            if (outputStream != null)
                outputStream.Dispose();
        }
    }

    public string GetEncryptedFilePath(string fileName)
    {
        return Path.Combine(encryptedFilesDir, fileName);
    }
}
