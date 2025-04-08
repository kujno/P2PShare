using System.Security.Cryptography;

namespace P2PShare.Libs
{
    public class SymmetricCryptography
    {
        public static int TagSize { get; } = 16;

        public static byte[] Encrypt(byte[] data, byte[] key, byte[] nonce)
        {
            AesGcm aes;
            byte[] encryptedData;
            byte[] tag = new byte[TagSize];

            do
            {
                aes = new (key, tag.Length);
            }
            while (aes.TagSizeInBytes is null);

            encryptedData = new byte[data.Length + (int)aes.TagSizeInBytes];

            aes.Encrypt(nonce, data, encryptedData, tag);

            Array.Copy(tag, 0, encryptedData, data.Length, tag.Length);

            return encryptedData;
        }

        public static byte[] Decrypt(byte[] data, byte[] key, byte[] nonce)
        {
            AesGcm aes;
            byte[] tag;
            byte[] decryptedData;

            do
            {
                aes = new(key, 16);
            }
            while (aes.TagSizeInBytes is null);

            tag = new byte[(int)aes.TagSizeInBytes];
            decryptedData = new byte[data.Length - tag.Length];

            Array.Copy(data, data.Length - tag.Length, tag, 0, tag.Length);

            aes.Decrypt(nonce, data, tag, decryptedData);

            return decryptedData;
        }
    }
}
