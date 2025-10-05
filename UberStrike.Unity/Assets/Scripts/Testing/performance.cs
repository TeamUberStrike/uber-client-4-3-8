//using UnityEngine;
//using System.Collections;
//using System.Diagnostics;
//using Cmune.Realtime.Common.Security;
//using System;
//using System.Runtime.InteropServices;
//using System.IO;
//using Cmune.Util;
//using Cmune.Util.Ciphers;

//public class performance : MonoBehaviour
//{
//    void Start3()
//    {
//        byte[] array = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0 };
//        MemoryStream stream = new MemoryStream(array);
//        const string pp = "s2a542av1a5d21";
//        const string iv = "C15netodx9234d67";

//        //RijndaelCipher c = new RijndaelCipher(pp, iv);
//        //var t1 = c.AddSalt(array);
//        //var t2 = c.AddSalt(stream);
//        //UnityEngine.Debug.Log("Salt Array: " + CmunePrint.Values(t1));
//        //UnityEngine.Debug.Log("Salt Stream: " + CmunePrint.Values(t2.ToArray()));

//        var encrypted1 = Cryptography.RijndaelEncrypt(array, pp, iv);
//        UnityEngine.Debug.Log("Source: " + CmunePrint.Values(encrypted1));
//        var decrypted1 = Cryptography.RijndaelDecrypt(encrypted1, pp, iv);
//        UnityEngine.Debug.Log("Dec Array: " + CmunePrint.Values(decrypted1));
//        var decrypted2 = Cryptography.RijndaelDecrypt(new MemoryStream(encrypted1), pp, iv);
//        UnityEngine.Debug.Log("Dec Stream: " + CmunePrint.Values(decrypted2.ToArray()));

//        var encrypted2 = Cryptography.RijndaelEncrypt(stream, pp, iv);
//        UnityEngine.Debug.Log("Source: " + CmunePrint.Values(encrypted2.ToArray()));
//        var decrypted3 = Cryptography.RijndaelDecrypt(encrypted2.ToArray(), pp, iv);
//        UnityEngine.Debug.Log("Enc Array: " + CmunePrint.Values(decrypted3));
//        var decrypted4 = Cryptography.RijndaelDecrypt(encrypted2, pp, iv);
//        UnityEngine.Debug.Log("Enc Stream: " + CmunePrint.Values(decrypted4.ToArray()));
//    }

//    void Start()
//    {
//        byte[] array = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0 };
//        MemoryStream stream = new MemoryStream(array);
//        const int counter = 1000;
//        const string pp = "s2a542av1a5d21";
//        const string iv = "C15netodx9234d67";
//        Stopwatch watch;

//        var encryptedArray = Cryptography.RijndaelEncrypt(array, pp, iv);
//        var encryptedStream = new MemoryStream(encryptedArray);

//        watch = Stopwatch.StartNew();
//        for (int i = 0; i < counter; i++)
//        {
//            //var encrypted = Cryptography.RijndaelEncrypt(array, pp, iv);
//            var decrypted = Cryptography.RijndaelDecrypt(encryptedArray, pp, iv);
//        }
//        watch.Stop();
//        UnityEngine.Debug.Log("Array: " + watch.ElapsedTicks);

//        watch = Stopwatch.StartNew();
//        for (int i = 0; i < counter; i++)
//        {
//            //var encrypted = Cryptography.RijndaelEncrypt(stream, pp, iv);
//            var decrypted = Cryptography.RijndaelDecrypt(encryptedStream, pp, iv);
//        }
//        watch.Stop();
//        UnityEngine.Debug.Log("Stream: " + watch.ElapsedTicks);
//    }

//    // Use this for initialization
//    void Start1()
//    {
//        const int counter = 10000;
//        Stopwatch watch;

//        SecureMemory<int> secure = new SecureMemory<int>(123);
//        int normal = 123;
//        FastSecureInteger dynamicSecure = new FastSecureInteger(123);

//        watch = Stopwatch.StartNew();
//        for (int i = 0; i < counter; i++)
//        {
//            normal++;
//            int k = normal;
//        }
//        watch.Stop();
//        UnityEngine.Debug.Log("Normal: " + watch.ElapsedTicks);
//        double compare = watch.ElapsedTicks;

//        watch = Stopwatch.StartNew();
//        for (int i = 0; i < counter; i++)
//        {
//            secure.WriteData(i);
//            int k = secure.ReadData(true);
//        }
//        watch.Stop();
//        UnityEngine.Debug.Log("Secure: " + watch.ElapsedTicks + " " + watch.ElapsedTicks / compare);

//        watch = Stopwatch.StartNew();
//        for (int i = 0; i < counter; i++)
//        {
//            dynamicSecure.Increment(1);
//            int k = dynamicSecure.Value;
//        }
//        watch.Stop();
//        UnityEngine.Debug.Log("Flexible: " + watch.ElapsedTicks + " " + watch.ElapsedTicks / compare);

//        watch = Stopwatch.StartNew();
//        for (int i = 0; i < counter; i++)
//        {
//            string s = i.ToString();
//            int k = int.Parse(s);
//        }
//        watch.Stop();
//        UnityEngine.Debug.Log("String: " + watch.ElapsedTicks + " " + watch.ElapsedTicks / compare);

//        watch = Stopwatch.StartNew();
//        for (int i = 0; i < counter; i++)
//        {
//            string s = Convert.ToBase64String(BitConverter.GetBytes(i));
//            int k = BitConverter.ToInt32(Convert.FromBase64String(s), 0);
//        }
//        watch.Stop();
//        UnityEngine.Debug.Log("Base64: " + watch.ElapsedTicks + " " + watch.ElapsedTicks / compare);
//    }

//    void OnGUI()
//    {
//        if (GUI.Button(new Rect(100, 100, 200, 20), "Test A"))
//        {
//            test.Value = UnityEngine.Random.Range(0, 1000);
//        }

//        if (GUI.Button(new Rect(100, 130, 200, 20), "Test B"))
//        {
//            test.Increment(UnityEngine.Random.Range(0, 1000));
//        }
//        GUI.Label(new Rect(100, 160, 200, 20), test.ToString());
//    }

//    FastSecureInteger test = new FastSecureInteger(123);
//}