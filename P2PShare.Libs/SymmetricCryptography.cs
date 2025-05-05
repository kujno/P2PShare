using System.Security.Cryptography;

namespace P2PShare.Libs
{
    public class SymmetricCryptography
    {
        public static int TagSize { get; } = 16;

        public static byte[] Encrypt(byte[] data, byte[] key, byte[] nonce)
        {
            AesGcm aes;
            byte[] cipherText = new byte[data.Length];
            byte[] tag = new byte[TagSize];

            aes = new(key, tag.Length);

            aes.Encrypt(nonce, data, cipherText, tag);

            return cipherText.Concat(tag).ToArray();
        }

        public static byte[] Decrypt(byte[] dataWithTag, byte[] key, byte[] nonce)
        {
            byte[] tag = new byte[TagSize];
            byte[] data = new byte[dataWithTag.Length - TagSize];
            byte[] decryptedData = new byte[data.Length];

            Array.Copy(dataWithTag, 0, data, 0, data.Length);
            Array.Copy(dataWithTag, data.Length, tag, 0, TagSize);

            using (AesGcm aes = new(key, TagSize))
            {
                aes.Decrypt(nonce, data, tag, decryptedData);
            }

            return decryptedData;
        }
    }
}
