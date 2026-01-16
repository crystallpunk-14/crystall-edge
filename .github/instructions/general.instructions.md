---
applyTo: "**"
---

# General Project Notes (Testing, Building, Running)

## Testing

### Running Tests
```bash
dotnet test Content.Tests
dotnet test Content.IntegrationTests
```

### Test Conventions
- Test classes: `*Tests.cs`
- Test methods: descriptive names using `Should` pattern
- Use fixtures from `Robust.UnitTesting`

## Building

### Prerequisites
- .NET SDK (see `global.json` for version)
- Run `RUN_THIS.py` to initialize submodules

### Build Commands
```bash
dotnet build              # Build all projects
dotnet build -c Release   # Build in Release mode
```

### Running
```bash
./runserver.sh   # Start server (Linux/Mac)
./runclient.sh   # Start client (Linux/Mac)
runserver.bat    # Start server (Windows)
runclient.bat    # Start client (Windows)
```

## Additional Resources
- [RobustToolbox Documentation](https://docs.spacestation14.com/)
- [Space Station 14 Development Docs](https://docs.spacestation14.com/en/general-development/setup.html)
- [Discord](https://discord.gg/Sud2DMfhCC)
