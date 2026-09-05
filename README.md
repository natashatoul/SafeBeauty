# SafeBeauty

A full-stack web application for cosmetic ingredient safety analysis, with personalisation based on the user's dermatological conditions.

SafeBeauty lets a user enter a cosmetic product — either by scanning its barcode or by pasting the ingredient list — and returns each ingredient with a colour-coded safety rating drawn from EU regulatory data, a plain-language summary of the whole product, and, for signed-in users, personalised warnings tied to their declared skin conditions.

This project was developed as an MSc Software Engineering dissertation at the University of Westminster.

**Live demo:** https://natashatoul.github.io/SafeBeauty/

> The frontend is hosted on GitHub Pages and the backend API on Azure App Service (free tier). The first request after a period of inactivity may take up to a minute while the backend restarts (a cold start); subsequent requests respond normally.

---

## Tech stack

**Frontend**
- React 19 with the Vite build tool
- react-router-dom for navigation
- axios for API calls
- html5-qrcode for camera-based barcode scanning
- Tests: Vitest

**Backend**
- ASP.NET Core Web API (.NET 8, C#)
- Entity Framework Core over SQLite
- JWT authentication via ASP.NET Core Identity
- CsvHelper for reading regulatory seed data
- MailKit for email verification
- Tests: xUnit (with in-memory SQLite)

**External services**
- Hugging Face Inference API — `facebook/bart-large-mnli` (zero-shot classification of unknown ingredients) and `meta-llama/Llama-3.1-8B-Instruct` (plain-language summaries)
- Open Beauty Facts API — product lookup by barcode

**Reference data** (in `backend/SafeBeauty.API/SeedData`)
- EU 2025/1175 common ingredient glossary
- CosIng annexes: II (prohibited), III (restricted), IV (colorants), V (preservatives), VI (UV filters)
- CosIng fragrance inventory
- `condition_rules.csv` — condition rules derived by hand from published clinical dermatology sources

---

## Repository structure

```
SafeBeauty/
├── backend/
│   ├── SafeBeauty.API/           ASP.NET Core Web API
│   │   ├── Controllers/          HTTP endpoints
│   │   ├── Services/             analysis pipeline, AI layer, validators
│   │   ├── Models/               domain entities, parsers, enums
│   │   ├── Data/                 DbContext and data seeding
│   │   └── SeedData/             regulatory CSV / TXT source files
│   └── SafeBeauty.API.Tests/     xUnit test suite
├── frontend/                     React (Vite) single-page application
│   └── src/
└── evaluation/                   AI classifier evaluation
```

---

## Running the application locally

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download)
- [Node.js](https://nodejs.org/) (18 or later) and npm
- A [Hugging Face](https://huggingface.co/) account and access token (for the AI features)

### 1. Backend

From the repository root:

```bash
cd backend/SafeBeauty.API
```

Configure the required settings. The committed `appsettings.json` contains placeholders only — provide your own values, ideally through a git-ignored `appsettings.Development.json` or user secrets:

- `HuggingFace:ApiKey` — your Hugging Face access token
- `Jwt:Key` — any signing key of at least 32 characters
- (optional) `EmailSettings` — SMTP credentials, only needed for email verification

Then run:

```bash
dotnet run
```

The API starts on `http://localhost:5166`. On first start it creates the SQLite database and seeds it from the files in `SeedData/`, so the initial launch takes a little longer.

### 2. Frontend

In a second terminal, from the repository root:

```bash
cd frontend
npm install
npm run dev
```

The app is served at the URL printed by Vite (default `http://localhost:5173`). Vite proxies `/api` requests to the backend on `http://localhost:5166`, so both must be running.

---

## Running the tests

**Backend (xUnit):**
```bash
cd backend
dotnet test
```

**Frontend (Vitest):**
```bash
cd frontend
npm run test:vitest
```

---

## Key components

| Area | File |
|------|------|
| Analysis pipeline (orchestrator) | `backend/SafeBeauty.API/Services/IngredientAnalysisService.cs` |
| Unknown-ingredient classifier | `backend/SafeBeauty.API/Services/HuggingFaceService.cs` |
| Plain-language summary | `backend/SafeBeauty.API/Services/AiSummaryService.cs` |
| Ingredient parsing | `backend/SafeBeauty.API/Models/Models/IngredientListParser.cs` |
| Ingredient normalisation | `backend/SafeBeauty.API/Models/Models/IngredientNormalizer.cs` |
| Data seeding | `backend/SafeBeauty.API/Data/DataSeeder.cs` |
| Barcode validation | `backend/SafeBeauty.API/Services/BarcodeValidator.cs` |

---

## Notes

- Safety **classification** is done by deterministic lookup against the regulatory data, not by the AI model. The zero-shot classifier only provides an indicative, clearly-labelled estimate for ingredients that cannot be resolved any other way. This decision is supported by the classifier evaluation reported in the dissertation.
- Personalised condition flags each carry a named clinical source, shown to the user, so every warning is traceable to recognised guidance.
