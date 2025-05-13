﻿using System.Security.Cryptography;

namespace P2PShare.Libs
{
    public class EncryptionSymmetrical
    {
        public int TagSize { get; }
        public int NonceSize { get; }
        private byte[] _key;
        private byte[]? _oldNonce;

        public EncryptionSymmetrical(byte[] key)
        {
            TagSize = 16;
            NonceSize = 12;
            _key = key;
        }

        public EncryptionSymmetrical()
        {
            TagSize = 16;
            NonceSize = 12;
            _key = Array.Empty<byte>();
        }

        public byte[] Encrypt(byte[] data)
        {
            AesGcm aes;
            byte[] cipherText = new byte[data.Length];
            byte[] tag = new byte[TagSize];
            byte[] nonce = new byte[NonceSize];

            do
            {
                RandomNumberGenerator.Fill(nonce);
            }
            while (nonce == _oldNonce);

            aes = new(_key, TagSize);

            aes.Encrypt(nonce, data, cipherText, tag);

            _oldNonce = nonce;

            return cipherText.Concat(tag).Concat(nonce).ToArray();
        }

        public byte[]? Decrypt(byte[] data)
        {
            byte[] tag = new byte[TagSize];
            byte[] cleanData = new byte[data.Length - TagSize - NonceSize];
            byte[] decryptedData = new byte[cleanData.Length];
            byte[] nonce = new byte[NonceSize];

            Array.Copy(data, 0, cleanData, 0, cleanData.Length);
            Array.Copy(data, cleanData.Length, tag, 0, TagSize);
            Array.Copy(data, cleanData.Length + TagSize, nonce, 0, NonceSize);

            using (AesGcm aes = new(_key, TagSize))
            {
                try
                {
                    aes.Decrypt(nonce, cleanData, tag, decryptedData);
                }
                catch
                {
                    return null;
                }
            }

            return decryptedData;
        }
    }
}
