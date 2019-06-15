# Cursively Release Notes

## [1.1.0](https://github.com/airbreather/Cursively/milestone/1)
- Several further performance optimizations.  Most significantly, inlining and tuning a critical `ReadOnlySpan<T>` extension method.
    - In some cases, this increased throughput by a factor of 3.
- Added hooks for visitor implementations to detect situations where the stream does not conform to the RFC 4180 rules for quoted fields ([#4](https://github.com/airbreather/Cursively/issues/4))
- Added support to customize the field delimiter byte ([#11](https://github.com/airbreather/Cursively/issues/11))
- Added helpers to avoid having to use `CsvTokenizer` directly in most cases ([#9](https://github.com/airbreather/Cursively/issues/9), [#10](https://github.com/airbreather/Cursively/issues/10))
- Added an intermediate abstract visitor class that handles UTF-8 encoded headers ([#5](https://github.com/airbreather/Cursively/issues/5))

## 1.0.0
- Initial release.
