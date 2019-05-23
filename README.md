# Cursively
A fast, [RFC 4180](https://tools.ietf.org/html/rfc4180)-conforming CSV reading library for .NET.  Written in C#.

Fully supports all UTF-8 encoded byte streams.
- Other encodings will work as well, as long as the bytes `0x0A`, `0x0D`, `0x22`, and `0x2C` are all guaranteed to mean the same thing that they mean in ASCII / UTF-8, and as long as the encoding defines no other byte sequences which identify the Unicode code points for `'\n'`, `'\r'`, `'"'`, or `','`, respectively.
- In practice, this means that most "Extended ASCII" code pages will probably work, probably including all SBCS.  Many "Extended ASCII" DBCS will probably work too, but it looks like Shift-JIS will *not* work.
- Notably, this library will fail to yield the correct result when used with byte streams encoded in any variant of UTF-16 or UTF-32, even with a BOM header.  If you require that support, there are other libraries that should work for you.

Fully supports all streams that completely conform to the RFC 4180 format, and defines rules for how to handle streams that break certain rules of RFC 4180 in a way that seems to be consistent with other popular tools, at a minor speed penalty.
