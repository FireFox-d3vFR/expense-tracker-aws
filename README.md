# Expense Tracker AWS

Projet scolaire AWS de gestion de notes de frais, construit progressivement avec une approche MVP.

## Stack cible

- .NET MAUI pour l'IHM mobile/desktop
- AWS Lambda en C#/.NET pour la logique backend
- Amazon API Gateway pour l'exposition REST
- Amazon Cognito pour l'authentification et les roles
- Amazon DynamoDB pour les notes de frais
- Amazon S3 pour les justificatifs via URLs pre-signees
- AWS IAM pour les permissions

## Etat actuel

Le socle backend est initialise avant l'ajout de l'IHM MAUI :

- backend foundation termine ;
- domaine metier cree et teste ;
- contrats REST definis ;
- abstractions infrastructure preparees ;
- routeur Lambda skeleton cree ;
- pas encore de projet MAUI ;
- pas encore d'integration AWS reelle.

## Tests

```powershell
dotnet test src/ExpenseTracker.sln
```

## Artefacts BMAD

Le cadrage projet est disponible ici :

[_bmad-output/planning-artifacts/index.md](_bmad-output/planning-artifacts/index.md)

## Securite

Aucun secret AWS, token, mot de passe ou fichier de configuration sensible ne doit etre versionne.
