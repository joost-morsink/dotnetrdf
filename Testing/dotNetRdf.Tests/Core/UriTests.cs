/*
dotNetRDF is free and open source software licensed under the MIT License

-----------------------------------------------------------------------------

Copyright (c) 2009-2012 dotNetRDF Project (dotnetrdf-developer@lists.sf.net)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is furnished
to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using FluentAssertions;
using System;
using System.Data;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace VDS.RDF;


public class UriTests
{
    [Fact]
    public void UriAbsoluteUriWithQuerystring()
    {
        var u = new Uri("http://example.org/?test");
        Assert.Equal("http://example.org/?test", u.AbsoluteUri);
    }

    [Fact]
    public void UriAbsoluteUriWithFragment()
    {
        var u = new Uri("http://example.org/#test");
        Assert.Equal("http://example.org/#test", u.AbsoluteUri);
    }

    [Theory]
    [InlineData("http://example.org/path?query=value", "http", "example.org", "", "example.org", "", "/", "path","?query=value")]
    [InlineData("http://example.org/path#fragment", "http", "example.org", "", "example.org", "", "/", "path", "", "#fragment")]
    [InlineData("http://example.org/path?query=value#fragment", "http", "example.org", "", "example.org", "", "/", "path", "?query=value", "#fragment")]
    [InlineData("http://example.org/path/to/resource","http", "example.org", "", "example.org", "", "/path/to/", "resource")]
    [InlineData("http://example.org/path/to/resource/","http", "example.org", "", "example.org", "", "/path/to/resource/")]
    [InlineData("http://example.org:8080", "http", "example.org:8080", "","example.org", "8080")]
    [InlineData("http://example.org:8080/", "http", "example.org:8080", "","example.org", "8080","/")]
    [InlineData("http://usr:pwd@example.org:8080", "http", "usr:pwd@example.org:8080", "usr:pwd","example.org", "8080")]
    [InlineData("urn:example:abc:123", "urn", "", "", "", "", "","example:abc:123")]
    [InlineData("urn:example:abc?query=value", "urn", "", "", "", "", "","example:abc", "?query=value")]
    [InlineData("urn:example:abc#fragment", "urn", "", "", "", "", "","example:abc", "", "#fragment")]
    [InlineData("urn:example:abc:123?query=value#fragment", "urn", "", "", "", "", "", "example:abc:123",  "?query=value", "#fragment")]
    [InlineData("urn:example:path/to/resource", "urn", "", "", "", "", "example:path/to/", "resource")]
    public void CanParseAbsoluteUri(string uri, string scheme, string authority, string userInfo, string host, string port="", string path="", string leaf="", string query="", string fragment="")
    {
        var u = Uri.REGEX_URI.Match(uri);
        u.Success.Should().BeTrue();
        u.Groups["Scheme"].Value.Should().Be(scheme);
        u.Groups["Authority"].Value.Should().Be(authority);
        u.Groups["UserInfo"].Value.Should().Be(userInfo);
        u.Groups["Host"].Value.Should().Be(host);
        u.Groups["Port"].Value.Should().Be(port);
        u.Groups["Path"].Value.Should().Be(path);
        u.Groups["Leaf"].Value.Should().Be(leaf);
        u.Groups["Query"].Value.Should().Be(query);
        u.Groups["Fragment"].Value.Should().Be(fragment);
    }

    [Theory]
    [InlineData("abc/def/", "", "", "", "", "abc/def/")]
    [InlineData("//example.org/abc/def", "example.org", "", "example.org", "", "/abc/", "def")]
    [InlineData("//example.org", "example.org", "", "example.org", "")]
    [InlineData("//example.org/", "example.org", "", "example.org", "", "/")]
    [InlineData("//example.org:8080/abc/def", "example.org:8080", "", "example.org", "8080", "/abc/", "def")]
    [InlineData("/abc/def", "", "", "", "", "/abc/", "def")]
    [InlineData("abc/def?query=value", "", "", "", "", "abc/", "def","?query=value")]
    [InlineData("?query=value", "", "", "", "", "", "", "?query=value")]
    [InlineData("?query=value#fragment", "", "", "", "", "", "", "?query=value", "#fragment")]
    [InlineData("#fragment", "", "", "", "", "", "", "","#fragment")]
    [InlineData("//usr:pwd@example.org:8080/abc/def?query=value#fragment", "usr:pwd@example.org:8080", "usr:pwd", "example.org", "8080", "/abc/", "def", "?query=value", "#fragment")]
    public void CanParseRelativeUri(string uri, string authority, string userInfo, string host, string port = "", string path = "",  string leaf = "", string query = "",
        string fragment = "")
    {
        var u = Uri.REGEX_RELATIVE_URI.Match(uri);
        u.Success.Should().BeTrue();
        u.Groups["Authority"].Value.Should().Be(authority);
        u.Groups["UserInfo"].Value.Should().Be(userInfo);
        u.Groups["Host"].Value.Should().Be(host);
        u.Groups["Port"].Value.Should().Be(port);
        u.Groups["Path"].Value.Should().Be(path);
        u.Groups["Query"].Value.Should().Be(query);
        u.Groups["Fragment"].Value.Should().Be(fragment);
    }

    [Theory]
    [InlineData("http://example.org/abc/def", "http", "example.org", "", "example.org", "","/abc/")]
    [InlineData("http://user:pwd@example.org:8080/abc/def", "http", "user:pwd@example.org:8080", "user:pwd", "example.org", "8080","/abc/")]
    [InlineData("http://user:pwd@example.org:8080/abc/def?query=value#fragment", "http", "user:pwd@example.org:8080", "user:pwd", "example.org", "8080","/abc/")]
    [InlineData("x://user:pwd@example.org:8080/abc/def?query=value#fragment", "x", "user:pwd@example.org:8080", "user:pwd", "example.org", "8080","/abc/")]
    [InlineData("urn:test:case:123", "urn", "", "", "", "","")]

    public void UriMatchFullTest(string uri, string scheme, string authority, string userInfo, string host, string port = "", string path="")
    {
        var m = Uri.REGEX_URI.Match(uri);
        var u = Uri.CreateNew(uri);
        var match = Uri.UriMatch.FromFullMatch(true, m).WithUri(u);
        m.Success.Should().BeTrue();
        match.Scheme.ToString().Should().Be(scheme);
        match.Authority.ToString().Should().Be(authority);
        match.UserInfo.ToString().Should().Be(userInfo);
        match.Host.ToString().Should().Be(host);
        match.Port.ToString().Should().Be(port);
        match.Path.ToString().Should().Be(path);
    }

    [Fact]
    public void Test()
    {
        var uri = "http://user:pwd@example.org:8080/abc/def?q=value#frag";
        var test = System.Uri.TryCreate(uri, UriKind.Absolute, out _);
        var m = Uri.REGEX_URI.Match(uri);
        var u = Uri.CreateNew(uri);
        var match = Uri.UriMatch.FromFullMatch(true, m).WithUri(u);
        var sb = new StringBuilder();
        sb.Append(match.Scheme);
        sb.ToString().Should().Be("http");
        $"{match.Scheme}://{match.Authority}{match.Path}".Should().Be("http://user:pwd@example.org:8080/abc/");
        var sysuri = new System.Uri(uri);

    }
    
}
