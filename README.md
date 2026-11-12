# Fitness-Website

A gym booking and member management website built with **ASP.NET Core** and **Razor Pages**. Members can browse gym rooms, book appointments, manage their profile, and chat with trainers. Admins and trainers can manage users, appointments, gym info, and quests.

## Features

- **Member side**: register/login, view gym info and operating hours, book trainer appointments, edit profile, view bookings, chat with trainers, complete quests.
- **Trainer side**: manage availability, accept/manage appointments, assign quests to members, chat.
- **Admin side**: manage users, gym info, equipment, appointments, and view admin dashboard.
- **Authentication & roles**: account registration, login, role-based access (member / trainer / admin).
- **Data**: Entity Framework Core with SQL Server migrations for users, gym info, appointments, notifications, quests, and chat messages.

## Tech Stack

- ASP.NET Core (Razor Pages / MVC)
- C#
- Entity Framework Core + SQL Server
- Bootstrap, jQuery
- HTML/CSS/JavaScript

## Project Structure

```
GymRoomsGG/
├── Controllers/          # MVC controllers (Account, Admin, GymRoom, Home, Member, Trainer, etc.)
├── Data/                 # ApplicationDbContext (EF Core)
├── Migrations/           # EF Core database migrations
├── Models/               # Entities, view models, chat/quest models
├── Views/                # Razor views (Account, GymRoom, Home, User, Shared, etc.)
├── wwwroot/              # Static assets (css, js, images, lib)
├── Helpers/              # Helper classes (auth, encryption, operating hours, etc.)
├── Program.cs            # Application entry point
├── appsettings.json      # App configuration
└── GymRoomsGG.csproj     # Project file
```

## Getting Started

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (ASP.NET Core runtime)
- [SQL Server](https://www.microsoft.com/sql-server) or SQL Server Express
- Visual Studio 2022 (recommended) or VS Code with the C# Dev Kit

### Run the project

1. Clone the repository:

```bash
git clone https://github.com/JHAO2401/Fitness-Website.git
cd Fitness-Website
```

2. Update the connection string in `appsettings.json` if needed to point to your SQL Server instance.

3. Apply the database migrations:

```bash
dotnet ef database update
```

4. Run the application:

```bash
dotnet run
```

Then open the URL shown in the terminal (typically `https://localhost:5001` or `http://localhost:5000`).

## License

This project is for personal/educational use.
