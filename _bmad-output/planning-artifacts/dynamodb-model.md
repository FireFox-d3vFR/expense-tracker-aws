---
project: expense-tracker-aws
artifact: Modele DynamoDB
status: refined-mvp
source:
  - docs/project-context.md
  - docs/source/Project_ExpenseTracker_5ENTAPP.pdf
---

# Modele DynamoDB

## 1. Decision MVP

Le MVP utilise une table unique `ExpenseReports` avec seulement deux GSIs :
- GSI1 pour lister les notes d'un Employee ;
- GSI2 pour alimenter la file Finance par statut.

On retire GSI3 du MVP. Le reporting des decisions recentes et l'historique detaille sont gardes comme ameliorations futures. Cela limite le deploiement, le cout cognitif et les explications pendant la soutenance.

## 2. Table principale

Nom : `ExpenseReports`

Cle primaire :
- `PK` : `EXPENSE#{expenseId}`
- `SK` : `METADATA`

Le MVP stocke une seule ligne par note de frais. Les champs `status`, `rejectionReason`, `reviewedAt`, `reviewedBy`, `submittedAt` et `updatedAt` fournissent un audit minimal suffisant pour expliquer le workflow.

## 3. Attributs principaux

| Attribut | Exemple | Description |
| --- | --- | --- |
| `PK` | `EXPENSE#exp_123` | Partition de la note. |
| `SK` | `METADATA` | Ligne principale. |
| `expenseId` | `exp_123` | Identifiant metier. |
| `employeeId` | Cognito `sub` | Proprietaire. |
| `employeeEmail` | `employee@example.com` | Affichage et audit leger. |
| `amount` | `42.50` | Montant. |
| `currency` | `EUR` | Devise. |
| `category` | `Travel` | Categorie. |
| `description` | `Train client` | Description. |
| `expenseDate` | `2026-05-13` | Date de depense. |
| `status` | `Submitted` | Etat courant. |
| `receiptKey` | `receipts/user/expense/file.jpg` | Cle S3 privee. |
| `createdAt` | ISO-8601 | Date creation. |
| `updatedAt` | ISO-8601 | Date derniere modification. |
| `submittedAt` | ISO-8601 | Date derniere soumission ou resoumission. |
| `reviewedAt` | ISO-8601 | Date derniere decision Finance. |
| `reviewedBy` | Cognito `sub` | Finance Manager ayant decide. |
| `rejectionReason` | texte | Motif obligatoire si `Rejected`. |
| `GSI1PK` | `EMPLOYEE#user-123` | Partition GSI1. |
| `GSI1SK` | `UPDATED#2026-05-13T10:00:00Z#EXPENSE#exp_123` | Tri GSI1. |
| `GSI2PK` | `STATUS#Submitted` | Partition GSI2 si la note est dans la file Finance. |
| `GSI2SK` | `SUBMITTED#2026-05-13T10:00:00Z#EXPENSE#exp_123` | Tri GSI2. |

## 4. GSI1 - Notes par Employee

But : afficher les notes de l'utilisateur connecte sans scan global.

- Partition key : `GSI1PK = EMPLOYEE#{employeeId}`
- Sort key : `GSI1SK = UPDATED#{updatedAt}#EXPENSE#{expenseId}`

Requete :

```text
Query GSI1
where GSI1PK = EMPLOYEE#{sub}
ScanIndexForward = false
```

Cette GSI sert l'ecran Employee "mes notes".

## 5. GSI2 - File Finance

But : afficher les notes en attente de revue.

- Partition key : `GSI2PK = STATUS#{status}`
- Sort key : `GSI2SK = SUBMITTED#{submittedAt}#EXPENSE#{expenseId}`

Statuts indexes :
- `Submitted`
- `Resubmitted`

Pour les statuts hors file Finance (`Draft`, `Rejected`, `Approved`), `GSI2PK` et `GSI2SK` peuvent etre absents. Ainsi, seules les notes actionnables apparaissent dans GSI2.

Requete :

```text
Query GSI2 where GSI2PK = STATUS#Submitted
Query GSI2 where GSI2PK = STATUS#Resubmitted
```

L'API execute deux queries et fusionne les resultats par `submittedAt`.

## 6. Access patterns MVP

| ID | Access pattern | Methode DynamoDB | Index |
| --- | --- | --- | --- |
| AP-01 | Creer une note Draft. | `PutItem` avec `attribute_not_exists(PK)` | Table |
| AP-02 | Lire le detail d'une note. | `GetItem PK=EXPENSE#id SK=METADATA` | Table |
| AP-03 | Modifier une note Draft ou Rejected. | `UpdateItem` condition owner + status | Table |
| AP-04 | Soumettre Draft ou resoumettre Rejected. | `UpdateItem` condition owner + status attendu | Table |
| AP-05 | Lister les notes Employee. | `Query GSI1PK=EMPLOYEE#sub` | GSI1 |
| AP-06 | Lister file Finance Submitted. | `Query GSI2PK=STATUS#Submitted` | GSI2 |
| AP-07 | Lister file Finance Resubmitted. | `Query GSI2PK=STATUS#Resubmitted` | GSI2 |
| AP-08 | Approuver ou rejeter une note. | `UpdateItem` condition status Submitted/Resubmitted | Table |
| AP-09 | Attacher ou remplacer une cle S3 de justificatif. | `UpdateItem` condition owner + status modifiable | Table |

## 7. Conditions importantes

Creation :

```text
ConditionExpression: attribute_not_exists(PK)
```

Modification Employee :

```text
ConditionExpression: employeeId = :employeeId AND (#status = :draft OR #status = :rejected)
```

Soumission :

```text
ConditionExpression: employeeId = :employeeId AND #status = :draft
```

Resoumission :

```text
ConditionExpression: employeeId = :employeeId AND #status = :rejected
```

Decision Finance :

```text
ConditionExpression: #status = :submitted OR #status = :resubmitted
```

## 8. Mise a jour des index selon le statut

| Nouveau statut | GSI1 | GSI2 |
| --- | --- | --- |
| `Draft` | Mettre a jour `GSI1SK` avec `updatedAt`. | Supprimer `GSI2PK/GSI2SK`. |
| `Submitted` | Mettre a jour `GSI1SK`. | `STATUS#Submitted`. |
| `Rejected` | Mettre a jour `GSI1SK`. | Supprimer `GSI2PK/GSI2SK`. |
| `Resubmitted` | Mettre a jour `GSI1SK`. | `STATUS#Resubmitted`. |
| `Approved` | Mettre a jour `GSI1SK`. | Supprimer `GSI2PK/GSI2SK`. |

## 9. Ameliorations futures

- Entites `EVENT#...` pour historiser toutes les transitions.
- GSI3 `REVIEWED#{status}` pour decisions recentes.
- Detection de doublons par montant/date/receipt hash.
- Export par periode pour usage comptable.
