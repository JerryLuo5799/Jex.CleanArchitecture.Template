# install.ps1 — Install Jex Clean Architecture Template
$ErrorActionPreference = 'Stop'

$PackageId = "Jex.CleanArchitecture.Template"

Write-Host "Installing $PackageId..."
dotnet new uninstall $PackageId 2>$null
dotnet new install $PackageId
Write-Host "Done. Run 'dotnet new jex -n YourProjectName' to create a new project."
