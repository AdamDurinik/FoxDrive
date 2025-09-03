# FoxDrive (Portfolio Preview)

> **Read-only for portfolio review.**
> This repository is shared to demonstrate approach and code quality.
> **No permission** is granted to use, copy, modify, or redistribute the code.

A self-hosted, OneDrive-style file manager with a modern dark UI, thumbnails, drag-and-drop uploads, and secure login. Built with **ASP.NET Core** for my home server and published via **Cloudflare Tunnel**.

---

## ✨ Features

* **Clean UI**: tile grid with thumbnails, context menu (open / download / rename / delete), breadcrumbs.
* **Folders & Files**: create, rename, delete, move (drag to folder).
* **Uploads**: drag-and-drop, multi-file; progress indicator.
* **Downloads**: single files or **Zip** entire folders.
* **Previews**: images, video/audio, PDFs, and text files.
* **Favorites**: quick access (stored in browser localStorage).
* **Auth**: cookie login against **SQLite** with **hashed passwords**.

---

## 🧱 Architecture (short)

| Layer    | Tech                                                        |
| -------- | ----------------------------------------------------------- |
| Backend  | ASP.NET Core (.NET 8), MVC controllers                      |
| Auth     | Cookie auth, `PasswordHasher<T>` (ASP.NET Identity hashing) |
| Data     | SQLite (`foxdrive_users.db`) via EF Core                    |
| Storage  | Local filesystem (configurable root)                        |
| Frontend | Vanilla JS + CSS (no SPA)                                   |
| Exposure | Cloudflare Tunnel (optional)                                |

---

## 🔐 Admin (users)

Run from the `FoxDrive.Web` project folder:

```bash
dotnet run -- listusers
dotnet run -- adduser <username> <password>
dotnet run -- changepw <username> <newpassword>
dotnet run -- deluser <username>
```

Passwords are **hashed + salted** before they’re stored.

## 🌐 Optional: publish via Cloudflare Tunnel

Sample `config.yml`:

```yaml
tunnel: foxdrive
credentials-file: C:/Users/<YOU>/.cloudflared/<UUID>.json
ingress:
  - hostname: foxdrive.yourdomain.com
    service: http://localhost:5010
  - service: http_status:404
```

Route DNS and run:

```bash
cloudflared tunnel route dns foxdrive foxdrive.yourdomain.com
cloudflared tunnel run foxdrive
```

---

## 🔒 Security notes

* **No plain passwords** — only password hashes are stored in SQLite.
* Keep `*.db`, `*.db-wal`, `*.db-shm` **out of the repository**.
* Use HTTPS end-to-end (Kestrel behind Cloudflare Tunnel is fine).
* Consider simple rate-limit/lockout on login (easy to add later).

---

## 🗺️ Roadmap

* Per-user roots & “Shared with me”
* Share links (per-file tokens / expiry)
* Image/video thumbnail service
* Mobile PWA polish

---

## 📜 License & usage

**All rights reserved.**
The contents of this repository are provided **for portfolio review only**.
**Use, copying, modification, distribution, or derivative works are not permitted** without prior written consent.

---

## 📬 Contact

* Email: **[adamduriniksfg@gmail.com](mailto:adamduriniksfg@gmail.com)**
* Website: **[https://foxhint.com](https://foxhint.com)**
