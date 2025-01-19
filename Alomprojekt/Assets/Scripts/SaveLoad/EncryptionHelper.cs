using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

public static class EncryptionHelper
{
    /// <summary>
    /// Változók
    /// </summary>
    private static readonly string _EncryptionKey = "Concordia"; // Titkosításhoz használt kulcs


    /// <summary>
    /// Titkosítja a megadott szöveget
    /// </summary>
    /// <param name="plainText">A titkosítandó szöveg</param>
    /// <returns>A titkosított szöveg base64 kódolt formában</returns>
    public static string Encrypt(string plainText)
    {
        // AES algoritmus létrehozása
        using (Aes aes = Aes.Create()) 
        {
            // A kulcs beállítása, 32 byte hosszúságúra igazítva (padolás a kulcshoz)
            aes.Key = Encoding.UTF8.GetBytes(_EncryptionKey.PadRight(32));
            // Inicializációs vektor (IV) beállítása nullákkal
            aes.IV = new byte[16];

            // Titkosító objektum létrehozása
            using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
            // MemoryStream létrehozása a titkosított adat tárolásához
            using (var ms = new MemoryStream())
            {
                // Titkosító stream létrehozása
                using (var cryptoStream = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                // Writer létrehozása az adat stream írásához
                using (var writer = new StreamWriter(cryptoStream))
                {
                    writer.Write(plainText); // A szöveg írása a streambe
                }

                // A titkosított adat base64 formátumban való visszaadása
                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }


    /// <summary>
    /// Visszafejti a titkosított szöveget
    /// </summary>
    /// <param name="encryptedText">A titkosított, base64 kódolt szöveg</param>
    /// <returns>A visszafejtett szöveg</returns>
    public static string Decrypt(string encryptedText)
    {
        // AES algoritmus létrehozása
        using (Aes aes = Aes.Create())
        {
            // A kulcs beállítása, 32 byte hosszúságúra igazítva (padolás a kulcshoz)
            aes.Key = Encoding.UTF8.GetBytes(_EncryptionKey.PadRight(32));
            // Inicializációs vektor (IV) beállítása nullákkal
            aes.IV = new byte[16];

            // Visszafejtő objektum létrehozása
            using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
            // A titkosított szöveg konvertálása byte tömbbé
            using (var ms = new MemoryStream(Convert.FromBase64String(encryptedText)))
            // Visszafejtő stream létrehozása
            using (var cryptoStream = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            // Reader létrehozása a visszafejtett adat kiolvasásához
            using (var reader = new StreamReader(cryptoStream))
            {
                return reader.ReadToEnd(); // A visszafejtett szöveg visszaadása
            }
        }
    }
}

