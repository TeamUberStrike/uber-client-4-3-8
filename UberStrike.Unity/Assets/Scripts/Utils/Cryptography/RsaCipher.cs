using System;
using System.Security.Cryptography;

namespace Cmune.Util.Ciphers
{
    public class RsaCipher
    {
        const int PROVIDER_RSA_FULL = 1;
        const string CONTAINER_NAME = "CmuneContainer";
        const string PROVIDER_NAME = "Microsoft Strong Cryptographic Provider";

        private RSACryptoServiceProvider provider;

        public RsaCipher (string key, bool useMono)
        {
        	if (useMono)
        		provider = GetMonoProvider ();
        	else
        		provider = GetNetProvider ();
            provider.FromXmlString(key);
        }

        private static RSACryptoServiceProvider GetNetProvider ()
        {
        	CspParameters cspParams;
        	cspParams = new CspParameters (PROVIDER_RSA_FULL);
        	cspParams.KeyContainerName = CONTAINER_NAME;
        	cspParams.Flags = CspProviderFlags.UseMachineKeyStore;
        	cspParams.ProviderName = PROVIDER_NAME;
        	return new RSACryptoServiceProvider (cspParams);
        }
		
		private static RSACryptoServiceProvider GetMonoProvider ()
		{
			CspParameters cspParams;
			cspParams = new CspParameters (PROVIDER_RSA_FULL);
			cspParams.KeyContainerName = CONTAINER_NAME;
			cspParams.Flags = CspProviderFlags.NoFlags;
			cspParams.ProviderName = PROVIDER_NAME;
			return new RSACryptoServiceProvider (cspParams);
		}

        public string EncryptString(string plainText)
        {
            //read plaintext, encrypt it to ciphertext
            byte[] cipherbytes = provider.Encrypt(System.Text.Encoding.UTF8.GetBytes(plainText), false);

            return Convert.ToBase64String(cipherbytes);
        }

        public string DecryptString(string encryptedText)
        {
            //read ciphertext, decrypt it to plaintext
            byte[] plain = provider.Decrypt(Convert.FromBase64String(encryptedText), false);
            return System.Text.Encoding.UTF8.GetString(plain);
        }

        public static void CreatePrivatePublicKeyPair (out string privateKey, out string publicKey, bool useMono)
        {
        	RSACryptoServiceProvider rsa;
			
        	if (useMono)
        		rsa = GetMonoProvider ();
        	else
        		rsa = GetNetProvider ();
			
            privateKey = rsa.ToXmlString(true);
            publicKey = rsa.ToXmlString(false);
        }
    }
}
