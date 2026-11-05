# ContosoInventory

A C# web application for managing Contoso's IT equipment inventory. Built with ASP.NET Core Web API, Blazor WebAssembly, and Entity Framework Core with SQLite.

This starter application is designed for the Microsoft Learn training module **"Configure and customize GitHub Copilot in Visual Studio Code."** It provides a fully functional Categories feature that learners extend with a Product management feature using GitHub Copilot customization capabilities.

## Prerequisites

- [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- [Git 2.48](https://git-scm.com/downloads) or later

## Getting Started

1. Clone the repository:

    ```bash
    git clone https://github.com/MicrosoftLearning/github-copilot-customization-starter-app.git
    cd github-copilot-customization-starter-app/ContosoInventory
    ```

2. Build the application:

    ```bash
    cd ContosoInventory.Server
    dotnet build
    ```

3. Run the application:

    ```bash
    dotnet run
    ```

4. Open a browser and navigate to `http://localhost:5240`.

On the first run, the application automatically creates the SQLite database, applies migrations, and seeds demo data.

## Demo Credentials

| User | Email | Password | Role |
|---|---|---|---|
| Mateo Gomez | `mateo@contoso.com` | `Password123!` | Admin |
| Megan Bowen | `megan@contoso.com` | `Password123!` | Viewer |

- **Admin** users can create, edit, delete, and toggle categories.
- **Viewer** users have read-only access.

## Project Structure

```
ContosoInventory/
├── ContosoInventory.Server/    ASP.NET Core Web API + EF Core + Identity
├── ContosoInventory.Client/    Blazor WebAssembly frontend
└── ContosoInventory.Shared/    Shared DTOs and models
```

- **Server**: REST API with controllers, services (repository pattern), SQLite database via EF Core, and ASP.NET Core Identity authentication.
- **Client**: Blazor WebAssembly SPA with Bootstrap 5 UI, authentication state management, and typed HTTP API clients.
- **Shared**: Data transfer objects (DTOs) with validation annotations, shared between server and client.

## API Documentation

See the [Categories API guide](ContosoInventory/Categories-API.md) for endpoint details and examples. When the application is running, Swagger UI is available at: `http://localhost:5240/swagger`

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
