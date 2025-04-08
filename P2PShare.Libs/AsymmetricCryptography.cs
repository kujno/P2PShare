using System.Security.Cryptography;

namespace P2PShare.Libs
{
    public class AsymmetricCryptography
    {
        public static int DwKeySize { get; } = 2048;

        public static RSAParameters[] GenerateKeys()
        {
            RSAParameters[] parameters = new RSAParameters[2];

            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(DwKeySize))
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

            using (RSA rsa = RSA.Create())
            {
                rsa.ImportParameters(key);

                encryptedData = rsa.Encrypt(originalData, RSAEncryptionPadding.Pkcs1);
            }

            return encryptedData;
        }

        public static byte[] Decrypt(byte[] encryptedData, RSAParameters key)
        {
            byte[] decryptedData;
            
            using (RSA rsa = RSA.Create())
            {
                rsa.ImportParameters(key);

                decryptedData = rsa.Decrypt(encryptedData, RSAEncryptionPadding.Pkcs1);
            }

            return decryptedData;
        }

        public static int? GetKeyLength(bool isPrivate, out int modulusLength, out int exponentLength)
        {
            int keyLength;

            modulusLength = 0;
            exponentLength = 0;

            using (RSACryptoServiceProvider rsaCSP = new(DwKeySize))
            {
                RSAParameters rsaParameters = rsaCSP.ExportParameters(isPrivate);

                if (rsaParameters.Modulus is null || rsaParameters.Exponent is null)
                {                    
                    return null;
                }

                modulusLength = rsaParameters.Modulus.Length;
                exponentLength = rsaParameters.Exponent.Length;
                keyLength = modulusLength + exponentLength;
            }

            return keyLength;
        }
    }
}
