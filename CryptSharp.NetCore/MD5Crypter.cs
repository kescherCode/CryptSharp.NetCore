﻿#region License

/*
CryptSharp
Copyright (c) 2013 James F. Bellinger <http://www.zer7.com/software/cryptsharp>

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
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using CryptSharp.NetCore.Internal;
using CryptSharp.NetCore.Utility;

namespace CryptSharp.NetCore;

// See http://www.vidarholen.net/contents/blog/?p=32 for algorithm details.
// One word of caution: In this post, it says "Pick out the bytes in this order: ...".
// The "starting with the least significant" refers to byte as well as bit, so the
// first character comes from the *last byte* listed.
/// <summary>
///     MD5 crypt, supported by nearly all systems. A variant supports Apache htpasswd files.
/// </summary>
public class MD5Crypter : Crypter
{
    private static readonly Regex _regex = new(Regex, RegexOptions.CultureInvariant);

    private static string Regex =>
        @"\A(?<prefix>\$(apr)?1\$)(?<salt>[^\$]{1,99})(\$(?<crypt>[A-Za-z0-9./]{22}))?\z";

    /// <inheritdoc />
    public override string GenerateSalt(CrypterOptions? options)
    {
        Check.Null("options", options);
        var prefix = options!.GetValue(CrypterOption.Variant, MD5CrypterVariant.Standard) switch
        {
            MD5CrypterVariant.Standard => "$1$",
            MD5CrypterVariant.Apache => "$apr1$",
            _ => throw Exceptions.ArgumentOutOfRange("CrypterOption.Variant", "Unknown variant.")
        };
        return prefix + Base64Encoding.UnixMD5.GetString(Security.GenerateRandomBytes(6));
    }

    /// <inheritdoc />
    public override bool CanCrypt(string salt)
    {
        Check.Null("salt", salt);

        return salt.StartsWith("$1$")
               || salt.StartsWith("$apr1$");
    }

    /// <inheritdoc />
    public override string Crypt(byte[] password, string salt)
    {
        Check.Null("password", password);
        Check.Null("salt", salt);

        var match = _regex.Match(salt);
        if (!match.Success) { throw Exceptions.Argument("salt", "Invalid salt."); }

        byte[]? prefixBytes = null, saltBytes = null, formattedKey = null, truncatedSalt = null, crypt = null;
        try
        {
            var prefixString = match.Groups["prefix"].Value;
            prefixBytes = Encoding.ASCII.GetBytes(prefixString);

            var saltString = match.Groups["salt"].Value;
            saltBytes = Encoding.ASCII.GetBytes(saltString);

            formattedKey = FormatKey(password);
            truncatedSalt = ByteArray.TruncateAndCopy(saltBytes, 8);
            crypt = Crypt(formattedKey, truncatedSalt, prefixBytes, System.Security.Cryptography.MD5.Create());

            var result = prefixString
                         + saltString + '$'
                         + Base64Encoding.UnixMD5.GetString(crypt);
            return result;
        }
        finally
        {
            Security.Clear(prefixBytes);
            Security.Clear(saltBytes);
            Security.Clear(formattedKey);
            Security.Clear(truncatedSalt);
            Security.Clear(crypt);
        }
    }

    private byte[] Crypt(byte[] key, byte[] salt, byte[] prefix, HashAlgorithm A)
    {
        byte[]? H = null, I = null;

        try
        {
            A.Initialize();
            AddToDigest(A, key);
            AddToDigest(A, salt);
            AddToDigest(A, key);
            FinishDigest(A);

            I = (byte[])A.Hash!.Clone();

            A.Initialize();
            AddToDigest(A, key);
            AddToDigest(A, prefix);
            AddToDigest(A, salt);

            AddToDigestRolling(A, I, 0, I.Length, key.Length);

            var length = key.Length;
            for (var i = 0; i < 31 && length != 0; i++)
            {
                AddToDigest(A, new[] { (length & (1 << i)) != 0 ? (byte)0 : key[0] });
                length &= ~(1 << i);
            }

            FinishDigest(A);

            H = (byte[])A.Hash!.Clone();

            for (var i = 0; i < 1000; i++)
            {
                A.Initialize();
                if ((i & 1) != 0) { AddToDigest(A, key); }

                if ((i & 1) == 0) { AddToDigest(A, H); }

                if (i % 3 != 0) { AddToDigest(A, salt); }

                if (i % 7 != 0) { AddToDigest(A, key); }

                if ((i & 1) != 0) { AddToDigest(A, H); }

                if ((i & 1) == 0) { AddToDigest(A, key); }

                FinishDigest(A);

                Array.Copy(A.Hash, H, H.Length);
            }

            var crypt = new byte[H.Length];
            int[] permutation = { 11, 4, 10, 5, 3, 9, 15, 2, 8, 14, 1, 7, 13, 0, 6, 12 };
            Array.Reverse(permutation);
            for (var i = 0; i < crypt.Length; i++)
            {
                crypt[i] = H[permutation[i]];
            }

            return crypt;
        }
        finally
        {
            A.Clear();
            Security.Clear(H);
            Security.Clear(I);
        }
    }

    private void AddToDigest(HashAlgorithm algorithm, byte[] buffer) =>
        AddToDigest(algorithm, buffer, 0, buffer.Length);

    private void AddToDigest(HashAlgorithm algorithm, byte[] buffer, int offset, int count) =>
        algorithm.TransformBlock(buffer, offset, count, buffer, offset);

    private void AddToDigestRolling(HashAlgorithm algorithm, byte[] buffer, int offset, int inputCount, int outputCount)
    {
        int count;
        for (count = 0; count < outputCount; count += inputCount)
        {
            AddToDigest(algorithm, buffer, offset, Math.Min(outputCount - count, inputCount));
        }
    }

    private void FinishDigest(HashAlgorithm algorithm) => algorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

    private byte[] FormatKey(byte[] key)
    {
        var length = ByteArray.NullTerminatedLength(key, key.Length);
        return ByteArray.TruncateAndCopy(key, length);
    }
}