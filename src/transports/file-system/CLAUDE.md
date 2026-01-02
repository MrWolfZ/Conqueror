# FileSystem Transport

File-based transport for testing, development, and simple local IPC scenarios.

**Warning**: Not intended for production use.

## Packages

- `Conqueror.Transport.FileSystem.Abstractions` - Core FileSystem interfaces
- `Conqueror.Transport.FileSystem` - FileSystem implementation

## Purpose

Provides a polling-based file transport useful for:

- Local testing without network dependencies
- Simple IPC between processes on the same machine
- Development and debugging
- Understanding transport concepts

## Building and Testing

YOU MUST use task commands for building and testing:

```bash
cd src/transports/file-system
task build
task test
```

Forward arguments: `task test -- --filter "FullyQualifiedName~FileSystem"`

**IMPORTANT:** DO NOT use `dotnet build` or `dotnet test` directly. Use the task commands.

## Architecture Fit

Follows the standard Conqueror transport pattern:

- **Marker interfaces**: `IFileSystemMessage<TMessage, TResponse>`, `IFileSystemSignal<TSignal>`
- **Attributes**: `[FileSystemMessage(Tag = "...", Version = "...")]` for source generator detection
- **Sender/Publisher**: Write message/signal files and poll for responses
- **Receiver**: Poll directory for new files, process them, and write response files

## Key Concepts

### Tag and Version

Messages and signals are identified by a tag (defaults to dasherized type name) and optional version. These determine the subdirectory where files are stored.

### Inbox Pattern

The transport uses an "inbox" pattern for ordered message processing:

- Messages are written to an inbox file (a fixed-width text file)
- Each entry has a sequence number, ID, tag ID, state (available/leased), and lease expiration
- Receivers poll the inbox for available entries
- Single-instance mode: No leasing, guaranteed ordering
- Multi-instance mode: Leasing prevents concurrent processing, enables horizontal scaling

### Time To Live

Messages can have a TTL configured on the sender. Expired messages are ignored by receivers.

### Receiver Modes

**Single Instance**: One receiver processes messages sequentially. No coordination overhead.

**Multiple Competing Instances**: Multiple receivers compete for messages via file-based leasing. Failed processing releases the lease for retry.

## Non-Obvious Design Decisions

### Fixed-Width Text Format

Inbox files use fixed-width text entries for efficient seeking and in-place updates. This allows atomic state transitions (available → leased) without rewriting the entire file.

### Polling with Append Notifications

Receivers primarily poll the file system, but use in-process events to eagerly check for new messages when the sender and receiver are in the same process, reducing latency.

### Tag ID Compression

Tag strings are mapped to fixed-width numeric IDs to reduce inbox file size and improve parsing performance.

## Limitations

- File system latency affects performance
- Polling-based (not truly event-driven)
- Requires shared file system for cross-process communication
- Not suitable for high-throughput scenarios
- No built-in persistence guarantees (depends on file system)
