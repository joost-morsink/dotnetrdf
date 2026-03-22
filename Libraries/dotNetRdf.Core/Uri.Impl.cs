/*
// <copyright>
// dotNetRDF is free and open source software licensed under the MIT License
// -------------------------------------------------------------------------
//
// Copyright (c) 2009-2026 dotNetRDF Project (http://dotnetrdf.org/)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is furnished
// to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
*/

using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using VDS.RDF.Parsing.Tokens;
using VDS.RDF.Query.Expressions;

namespace VDS.RDF;

public partial class Uri
{

    internal static readonly Regex REGEX_URI = new Regex(
        """
        ^
        ((?# Scheme)
            (?<Scheme>[a-z][a-z0-9\+\-\.]*):
            (?# HierPart or Opaque URN)
            (?<HierPart>
                (?# Hierarchical with Authority)
                (?:\/\/(?<Authority>
                    (?# UserInfo)
                    (?:(?<UserInfo>
                        (?:\%[0-9a-f][0-9a-f]|[a-z0-9\-\._\~]|[\!\$\&\'\(\)\*\+\,\;\=]|\:)*
                    )\@)?
                    (?# Host)
                    (?<Host>
                        (?# IPv6)
                        (?:\[
                            (?:(?:[0-9a-f]{1,4}:){7}[0-9a-f]{1,4})|
                            (?:(?:[0-9a-f]{1,4}:){1,7}:)|
                            (?:(?:[0-9a-f]{1,4}:){1,6}:[0-9a-f]{1,4})|
                            (?:(?:[0-9a-f]{1,4}:){1,5}(?::[0-9a-f]{1,4}){1,2})|
                            (?:(?:[0-9a-f]{1,4}:){1,4}(?::[0-9a-f]{1,4}){1,3})|
                            (?:(?:[0-9a-f]{1,4}:){1,3}(?::[0-9a-f]{1,4}){1,4})|
                            (?:(?:[0-9a-f]{1,4}:){1,2}(?::[0-9a-f]{1,4}){1,5})|
                            (?:[0-9a-f]{1,4}:(?::[0-9a-f]{1,4}){1,6})|
                            (?::(?:(?::[0-9a-f]{1,4}){1,7}|:))|
                            (?:fe80:(?::[0-9a-f]{0,4}){0,4}%[0-9a-z]+)|
                            (?:::(?:ffff(?::0{1,4})?:)?(?:(?:25[0-5]|(?:2[0-4]|1?[0-9])?[0-9])\.){3}(?:25[0-5]|(?:2[0-4]|1?[0-9])?[0-9]))|
                            (?:(?:[0-9a-f]{1,4}:){1,4}:(?:(?:25[0-5]|(?:2[0-4]|1?[0-9])?[0-9])\.){3}(?:25[0-5]|(?:2[0-4]|1?[0-9])?[0-9]))
                        \])|
                        (?# IPv4 or hostname)
                        (?:(?:(?:[0-9]|[1-9][0-9]|1[0-9][0-9]|2[0-4][0-9]|25[0-5])\.){3,3}(?:[0-9]|[1-9][0-9]|1[0-9][0-9]|2[0-4][0-9]|25[0-5]))|
                        (?:[a-z0-9\-\._\~]|\%[0-9a-f][0-9a-f]|[\!\$\&\'\(\)\*\+\,\;\=])*
                    )
                    (?# Port)
                    (?::(?<Port>[0-9]+))?
                ))
                (?# Path)
                (?<Path>(?:\/(?:[a-z0-9\-\._\~\!\$\&\'\(\)\*\+\,\;\=\:\@]|(?:%[a-f0-9]{2,2}))*)*)
            |
            (?# Opaque URN)
            (?<Path>
            (?:[a-z0-9\-\._\~\!\$\&\'\(\)\*\+\,\;\=\:\@\/]|(?:%[a-f0-9]{2,2}))+)))
        (?# Query)
        (?<Query>\?(?:[a-z0-9\-\._\~\!\$\&\'\(\)\*\+\,\;\=\:\@\/\?]|(?:%[a-f0-9]{2,2}))*)?
        (?# Fragment)
        (?<Fragment>[#](?:[a-z0-9\-\._\~\!\$\&\'\(\)\*\+\,\;\=\:\@\/\?]|(?:%[a-f0-9]{2,2}))*)?
        $
        """,
        RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace
    );

    // NEW: Regex for relative references (network-path, absolute-path, relative-path)
    internal static readonly Regex REGEX_RELATIVE_URI = new Regex(
        """
        ^
        (?# Relative reference per RFC 3986)
        (?:
            (?# Network-path reference)
            (?:\/\/(?<Authority>
                (?# UserInfo)
                (?:(?<UserInfo>(?:\%[0-9a-f][0-9a-f]|[a-z0-9\-\._\~]|[\!\$\&\'\(\)\*\+\,\;\=]|\:)*)\@)?
                (?# Host: allow IP-literal, IPv4 or reg-name simplified)
                (?<Host>
                    (?:\[[^\]]+\])|
                    (?:(?:[0-9]{1,3}\.){3}[0-9]{1,3})|
                    (?:[a-z0-9\-\._\~]|\%[0-9a-f][0-9a-f]|[\!\$\&\'\(\)\*\+\,\;\=])*
                )
                (?# Port)
                (?::(?<Port>[0-9]+))?
            ))
            (?<Path>(?:\/(?:[a-z0-9\-\._\~\!\$\&\'\(\)\*\+\,\;\=\:\@\/]|(?:%[a-f0-9]{2,2}))*)*)
            |
            (?# Absolute-path reference: "/" or "/segment[/seg...]")
            (?<Path>\/(?:[a-z0-9\-\._\~\!\$\&\'\(\)\*\+\,\;\=\:\@\/]|(?:%[a-f0-9]{2,2}))(?:\/(?:[a-z0-9\-\._\~\!\$\&\'\(\)\*\+\,\;\=\:\@]|(?:%[a-f0-9]{2,2}))*)?)
            |
            (?# Relative-path reference: segment *("/" segment)
            (?<Path>(?:[a-z0-9\-\._\~\!\$\&\'\(\)\*\+\,\;\=\:\@\/]|(?:%[a-f0-9]{2,2}))+(?:\/(?:[a-z0-9\-\._\~\!\$\&\'\(\)\*\+\,\;\=\:\@]|(?:%[a-f0-9]{2,2}))*)*)
            |
            (?# Path-empty)
            (?<Path>)
        )
        (?# Query)
        (?<Query>\?(?:[a-z0-9\-\._\~\!\$\&\'\(\)\*\+\,\;\=\:\@\/\?]|(?:%[a-f0-9]{2,2}))*)?
        (?# Fragment)
        (?<Fragment>\#(?:[a-z0-9\-\._\~\!\$\&\'\(\)\*\+\,\;\=\:\@\/\?]|(?:%[a-f0-9]{2,2}))*)?
        $
        """,
        RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace
    );

    internal class UriMatch(Uri parent, short scheme, short hierPart, short userInfo, short host, short port, short path, short end)
    {
        public ReadOnlyMemory<char> Scheme
            => scheme >= 0
                ? parent._baseUri.AsMemory(scheme, hierPart - scheme - 1)
                : ReadOnlyMemory<char>.Empty;

        public ReadOnlyMemory<char> Authority
            => userInfo > 0
                ? parent._baseUri.AsMemory(userInfo, path - userInfo)
                : host > 0
                    ? parent._baseUri.AsMemory(host, path - host)
                    : ReadOnlyMemory<char>.Empty;
        public ReadOnlyMemory<char> UserInfo
            => userInfo > 0
                ? parent._baseUri.AsMemory(userInfo, host - userInfo - 1)
                : ReadOnlyMemory<char>.Empty;

        public ReadOnlyMemory<char> Host
            => host > 0
                ? parent._baseUri.AsMemory(host, port > 0 ? port - host - 1 : path - host)
                : ReadOnlyMemory<char>.Empty;

        public ReadOnlyMemory<char> Port
            => port > 0
                ? parent._baseUri.AsMemory(port, path - port)
                : ReadOnlyMemory<char>.Empty;

        public ReadOnlyMemory<char> Path
            => path > 0
                ? parent._baseUri.AsMemory(path, end - path)
                : ReadOnlyMemory<char>.Empty;

        internal UriMatch WithUri(Uri uri)
            => new UriMatch(uri, scheme, hierPart, userInfo, host, port, path, end);

        public static UriMatch FromFullMatch(bool absolute, Match m)
        {
            // We store the Scheme group index explicitly so we don't infer scheme presence from other offsets.
            return new(null!, Index(m.Groups["Scheme"]), Index(m.Groups["HierPart"]), Index(m.Groups["UserInfo"]), Index(m.Groups["Host"]),
                Index(m.Groups["Port"]), Index(m.Groups["Path"]), Index(m.Groups["Leaf"]));
        }

        private static short Length(Group g) => g.Success ? (short)g.Length : (short)0;

        private static short Index(Group g) => g.Success ? (short)g.Index : (short)-1;

        public static UriMatch FromNewMatch(bool absolute, Match m)
        {
            return new(null!, Index(m.Groups["Scheme"]), Index(m.Groups["HierPart"]), Index(m.Groups["UserInfo"]), Index(m.Groups["Host"]),
                Index(m.Groups["Port"]), Index(m.Groups["Path"]), (short)(Index(m.Groups["Leaf"]) + Length(m.Groups["Leaf"])));
        }
    }

    private bool _isAbsolute;
    private string _baseUri;
    private string _leaf;
    private string _query;
    private string _fragment;
    private UriMatch _uriMatch;
    
    private Uri() { }

    public static Uri CreateNew(string uri, StringInterner interner = null)
    {
        interner ??= StringInterner.Instance;
        var abs = true;
        var match = REGEX_URI.Match(uri);
        if (!match.Success)
        {
            match = REGEX_RELATIVE_URI.Match(uri);
            abs = false;
        }
        if (match.Success)
        {
            var result = new Uri()
            {
                _isAbsolute = abs,
                _baseUri = interner.Intern(uri),
                _leaf = Get(match.Groups["Leaf"]),
                _query = Get(match.Groups["Query"]),
                _fragment = Get(match.Groups["Fragment"]),
                _uriMatch = UriMatch.FromFullMatch(abs, match),
            };
            result._uriMatch = result._uriMatch.WithUri(result);
            return result;
        }
        return null;

        string Get(Group g) => g.Success ? interner.Intern(uri.AsMemory(g.Index, g.Length)) : string.Empty;
    }
}

public class StringInterner
{
    public static StringInterner Instance { get; } = new();
    private ConcurrentDictionary<ReadOnlyMemory<char>, string> _values = new(EqImpl.Instance);

    private class EqImpl : IEqualityComparer<ReadOnlyMemory<char>>
    {
        public static EqImpl Instance { get; } = new();

        public bool Equals(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y)
            => x.Span.SequenceEqual(y.Span);

        public int GetHashCode(ReadOnlyMemory<char> obj)
        {
            unchecked
            {
                var hash = 17;
                foreach (var c in obj.Span)
                    hash = hash * 31 + c;
                return hash;
            }
        }
    }

    public string Intern(ReadOnlyMemory<char> value)
    {
        if (_values.TryGetValue(value, out var str))
        {
            return str;
        }

        var key = new string(value.ToArray());
        return _values.GetOrAdd(key.AsMemory(), key);
    }

    public string Intern(string value)
        => Intern(value.AsMemory());

 
}