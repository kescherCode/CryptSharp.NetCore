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

using System.Text.RegularExpressions;
using CryptSharp.NetCore.Internal;
using CryptSharp.NetCore.Utility;

namespace CryptSharp.NetCore;

/// <summary>
///     Extended DES crypt.
/// </summary>
public class ExtendedDesCrypter : Crypter
{
    private const int MinRounds = 1;
    private const int MaxRounds = (1 << 24) - 1;
    private static readonly Regex _regex = new(Regex, RegexOptions.CultureInvariant);

    private static readonly CrypterOptions _properties = new CrypterOptions
    {
        { CrypterProperty.MinRounds, MinRounds }, { CrypterProperty.MaxRounds, MaxRounds }
    }.MakeReadOnly();

    /// <inheritdoc />
    public override CrypterOptions Properties => _properties;

    private static string Regex =>
        @"\A_(?<rounds>[A-Za-z0-9./]{4})(?<salt>[A-Za-z0-9./]{4})(?<hash>[A-Za-z0-9./]{11})?\z";

    /// <inheritdoc />
    public override string GenerateSalt(CrypterOptions? options)
    {
        Check.Null("options", options);

        var rounds = options!.GetValue<int?>(CrypterOption.Rounds);
        if (rounds is { })
        {
            Check.Range("CrypterOption.Rounds", (int)rounds, MinRounds, MaxRounds);
        }

        var roundsBytes = new byte[3];
        byte[]? saltBytes = null;
        try
        {
            BitPacking.LEBytesFromUInt24((uint)(rounds ?? 4321), roundsBytes, 0);
            saltBytes = Security.GenerateRandomBytes(3);

            return "_"
                   + Base64Encoding.UnixMD5.GetString(roundsBytes)
                   + Base64Encoding.UnixMD5.GetString(saltBytes);
        }
        finally
        {
            Security.Clear(roundsBytes);
            Security.Clear(saltBytes);
        }
    }

    /// <inheritdoc />
    public override bool CanCrypt(string salt)
    {
        Check.Null("salt", salt);

        return _regex.IsMatch(salt);
    }

    /// <inheritdoc />
    public override string Crypt(byte[] password, string salt)
    {
        Check.Null("password", password);
        Check.Null("salt", salt);

        var match = _regex.Match(salt);
        if (!match.Success) { throw Exceptions.Argument("salt", "Invalid salt."); }

        byte[]? roundsBytes = null, saltBytes = null, crypt = null, input = null;
        try
        {
            var roundsString = match.Groups["rounds"].Value;
            roundsBytes = Base64Encoding.UnixMD5.GetBytes(roundsString);
            var roundsValue = (int)BitPacking.UInt24FromLEBytes(roundsBytes, 0);

            var saltString = match.Groups["salt"].Value;
            saltBytes = Base64Encoding.UnixMD5.GetBytes(saltString);
            var saltValue = (int)BitPacking.UInt24FromLEBytes(saltBytes, 0);

            input = new byte[8];
            var length = ByteArray.NullTerminatedLength(password, password.Length);

            for (var m = 0; m < length; m += 8)
            {
                if (m != 0)
                {
                    using var cipher = DesCipher.Create(input);
                    cipher.Encipher(input, 0, input, 0);
                }

                for (var n = 0; n < 8 && n < length - m; n++)
                {
                    // DES Crypt ignores the high bit of every byte.
                    input[n] ^= (byte)(password[m + n] << 1);
                }
            }

            using (var cipher = DesCipher.Create(input))
            {
                crypt = new byte[8];
                cipher.Crypt(crypt, 0, roundsValue, saltValue);
            }

            return "_" + roundsString + saltString + Base64Encoding.UnixCrypt.GetString(crypt);
        }
        finally
        {
            Security.Clear(roundsBytes);
            Security.Clear(saltBytes);
            Security.Clear(crypt);
            Security.Clear(input);
        }
    }
}