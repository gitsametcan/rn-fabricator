# Configuration

Copy the example files before connecting the app to real services:

```bash
cp .env.example .env
cp credentials.example.json credentials.json
```

Keep real local configuration files out of Git:

- `.env`
- `.env.*`
- `credentials.json`
- `credentials.*.json`

The example files use placeholders only. Do not put production secrets in template files.
