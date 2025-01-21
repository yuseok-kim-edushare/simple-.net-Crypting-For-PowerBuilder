# Download nuget.exe if not exists
if (-not (Test-Path "nuget.exe")) {
    Invoke-WebRequest -Uri "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe" -OutFile "nuget.exe"
}

# Restore packages
.\nuget.exe restore simple-.net-Crypting-For-PowerBuilder.sln -ConfigFile nuget.config

# Copy packages to project directories
Copy-Item -Path "packages\*" -Destination "net481PB\packages\" -Recurse -Force
Copy-Item -Path "packages\*" -Destination "net481SQL-server\packages\" -Recurse -Force 