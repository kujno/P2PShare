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
            AesGcm aes;
            byte[] tag, decryptedData;
            byte[] data = new byte[dataWithTag.Length - TagSize];

            Array.Copy(dataWithTag, 0, data, 0, dataWithTag.Length - TagSize);

            do
            {
                aes = new(key, TagSize);
            }
            while (aes.TagSizeInBytes is null);

            tag = new byte[TagSize];
            decryptedData = new byte[data.Length];

            Array.Copy(dataWithTag, dataWithTag.Length - TagSize, tag, 0, TagSize);

            aes.Decrypt(nonce, data, tag, decryptedData);

            return decryptedData;
        }
    }
}
