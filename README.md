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

Switch to custom target file:

```bash
env-swap prod config/app.env
env-swap staging docker/.env
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

# Custom target
env-swap prod docker/.env # Copies .env.prod to docker/.env

# Check what's available
env-swap --list
```

## Configuration

Environment mappings are stored in `.env.json`:

```json
{
  "dev": [".env.dev"],
  "prod": [".env.prod"],
  "staging": [".env.staging"]
}
```