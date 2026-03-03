#!/usr/bin/env bash
# install.sh — Install Jex Clean Architecture Template
set -e

PACKAGE_ID="Jex.CleanArchitecture.Template"

echo "Installing $PACKAGE_ID..."
dotnet new uninstall "$PACKAGE_ID" 2>/dev/null || true
dotnet new install "$PACKAGE_ID"
echo "Done. Run 'dotnet new jex -n YourProjectName' to create a new project."
