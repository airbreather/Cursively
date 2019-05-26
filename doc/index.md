# Cursively
A fast, [RFC 4180](https://tools.ietf.org/html/rfc4180)-conforming CSV reading library for .NET.  Written in C#.

Fully supports all UTF-8 encoded byte streams.
- Other encodings will work as well, as long as the bytes `0x0A`, `0x0D`, `0x22`, and `0x2C` are all guaranteed to mean the same thing that they mean in ASCII / UTF-8, and as long as the encoding defines no other byte sequences which identify the Unicode code points for `'\n'`, `'\r'`, `'"'`, or `','`, respectively.
- In practice, this means that most "Extended ASCII" code pages will probably work, probably including all SBCS.  Many "Extended ASCII" DBCS will probably work too, but it looks like Shift-JIS will *not* work.
- Notably, this library will fail to yield the correct result when used with byte streams encoded in any variant of UTF-16 or UTF-32, even with a BOM header.  If you require that support, there are other libraries that should work for you.

Fully supports all streams that completely conform to the RFC 4180 format, and defines rules for how to handle streams that break certain rules of RFC 4180 in a way that seems to be consistent with other popular tools, at a minor speed penalty.

This library exists because the original developer was unsatisfied with the performance characteristics of raw CSV processing tools.  Everything out there seemed to have some combination of these flaws:
1. Tons of managed heap allocations on hot paths, often baked into the API requirements
1. Decoding to UTF-16LE **before** scanning for critical bytes, which could be considered a subset of:
1. The design forces a ton of processing to happen on the input which the caller might not even care about
1. Omitting important parts of RFC 4180
1. Disappointing options for mitigating DDoS risk

"RFC 4180 over UTF-8" is a very simple byte stream format, and the state machine requires only a few extra states to define how to handle all UTF-8 streams that are non-RFC 4180, so it seemed odd that there wasn't a reader without these flaws.

With Cursively,
1. each stream only strictly requires a grand total of two objects to be allocated on the managed heap*,
    - *in case this is too much, both could be reset and put into a pool to be reused for processing other streams
1. processing happens directly on the input bytes (no decoding is done by Cursively itself),
1. the only processing that Cursively necessarily does is the bare minimum needed to describe the data to the caller,
1. inputs that conform to RFC 4180* are processed according to all the rules of RFC 4180, and
    - *inputs that do not conform to RFC 4180 are handled according to consistent, intuitive rules
1. there is a very low risk* of DDoS directly from using Cursively, and the caller has the tools that they need in order to prevent (or respond to) attacks in a more "natural" way than other CSV libraries that the developer has seen.
    - *There is no such thing as "risk-free" in our world.  Cursively itself cannot eliminate the risk of attacks that use it as a vector to exploit defects in CoreFX / C# compiler / runtime / OS / hardware.

Future enhancements may add support for byte streams in other encodings if there's demand for it, but not at the expense of anything that matters to the "RFC 4180 over UTF-8" use case.
