Write me an introductory post about my .NET libraries on GitHub:
- Ploch.Common published on https://github.com/mrploch/ploch-common/
- Ploch.Data published on https://github.com/mrploch/ploch-data

Make sure to review the contents of those repositories, the contents of documentation sites:
For Ploch.Common -  https://github.ploch.dev/ploch-common/api/index.html,
For Ploch.Data - https://github.ploch.dev/ploch-data/index.html, https://github.ploch.dev/ploch-data/api/index.html
 
Overview of Ploch.Common libraries:
Utility classes and extension methods for common (and uncommon) tasks.
More details in the repository, for example:
- https://github.com/mrploch/ploch-common/blob/master/src/Common/README.md
- https://github.com/mrploch/ploch-common/blob/master/src/Common/Collections/README.md
Lots of examples can be extracted from unit tests, for example here:
- https://github.com/mrploch/ploch-common/tree/master/src/Common.Tests

Overview of Ploch.Data (https://github.com/mrploch/ploch-data)

This repository contains various projects for working with data in .NET.
It provides following features:
- A common data model for standardization for entities, bringing interfaces for things like
  - a type with `Id`
  - a type with `Title`
  - a type with `Children` and `Parent` properties making it best candidates for storing things like categories or folder structure in DB
  - and many more
- A Generic Repository which is the flagship of this repository. It brings a ready to use, battle-tested generic repository
  - CRUD operations provided out-of-the-box.
  - Helps implementation of CQRS vaarious types of repositories:
    - `IReadRepository`, `IReadRepositoryAsync` - read only repositories for queries
    - `IReadWriteRepository` and `IReadWriteRepositoryAsync` - read / write repositories for commands
  - Finally provides Unit of Work pattern implementation
- Integration Testing library making it super easy to write integration tests, with real database (SQLite In-Memory), which provides full support and handling of all types of queries, including the complex ones.