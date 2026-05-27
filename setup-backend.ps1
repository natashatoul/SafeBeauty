# SafeBeauty Backend Setup Script
# Creates ASP.NET Core Web API project with Entity Framework Core

# 1. Create solution
dotnet new sln -n SafeBeauty --output backend

# 2. Create ASP.NET Core Web API project
dotnet new webapi -n SafeBeauty.API --output backend/SafeBeauty.API

# 3. Add project to solution
dotnet sln backend/SafeBeauty.sln add backend/SafeBeauty.API/SafeBeauty.API.csproj

# 4. Install NuGet packages
dotnet add backend/SafeBeauty.API package Microsoft.EntityFrameworkCore --version 8.0.15
dotnet add backend/SafeBeauty.API package Microsoft.EntityFrameworkCore.Sqlite --version 8.0.15
dotnet add backend/SafeBeauty.API package Microsoft.EntityFrameworkCore.Tools --version 8.0.15
dotnet add backend/SafeBeauty.API package Microsoft.EntityFrameworkCore.Design --version 8.0.15

# 5. Create architecture folders
New-Item -ItemType Directory -Force backend/SafeBeauty.API/Services
New-Item -ItemType Directory -Force backend/SafeBeauty.API/Models
New-Item -ItemType Directory -Force backend/SafeBeauty.API/Data

Write-Host "Backend setup complete!" -ForegroundColor Green
