# Angular 19 Generic Admin Project

This template includes Angular 19 and provides the following features:

1. Ready-to-use SCSS structure.
2. Bootstrap with RTL support.
3. Basic configuration file implementation.
4. Basic layout with loader.
5. Internationalization (i18n) preparation (refer to notes).
6. AuthGuard setup (refer to notes).

## How to Start

1. Find and replace the following value in **ALL FILES**:
   `"angular-project-template"`
   with your project name. If your project name is "MyFolderName", name it 'my-folder-name'.

2. Uncomment any required dependencies or configurations in the files and then execute `npm install`.

## Notes

### Defaults
The default mechanism does **NOT** include language support. The libraries, code and initial support **IS** loaded to the project, and you should be aware of that. That includes the i18n support, rtl-ltr support, etc.

### Internationalization (i18n) Setup

1. Inside `app.module.ts` you can find a const named `TRANSLATIONS_SUPPORT` at the top. Change it to `true` if you need multilingual support.

### AuthGuard Setup

1. Utilize the routing demo provided in `app-routing.module.ts`.
2. Implement the ConnectedUser model in `models.ts`.
3. Implement the `getConnectedUser` method in `app.service.ts` (a commented template is provided).
