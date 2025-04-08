using System.Security.Cryptography;

namespace P2PShare.Libs
{
    class AsymmetricCryptography
    {
        public static RSAParameters[] GenerateKeys()
        {
            RSAParameters[] parameters = new RSAParameters[2];

            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048))
            {
                // public key
                parameters[0] = rsa.ExportParameters(false);
                //private key
                parameters[1] = rsa.ExportParameters(true);
            }

            return parameters;
        }

        public static byte[] Encrypt(byte[] originalData, RSAParameters key)
        {
            byte[] encryptedData;

            using (RSA rsa = RSA.Create(key))
            {
                encryptedData = rsa.Encrypt(originalData, RSAEncryptionPadding.OaepSHA256);
            }

            return encryptedData;
        }

        public static byte[] Decrypt(byte[] encryptedData, RSAParameters key)
        {
            byte[] decryptedData;
            
            using (RSA rsa = RSA.Create(key))
            {
                decryptedData = rsa.Decrypt(encryptedData, RSAEncryptionPadding.OaepSHA256);
            }
            
            return decryptedData;
        }
    }
}
