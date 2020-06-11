# intended to be run from the root dir of the solution

param(
    [switch] $modified,
    [switch] $staged
)

# make sure ReGitLint is present
nuget install regitlint -version 1.5.0 -outputDirectory packages

if (!$modified -and !$staged) {
    packages/ReGitLint.1.5.0/tools/ReGitLint.exe -s SchemaZen.sln
}
if ($modified) {
    packages/ReGitLint.1.5.0/tools/ReGitLint.exe -s SchemaZen.sln -f Modified -d
}
if ($staged) {
    packages/ReGitLint.1.5.0/tools/ReGitLint.exe -s SchemaZen.sln -f Staged -d
}

