﻿#region License

/*
CryptSharp
Copyright (c) 2011, 2013 James F. Bellinger <http://www.zer7.com/software/cryptsharp>

Permission to use, copy, modify, and/or distribute this software for any
purpose with or without fee is hereby granted, provided that the above
copyright notice and this permission notice appear in all copies.

THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
*/

#endregion

using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace CryptSharp.NetCore.Demo.CrypterTest;

internal static class TestVectors
{
    public static void Test()
    {
        Console.Write("Testing LDAP");
        TestFile("CryptSharp.NetCore.Demo.Crypter.TestVectors-LDAP.txt", Crypter.Ldap);
        TestRoundtrip(Crypter.Ldap, CrypterOptions.None);
        TestRoundtrip(Crypter.Ldap, new() { { CrypterOption.Variant, LdapCrypterVariant.Cleartext } });
        TestRoundtrip(Crypter.Ldap, new() { { CrypterOption.Variant, LdapCrypterVariant.SSha512 } });
        TestRoundtrip(Crypter.Ldap, new() { { CrypterOption.Variant, LdapCrypterVariant.SSha384 } });
        TestRoundtrip(Crypter.Ldap, new() { { CrypterOption.Variant, LdapCrypterVariant.SSha256 } });
        TestRoundtrip(Crypter.Ldap, new() { { CrypterOption.Variant, LdapCrypterVariant.SMD5 } });
        TestRoundtrip(Crypter.Ldap, new() { { CrypterOption.Variant, LdapCrypterVariant.Sha512 } });
        TestRoundtrip(Crypter.Ldap, new() { { CrypterOption.Variant, LdapCrypterVariant.Sha384 } });
        TestRoundtrip(Crypter.Ldap, new() { { CrypterOption.Variant, LdapCrypterVariant.Sha256 } });
        TestRoundtrip(Crypter.Ldap, new() { { CrypterOption.Variant, LdapCrypterVariant.Sha } });
        TestRoundtrip(Crypter.Ldap, new() { { CrypterOption.Variant, LdapCrypterVariant.MD5 } });
        TestRoundtrip(Crypter.Ldap,
            new()
            {
                { CrypterOption.Variant, LdapCrypterVariant.Crypt }, { LdapCrypterOption.Crypter, Crypter.Blowfish }
            });
        TestRoundtrip(Crypter.Ldap,
            new()
            {
                { CrypterOption.Variant, LdapCrypterVariant.Crypt },
                { LdapCrypterOption.Crypter, Crypter.ExtendedDes },
                { LdapCrypterOption.CrypterOptions, new CrypterOptions { { CrypterOption.Rounds, 12345 } } }
            });
        TestEnd();

        Console.Write("Testing BCrypt");
        TestFile("CryptSharp.NetCore.Demo.Crypter.TestVectors-BCrypt.txt", Crypter.Blowfish);
        TestRoundtrip(Crypter.Blowfish, CrypterOptions.None);
        TestRoundtrip(Crypter.Blowfish, new() { { CrypterOption.Variant, BlowfishCrypterVariant.Compatible } });
        TestRoundtrip(Crypter.Blowfish, new() { { CrypterOption.Variant, BlowfishCrypterVariant.Corrected } });
        TestEnd();

        Console.Write("Testing Traditional DES");
        TestFile("CryptSharp.NetCore.Demo.Crypter.TestVectors-DES.txt", Crypter.TraditionalDes);
        TestRoundtrip(Crypter.TraditionalDes, CrypterOptions.None);
        TestEnd();

        Console.Write("Testing Extended DES");
        TestFile("CryptSharp.NetCore.Demo.Crypter.TestVectors-ExtendedDES.txt", Crypter.ExtendedDes);
        TestRoundtrip(Crypter.ExtendedDes, CrypterOptions.None);
        TestEnd();

        Console.Write("Testing MD5");
        TestFile("CryptSharp.NetCore.Demo.Crypter.TestVectors-MD5.txt", Crypter.MD5);
        TestRoundtrip(Crypter.MD5, CrypterOptions.None);
        TestEnd();

        Console.Write("Testing MD5-Apache");
        TestPassword(Encoding.ASCII.GetBytes("HelloWorld"), "$apr1$DsThrrHW$FKgfcgTz5xN0QDgHpizQ//", Crypter.MD5);
        TestPassword(Encoding.ASCII.GetBytes("HiEarth"), "$apr1$eXz.uqW9$ey6k6fGA6sHUlfu4TFvhf0", Crypter.MD5);
        TestRoundtrip(Crypter.MD5, new() { { CrypterOption.Variant, MD5CrypterVariant.Apache } });
        TestEnd();

        Console.Write("Testing PHPass");
        TestFile("CryptSharp.NetCore.Demo.Crypter.TestVectors-PHPass.txt", Crypter.Phpass);
        TestPassword(Encoding.ASCII.GetBytes("admin123"), "$S$DlagvsBQGWTktiD4cAA2IHTLFzQw7pLuH4427TAq9QxK2b3xtJBT",
            Crypter.Phpass);
        TestPassword(Encoding.ASCII.GetBytes("123456"), "$S$DT7pDUXAjEVfBOF6EE8vZz/wxoAuy1VcmMj44J66Y8ZuQUytJX9.",
            Crypter.Phpass);
        TestRoundtrip(Crypter.Phpass, CrypterOptions.None);
        TestRoundtrip(Crypter.Phpass, new() { { CrypterOption.Variant, PhpassCrypterVariant.Phpbb } });
        TestRoundtrip(Crypter.Phpass, new() { { CrypterOption.Variant, PhpassCrypterVariant.Drupal } });
        TestEnd();

        Console.Write("Testing SHA256");
        TestFile("CryptSharp.NetCore.Demo.Crypter.TestVectors-SHA256.txt", Crypter.Sha256);
        TestRoundtrip(Crypter.Sha256, CrypterOptions.None);
        TestEnd();

        Console.Write("Testing SHA512");
        TestFile("CryptSharp.NetCore.Demo.Crypter.TestVectors-SHA512.txt", Crypter.Sha512);
        TestRoundtrip(Crypter.Sha512, CrypterOptions.None);
        TestEnd();
    }

    private static void TestFile(string filename, Crypter crypter)
    {
        using var stream = Assembly.GetEntryAssembly()!.GetManifestResourceStream(filename)!;

        // Run test vectors.
        using StreamReader reader = new(stream);
        var startTime = Environment.TickCount;

        string line;
        var count = 0;
        while ((line = reader.ReadLine()!) is { })
        {
            var parts = line.Split(new[] { ',' }, 2);
            if (parts.Length != 2) { continue; }

            // NOTE: You may have noticed that the BCrypt test vectors have some
            //       passwords that are longer than 72 bytes, despite BCrypt
            //       only using the first 72 bytes of a password. Don't worry --
            //       this is testing to ensure truncation works correctly.
            var password = Convert.FromBase64String(parts[0]);
            var refCrypt = parts[1];

            TestPassword(password, refCrypt, crypter);
            count++;
        }

        Console.Write("...({0} ms for {1} vectors)...", Environment.TickCount - startTime, count);
    }

    private static void TestRoundtrip(Crypter crypter, CrypterOptions options)
    {
        var rng = RandomNumberGenerator.Create();

        for (var i = 0; i < 25; i++)
        {
            var length = new byte[1];
            rng.GetBytes(length);

            var password = new byte[length[0]];
            rng.GetBytes(password);

            var refCrypt = crypter.Crypt(password, options);
            TestPassword(password, refCrypt, crypter);
        }
    }

    private static void TestPassword(byte[] password, string refCrypt, Crypter crypter)
    {
        Console.Write(".");

        if (!Crypter.CheckPassword(password, refCrypt))
        {
            var testCrypt = crypter.Crypt(password, refCrypt);
            Console.WriteLine("WARNING: Crypt failed\n  {0}, instead of\n  {1}", testCrypt, refCrypt);
        }
    }

    private static void TestEnd() => Console.WriteLine("done.");
}