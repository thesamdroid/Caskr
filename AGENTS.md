# Instructions for Contributors

All changes must ensure the .NET solution builds and **all tests run and pass**.

Run this branch through the testing protocol. Make sure all objects are loaded
into the execution environments to appropriately run the tasks. If any tests
fail, make sure they get fixed. If they fail because of missing elements in the
environment, bring those elements into the environment.

Each pull request must include at least one new automated test that exercises the
behaviour introduced or modified by the change.

Before finishing a task, verify that every project builds successfully and that
the full test suite completes without failures.

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
