---
project: expense-tracker-aws
artifact: Plan d'implementation technique
status: final
source:
  - _bmad-output/planning-artifacts/architecture.md
  - _bmad-output/planning-artifacts/dynamodb-model.md
  - _bmad-output/planning-artifacts/repository-structure.md
  - _bmad-output/planning-artifacts/epics-and-stories.md
---

# Plan d'implementation technique

Etat final : ce plan conserve la trace de la sequence d'implementation. Le projet est termine en `v1.0.1`; les commandes initiales ci-dessous sont historiques et ne remplacent pas les instructions du README.

Ce plan transforme le cadrage MVP en sequence d'execution progressive. Il s'appuie sur :

- [architecture.md](architecture.md) pour la decision Lambda API unique, les endpoints REST et les limites MVP.
- [dynamodb-model.md](dynamodb-model.md) pour la table `ExpenseReports`, `GSI1` et `GSI2`.
- [repository-structure.md](repository-structure.md) pour la structure `src/mobile`, `src/backend`, `infra`, `docs`, `tests`.
- [epics-and-stories.md](epics-and-stories.md) pour l'ordre fonctionnel et les criteres d'acceptation.

## Decisions techniques validees

- Le MVP utilise une Lambda API unique en C#/.NET avec routage interne.
- Le backend garde une separation nette entre `Api/`, `Domain/`, `Infrastructure/` et `Contracts/`.
- Le domaine metier est implemente et teste avant les appels AWS.
- DynamoDB reste minimal : table unique `ExpenseReports`, `GSI1` pour les notes Employee, `GSI2` pour la file Finance.
- Les endpoints REST MVP sont regroupes : `/submit` gere submit/resubmit, `/review` gere approve/reject, `/receipt-url` gere upload/view.
- L'IHM MAUI est creee apres le socle backend Domain afin d'eviter une UI deconnectee des regles metier.
- Chaque etape doit etre testable et committable.

## Ordre d'implementation

### Etape 1 - Initialiser la solution .NET

But : creer la solution, le projet backend et le projet de tests sans logique applicative.

Structure attendue :

```text
src/
  ExpenseTracker.sln
  backend/
    ExpenseTracker.Api/
      ExpenseTracker.Api.csproj
    ExpenseTracker.Api.Tests/
      ExpenseTracker.Api.Tests.csproj
```

Commit recommande :

```text
chore: initialize dotnet solution and backend test projects
```

### Etape 2 - Poser le domaine metier critique

But : coder les regles qui ne dependent pas d'AWS.

Fichiers attendus :

```text
src/backend/ExpenseTracker.Api/
  Domain/
    ExpenseReport.cs
    ExpenseStatus.cs
    ExpenseStateMachine.cs
    ExpensePolicy.cs
    ReviewDecision.cs
    DomainError.cs
```

Classes prioritaires :
- `ExpenseStatus` : `Draft`, `Submitted`, `Rejected`, `Resubmitted`, `Approved`.
- `ReviewDecision` : `Approve`, `Reject`.
- `ExpenseReport` : modele metier minimal.
- `ExpenseStateMachine` : transitions autorisees et refusees.
- `ExpensePolicy` : ownership et roles.
- `DomainError` : erreurs metier explicites.

Tests prioritaires :

```text
src/backend/ExpenseTracker.Api.Tests/
  Domain/
    ExpenseStateMachineTests.cs
    ExpensePolicyTests.cs
```

Commit recommande :

```text
feat: add expense domain model and state machine tests
```

### Etape 3 - Definir les contrats REST

But : stabiliser les DTOs avant le routage Lambda et avant MAUI.

Fichiers attendus :

```text
src/backend/ExpenseTracker.Api/
  Contracts/
    CreateExpenseRequest.cs
    UpdateExpenseRequest.cs
    ReviewExpenseRequest.cs
    ReceiptUrlRequest.cs
    ExpenseResponse.cs
    PresignedUrlResponse.cs
```

Commit recommande :

```text
feat: define backend API contracts
```

### Etape 4 - Ajouter les abstractions infrastructure

But : preparer DynamoDB, S3 et l'identite Cognito sans implementer les appels AWS.

Fichiers attendus :

```text
src/backend/ExpenseTracker.Api/
  Infrastructure/
    Auth/
      CognitoUserContext.cs
      UserContextFactory.cs
    DynamoDb/
      IExpenseRepository.cs
    S3/
      IReceiptService.cs
      ReceiptKeyBuilder.cs
    Clock.cs
```

Test prioritaire :

```text
src/backend/ExpenseTracker.Api.Tests/
  Infrastructure/
    ReceiptKeyBuilderTests.cs
```

Commit recommande :

```text
feat: add backend infrastructure abstractions
```

### Etape 5 - Creer le squelette de routage Lambda

But : rendre les endpoints MVP routables sans encore brancher completement AWS.

Fichiers attendus :

```text
src/backend/ExpenseTracker.Api/
  Api/
    LambdaEntryPoint.cs
    RequestRouter.cs
    RouteMatch.cs
    ApiResponse.cs
    Handlers/
      MeHandler.cs
      ExpenseHandlers.cs
      FinanceHandlers.cs
      ReceiptHandlers.cs
```

Tests prioritaires :

```text
src/backend/ExpenseTracker.Api.Tests/
  Api/
    RequestRouterTests.cs
```

Endpoints a couvrir par le routeur :
- `GET /me`
- `POST /expenses`
- `GET /expenses`
- `GET /expenses/{expenseId}`
- `PUT /expenses/{expenseId}`
- `POST /expenses/{expenseId}/submit`
- `GET /finance/queue`
- `POST /finance/expenses/{expenseId}/review`
- `POST /expenses/{expenseId}/receipt-url`

Commit recommande :

```text
feat: add lambda api routing skeleton
```

### Etape 6 - Ajouter le client MAUI

But initial : creer le projet MAUI et les dossiers de base. Etat final : le client contient les pages de login, liste Employee, creation, detail et file Finance.

Structure finale :

```text
src/mobile/ExpenseTracker.Maui/
  Views/
  Services/
  Models/
  Resources/Styles/
```

Fichiers principaux :
- `Models/ExpenseReportDto.cs`
- `Models/ExpenseStatus.cs`
- `Models/MeDto.cs`
- `Models/PresignedUrlDto.cs`
- `Services/AuthService.cs`
- `Services/ExpenseApiClient.cs`
- `Services/SecureTokenStore.cs`
- `Views/LoginPage.xaml`
- `Views/EmployeeExpensesPage.xaml`
- `Views/CreateExpensePage.xaml`
- `Views/ExpenseDetailPage.xaml`
- `Views/FinanceQueuePage.xaml`
- `Resources/Styles/Colors.xaml`
- `Resources/Styles/Styles.xaml`

Commit recommande :

```text
chore: add maui project shell
```

## Commandes initiales

Commandes a executer depuis la racine du repository :

```powershell
mkdir src
mkdir src\backend
dotnet new sln -n ExpenseTracker -o src
dotnet new classlib -n ExpenseTracker.Api -o src\backend\ExpenseTracker.Api
dotnet new xunit -n ExpenseTracker.Api.Tests -o src\backend\ExpenseTracker.Api.Tests
dotnet sln src\ExpenseTracker.sln add src\backend\ExpenseTracker.Api\ExpenseTracker.Api.csproj
dotnet sln src\ExpenseTracker.sln add src\backend\ExpenseTracker.Api.Tests\ExpenseTracker.Api.Tests.csproj
dotnet add src\backend\ExpenseTracker.Api.Tests\ExpenseTracker.Api.Tests.csproj reference src\backend\ExpenseTracker.Api\ExpenseTracker.Api.csproj
dotnet test src\ExpenseTracker.sln
```

Commandes MAUI a executer seulement apres les etapes backend Domain, Contracts et Routing :

```powershell
mkdir src\mobile
dotnet new maui -n ExpenseTracker.Maui -o src\mobile\ExpenseTracker.Maui
dotnet sln src\ExpenseTracker.sln add src\mobile\ExpenseTracker.Maui\ExpenseTracker.Maui.csproj
```

## Mapping etapes vers stories BMAD

| Etape | Stories BMAD liees | Resultat attendu |
| --- | --- | --- |
| 1. Solution .NET | Story 1.1 | Base repository et projets backend/tests. |
| 2. Domaine metier | Stories 2.3, 5.1, 5.2, 6.2 | Regles RBAC et machine d'etats testees. |
| 3. Contrats REST | Stories 3.1, 3.2, 3.3, 4.1, 4.2, 6.2 | DTOs stables pour backend et MAUI. |
| 4. Abstractions infrastructure | Stories 3.3, 4.1, 4.2, 6.1 | Interfaces propres avant SDK AWS. |
| 5. Routage Lambda | Stories 1.2, 2.2, 3.1, 5.1, 6.1, 6.2 | Endpoints MVP routables et testables. |
| 6. Shell MAUI | Stories 3.3, 4.1, 6.1 | Base IHM prete a consommer l'API. |

## Risques et points de vigilance

- Ne pas laisser la Lambda unique devenir un fichier unique : garder les handlers minces.
- Ne pas appeler DynamoDB directement depuis les handlers : passer par `IExpenseRepository`.
- Ne pas encoder les roles seulement dans MAUI : le RBAC doit rester cote Lambda.
- Ne pas creer GSI3 par anticipation : GSI1 et GSI2 suffisent au MVP.
- Ne pas multiplier les routes REST si une route serveur peut porter la decision proprement.
- Ne pas logger de JWT, d'URL pre-signee complete ou de secret.
- Garder les tests Domain rapides et sans dependance AWS.
- Verifier que chaque commit compile et que `dotnet test` passe.

## Etat final et limites

Realise dans la release :

- backend Lambda API C#/.NET ;
- routage REST complet ;
- DynamoDB reel avec GSIs ;
- Cognito authorizer et RBAC cote Lambda ;
- S3 pre-signed URLs ;
- client MAUI connecte a l'API ;
- design system MAUI corporate ;
- scripts SAM, seed users et smoke tests ;
- tests backend.

Hors scope conserve :

- Plusieurs Lambdas par domaine.
- GSI3 et reporting Finance.
- Historique d'audit detaille sous forme d'entites DynamoDB separees.
- Notifications email.
- OCR des justificatifs.
- Approbation par seuil ou double validation.
- Export comptable.
