#region License

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

namespace CryptSharp.Core;

public partial class Crypter
{
    static Crypter()
    {
        Blowfish = new();
        TraditionalDes = new();
        ExtendedDes = new();
        Ldap = new(CrypterEnvironment.Default);
        MD5 = new();
        Phpass = new();
        Sha256 = new();
        Sha512 = new();

        var crypters = CrypterEnvironment.Default.Crypters;
        crypters.Add(Blowfish);
        crypters.Add(MD5);
        crypters.Add(Phpass);
        crypters.Add(Sha256);
        crypters.Add(Sha512);
        crypters.Add(Ldap);
        crypters.Add(ExtendedDes);
        crypters.Add(TraditionalDes);
    }

    /// <summary>
    ///     Blowfish crypt, sometimes called BCrypt. A very good choice.
    /// </summary>
    public static BlowfishCrypter Blowfish
    {
        get;
    }

    /// <summary>
    ///     Traditional DES crypt.
    /// </summary>
    public static TraditionalDesCrypter TraditionalDes
    {
        get;
    }

    /// <summary>
    ///     Extended DES crypt.
    /// </summary>
    public static ExtendedDesCrypter ExtendedDes
    {
        get;
    }

    /// <summary>
    ///     LDAP schemes such as {SHA}.
    /// </summary>
    public static LdapCrypter Ldap
    {
        get;
    }

    /// <summary>
    ///     MD5 crypt, supported by nearly all systems. A variant supports Apache htpasswd files.
    /// </summary>
    public static MD5Crypter MD5
    {
        get;
    }

    /// <summary>
    ///     PHPass crypt. Used by WordPress. Variants support phpBB and Drupal 7+.
    /// </summary>
    public static PhpassCrypter Phpass
    {
        get;
    }

    /// <summary>
    ///     SHA256 crypt. A reasonable choice if you cannot use Blowfish crypt for policy reasons.
    /// </summary>
    public static Sha256Crypter Sha256
    {
        get;
    }

    /// <summary>
    ///     SHA512 crypt. A reasonable choice if you cannot use Blowfish crypt for policy reasons.
    /// </summary>
    public static Sha512Crypter Sha512
    {
        get;
    }
}