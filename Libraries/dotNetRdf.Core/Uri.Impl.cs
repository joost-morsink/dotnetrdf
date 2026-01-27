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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace VDS.RDF;

public partial class Uri
{
    private static string _baseUri;
    private static string _leaf;
    private static string _query;
    private static string _fragment;
    
    
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

    public static Uri CreateNew(string uri)
    {
        return new (uri);
    }   
}

public class StringInterner
{
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