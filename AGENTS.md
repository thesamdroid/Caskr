# Instructions for Contributors

All changes must ensure the .NET solution builds and **all tests run and pass**.

Before committing, run:

```bash
dotnet restore
# build without restoring again
dotnet build --no-restore
# run server tests without building again
dotnet test --no-build
# run client tests (requires Google Chrome)
npm --prefix caskr.client test
```

If any command fails, fix the issues before committing.
