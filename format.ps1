param(
	[switch] $failOnDiff
)

# format c# code
.\packages\JetBrains.ReSharper.CommandLineTools.2018.3.3\tools\cleanupcode.exe `
	SchemaZen.sln -dsl=GlobalAll -dsl=SolutionPersonal -dsl=ProjectPersonal `
	--exclude="**\*.config"

if ($failOnDiff){
	git diff --exit-code
	if ($LastExitCode -ne 0){
		Write-Error "code formatter produced a diff"
		exit 1;
	}
}

exit 0;
