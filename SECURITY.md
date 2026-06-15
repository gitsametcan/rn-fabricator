# Security Policy

## Supported Versions

Security reporting is available for the active development version of rn-fabricator.

Formal version support will be defined after the first public release.

## Reporting a Vulnerability

Please do not open a public issue for security vulnerabilities.

Report security concerns privately through the repository owner's preferred GitHub security reporting channel once the repository is public.

Include:

- A description of the issue.
- Steps to reproduce.
- Impact assessment.
- Suggested fix, if available.

## Security Expectations

rn-fabricator must not:

- Generate real credentials.
- Commit secret files.
- Log secret values.
- Overwrite user files silently.

Generated projects should include example files such as `.env.example` and `credentials.example.json`, while real secret files should stay ignored by git.
