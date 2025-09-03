FoxDrive (Portfolio Preview)

A self-hosted, OneDrive-style file manager with a modern dark UI, thumbnails, drag & drop uploads, and secure login. Built with ASP.NET Core for my home server and published via Cloudflare Tunnel.

This repository is for portfolio review only. Code is provided to demonstrate approach and quality.

Highlights

Tile grid with thumbnails, context menu, drag-and-drop uploads

Folder tree, breadcrumbs, rename/move/delete, ZIP download

Image/video/audio/PDF/text previews

Auth via cookie sign-in against SQLite with hashed passwords

Designed for Windows; tunnelled with Cloudflare

Architecture (short)

Backend: ASP.NET Core (.NET 8) MVC + controllers

Auth: Cookie auth, PasswordHasher<T> (ASP.NET Identity)

Storage: Local filesystem (configurable root)

Data: SQLite (foxdrive_users.db) via EF Core

Frontend: Vanilla JS + CSS (no SPA)

Admin (users)

From the FoxDrive.Web folder:

dotnet run -- listusers
dotnet run -- adduser <username> <password>
dotnet run -- changepw <username> <newpassword>
dotnet run -- deluser <username>


The SQLite DB file (Data/foxdrive_users.db) is not committed.

Configuration

File root: set in appsettings.json → FoxDrive:RootPath

Port: via ASPNETCORE_URLS or launch settings

Static files served from wwwroot/

Security notes

Passwords are hashed + salted (no plain text)

Keep *.db, *.db-wal, *.db-shm out of Git

Run behind HTTPS (e.g., Cloudflare Tunnel)

License & usage

Copyright © Adam. All rights reserved.
This code is shared for viewing as part of a job portfolio. Use, copying, redistribution, or derivative works are not permitted without written permission.

If you’re evaluating my work and need more context, please reach out:

Email: adamduriniksfg@gmail.com

Website: https://foxhint.com