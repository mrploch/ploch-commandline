// Copyright (c) Nate McMaster.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.ExceptionServices;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Hosting;

namespace Ploch.CommandLine.Hosting.Internal;

/// <summary>
///     Waits from completion of the <see cref="CommandLineApplication" /> and
///     initiates shutdown.
/// </summary>
internal class CommandLineLifetime : IHostLifetime, IDisposable
{
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly ICommandLineService _cliService;
    private readonly IConsole _console;
    private readonly IUnhandledExceptionHandler? _unhandledExceptionHandler;

    /// <summary>
    ///     Creates a new instance.
    /// </summary>
    public CommandLineLifetime(IHostApplicationLifetime applicationLifetime,
                               ICommandLineService cliService,
                               IConsole console,
                               IUnhandledExceptionHandler? unhandledExceptionHandler = null)
    {
        _applicationLifetime = applicationLifetime;
        _cliService = cliService;
        _console = console;
        _unhandledExceptionHandler = unhandledExceptionHandler;
    }

    public void Dispose()
    { }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Registers an <code>ApplicationStarted</code> hook that runs the
    ///     <see cref="ICommandLineService" />. This ensures the container and all
    ///     hosted services are started before the
    ///     <see cref="CommandLineApplication" /> is run.  After the
    ///     <code>ICliService</code> completes, the <code>ExitCode</code> is
    ///     recorded and the application is stopped.
    /// </summary>
    /// <param name="cancellationToken">Used to indicate when stop should no longer be graceful.</param>
    /// <returns></returns>
    /// <seealso cref="IHostLifetime.WaitForStartAsync(CancellationToken)" />
    public Task WaitForStartAsync(CancellationToken cancellationToken)
    {
        _applicationLifetime.ApplicationStarted.Register(async () =>
                                                         {
                                                             try
                                                             {
                                                                 ExitCode = await _cliService.RunAsync(cancellationToken).ConfigureAwait(false);
                                                             }
                                                             catch (Exception e)
                                                             {
                                                                 if (_unhandledExceptionHandler != null)
                                                                 {
                                                                     _unhandledExceptionHandler.HandleException(e);
                                                                 }
                                                                 else
                                                                 {
                                                                     ExceptionDispatchInfo.Capture(e).Throw();
                                                                 }
                                                             }
                                                             finally
                                                             {
                                                                 _applicationLifetime.StopApplication();
                                                             }
                                                         });

        // Capture CTRL+C and prevent it from immediately force killing the app.
        _console.CancelKeyPress += (_, e) =>
                                   {
                                       e.Cancel = true;
                                       _applicationLifetime.StopApplication();
                                   };

        return Task.CompletedTask;
    }

    /// <summary>The exit code returned by the command line application</summary>
    public int ExitCode { get; private set; }
}