# ForumSimpleAdmin

ForumSimpleAdmin is a ForumSimpleAdmin.Framework-based forum project with:

- ASP.NET Core API (`ForumSimpleAdmin.Api`)
- BL + EF Core (`ForumSimpleAdmin.BL`)
- SQL Server schema script for local `SQLEXPRESS`
- Dino admin entities/controllers for managing forums, users, and site settings

## Features Implemented

- User register and login
- Optional profile picture upload on register
- 3 default forums auto-seeded on startup and included in SQL script
- Create posts
- Create comments
- Delete comments only by the post creator
- Forum-level permission: managers only posting
- Site-level lock through `SiteSettings.IsLocked`
- Small in-memory cache for first posts by forum
- API request/response contracts suitable for Angular

## API Endpoints (Angular-ready)

- `GET /api/forum/forums`
- `POST /api/forum/register` (multipart/form-data; optional `profilePicture`)
- `POST /api/forum/login`
- `GET /api/forum/posts/{forumId}?take=20`
- `GET /api/forum/post/{postId}`
- `POST /api/forum/post` (`X-Forum-Token` required)
- `POST /api/forum/comment` (`X-Forum-Token` required)
- `DELETE /api/forum/comment` (`X-Forum-Token` required)

## DB Script

Use:

- `DB Scripts/ForumSimpleAdmin DB creation.sql`

It creates:

- `ForumUsers`
- `Forums`
- `ForumPosts`
- `ForumComments`
- `ForumSessions`
- `SiteSettings`

And seeds:

- 3 forums
- 1 site settings row (`IsLocked = 0`)

## Notes

- The solution expects Dino submodules/dependencies to exist in the environment.
- If submodules are missing, build will fail until they are restored.
