# Security Guide: Handling Sensitive Data

To avoid a "security disaster" when pushing to GitHub, you should **never** hardcode your MongoDB URL in the source code.

### 1. How we fixed it
The `ActivationService.cs` has been updated to search for an **Environment Variable** named `BUSINESS_SUITE_API_URL`.

### 2. How to set your real API URL locally
Open your terminal and run:

**On Mac/Linux:**
```bash
# Add this to your ~/.zshrc or ~/.bash_profile for persistence
export BUSINESS_SUITE_API_URL="https://your-domain.com/api/business-suite/verify-license"
```

**On Windows (PowerShell):**
```powershell
[System.Environment]::SetEnvironmentVariable("BUSINESS_SUITE_API_URL", "https://your-domain.com/api/business-suite/verify-license", "User")
```

### 3. Why this is safe
- You can push the code to GitHub without any database credentials exposed.
- All verification logic and database access are handled safely on your Next.js server.
- Only the endpoint URL is needed by the client.
