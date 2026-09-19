using System.Text.Json;
using Ardalis.Result;
using Ploch.CommandLine.Spectre.SampleApp.Services;
using Ploch.CommandLine.UseCases;

namespace Ploch.CommandLine.Spectre.SampleApp.Commands.Projects.UseCases;

/// <summary>
///     Use case for exporting a project: writes a manifest for the project into the requested directory.
/// </summary>
/// <remarks>
///     <para>
///         The export is guarded against three ways of landing somewhere other than the requested directory:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 <b>A hostile project name.</b> The name becomes a path segment, so a name such as
///                 <c>../outside</c> is rejected, and the resolved parent directory of the manifest is compared with
///                 the export directory before anything is written.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <b>A symbolic link planted at the destination file.</b> The manifest is written to a new, uniquely
///                 named temporary file opened with <see cref="FileMode.CreateNew" /> (<c>O_CREAT|O_EXCL</c>), which
///                 never follows or reuses an existing entry, and is then renamed over the destination. A rename
///                 replaces the directory entry itself, so a link at the destination is replaced rather than followed
///                 and the file it pointed at is left untouched. Re-exporting a project still overwrites its previous
///                 manifest.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <b>A symbolic link (or Windows junction) as the output directory itself.</b> The requested
///                 directory is rejected when it is a link at the time of the export, so a link planted in a shared
///                 location such as <c>/tmp</c> cannot redirect the export into another directory. Only the final
///                 component is checked: <c>--output /tmp/exports</c> works on macOS, where <c>/tmp</c> is a link, but
///                 <c>--output /tmp</c> there is refused because the output directory itself is the link.
///             </description>
///         </item>
///     </list>
///     <para>
///         What the guard does <b>not</b> cover, because .NET exposes no <c>openat</c>/<c>O_NOFOLLOW</c> directory
///         handles to close it: links in the <em>ancestors</em> of the output directory (these are trusted, which is
///         what keeps paths such as macOS's <c>/tmp</c> -&gt; <c>/private/tmp</c> working), and an attacker who owns the
///         output directory itself and swaps it for a link between the check and the write. Nor does it protect the
///         <em>content</em> of the export from a writer who can replace entries in the output directory: the temporary
///         file is closed before it is renamed, so such a writer can substitute it and the rename then publishes their
///         entry as the manifest. That stays inside the output directory - the rename never follows or truncates
///         anything - but it means the output directory itself is assumed to be under the caller's control, even when
///         its parent (such as <c>/tmp</c>) is shared. Within that assumption the guard stops a planted link from
///         redirecting the export or truncating files elsewhere.
///     </para>
/// </remarks>
public class ExportProjectUseCase(IProjectRepository projectRepository) : IResultUseCase<ExportProjectRequest, ExportProjectResponse>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <inheritdoc />
    public async Task<Result<ExportProjectResponse>> ExecuteAsync(ExportProjectRequest request, CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByNameAsync(request.Name, cancellationToken);
        if (project == null)
        {
            return Result<ExportProjectResponse>.NotFound($"Project '{request.Name}' was not found.");
        }

        // The project name reaches this method from a command argument and is about to become a path
        // segment. Left unchecked, a name such as "../outside" turns an export requested for "./exports"
        // into a write to "./outside.json" - outside the directory the caller asked for.
        var fileName = $"{project.Name}.json";
        if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            return Result<ExportProjectResponse>.Invalid(new ValidationError
                                                         {
                                                             Identifier = nameof(request.Name),
                                                             ErrorMessage =
                                                                 $"Project name '{project.Name}' cannot be used as a file name. Names must not contain path separators or other characters that are invalid in a file name."
                                                         });
        }

        // A use case that reports a successful export has to have exported something: the file is
        // written here, and an I/O failure becomes a failed Result rather than a silent success.
        string manifestPath;
        try
        {
            Directory.CreateDirectory(request.OutputPath);

            // Path.Join, not Path.Combine: Combine discards everything before a later rooted segment, so the
            // export directory can vanish from the result. Join concatenates unconditionally.
            var exportDirectory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(request.OutputPath));

            // Path.GetFullPath is purely lexical, so every check below reasons about a string. That is only
            // meaningful while the directory the string names is the directory the write lands in - which a
            // link at the output path itself would silently break.
            if (new DirectoryInfo(exportDirectory).LinkTarget is not null)
            {
                return Result<ExportProjectResponse>.Invalid(new ValidationError
                                                             {
                                                                 Identifier = nameof(request.OutputPath),
                                                                 ErrorMessage =
                                                                     $"Output path '{request.OutputPath}' is a symbolic link or junction. Export to the directory it points at instead."
                                                             });
            }

            var candidatePath = Path.GetFullPath(Path.Join(exportDirectory, fileName));
            if (!IsDirectlyInside(candidatePath, exportDirectory))
            {
                return Result<ExportProjectResponse>.Invalid(new ValidationError
                                                             {
                                                                 Identifier = nameof(request.Name),
                                                                 ErrorMessage =
                                                                     $"Project name '{project.Name}' would write outside the requested export directory."
                                                             });
            }

            manifestPath = candidatePath;

            var manifest = JsonSerializer.Serialize(project, JsonOptions);
            await WriteReplacingEntryAsync(exportDirectory, manifestPath, manifest, cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            return Result<ExportProjectResponse>.Error($"Could not write the export to '{request.OutputPath}': {exception.Message}");
        }

        return Result<ExportProjectResponse>.Success(new(project.Name, manifestPath, 1, DateTime.UtcNow));
    }

    /// <summary>
    ///     Checks the outcome rather than the input, so containment survives the name guard being weakened or
    ///     bypassed by a platform quirk - <see cref="Path.GetInvalidFileNameChars" /> is far narrower on Unix
    ///     (<c>'\0'</c> and <c>'/'</c> only) than on Windows.
    /// </summary>
    /// <remarks>
    ///     Comparing the resolved parent directory, rather than testing the relative path for a <c>..</c> prefix, is
    ///     deliberate: a project legitimately named <c>..foo</c> yields <c>..foo.json</c>, which sits inside the
    ///     directory yet starts with <c>..</c>. Ordinal is correct because both strings are derived from the export
    ///     directory itself, so their shared prefix is character-identical and no case folding is involved.
    /// </remarks>
    private static bool IsDirectlyInside(string candidatePath, string exportDirectory)
    {
        var containingDirectory = Path.GetDirectoryName(candidatePath);

        return containingDirectory is not null
               && string.Equals(Path.TrimEndingDirectorySeparator(containingDirectory), exportDirectory, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Writes <paramref name="contents" /> to <paramref name="destinationPath" /> without ever opening an existing
    ///     directory entry at the destination.
    /// </summary>
    /// <remarks>
    ///     <see cref="File.WriteAllTextAsync(string, string, CancellationToken)" /> opens with
    ///     <see cref="FileMode.Create" /> (<c>O_CREAT|O_TRUNC</c>), which follows a symbolic link at the destination and
    ///     truncates whatever it points at. Here the content goes to a fresh temporary file created with
    ///     <see cref="FileMode.CreateNew" /> (<c>O_CREAT|O_EXCL</c> on Unix, <c>CREATE_NEW</c> on Windows); both refuse
    ///     any existing entry, a dangling link included, so there is no window in which a planted link is followed.
    ///     The temporary file is then moved over the destination: <c>rename(2)</c> on Unix and
    ///     <c>MoveFileEx(MOVEFILE_REPLACE_EXISTING)</c> on Windows both replace the destination entry itself, so a link
    ///     there is replaced and its target is untouched. The temporary file lives in the same directory, so the move
    ///     is a same-volume rename rather than a copy.
    ///     <para>
    ///         The trade-off of replacing the entry is that an earlier manifest's identity does not survive a re-export:
    ///         hard links to it keep the old content, and custom permissions, ACLs or ownership on it are replaced by
    ///         the defaults for a newly created file. For a generated export that is the right trade.
    ///     </para>
    /// </remarks>
    private static async Task WriteReplacingEntryAsync(string directory, string destinationPath, string contents, CancellationToken cancellationToken)
    {
        // The random segment is short on purpose: prefixing the destination's own name would push a long project
        // name past the 255-character file-name limit before the destination itself reaches it.
        var temporaryPath = Path.Join(directory, $".export-{Path.GetRandomFileName()}.tmp");

        // Only an entry this method created may be deleted on failure: if CreateNew itself fails, whatever sits
        // at the temporary path belongs to someone else.
        var created = false;
        try
        {
            await using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
            {
                created = true;
                await using var writer = new StreamWriter(stream);
                await writer.WriteAsync(contents.AsMemory(), cancellationToken);
            }

            File.Move(temporaryPath, destinationPath, overwrite: true);
        }
        catch when (created)
        {
            TryDelete(temporaryPath);
            throw;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Best-effort clean-up after a failed write: the original exception, rethrown by the caller, is the one
            // worth reporting, so a second failure here is deliberately not allowed to replace it.
        }
    }
}
