---
project: expense-tracker-aws
artifact: Structure repository
status: refined-mvp
source:
  - docs/project-context.md
  - docs/source/Project_ExpenseTracker_5ENTAPP.pdf
---

# Structure de repository

## 1. Decision MVP

Le repository garde une separation nette entre :
- l'IHM .NET MAUI ;
- le backend Lambda API C# ;
- le domaine metier testable ;
- l'infrastructure AWS ;
- la documentation et les supports de demo.

Le backend MVP est une seule Lambda API, mais le code reste decoupe en dossiers par responsabilite.

## 2. Structure recommandee

```text
expense-tracker-aws/
  README.md
  .gitignore
  docs/
    project-context.md
    architecture-diagram.md
    report/
      report.md
    source/
      Project_ExpenseTracker_5ENTAPP.pdf
  src/
    ExpenseTracker.sln
    mobile/
      ExpenseTracker.Maui/
        ExpenseTracker.Maui.csproj
        App.xaml
        MauiProgram.cs
        Views/
        ViewModels/
        Services/
        Models/
    backend/
      ExpenseTracker.Api/
        ExpenseTracker.Api.csproj
        Api/
        Domain/
        Infrastructure/
        Contracts/
      ExpenseTracker.Api.Tests/
        ExpenseTracker.Api.Tests.csproj
  infra/
    cloudformation/
      template.yaml
      parameters.example.json
    scripts/
      deploy.ps1
      seed-users.ps1
      invoke-samples.ps1
    samples/
      create-expense.json
      submit-expense.json
      review-expense.json
      receipt-url.json
  tests/
    manual-test-plan.md
    api-test-cases.md
  _bmad-output/
    planning-artifacts/
```

## 3. Backend C# concret

```text
ExpenseTracker.Api/
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
  Domain/
    ExpenseReport.cs
    ExpenseStatus.cs
    ExpenseStateMachine.cs
    ExpensePolicy.cs
    ReviewDecision.cs
    DomainErrors.cs
  Infrastructure/
    Auth/
      CognitoUserContext.cs
      UserContextFactory.cs
    DynamoDb/
      IExpenseRepository.cs
      DynamoExpenseRepository.cs
      DynamoExpenseItem.cs
    S3/
      IReceiptService.cs
      S3ReceiptService.cs
      ReceiptKeyBuilder.cs
    Clock.cs
  Contracts/
    CreateExpenseRequest.cs
    UpdateExpenseRequest.cs
    ReviewExpenseRequest.cs
    ReceiptUrlRequest.cs
    ExpenseResponse.cs
    PresignedUrlResponse.cs
```

Responsabilites :
- `Api/` : convertir HTTP en appels applicatifs et retourner des reponses propres.
- `Domain/` : statuts, transitions, regles RBAC/ownership, erreurs metier.
- `Infrastructure/` : details AWS Cognito claims, DynamoDB, S3, horloge.
- `Contracts/` : DTOs REST serialises en JSON.

## 4. Tests backend

```text
ExpenseTracker.Api.Tests/
  Domain/
    ExpenseStateMachineTests.cs
    ExpensePolicyTests.cs
  Infrastructure/
    ReceiptKeyBuilderTests.cs
  Api/
    RequestRouterTests.cs
```

Priorite MVP :
- tester les transitions ;
- tester les autorisations ;
- tester le routage des endpoints principaux ;
- tester la construction des cles S3.

## 5. Mobile MAUI propose

```text
ExpenseTracker.Maui/
  Views/
    LoginPage.xaml
    EmployeeExpensesPage.xaml
    ExpenseEditPage.xaml
    ExpenseDetailPage.xaml
    FinanceQueuePage.xaml
    FinanceReviewPage.xaml
  ViewModels/
    LoginViewModel.cs
    EmployeeExpensesViewModel.cs
    ExpenseEditViewModel.cs
    ExpenseDetailViewModel.cs
    FinanceQueueViewModel.cs
    FinanceReviewViewModel.cs
  Services/
    AuthService.cs
    ExpenseApiClient.cs
    ReceiptUploadService.cs
    SecureTokenStore.cs
  Models/
    ExpenseReportDto.cs
    ExpenseStatus.cs
```

## 6. Infrastructure AWS

Pour limiter la complexite, un seul template CloudFormation ou SAM suffit.

Ressources MVP :
- Cognito User Pool et groupes `Employee`, `FinanceManager` ;
- API Gateway REST API avec Cognito Authorizer ;
- Lambda API C# unique ;
- DynamoDB `ExpenseReports` avec GSI1 et GSI2 ;
- bucket S3 prive ;
- role IAM Lambda ;
- logs CloudWatch.

## 7. Ce qui n'est pas cree maintenant

- dossiers separes pour plusieurs Lambdas ;
- microservices ou projets backend multiples ;
- pipeline CI/CD complet ;
- scripts de migration complexes ;
- infrastructure multi-environnement avancee.

Ces elements peuvent etre ajoutes si le projet depasse le MVP, mais ils ne sont pas necessaires pour obtenir une demo professionnelle.
