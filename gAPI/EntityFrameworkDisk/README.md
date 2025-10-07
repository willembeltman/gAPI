# gAPI.EntityFrameworkDisk

A high-performance, file-based database engine for .NET Standard 2.0, designed as a full replacement for Entity Framework — but without a traditional RDBMS.
Instead of connecting to SQL Server, PostgreSQL, or other database engines, gAPI.EntityFrameworkDisk stores your data directly as optimized binary files on disk.

It uses custom serializers, dedicated index files, and an advanced locking system to achieve performance and behavior comparable to (and in some cases faster than) Entity Framework.

## Core Concepts

### 1. DataSet<T> — The disk-based database core

- Stores entity data in binary format on disk, accompanied by a primary key index file.
- The index maps primary keys directly to data locations for fast retrieval.
- Implements IQueryable, with intelligent detection of primary key queries for instant index-based lookups.
- Internally powered by IndexSet<T> and EntityDefinition<T> for efficient data management.
- Built-in concurrency control through a custom locking mechanism.

### 2. DbSet<T> / DbContext — In-memory shadow database

- Extends DataSet<T> by keeping a shadow copy of all data in memory for faster access.
- Supports navigation properties via:
  - ILazy<T> for foreign key references (lazy-loaded).
  - ICollection<T> for foreign key collections.
  - Fully compatible with [ForeignKey] and [NotMapped] attributes.
  - Behaves almost identically to Entity Framework’s DbSet, including LINQ query support.

### Advanced Serialization & Comparison

- gAPI.AutoSerializer — Runtime-generated, ultra-fast binary serializers for entities.
- gAPI.AutoComparer — Runtime-generated comparison modules for high-speed dirty state detection.
- Both are optimized for minimal overhead, making them suitable for large datasets and high-throughput scenarios.

### EF-like Developer Experience

- Familiar SaveChanges pattern with full change tracking.
- Supports overriding SaveChanges to implement custom persistence logic.
- Integrates seamlessly with LINQ queries, entity navigation, and data annotations.
- No external database dependency — everything is self-contained in your application’s data folder.

### Typical Use Cases

- Embeddable databases in desktop or server applications.
- Storage engines for specialized data-heavy services without needing a SQL server.
- Scenarios where deployment simplicity and performance are both critical.