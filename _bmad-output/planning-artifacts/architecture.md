---
project: expense-tracker-aws
artifact: Architecture serverless AWS
status: refined-mvp
source:
  - docs/project-context.md
  - docs/source/Project_ExpenseTracker_5ENTAPP.pdf
---

# Architecture serverless AWS

## 1. Decision MVP

Le MVP utilise une Lambda API unique en C#/.NET, exposee derriere API Gateway, avec un routage interne leger.

Cette decision reduit la complexite de deploiement tout en gardant une architecture propre :
- un seul package Lambda a compiler et deployer ;
- une seule integration API Gateway -> Lambda a maintenir ;
- un seul role IAM Lambda a configurer ;
- une separation interne claire entre API, domaine metier, persistence DynamoDB et S3.

Les Lambdas multiples par domaine restent une amelioration future si le projet grossit. Pour un projet scolaire, elles ajoutent surtout de la configuration sans apporter beaucoup de valeur au MVP.

## 2. Vue d'ensemble

```text
.NET MAUI App
    |
    | HTTPS + JWT Cognito
    v
Amazon API Gateway REST API
    |
    | Cognito Authorizer
    v
Single AWS Lambda API C#/.NET
    |        |          |
    |        |          +--> Amazon S3 private receipts bucket
    |        +--> Amazon DynamoDB ExpenseReports table
    +--> CloudWatch Logs

IAM limite les permissions Lambda vers DynamoDB, S3 et CloudWatch.
```

## 3. Justification des services

| Service | Role | Justification |
| --- | --- | --- |
| .NET MAUI | IHM mobile/desktop | Technologie imposee, adaptee a un client cross-platform avec vues Employee et Finance. |
| Amazon Cognito | Authentification et groupes | Fournit JWT, groupes Employee/Finance et claims utilises par API Gateway et Lambda. |
| API Gateway | Point d'entree REST | Expose les endpoints REST et valide les tokens Cognito avant Lambda. |
| AWS Lambda C#/.NET | Logique metier | Porte le routage interne, le RBAC, la machine d'etats et les appels DynamoDB/S3. |
| DynamoDB | Stockage notes de frais | Table unique optimisee pour les acces par employee et par file Finance. |
| S3 | Justificatifs | Stockage prive des fichiers, consultes uniquement via URLs pre-signees. |
| IAM | Permissions | Principe du moindre privilege pour DynamoDB, S3 et CloudWatch. |
| CloudWatch | Logs | Debug, traces de refus RBAC et erreurs backend. |

## 4. Endpoints REST MVP

Ces endpoints couvrent le workflow complet sans multiplier inutilement les routes.

| Methode | Route | Role | Description |
| --- | --- | --- | --- |
| GET | `/me` | Authenticated | Retourne l'identite et les roles utiles a l'IHM. |
| POST | `/expenses` | Employee | Cree une note en `Draft`. |
| GET | `/expenses` | Employee | Liste les notes de l'Employee connecte via GSI1. |
| GET | `/expenses/{expenseId}` | Owner ou Finance | Retourne le detail d'une note. |
| PUT | `/expenses/{expenseId}` | Owner | Modifie une note `Draft` ou `Rejected`. |
| POST | `/expenses/{expenseId}/submit` | Owner | Soumet ou resoumet selon l'etat courant : `Draft -> Submitted`, `Rejected -> Resubmitted`. |
| GET | `/finance/queue` | Finance Manager | Liste les notes `Submitted` et `Resubmitted` via GSI2. |
| POST | `/finance/expenses/{expenseId}/review` | Finance Manager | Approuve ou rejette selon `decision=approve/reject`. |
| POST | `/expenses/{expenseId}/receipt-url` | Owner ou Finance | Genere une URL pre-signee `upload` ou `view` selon le corps de requete. |

Routes volontairement evitees pour le MVP :
- routes separees `/approve` et `/reject`, remplacees par `/review` ;
- route separee `/resubmit`, remplacee par `/submit` avec decision serveur ;
- endpoints d'administration, d'export, de reporting et d'historique global.

## 5. Structure interne de la Lambda API

La Lambda unique ne doit pas devenir un monolithe illisible. Elle est structuree ainsi :

```text
ExpenseTracker.Api/
  Api/
    LambdaEntryPoint.cs
    RequestRouter.cs
    ApiResponse.cs
    RouteMatch.cs
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
  Infrastructure/
    Auth/
      CognitoUserContext.cs
      UserContextFactory.cs
    DynamoDb/
      DynamoExpenseRepository.cs
      DynamoExpenseItem.cs
    S3/
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

Regle importante : les handlers API sont minces. Les decisions metier vivent dans `Domain/`, et les appels AWS vivent dans `Infrastructure/`.

## 6. Securite et RBAC

API Gateway utilise un Cognito Authorizer. Lambda relit les claims transmis par API Gateway :
- `sub` : identifiant stable utilisateur ;
- `email` ou `cognito:username` : affichage et audit ;
- `cognito:groups` : role `Employee` ou `FinanceManager`.

Regles cote Lambda :
- un Employee ne peut lire et modifier que les notes dont `employeeId == sub` ;
- un Finance Manager peut lire la file Finance et prendre une decision ;
- les transitions d'etat sont refusees si le role, l'owner ou l'etat courant ne correspondent pas ;
- le client MAUI ne contient aucune cle AWS.

## 7. Workflow serveur

Etats MVP :

```text
Draft -> Submitted -> Approved
Draft -> Submitted -> Rejected -> Resubmitted -> Approved
Draft -> Submitted -> Rejected -> Resubmitted -> Rejected
```

La route `/expenses/{expenseId}/submit` choisit la transition selon l'etat courant :
- `Draft` devient `Submitted` ;
- `Rejected` devient `Resubmitted` ;
- tout autre etat est refuse.

La route `/finance/expenses/{expenseId}/review` choisit la transition selon `decision` :
- `approve` : `Submitted/Resubmitted -> Approved` ;
- `reject` : `Submitted/Resubmitted -> Rejected`, avec justification obligatoire.

DynamoDB utilise des expressions conditionnelles pour eviter les mises a jour concurrentes ou invalides.

## 8. S3 et URLs pre-signees

Le bucket S3 est prive :
- `BlockPublicAccess` active ;
- aucune policy publique ;
- aucune cle AWS cote client ;
- acces uniquement via URLs pre-signees generees par Lambda.

Cle objet recommandee :

```text
receipts/{employeeId}/{expenseId}/{fileNameOrGuid}
```

La route `/expenses/{expenseId}/receipt-url` accepte :
- `operation = upload` pour un Employee owner sur une note modifiable ;
- `operation = view` pour l'owner ou Finance.

## 9. IAM MVP

Un role IAM Lambda unique suffit :
- `dynamodb:GetItem`, `PutItem`, `UpdateItem`, `Query` sur la table et GSI1/GSI2 ;
- `s3:PutObject`, `s3:GetObject` sur le prefixe `receipts/*` ;
- permissions CloudWatch Logs minimales.

Cette approche est moins fine que des roles par Lambda, mais elle reste professionnelle si le scope des ressources est strictement limite.

## 10. Maintenant vs plus tard

### A implementer maintenant

- Lambda API unique avec routage interne.
- Cognito Authorizer API Gateway.
- RBAC cote Lambda.
- Machine d'etats serveur.
- DynamoDB table unique avec GSI1 et GSI2.
- S3 prive avec URLs pre-signees upload/view.
- Logs CloudWatch sans secrets.
- Tests unitaires du domaine.

### Garder pour amelioration future

- Plusieurs Lambdas par domaine.
- Historique d'audit detaille sous forme d'entites DynamoDB separees.
- GSI3 pour decisions recentes ou reporting.
- Notifications email.
- OCR des justificatifs.
- Workflow d'approbation a seuil ou multi-approbateur.
- Exports comptables.
