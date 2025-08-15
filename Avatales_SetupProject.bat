@echo off
echo ========================================
echo    Avatales Backend Setup
echo    Modularer Monolith + DDD + CQRS
echo ========================================

:: Hauptprojekt erstellen
dotnet new sln -n Avatales
cd Avatales

:: API Projekt (Presentation Layer)
dotnet new webapi -n Avatales.API --use-controllers
dotnet sln add Avatales.API

:: Application Layer (Use Cases & Services)
dotnet new classlib -n Avatales.Application
dotnet sln add Avatales.Application

:: Domain Layer (Entities & Business Logic)
dotnet new classlib -n Avatales.Domain
dotnet sln add Avatales.Domain

:: Infrastructure Layer (Data Access & External Services)
dotnet new classlib -n Avatales.Infrastructure
dotnet sln add Avatales.Infrastructure

:: Shared Kernel (Cross-cutting Concerns)
dotnet new classlib -n Avatales.Shared
dotnet sln add Avatales.Shared

:: Tests Projekte
dotnet new classlib -n Avatales.UnitTests
dotnet new classlib -n Avatales.IntegrationTests
dotnet sln add Avatales.UnitTests
dotnet sln add Avatales.IntegrationTests

:: Projektabhängigkeiten einrichten
cd Avatales.API
dotnet add reference ../Avatales.Application
dotnet add reference ../Avatales.Infrastructure
dotnet add reference ../Avatales.Shared

cd ../Avatales.Application
dotnet add reference ../Avatales.Domain
dotnet add reference ../Avatales.Shared

cd ../Avatales.Infrastructure
dotnet add reference ../Avatales.Domain
dotnet add reference ../Avatales.Application
dotnet add reference ../Avatales.Shared

cd ../Avatales.Domain
dotnet add reference ../Avatales.Shared

cd ../Avatales.UnitTests
dotnet add reference ../Avatales.API
dotnet add reference ../Avatales.Application
dotnet add reference ../Avatales.Domain

cd ../Avatales.IntegrationTests
dotnet add reference ../Avatales.API
dotnet add reference ../Avatales.Infrastructure

:: Zurück zum Root-Verzeichnis
cd ..

:: NuGet-Pakete hinzufügen
echo.
echo Installing NuGet packages...

:: API Pakete
cd Avatales.API
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Microsoft.AspNetCore.OpenApi
dotnet add package Swashbuckle.AspNetCore
dotnet add package Carter
dotnet add package FluentValidation.AspNetCore
dotnet add package MediatR
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File

:: Application Pakete
cd ../Avatales.Application
dotnet add package MediatR
dotnet add package FluentValidation
dotnet add package AutoMapper
dotnet add package Microsoft.Extensions.DependencyInjection.Abstractions
dotnet add package Microsoft.Extensions.Logging.Abstractions

:: Infrastructure Pakete
cd ../Avatales.Infrastructure
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package StackExchange.Redis
dotnet add package Microsoft.Extensions.Http
dotnet add package Polly
dotnet add package MassTransit
dotnet add package MassTransit.RabbitMQ

:: Test Pakete
cd ../Avatales.UnitTests
dotnet add package Microsoft.NET.Test.Sdk
dotnet add package xunit
dotnet add package xunit.runner.visualstudio
dotnet add package Moq
dotnet add package FluentAssertions
dotnet add package Microsoft.AspNetCore.Mvc.Testing

cd ../Avatales.IntegrationTests
dotnet add package Microsoft.NET.Test.Sdk
dotnet add package xunit
dotnet add package xunit.runner.visualstudio
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Testcontainers

cd ..

echo.
echo ========================================
echo    Verzeichnisstruktur wird erstellt...
echo ========================================

:: API Verzeichnisse
mkdir "Avatales.API\Controllers\Users"
mkdir "Avatales.API\Controllers\Characters"
mkdir "Avatales.API\Controllers\Stories"
mkdir "Avatales.API\Controllers\Social"
mkdir "Avatales.API\Controllers\Learning"
mkdir "Avatales.API\Middleware"
mkdir "Avatales.API\Configuration"
mkdir "Avatales.API\Extensions"
mkdir "Avatales.API\Filters"

:: Application Verzeichnisse
mkdir "Avatales.Application\Users\Commands"
mkdir "Avatales.Application\Users\Queries"
mkdir "Avatales.Application\Users\DTOs"
mkdir "Avatales.Application\Users\Validators"
mkdir "Avatales.Application\Users\Handlers"

mkdir "Avatales.Application\Characters\Commands"
mkdir "Avatales.Application\Characters\Queries"
mkdir "Avatales.Application\Characters\DTOs"
mkdir "Avatales.Application\Characters\Validators"
mkdir "Avatales.Application\Characters\Handlers"

mkdir "Avatales.Application\Stories\Commands"
mkdir "Avatales.Application\Stories\Queries"
mkdir "Avatales.Application\Stories\DTOs"
mkdir "Avatales.Application\Stories\Validators"
mkdir "Avatales.Application\Stories\Handlers"

mkdir "Avatales.Application\Social\Commands"
mkdir "Avatales.Application\Social\Queries"
mkdir "Avatales.Application\Social\DTOs"
mkdir "Avatales.Application\Social\Handlers"

mkdir "Avatales.Application\Learning\Commands"
mkdir "Avatales.Application\Learning\Queries"
mkdir "Avatales.Application\Learning\DTOs"
mkdir "Avatales.Application\Learning\Handlers"

mkdir "Avatales.Application\Memory\Commands"
mkdir "Avatales.Application\Memory\Queries"
mkdir "Avatales.Application\Memory\DTOs"
mkdir "Avatales.Application\Memory\Handlers"

mkdir "Avatales.Application\Common\Interfaces"
mkdir "Avatales.Application\Common\Services"
mkdir "Avatales.Application\Common\Behaviors"
mkdir "Avatales.Application\Common\Mappings"
mkdir "Avatales.Application\Common\Exceptions"

:: Domain Verzeichnisse
mkdir "Avatales.Domain\Users\Entities"
mkdir "Avatales.Domain\Users\ValueObjects"
mkdir "Avatales.Domain\Users\Events"
mkdir "Avatales.Domain\Users\Specifications"

mkdir "Avatales.Domain\Characters\Entities"
mkdir "Avatales.Domain\Characters\ValueObjects"
mkdir "Avatales.Domain\Characters\Events"
mkdir "Avatales.Domain\Characters\Specifications"

mkdir "Avatales.Domain\Stories\Entities"
mkdir "Avatales.Domain\Stories\ValueObjects"
mkdir "Avatales.Domain\Stories\Events"

mkdir "Avatales.Domain\Social\Entities"
mkdir "Avatales.Domain\Social\ValueObjects"
mkdir "Avatales.Domain\Social\Events"

mkdir "Avatales.Domain\Learning\Entities"
mkdir "Avatales.Domain\Learning\ValueObjects"
mkdir "Avatales.Domain\Learning\Events"

mkdir "Avatales.Domain\Memory\Entities"
mkdir "Avatales.Domain\Memory\ValueObjects"
mkdir "Avatales.Domain\Memory\Events"

mkdir "Avatales.Domain\Common\Entities"
mkdir "Avatales.Domain\Common\ValueObjects"
mkdir "Avatales.Domain\Common\Events"
mkdir "Avatales.Domain\Common\Interfaces"
mkdir "Avatales.Domain\Common\Enums"

:: Infrastructure Verzeichnisse
mkdir "Avatales.Infrastructure\Data\Contexts"
mkdir "Avatales.Infrastructure\Data\Configurations"
mkdir "Avatales.Infrastructure\Data\Repositories"
mkdir "Avatales.Infrastructure\Data\Migrations"

mkdir "Avatales.Infrastructure\ExternalServices\OpenAI"
mkdir "Avatales.Infrastructure\ExternalServices\ImageGeneration"
mkdir "Avatales.Infrastructure\ExternalServices\ContentModeration"

mkdir "Avatales.Infrastructure\Caching"
mkdir "Avatales.Infrastructure\Messaging"
mkdir "Avatales.Infrastructure\Authentication"
mkdir "Avatales.Infrastructure\BackgroundServices"
mkdir "Avatales.Infrastructure\Extensions"

:: Shared Verzeichnisse
mkdir "Avatales.Shared\Constants"
mkdir "Avatales.Shared\Extensions"
mkdir "Avatales.Shared\Helpers"
mkdir "Avatales.Shared\Models"

:: Test Verzeichnisse
mkdir "Avatales.UnitTests\Users"
mkdir "Avatales.UnitTests\Characters"
mkdir "Avatales.UnitTests\Stories"
mkdir "Avatales.UnitTests\Common"

mkdir "Avatales.IntegrationTests\Controllers"
mkdir "Avatales.IntegrationTests\Database"
mkdir "Avatales.IntegrationTests\Common"

echo.
echo ========================================
echo    Projekt-Setup abgeschlossen!
echo    Solution Datei: Avatales.sln
echo ========================================
echo.
echo Nächste Schritte:
echo 1. dotnet restore
echo 2. dotnet build
echo 3. Datenbank Connection String konfigurieren
echo 4. dotnet ef migrations add InitialCreate -p Avatales.Infrastructure -s Avatales.API
echo.

pause