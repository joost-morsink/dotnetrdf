/*
// <copyright>
// dotNetRDF is free and open source software licensed under the MIT License
// -------------------------------------------------------------------------
//
// Copyright (c) 2009-2025 dotNetRDF Project (http://dotnetrdf.org/)
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
using System.Runtime.Serialization;

namespace VDS.RDF;

public class Uri
{
    private readonly System.Uri inner;
    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        ((ISerializable) inner).GetObjectData(info, context);
    }

    public string GetComponents(UriComponents components, UriFormat format)
    {
        return inner.GetComponents(components, format);
    }

    public string GetLeftPart(UriPartial part)
    {
        return inner.GetLeftPart(part);
    }

    public bool IsBaseOf(System.Uri uri)
    {
        return inner.IsBaseOf(uri);
    }

    public bool IsWellFormedOriginalString()
    {
        return inner.IsWellFormedOriginalString();
    }

    public string MakeRelative(System.Uri toUri)
    {
        return inner.MakeRelative(toUri);
    }

    public System.Uri MakeRelativeUri(System.Uri uri)
    {
        return inner.MakeRelativeUri(uri);
    }

    public string AbsolutePath => inner.AbsolutePath;

    public string AbsoluteUri => inner.AbsoluteUri;

    public string Authority => inner.Authority;

    public string DnsSafeHost => inner.DnsSafeHost;

    public string Fragment => inner.Fragment;

    public string Host => inner.Host;

    public UriHostNameType HostNameType => inner.HostNameType;

    public string IdnHost => inner.IdnHost;

    public bool IsAbsoluteUri => inner.IsAbsoluteUri;

    public bool IsDefaultPort => inner.IsDefaultPort;

    public bool IsFile => inner.IsFile;

    public bool IsLoopback => inner.IsLoopback;

    public bool IsUnc => inner.IsUnc;

    public string LocalPath => inner.LocalPath;

    public string OriginalString => inner.OriginalString;

    public string PathAndQuery => inner.PathAndQuery;

    public int Port => inner.Port;

    public string Query => inner.Query;

    public string Scheme => inner.Scheme;

    public string[] Segments => inner.Segments;

    public bool UserEscaped => inner.UserEscaped;

    public string UserInfo => inner.UserInfo;

    private Uri(System.Uri inner)
    {
        this.inner = inner;
    }

    public Uri(string uri)
        : this(new System.Uri(uri))
    {
    }

    public Uri(Uri baseUri, Uri relativeUri)
        : this(new System.Uri(baseUri, relativeUri))
    {
    }

    public Uri(Uri baseUri, string relativeUri)
        : this(new System.Uri(baseUri, relativeUri))
    {
    }
    
    public Uri(string uri, UriKind uriKind)
        : this(new System.Uri(uri, uriKind))
    {
    }

    public override string ToString()
        => inner.ToString();

    public static implicit operator System.Uri(Uri uri)
        => uri.inner;

    public static implicit operator Uri(System.Uri inner)
        => new(inner);

    
    public static string EscapeUriString(string value) => System.Uri.EscapeUriString(value);
    public static string EscapeDataString(string value) => System.Uri.EscapeDataString(value);
    public static string UnescapeDataString(string value) => System.Uri.UnescapeDataString(value);
    public static bool IsHexEncoding(string str, int index) => System.Uri.IsHexEncoding(str, index);
    public static bool IsWellFormedUriString(string str, UriKind uriKind) => System.Uri.IsWellFormedUriString(str, uriKind);

    public static bool TryCreate(string str, UriKind uriKind, out Uri result)
    {
        if (System.Uri.TryCreate(str, uriKind, out var innerResult))
        {
            result = innerResult;
            return true;
        }

        result = null;
        return false;
    }

    public static string HexEscape(char x) => System.Uri.HexEscape(x);
}