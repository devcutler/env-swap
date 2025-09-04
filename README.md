# env-swap

Environment file switching utility.

## Installation

Build the self-contained executable:

```bash
# Windows
dotnet publish -r win-x64

# Linux  
dotnet publish -r linux-x64

# macOS
dotnet publish -r osx-x64
```

Copy the executable to a directory in your PATH and alias it:

```bash
alias envs='./env-swap'
```

## Usage

```
$ env-swap --help
Usage:
  env-swap [environment] [options]                 - Switch to environment
  env-swap --add [name] [path] [--allow-missing]   - Add environment file path
  env-swap --remove [name] [path]                  - Remove environment file path
  env-swap --list                                  - List all available environments

Options:
  --local            Copy to .env.local
  --target [name]    Custom target filename
  --allow-missing    Add non-existent files
  --help, -h         Show this help
```

### Add environment files

```bash
env-swap --add dev .env.dev
env-swap --add prod .env.prod
env-swap --add staging .env.staging
```

Add files that don't exist yet:

```bash
env-swap --add test .env.test --allow-missing
```

### Remove environment files

```bash
env-swap --remove dev .env.dev
env-swap --remove prod .env.prod
```

### List environments

```bash
env-swap --list
```

### Switch environments

Switch to default `.env`:

```bash
env-swap dev
env-swap prod
```

Switch to `.env.local`:

```bash
env-swap dev --local
```

Switch to custom target filename:

```bash
env-swap prod --target .env.testing
env-swap staging --target .env.backup
```

### Monorepo support

Add environment files from different directories:

```bash
env-swap --add dev api/.env.dev
env-swap --add dev web/.env.dev
```

Each file copies to its respective directory:
- `api/.env.dev` → `api/.env`
- `web/.env.dev` → `web/.env`

## Examples

```bash
# Set up environments
env-swap --add dev .env.dev
env-swap --add prod .env.prod
env-swap --add staging .env.staging

# Switch between environments
env-swap dev        # Copies .env.dev to .env
env-swap prod       # Copies .env.prod to .env

# Use local development
env-swap dev --local # Copies .env.dev to .env.local

# Custom target filename
env-swap prod --target .env.testing # Copies prod files to .env.testing in each directory

# Check what's available
env-swap --list
```

## Configuration

Environment mappings are stored in `.env.json`:

```json
{
  "dev": [
    ".env.dev",
    "api/.env.dev",
    "web/.env.dev"
  ],
  "prod": [
    ".env.production", // filenames don't need to be the same as env name
    "api/.env.prod",
    "web/.env.prod"
  ],
  "staging": [
    ".env.staging",
    "api/.env.staging",
    "web/.env.staging"
  ]
}
```