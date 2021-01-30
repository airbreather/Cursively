# Cursively Release Notes
## [1.3.0] (https://github.com/airbreather/Cursively/milestone/5)
- Fixed a couple of issues where streams that do not conform to the RFC 4180 rules for quoted fields would be improperly marked as such when the buffers are cut between reads ([#24](https://github.com/airbreather/Cursively/issues/24)).

## [1.2.0](https://github.com/airbreather/Cursively/milestone/4)
- Added fluent helpers to replace the `Csv.ProcessFoo` methods with something that's easier to maintain without being meaningfully less convenient to use ([#15](https://github.com/airbreather/Cursively/issues/15)).
- Deprecated the ability to ignore a leading UTF-8 byte order mark inside the header-aware visitor, per [#14](https://github.com/airbreather/Cursively/issues/14).
    - Instead, it's up to the source of the input to skip (or not skip) sending a leading UTF-8 BOM to the tokenizer in the first place.
    - By default, all the fluent helpers from the previous bullet point will ignore a leading UTF-8 BOM if present.  This behavior may be disabled by chaining `.WithIgnoreUTF8ByteOrderMark(false)`.
- Improved how the header-aware visitor behaves when the creator requests very high limits ([#17](https://github.com/airbreather/Cursively/issues/17)).
- Fixed a rare off-by-one issue in the header-aware visitor that would happen when a header is exactly as long as the configured maximum **and** its last byte is exactly the last byte of the input chunk that happens to contain it ([#16](https://github.com/airbreather/Cursively/issues/16)).

## [1.1.0](https://github.com/airbreather/Cursively/milestone/1)
- Several further performance optimizations.  Most significantly, inlining and tuning a critical `ReadOnlySpan<T>` extension method.
    - In some cases, this increased throughput by a factor of 3.
- Added hooks for visitor implementations to detect situations where the stream does not conform to the RFC 4180 rules for quoted fields ([#4](https://github.com/airbreather/Cursively/issues/4))
- Added support to customize the field delimiter byte ([#11](https://github.com/airbreather/Cursively/issues/11))
- Added helpers to avoid having to use `CsvTokenizer` directly in most cases ([#9](https://github.com/airbreather/Cursively/issues/9), [#10](https://github.com/airbreather/Cursively/issues/10))
- Added an intermediate abstract visitor class that handles UTF-8 encoded headers ([#5](https://github.com/airbreather/Cursively/issues/5))

## 1.0.0
- Initial release.
