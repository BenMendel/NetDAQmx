Publish a new NetDAQmx NuGet version. Run the script that bumps the csproj version, commits, tags, packs Release/x86, pushes to RUTester_Feed, and creates a GitHub release.

Steps:
1. Ask the user for the new version string if not provided (e.g. `2.0.0-beta4`).
2. Confirm all pending changes are already committed — the script only commits the version bump.
3. Tell the user to run in their terminal:
   ```
   .\scripts\Publish-NuGet.ps1 -Version <version>
   ```
   The script must be run by the user (not via the Bash/PowerShell tool) because NuGet push to RUTester_Feed requires interactive Windows credential context.
4. Do NOT use `dotnet build` or reference any nupkg under `bin\`. The script uses `dotnet pack -c Release` which always recompiles from source and writes to `artifacts\`.
