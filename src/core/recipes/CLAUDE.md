# Core Recipes

Recipes for teaching users how to use Conqueror's core messaging, signalling, and iterating features.

## Purpose

Recipes are hands-on, code-along tutorials that demonstrate specific Conqueror features or patterns. They are designed to be minimal, focused examples that users can follow step-by-step to learn how to use Conqueror effectively.

## Recipe Structure

Each recipe follows this directory structure:

```txt
recipes/
└── <feature>/                    # e.g., messaging, signalling, iterating
    └── <recipe-name>/            # e.g., getting-started, testing-handlers
        ├── README.md             # The recipe tutorial
        ├── Taskfile.yml          # Taskfile for building and testing
        ├── <RecipeName>.sln      # Standalone solution for the recipe
        ├── <ProjectName>/        # Starting point for the recipe
        │   ├── <ProjectName>.csproj
        │   ├── Program.cs
        │   └── ...               # Other source files
        └── .completed/           # Completed implementation
            └── <ProjectName>/
                ├── <ProjectName>.csproj
                ├── Program.cs
                └── ...           # Other source files
```

## Creating a New Recipe

### 1. Directory and File Structure

Create the recipe in the appropriate feature subdirectory:

```bash
mkdir -p src/core/recipes/<feature>/<recipe-name>/.completed/<ProjectName>
```

**Naming conventions:**

- Recipe directory: `kebab-case` (e.g., `getting-started`, `testing-handlers`)
- Project name: `Conqueror.Recipes.<Feature>.<RecipeName>` (e.g., `Conqueror.Recipes.Messaging.GettingStarted`)

### 2. Project Files (.csproj)

The project files must support both standalone use (with NuGet packages) and integrated development (with project references). See the [getting-started recipe as an example](./messaging/getting-started/.completed/Conqueror.Recipes.Messaging.GettingStarted/Conqueror.Recipes.Messaging.GettingStarted.csproj).

**Important notes:**

- The condition checks if `$(SolutionName)` equals the recipe's standalone solution name
- When using project references, you **must** explicitly reference the source generators project as an Analyzer
- When using packages, the source generators are included automatically with the Conqueror package

### 3. Standalone Solution File

Create a solution file that includes all the recipe's projects, with the completed projects being in a `.completed` solution directory.

### 4. Integrating with core.sln and Conqueror.sln

Recipes must be added to both solution files with proper solution folder hierarchy.

#### core.sln

Add solution folders mirroring the directory structure:

```txt
recipes (solution folder)
└── <feature> (solution folder)
    └── <recipe-name> (solution folder)
        └── .completed (solution folder)
            └── <ProjectName> (project)
```

#### Conqueror.sln

Add solution folders under the `core` folder:

```txt
core (solution folder)
└── recipes (solution folder)
    └── <feature> (solution folder)
        └── <recipe-name> (solution folder)
            └── .completed (solution folder)
                └── <ProjectName> (project)
```

**To add to solution files:**

```sh
dotnet sln <path_to_solution_file> add <path_to_project_file> --solution-folder <SolutionFolderName>
```

### 5. Writing Recipe Content (README.md)

#### Tone and Style

- **Minimal and focused**: Remove all unnecessary complexity that doesn't directly serve the teaching goal
- **Step-by-step**: Guide users through building the example from scratch
- **Code-along format**: Designed for users to type along, not just read
- **Same tone as existing recipes**: Maintain consistency with the established voice (see the [getting-started recipe](./messaging/getting-started/README.md) as a reference)
- **No unnecessary keywords**: Avoid distracting keywords like `sealed`, `readonly`, etc. unless they're essential to the concept being taught

#### Content Structure

1. **Title and Introduction**: Brief explanation of what the recipe teaches
2. **Prerequisites**: Link to downloadable recipe folder and .NET version requirements
3. **Problem Statement**: Describe what you'll build (e.g., "a console app that manages counters")
4. **Setup**: Creating the project and adding dependencies
5. **Implementation**: Step-by-step code development with:
   - Code snippets showing what to add
   - `diff` blocks showing changes to existing files
   - Links to completed files for reference
   - Explanations of key concepts
6. **Testing**: Running the application to verify it works
7. **Summary**: Key takeaways and next steps
8. **Links**: To related recipes and feedback mechanisms

#### Code Examples

- Use **complete, runnable code** in examples
- Show the **before and after** using diff blocks when modifying existing code
- Always link to the completed file: `([view completed file](.completed/<ProjectName>/<FileName>.cs))`
- Use comments sparingly - only when explaining non-obvious decisions
- Keep variable and method names clear and self-documenting

#### Formatting

- Use GitHub-flavored markdown
- Code blocks should specify language (e.g., ` ```cs `, ` ```sh `, ` ```diff `)
- Use blockquotes (`>`) for important notes and tips
- Use **bold** for emphasis on key concepts

### 7. Testing the Recipe

Before considering a recipe complete:

1. **Build with core.sln**:

   ```bash
   cd src/core
   task build
   ```

2. **Build with Conqueror.sln**:

   ```bash
   cd /home/dev/src/conqueror
   task build
   ```

3. **Test the standalone solution**:

   ```bash
   cd src/core/recipes/<feature>/<recipe-name>
   task build
   task run:completed -- <args, e.g. as HEREDOC>
   ```

4. **Verify the code works** as described in the README

## Key Design Principles

1. **Conditional References**: Recipes must work both standalone (with packages) and integrated (with project references)
2. **Minimal Noise**: Remove all code that doesn't directly serve the teaching goal (no `sealed`, no excessive error handling, etc.)
3. **Consistency**: Match the tone and structure of existing recipes
4. **Self-Contained**: Each recipe should be complete and runnable on its own
5. **Progressive**: Build complexity gradually through the recipe
6. **Practical**: Use realistic examples that demonstrate real-world usage

## Test Projects in Recipes

In recipe test projects, use a composition-based `TestHost` class, never a test base class. See the [testing-handlers recipe](./messaging/testing-handlers/.completed/Conqueror.Recipes.Messaging.TestingHandlers.Tests/TestHost.cs) as example.

## Common Pitfalls

1. **Forgetting the source generators project reference** when using project references (causes `IHandler` not found errors)
2. **Not creating proper solution folder hierarchy** in core.sln and Conqueror.sln
3. **Including unnecessary complexity** like `sealed` keywords or defensive programming patterns
4. **Breaking the conditional reference logic** by using the wrong solution name in the condition
