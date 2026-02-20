# PiPlanningUi

This project was generated using [Angular CLI](https://github.com/angular/angular-cli) version 20.3.4 and uses Angular 15+ with standalone components and signals.

## Component Architecture (Phase 3A)

The application uses a modern Angular architecture with standalone components and clear separation of concerns.

### Board Component Structure

**Main Components (src/app/Components/board/):**

```
board/
├── board.ts/html/css              # Main container, state management
├── board-header/                  # Toggle switch, dev/test mode
│   ├── board-header.ts
│   ├── board-header.html
│   └── board-header.css
├── team-bar/                      # Team member management
│   ├── team-bar.ts
│   ├── team-bar.html
│   └── team-bar.css
├── capacity-row/                  # Capacity display & edit
│   ├── capacity-row.ts
│   ├── capacity-row.html
│   └── capacity-row.css
├── sprint-header/                 # Column headers with metrics
│   ├── sprint-header.ts
│   ├── sprint-header.html
│   └── sprint-header.css
├── feature-row/                   # Feature cards with drag-drop
│   ├── feature-row.ts
│   ├── feature-row.html
│   └── feature-row.css
└── board-modals/                  # Import, finalize, delete dialogs
    ├── board-modals.ts
    ├── board-modals.html
    └── board-modals.css
```

### Key Development Patterns

1. **Standalone Components:** All components use `standalone: true` with explicit imports
2. **Signals:** Reactive state management using Angular Signals (no RxJS observables needed)
3. **Scoped CSS:** Each component owns its styles; no global conflicts
4. **Dark Mode:** `:host-context(.dark-theme)` for app-controlled theming (not OS-detected)
5. **Props-based Communication:** Child components receive data and callbacks via @Input/@Output
6. **Local State:** Each component manages its own modals, forms, and ephemeral state

## Development server

To start a local development server, run:

```bash
ng serve
```

Once the server is running, open your browser and navigate to `http://localhost:4200/`. The application will automatically reload whenever you modify any of the source files.

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
ng generate --help
```

## Building

To build the project run:

```bash
ng build
```

This will compile your project and store the build artifacts in the `dist/` directory. By default, the production build optimizes your application for performance and speed.

## Running unit tests

To execute unit tests with the [Karma](https://karma-runner.github.io) test runner, use the following command:

```bash
ng test
```

## Running end-to-end tests

For end-to-end (e2e) testing, run:

```bash
ng e2e
```

Angular CLI does not come with an end-to-end testing framework by default. You can choose one that suits your needs.

## Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.
