#region License

/*
CryptSharp
Copyright (c) 2010, 2013 James F. Bellinger <http://www.zer7.com/software/cryptsharp>

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

namespace CryptSharp.NetCore.Internal;

internal static class Security
{
    public static void Clear(Array? array)
    {
        if (array is { }) { Array.Clear(array, 0, array.Length); }
    }

    public static byte[] GenerateRandomBytes(int count)
    {
        Check.Range("count", count, 0, int.MaxValue);

        var rng = RandomNumberGenerator.Create();
        var bytes = new byte[count];
        rng.GetBytes(bytes);
        return bytes;
    }
}