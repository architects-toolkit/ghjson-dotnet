# Contributing to GhJSON.NET

Thank you for your interest in contributing to GhJSON.NET!

## Development Setup

1. Clone the repository:

   ```bash
   git clone https://github.com/architects-toolkit/ghjson-dotnet.git
   cd ghjson-dotnet
   ```

2. Restore dependencies:

   ```bash
   dotnet restore
   ```

3. Build the solution:

   ```bash
   dotnet build
   ```

4. Run tests:

   ```bash
   dotnet test
   ```

## Project Structure

```
ghjson-dotnet/
├── src/
│   ├── GhJSON.Core/                      # Platform-independent models & operations
│   └── GhJSON.Grasshopper/              # Grasshopper integration
├── tests/
│   ├── GhJSON.Core.Tests/               # Core unit tests
│   ├── GhJSON.Grasshopper.Tests/        # Grasshopper integration tests
│   └── GhJSON.Grasshopper.TestComponents/ # Test components for GH tests
├── tools/                                # Build and utility scripts
├── .github/
│   └── workflows/                        # CI/CD workflows
└── docs/                                 # Documentation
```

## Pull Request Process

1. Fork the repository
2. Create a topic branch from `main` (`git checkout -b feature/amazing-feature`)
3. Make your changes
4. Run tests (`dotnet test`)
5. Commit your changes using Conventional Commit style (`git commit -m 'feat: add amazing feature'`)
6. Push to the branch (`git push origin feature/amazing-feature`)
7. Open a Pull Request **targeting `main`**

All pull requests target `main` — there is no `dev` branch. Fixes for a
specific released line target its `release/X.Y` stabilization branch instead.
See [Branching and Release Workflow](docs/RELEASE_WORKFLOW.md) for details.

## Code Style

- Follow the existing code style
- Use XML documentation comments for public APIs
- Keep methods focused and small
- Write unit tests for new functionality

## Publishing Releases

### Release Workflow

The release process is automated via GitHub Actions:

1. **Release 1 - Prepare Release** (manual) → opens a `release-prep/<version>` PR
2. **Release 2 - Tag on Merge** → creates the bare version tag and a draft GitHub Release
3. **Release Published** → `release-3-build.yml` builds packages and attaches them to the release
4. **Publish to NuGet** (manual) → pushes packages to nuget.org via trusted publishing

See [docs/RELEASE_WORKFLOW.md](docs/RELEASE_WORKFLOW.md) for the full branch
model, stabilization lines, and hotfix flow.

## License

By contributing, you agree that your contributions will be licensed under the Apache-2.0 license.
