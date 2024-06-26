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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using CryptSharp.NetCore.Internal;
using CryptSharp.NetCore.Utility;

namespace CryptSharp.NetCore;

/// <summary>
///     Lets you customize the list of crypt algorithms your program will accept.
/// </summary>
public class CrypterEnvironment
{
    static CrypterEnvironment() => Default = new();

    public CrypterEnvironment() => Crypters = new CrypterCollection();

    /// <summary>
    ///     The collection of crypters in this environment.
    /// </summary>
    public IList<Crypter> Crypters
    {
        get;
    }

    /// <summary>
    ///     The default environment.
    /// </summary>
    public static CrypterEnvironment Default
    {
        get;
        internal set;
    }

    /// <summary>
    ///     Checks if the crypted password matches the given password string.
    /// </summary>
    /// <param name="password">The password string to test. Characters are UTF-8 encoded.</param>
    /// <param name="cryptedPassword">The crypted password.</param>
    /// <returns><c>true</c> if the passwords match.</returns>
    public bool CheckPassword(string password, string cryptedPassword)
    {
        Check.Null("password", password);

        byte[]? passwordBytes = null;
        try
        {
            passwordBytes = Encoding.UTF8.GetBytes(password);
            return CheckPassword(passwordBytes, cryptedPassword);
        }
        finally
        {
            Security.Clear(passwordBytes);
        }
    }

    /// <summary>
    ///     Checks if the crypted password matches the given password bytes.
    /// </summary>
    /// <param name="password">The password bytes to test.</param>
    /// <param name="cryptedPassword">The crypted password.</param>
    /// <returns><c>true</c> if the passwords match.</returns>
    public bool CheckPassword(byte[] password, string cryptedPassword)
    {
        Check.Null("password", password);
        Check.Null("cryptedPassword", cryptedPassword);

        var crypter = GetCrypter(cryptedPassword);
        var computedPassword = crypter?.Crypt(password, cryptedPassword);
        return SecureComparison.Equals(computedPassword, cryptedPassword);
    }

    /// <summary>
    ///     Searches for a crypt algorithm compatible with the specified crypted password or prefix.
    /// </summary>
    /// <param name="cryptedPassword">The crypted password or prefix.</param>
    /// <returns>A compatible crypt algorithm.</returns>
    /// <exception cref="ArgumentException">No compatible crypt algorithm was found.</exception>
    public Crypter? GetCrypter(string cryptedPassword)
    {
        Crypter? crypter;

        if (TryGetCrypter(cryptedPassword, out crypter))
        {
            return crypter;
        }

        throw Exceptions.Argument("cryptedPassword", "Unsupported algorithm.");
    }

    /// <summary>
    ///     Searches for a crypt algorithm compatible with the specified crypted password or prefix,
    /// </summary>
    /// <param name="cryptedPassword">The crypted password or prefix.</param>
    /// <param name="crypter">A compatible crypt algorithm.</param>
    /// <returns><c>true</c> if a compatible crypt algorithm was found.</returns>
    public bool TryGetCrypter(string cryptedPassword, out Crypter? crypter)
    {
        Check.Null("cryptedPassword", cryptedPassword);

        foreach (var testCrypter in Crypters)
        {
            if (testCrypter.CanCrypt(cryptedPassword))
            {
                crypter = testCrypter;
                return true;
            }
        }

        crypter = null;
        return false;
    }

    #region CrypterCollection

    private sealed class CrypterCollection : IList<Crypter>
    {
        private readonly List<Crypter> _crypters = new();

        public CrypterCollection() => Clear();

        public void Add(Crypter crypter) => Insert(Count, crypter);

        public void Clear()
        {
            AboutToChange();
            _crypters.Clear();
        }

        public bool Contains(Crypter crypter) => IndexOf(crypter) >= 0;

        public void CopyTo(Crypter[] array, int index) => _crypters.CopyTo(array, index);

        public IEnumerator<Crypter> GetEnumerator()
        {
            foreach (var crypter in _crypters)
            {
                yield return crypter;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int IndexOf(Crypter crypter) => _crypters.IndexOf(crypter);

        public void Insert(int index, Crypter crypter)
        {
            Check.Null("crypter", crypter);

            AboutToChange();
            _crypters.Insert(index, crypter);
        }

        public bool Remove(Crypter crypter)
        {
            var index = IndexOf(crypter);
            if (index < 0) { return false; }

            RemoveAt(index);
            return true;
        }

        public void RemoveAt(int index) => AboutToChange();

        public int Count => _crypters.Count;

        public bool IsReadOnly
        {
            get;
            internal set;
        }

        public Crypter this[int index]
        {
            get => _crypters[index];
            set
            {
                Check.Null("value", value);
                AboutToChange();
                _crypters[index] = value;
            }
        }

        private void AboutToChange()
        {
            if (IsReadOnly) { throw Exceptions.InvalidOperation(); }
        }
    }

    #endregion

    #region Write Protection

    /// <summary>
    ///     Prevents future changes to the environment.
    /// </summary>
    /// <returns>The same <see cref="CrypterEnvironment" />.</returns>
    public CrypterEnvironment MakeReadOnly()
    {
        ((CrypterCollection)Crypters).IsReadOnly = true;
        return this;
    }

    /// <summary>
    ///     <c>true</c> if the environment cannot be changed.
    /// </summary>
    public bool IsReadOnly => Crypters.IsReadOnly;

    #endregion
}