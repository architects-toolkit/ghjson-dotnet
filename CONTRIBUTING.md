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
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes
4. Run tests (`dotnet test`)
5. Commit your changes (`git commit -m 'Add amazing feature'`)
6. Push to the branch (`git push origin feature/amazing-feature`)
7. Open a Pull Request

## Code Style

- Follow the existing code style
- Use XML documentation comments for public APIs
- Keep methods focused and small
- Write unit tests for new functionality

## Publishing Releases

### Release Workflow

The release process is automated via GitHub Actions:

1. **Milestone Close** → Creates a release draft
2. **Release Published** → Builds packages and attaches to release
3. **Manual Trigger** → Publishes packages to NuGet

## License

By contributing, you agree that your contributions will be licensed under the Apache-2.0 license.
