---
project: expense-tracker-aws
artifact: Strategie demo tests README rapport
status: draft
source:
  - docs/project-context.md
  - docs/source/Project_ExpenseTracker_5ENTAPP.pdf
---

# Strategie demo, tests, README et rapport

## 1. Strategie de demo

Objectif : demontrer en moins de 8 minutes les exigences centrales du sujet.

### Scenario recommande

1. Connexion Employee.
2. Creation d'une note Draft.
3. Generation d'une URL pre-signee PUT et upload d'un justificatif.
4. Soumission de la note : `Draft -> Submitted`.
5. Connexion Finance Manager.
6. Consultation de la file Finance via statuts Submitted/Resubmitted.
7. Consultation du justificatif via URL pre-signee GET.
8. Rejet avec justification : `Submitted -> Rejected`.
9. Retour Employee, affichage du motif, correction et resoumission : `Rejected -> Resubmitted`.
10. Retour Finance, approbation : `Resubmitted -> Approved`.
11. Montrer rapidement CloudWatch ou logs Lambda prouvant la validation serveur.

### Point defensif a montrer

Preparer un appel API ou test qui tente une transition invalide, par exemple :
- `Draft -> Approved`
- Employee qui tente `/approve`
- Employee qui tente de lire une note d'un autre utilisateur

Resultat attendu : refus par Lambda, pas par l'IHM.

## 2. Strategie de tests

### Tests unitaires backend

Priorite haute :
- `ExpenseStateMachineTests`
- `ExpensePolicyTests`
- `ReceiptKeyBuilderTests`
- tests de mapping claims Cognito vers role applicatif

Cas minimum :
- `Draft -> Submitted` autorise pour owner.
- `Submitted -> Approved` autorise pour Finance.
- `Submitted -> Rejected` exige justification.
- `Rejected -> Resubmitted` autorise pour owner.
- `Approved -> Rejected` interdit.
- Employee non-owner interdit.

### Tests d'integration legers

Avec DynamoDB local ou tests controles sur environnement AWS :
- creation puis lecture detail,
- query GSI1 par employee,
- query GSI2 par statut,
- update conditionnel qui echoue si statut inattendu.

### Tests manuels MAUI

Checklist :
- login Employee,
- creation note,
- upload justificatif,
- soumission,
- affichage statut,
- login Finance,
- rejet,
- resoumission,
- approbation,
- affichage final Approved.

### Tests securite

- Appel API sans token : 401.
- Token Employee sur endpoint Finance : 403.
- Acces a note autre Employee : 403 ou 404.
- Objet S3 inaccessible sans URL pre-signee.
- URL pre-signee expiree refusee par S3.

## 3. Strategie README

Structure recommandee :

```text
# Expense Tracker AWS

## Contexte
## Fonctionnalites
## Architecture
## Modele DynamoDB
## Prerequis
## Configuration
## Deploiement AWS
## Lancement MAUI
## Comptes de demo
## Parcours de demonstration
## Tests
## Securite et secrets
## Limites connues
```

Regles :
- Ne jamais mettre de mot de passe reel.
- Utiliser `parameters.example.json`.
- Expliquer ou trouver les valeurs Cognito/API Gateway apres deploiement.
- Mettre le lien du repository GitHub dans le rapport final, pas forcement dans le README local.

## 4. Strategie rapport 5 pages

Le rapport officiel ne doit pas depasser 5 pages hors annexes. Il faut donc viser dense et defendable.

Plan propose :

### Page 1 - Introduction et probleme

- Probleme industriel : email, justificatifs disperses, delais, audit difficile.
- Objectif : workflow serverless de notes de frais.
- Roles : Employee et Finance Manager.

### Page 2 - Architecture AWS

- Diagramme annote.
- Justification courte de chaque service obligatoire.
- Flux principal : MAUI -> API Gateway -> Lambda -> DynamoDB/S3.

### Page 3 - Modele DynamoDB

- Table `ExpenseReports`.
- PK/SK.
- GSI1 Employee.
- GSI2 Finance queue.
- Eventuellement GSI3 optionnelle, ou expliquer pourquoi elle n'est pas retenue.
- Tableau des access patterns.

### Page 4 - Elements d'implementation

Extraits de code commentes :
- lecture des claims Cognito et RBAC,
- machine d'etats,
- `UpdateItem` conditionnel,
- generation URL pre-signee S3.

### Page 5 - Conclusion

- Difficultes : modele DynamoDB, securisation S3, transitions serveur.
- Solutions : access patterns, conditions DynamoDB, Cognito groups.
- Ameliorations : notifications, OCR, seuils d'approbation, export comptable.

## 5. Definition of Done projet

- Les artefacts BMAD sont disponibles.
- Le repository est structure.
- Le README permet a un evaluateur de comprendre et lancer le projet.
- Le rapport couvre explicitement les livrables officiels.
- La demo montre les deux roles et le workflow complet.
- Les tests prouvent au moins la machine d'etats et le RBAC.
- Le ZIP final contient source MAUI, Lambdas, scripts/instructions et rapport PDF.
