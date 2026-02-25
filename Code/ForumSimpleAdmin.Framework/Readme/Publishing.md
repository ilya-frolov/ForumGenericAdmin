# Single Domain Publishing Guide (App + Admin + API)

This guide describes how to publish an Angular client (`/`), an Angular admin (`/admin/`), and an ASP.NET Core API (`/api`) under a **single domain**, such as `https://yourdomain.com`, using IIS.

---

## ✅ Requirements

### For all setups:

* **Create a folder at**: `api/wwwroot/uploads`
* **Grant write permissions** to the IIS process (`IIS_IUSRS`) on the `uploads` folder

---

## 📁 Folder Structure (Under the root publishing folder)

```
/root
  /api            ← ASP.NET Core project
    /wwwroot/uploads
  /app            ← Angular app build (base-href: "/")
  /admin          ← Angular admin build (base-href: "/admin/")
  web.config      ← Located at the root (see below)
```

---

## 💪 Admin Build Command

```bash
npm run build -- --base-href /admin/
```

This ensures all routes and assets resolve correctly from `/admin/`.

---

## 📄 Root `web.config` File

Place this file at the root (alongside `/api`, `/app`, `/admin` folders):

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>

    <defaultDocument enabled="true">
      <files>
        <add value="app/index.html" />
      </files>
    </defaultDocument>

    <rewrite>
      <rules>

        <!-- Redirect /uploads to /api/uploads -->
        <rule name="Uploads Proxy" stopProcessing="true">
          <match url="^uploads/(.*)" />
          <action type="Rewrite" url="/api/uploads/{R:1}" />
        </rule>

        <!-- Skip /api -->
        <rule name="Skip API" stopProcessing="true">
          <match url="^api(/.*)?$" />
          <action type="None" />
        </rule>

        <!-- Admin static files -->
        <rule name="Admin Static Files" stopProcessing="true">
          <match url="^admin/(.*\.[^/]+)$" />
          <action type="Rewrite" url="/admin/{R:1}" />
        </rule>

        <!-- Admin SPA fallback -->
        <rule name="Admin SPA Fallback" stopProcessing="true">
          <match url="^admin(/.*)?$" />
          <conditions>
            <add input="{REQUEST_FILENAME}" matchType="IsFile" negate="true" />
            <add input="{REQUEST_FILENAME}" matchType="IsDirectory" negate="true" />
          </conditions>
          <action type="Rewrite" url="/admin/index.html" />
        </rule>

        <!-- App static files -->
        <rule name="App Static Files" stopProcessing="true">
          <match url="^(.*\.[^/]+)$" />
          <action type="Rewrite" url="/app/{R:1}" />
        </rule>

        <!-- App SPA fallback (includes root "/") -->
        <rule name="App SPA Fallback" stopProcessing="true">
          <match url=".*" />
          <conditions>
            <add input="{REQUEST_FILENAME}" matchType="IsFile" negate="true" />
            <add input="{REQUEST_FILENAME}" matchType="IsDirectory" negate="true" />
          </conditions>
          <action type="Rewrite" url="/app/index.html" />
        </rule>

      </rules>
    </rewrite>

  </system.webServer>
</configuration>
```

---

## ✅ Final Notes

* The API project (`/api`) must be configured as a **virtual application** in IIS.
* `/app` and `/admin` are just static folders, not virtual apps.
* In the admin, you need to update the API path in `/configs/config.json`.
* You do **not** need `--base-href` for the main Angular app (`/`) since it's rewritten to `/`.
* You **must** use `--base-href /admin/` for the admin build so assets and routing work properly.
