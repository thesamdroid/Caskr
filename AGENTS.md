# Instructions for Contributors

All changes must ensure the .NET solution builds and the tests pass.

Before committing, run:

```bash
dotnet restore
# build without restoring again
dotnet build --no-restore
# run tests without building again
dotnet test --no-build
```

If any command fails, fix the issues before committing.
