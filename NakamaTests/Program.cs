using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NakamaTests
{
    [AttributeUsage(AttributeTargets.Method)]
    sealed class TestAttribute : Attribute { }

    sealed class AssertException : Exception
    {
        public AssertException(string m) : base(m) { }
    }

    static class A
    {
        public static void True(bool c, string what)
        {
            if (!c) throw new AssertException(what);
        }

        public static void Eq<T>(T want, T got, string what)
        {
            if (!EqualityComparer<T>.Default.Equals(want, got))
                throw new AssertException(what + ": want <" + want + "> got <" + got + ">");
        }

        public static void Bytes(byte[] want, byte[] got, string what)
        {
            if (want == null || got == null || !want.SequenceEqual(got))
                throw new AssertException(what + ": want " + Hex(want) + " got " + Hex(got));
        }

        public static void Throws<TE>(Action a, string what) where TE : Exception
        {
            try { a(); }
            catch (TE) { return; }
            catch (Exception e) { throw new AssertException(what + ": threw " + e.GetType().Name + " not " + typeof(TE).Name); }
            throw new AssertException(what + ": did not throw");
        }

        public static string Hex(byte[] b)
        {
            return b == null ? "null" : BitConverter.ToString(b).Replace("-", "").ToLowerInvariant();
        }

        public static byte[] FromHex(string h)
        {
            var b = new byte[h.Length / 2];
            for (int i = 0; i < b.Length; i++)
                b[i] = Convert.ToByte(h.Substring(i * 2, 2), 16);
            return b;
        }
    }

    static class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 2 && args[0] == "--emit")
                return Contract.Emit(args[1]);
            if (args.Length == 2 && args[0] == "--verify")
            {
                try { return Contract.Verify(args[1]); }
                catch (Exception e) { Console.WriteLine("FAIL contract: " + e.Message); return 1; }
            }

            string filter = args.Length > 0 ? args[0] : null;
            var tests = typeof(Program).Assembly.GetTypes()
                .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                .Where(m => m.GetCustomAttribute<TestAttribute>() != null)
                .Where(m => filter == null || (m.DeclaringType.Name + "." + m.Name).IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(m => m.DeclaringType.Name).ThenBy(m => m.Name)
                .ToList();

            int pass = 0, fail = 0;
            foreach (MethodInfo m in tests)
            {
                string name = m.DeclaringType.Name + "." + m.Name;
                try
                {
                    m.Invoke(null, null);
                    pass++;
                    Console.WriteLine("PASS " + name);
                }
                catch (TargetInvocationException e)
                {
                    fail++;
                    Exception x = e.InnerException ?? e;
                    Console.WriteLine("FAIL " + name + ": " + (x is AssertException ? x.Message : x.ToString()));
                }
            }
            Console.WriteLine();
            Console.WriteLine(pass + " passed, " + fail + " failed, " + tests.Count + " total");
            return fail == 0 && tests.Count > 0 ? 0 : 1;
        }
    }
}
