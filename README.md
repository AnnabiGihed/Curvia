# Curvia
Curvia is a comprehensive motorcycle route planning application designed to generate "fun" routes for riders. It leverages the Valhalla routing engine and a sophisticated scoring system to create routes based on user preferences for twistiness, elevation, and scenery. The backend is built with .NET using a clean architecture pattern, and the frontend is a .NET MAUI Blazor Hybrid application.

## Key Features

*   **Intelligent Route Generation**:
    *   Generates point-to-point and loop routes tailored for enjoyable motorcycle rides.
    *   Utilizes a self-hosted Valhalla routing engine for pathfinding.
    *   Employs a custom scoring system to rank routes based on curvature, elevation changes, and scenic proxies.
    *   Supports presets (`Twisty`, `Panoramic`, `Balanced`) and custom ride profiles.
    *   Allows constraints like avoiding tolls, highways, and unpaved roads.

*   **Route Management**:
    *   authenticated users can save generated routes to their personal profile.
    *   Routes can be named and annotated with private notes.
    *   Visibility can be set to `Public` (shared with the community) or `Private`.
    *   Export any generated route to GPX 1.1 format for use in navigation devices and apps.

*   **Community & Social Features**:
    *   A community feed displays public routes from all users, sorted by average rating.
    *   Users can review any public route with a 1-5 star rating and an optional comment.

*   **Motorcycle Catalog**:
    *   A pre-seeded, official catalog of motorcycle manufacturers and models.
    *   Users can suggest new makers and models for inclusion.
    *   Administrator-only endpoints for approving user suggestions and managing the official catalog.

*   **User Authentication**:
    *   Secure authentication and authorization managed by Keycloak.
    *   Just-In-Time (JIT) user provisioning creates an application user profile on their first login.

## Architecture

The project is structured following the principles of **Clean Architecture** to ensure a separation of concerns, maintainability, and testability.

*   **`Domain`**: Contains the core business logic, including aggregates, entities, value objects, domain events, and repository interfaces. This layer is the heart of the application and has no external dependencies.
*   **`Application`**: Orchestrates the domain layer by implementing use cases through CQRS (Commands and Queries) with MediatR. It defines DTOs and interfaces for infrastructure services.
*   **`Infrastructure`**: Provides implementations for external services defined in the application layer. This includes clients for the Valhalla routing engine and other third-party services.
*   **`Persistence.EntityFrameworkCore`**: Implements the data access layer using EF Core. It contains the `DbContext`, migrations, and repository implementations.
*   **`API`**: An ASP.NET Core project that exposes the application's features via a RESTful API. It handles web-related concerns like controllers, authentication, and middleware.
*   **`App`**: A .NET MAUI Blazor Hybrid project serving as the cross-platform client application for user interaction.

## Technology Stack

*   **Backend**: .NET 10, ASP.NET Core, C#
*   **Frontend**: .NET MAUI, Blazor Hybrid
*   **Data Persistence**: Entity Framework Core, SQL Server
*   **Routing Engine**: Valhalla
*   **Identity & Security**: Keycloak
*   **Infrastructure**: Docker, Docker Compose
*   **Caching**: Redis
*   **Messaging**: RabbitMQ
*   **Tooling**: Serilog, FluentValidation, MediatR

## Getting Started

### Prerequisites

*   Docker and Docker Compose
*   .NET SDK 10 or later

### Local Environment Setup

The entire local development environment, including the database, routing engine, and identity provider, is managed via Docker Compose.

1.  **Clone the repository:**
    ```sh
    git clone https://github.com/AnnabiGihed/Curvia.git
    cd Curvia
    ```

2.  **Start all required services:**
    Navigate to the `Docker/` directory and run the following command. This will pull the necessary images and start containers for SQL Server, Keycloak, Valhalla, Redis, and more.
    ```sh
    docker-compose -p curvia up -d
    ```
    The services will be available at the following local ports:
    *   **API**: `http://localhost:5027`
    *   **Keycloak**: `http://localhost:8080`
    *   **SQL Server**: `localhost,1433`
    *   **Valhalla**: `http://localhost:8002`
    *   **Redis**: `localhost:6379`
    *   **RabbitMQ**: `http://localhost:15672`
    *   **Kibana**: `http://localhost:5601`

3.  **Run the API:**
    Open the solution in your preferred IDE (like Visual Studio or JetBrains Rider) and run the `Curvia.API` project. It is configured to launch on `http://localhost:5027`. The application will automatically apply database migrations and seed the motorcycle catalog on startup.

4.  **Run the MAUI App:**
    Set `Curvia.App` as the startup project and run it on your desired target (Windows, Android, iOS, etc.).

## License

This project is licensed under the **Apache License 2.0**. See the [LICENSE.txt](LICENSE.txt) file for details.
