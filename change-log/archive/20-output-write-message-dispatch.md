### Fixed

- `IOutput.Write` threw `InvalidCastException` for any message whose registered
  writer expects a type a `string` cannot be cast to — writing an `Exception`
  through `Write` rather than `WriteException` always crashed (#20).

### Changed

- **Breaking:** `IMessageFormatterProcessor.WriteMessage` now hands the writer
  the original message and the processor itself, instead of the already-formatted
  text. The writer is selected by the type of the message, so it now receives a
  value of that type and formats it itself (#20).

  A custom `IMessageWriter<TMessage>` that relied on being given pre-formatted
  text must format the message itself, using the `IMessageFormatterProcessor`
  passed to `Write`. As a consequence, `IOutput.Write(anException)` renders the
  full exception through `ExceptionMessageWriter`, and `IOutput.Write(aCollection)`
  writes one line per item rather than one line per character of a formatted
  string.
