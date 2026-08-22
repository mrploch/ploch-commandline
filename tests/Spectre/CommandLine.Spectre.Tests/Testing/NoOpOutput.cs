using Ploch.CommandLine.Spectre.Output;
using Spectre.Console.Rendering;

namespace Ploch.CommandLine.Spectre.Tests.Testing;

/// <summary>
///     An <see cref="IOutput" /> that discards everything, so a test does not write to the console. Every member is
///     virtual so a test can override just the call it needs to observe, except <see cref="WriteException{TException}" />:
///     its generic constraint makes it awkward to override, so it forwards to the non-generic
///     <see cref="OnException" /> hook instead.
/// </summary>
internal class NoOpOutput : IOutput
{
    public virtual IOutput EndLine() => this;

    public virtual IOutput MarkupInterpolated(FormattableString value) => this;

    public virtual IOutput MarkupLineInterpolated(FormattableString value) => this;

    public virtual IOutput Write<TMessage>(TMessage message, IFormatProvider? format = null) => this;

    public virtual IOutput Write(IRenderable renderable) => this;

    public virtual IOutput WriteBold<TMessage>(TMessage? message) => this;

    public virtual IOutput WriteBoldLine<TMessage>(TMessage? message) => this;

    public virtual IOutput WriteError<TMessage>(TMessage? message) => this;

    public virtual IOutput WriteErrorLine<TMessage>(TMessage? message) => this;

    public IOutput WriteException<TException>(TException? exception) where TException : Exception
    {
        OnException(exception);

        return this;
    }

    public virtual IOutput WriteLine() => this;

    public virtual IOutput WriteLine<TMessage>(TMessage message) => this;

    /// <summary>Called for every exception written; overridden by tests that need to observe the call.</summary>
    protected virtual void OnException(Exception? exception)
    {
        // Discarded by default.
    }
}
