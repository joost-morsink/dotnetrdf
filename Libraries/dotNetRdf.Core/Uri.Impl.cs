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
    private readonly System.Uri _base;
    private readonly string _leaf;
    private readonly string _query;
    private readonly string _fragment;

    private Uri(System.Uri @base, string leaf, string query, string fragment)
    {
        _base = @base;
        _leaf = leaf;
        _query = query;
        _fragment = fragment;
    }
    
    public static implicit operator Uri(System.Uri uri)
    {
        var bas = new System.Uri(uri, ".");
        var absPath = uri.AbsolutePath;
        return new Uri(bas, absPath.Substring(absPath.IndexOf('/') + 1), uri.Query, uri.Fragment);
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