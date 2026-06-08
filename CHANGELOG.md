# Changelog

## 0.1.1
- **Hardened `Unfix()` for partial shaping** — mirrors the Python 0.1.4 fix.
  Previously any presentation-form character caused the entire line to be
  reversed, scrambling raw Arabic words alongside shaped ones (OCR artefact /
  mixed legacy source). Now detects per-line whether all Arabic tokens are
  shaped (fully baked → full-line reversal as before) or only some are shaped
  (partially baked → de-shape in place, no reversal). Zero regression on
  existing round-trip behaviour.
- Added `IsArabicCp`, `LineIsFullyShaped`, and `Dechar` private helpers.

## 0.1.0
- Initial release of the .NET / Unity engine.
- `Arabic.Shape` / `Arabic.Fix` / `Arabic.Unfix`, `Options` + `Options.Game` preset, `ContainsArabic` / `IsShaped`.
- Targets netstandard2.0 and netstandard2.1 (Unity-compatible).
- Output validated byte-for-byte against the Python `arabic-rt` package.
