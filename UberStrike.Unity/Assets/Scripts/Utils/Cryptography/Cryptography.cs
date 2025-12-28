using System.Security.Cryptography;
using System.Text;
using Cmune.Realtime.Photon.Client;
using Cmune.Util.Ciphers;

public class CryptographyPolicy : ICryptographyPolicy
{
    public string SHA256Encrypt(string inputString)
    {
        UTF8Encoding ue = new UTF8Encoding();
        byte[] bytes = ue.GetBytes(inputString);
        SHA256Managed encSHA256 = new SHA256Managed();
        byte[] hashBytes = encSHA256.ComputeHash(bytes);
        string hashString = "";
        for (int i = 0; i < hashBytes.Length; i++)
        {
            hashString += System.Convert.ToString(hashBytes[i], 16).PadLeft(2, '0');
        }
        return hashString.PadLeft(32, '0');
    }

    public byte[] RijndaelEncrypt(byte[] inputClearText, string passPhrase, string initVector)
    {
        // Before encrypting data, we will append plain text to a random
        // salt value, which will be between 4 and 8 bytes long (implicitly
        // used defaults).
        RijndaelCipher rijndaelKey = new RijndaelCipher(passPhrase, initVector);
        return rijndaelKey.EncryptToBytes(inputClearText);
    }

    public byte[] RijndaelDecrypt(byte[] inputCipherText, string passPhrase, string initVector)
    {
        // Before encrypting data, we will append plain text to a random
        // salt value, which will be between 4 and 8 bytes long (implicitly
        // used defaults).
        RijndaelCipher rijndaelKey = new RijndaelCipher(passPhrase, initVector);
        return rijndaelKey.DecryptToBytes(inputCipherText);
    }
}