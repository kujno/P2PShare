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
