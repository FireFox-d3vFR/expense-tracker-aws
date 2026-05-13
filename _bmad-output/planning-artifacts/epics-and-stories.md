---
project: expense-tracker-aws
artifact: Epics et user stories
status: refined-mvp
source:
  - docs/project-context.md
  - docs/source/Project_ExpenseTracker_5ENTAPP.pdf
---

# Epics et user stories

## Priorisation MVP

Le MVP privilegie une architecture simple et defendable :
- une Lambda API unique ;
- une table DynamoDB avec GSI1 et GSI2 uniquement ;
- endpoints REST regroupes quand cela reduit la complexite sans brouiller le metier ;
- tests concentres sur RBAC, machine d'etats et acces DynamoDB principaux.

## Epic 1 - Fondations projet et backend MVP

Objectif : poser une base deployable et maintenable.

### Story 1.1 - Initialiser le repository

En tant qu'equipe projet, je veux une structure de repository claire afin de separer MAUI, backend, infrastructure et documentation.

Acceptance criteria :
- La solution contient `src/mobile`, `src/backend`, `infra`, `docs`, `tests`.
- Le backend contient un projet `ExpenseTracker.Api`.
- `.gitignore` exclut les artefacts .NET et secrets.

### Story 1.2 - Creer la Lambda API unique

En tant que developpeur, je veux une Lambda API unique avec routage interne afin de simplifier le deploiement du MVP.

Acceptance criteria :
- `LambdaEntryPoint` recoit les requetes API Gateway.
- `RequestRouter` route les endpoints MVP.
- Les handlers restent minces et deleguent au domaine et a l'infrastructure.

### Story 1.3 - Documenter l'architecture cible

En tant qu'etudiant, je veux un diagramme et une justification des services AWS afin de defendre l'architecture.

Acceptance criteria :
- Le diagramme montre MAUI, Cognito, API Gateway, Lambda unique, DynamoDB, S3, IAM et CloudWatch.
- Le choix Lambda unique vs Lambdas multiples est justifie.
- Les ameliorations futures sont separees du MVP.

## Epic 2 - Authentification et RBAC

Objectif : securiser l'application avec Cognito et roles.

### Story 2.1 - Configurer Cognito

En tant qu'utilisateur, je veux me connecter avec un compte afin d'acceder a l'application selon mon role.

Acceptance criteria :
- Un User Pool Cognito existe.
- Deux groupes existent : `Employee`, `FinanceManager`.
- Les comptes de demo sont documentes sans secret.

### Story 2.2 - Proteger l'API avec Cognito

En tant que systeme, je veux valider les JWT avant Lambda afin de refuser les appels non authentifies.

Acceptance criteria :
- API Gateway utilise un Cognito Authorizer.
- Les routes MVP exigent un token valide.
- Lambda extrait `sub`, email/username et groupes.

### Story 2.3 - Appliquer le RBAC dans le domaine

En tant qu'utilisateur, je veux que mes droits soient controles cote serveur afin que l'IHM ne soit pas la seule protection.

Acceptance criteria :
- Les actions Employee verifient l'ownership.
- Les actions Finance exigent le groupe `FinanceManager`.
- Les tests unitaires couvrent au moins un refus Employee et un refus Finance.

## Epic 3 - Notes de frais Employee

Objectif : permettre la creation, modification et consultation des notes.

### Story 3.1 - Creer une note Draft

En tant qu'Employee, je veux creer une note de frais afin de preparer une demande de remboursement.

Acceptance criteria :
- `POST /expenses` cree une note `Draft`.
- La note est rattachee au `sub` Cognito.
- L'item DynamoDB alimente GSI1.

### Story 3.2 - Modifier une note Draft ou Rejected

En tant qu'Employee, je veux modifier une note corrigeable afin de mettre a jour mes informations.

Acceptance criteria :
- `PUT /expenses/{expenseId}` accepte `Draft` et `Rejected`.
- La modification est refusee pour `Submitted`, `Resubmitted` et `Approved`.
- L'owner est verifie cote Lambda.

### Story 3.3 - Lister et consulter mes notes

En tant qu'Employee, je veux consulter mes notes afin de suivre leur statut.

Acceptance criteria :
- `GET /expenses` utilise GSI1.
- `GET /expenses/{expenseId}` verifie owner ou role Finance.
- Un Employee ne voit pas les notes des autres.

## Epic 4 - Justificatifs S3

Objectif : gerer les justificatifs sans exposition publique.

### Story 4.1 - Generer une URL pre-signee d'upload

En tant qu'Employee, je veux uploader un justificatif afin d'associer une preuve a ma note.

Acceptance criteria :
- `POST /expenses/{expenseId}/receipt-url` avec `operation=upload` retourne une URL PUT.
- Le bucket S3 reste prive.
- Seul le proprietaire peut demander l'upload sur une note modifiable.

### Story 4.2 - Generer une URL pre-signee de consultation

En tant qu'utilisateur autorise, je veux consulter un justificatif afin de verifier une note.

Acceptance criteria :
- `operation=view` retourne une URL GET.
- Le proprietaire et Finance sont autorises.
- Aucune URL permanente ni objet public n'est expose.

## Epic 5 - Workflow serveur

Objectif : imposer la machine d'etats dans Lambda.

### Story 5.1 - Soumettre ou resoumettre une note

En tant qu'Employee, je veux envoyer ma note a Finance afin qu'elle soit traitee.

Acceptance criteria :
- `POST /expenses/{expenseId}/submit` transforme `Draft` en `Submitted`.
- La meme route transforme `Rejected` en `Resubmitted`.
- Tout autre etat est refuse.
- DynamoDB utilise une condition sur owner et statut.

### Story 5.2 - Tester la machine d'etats

En tant que developpeur, je veux des tests unitaires sur les transitions afin de prouver que les contournements client sont impossibles.

Acceptance criteria :
- Les transitions autorisees passent.
- Les transitions interdites echouent.
- Les roles non autorises echouent.

## Epic 6 - Revue Finance

Objectif : permettre au Finance Manager de traiter la file.

### Story 6.1 - Afficher la file Finance

En tant que Finance Manager, je veux voir les notes Submitted et Resubmitted afin de les traiter.

Acceptance criteria :
- `GET /finance/queue` interroge GSI2 pour `Submitted` et `Resubmitted`.
- Les resultats sont fusionnes et tries.
- Un Employee ne peut pas acceder a la file.

### Story 6.2 - Approuver ou rejeter une note

En tant que Finance Manager, je veux prendre une decision afin de terminer ou relancer le workflow.

Acceptance criteria :
- `POST /finance/expenses/{expenseId}/review` accepte `decision=approve` ou `decision=reject`.
- `approve` transforme `Submitted/Resubmitted` en `Approved`.
- `reject` transforme `Submitted/Resubmitted` en `Rejected` avec justification obligatoire.
- GSI2 est nettoyee apres decision.

## Epic 7 - Demo, tests et documentation

Objectif : rendre le projet defendable et livrable.

### Story 7.1 - Rediger README et instructions de deploiement

En tant qu'evaluateur, je veux comprendre comment lancer et deployer le projet afin de verifier le travail.

Acceptance criteria :
- README couvre architecture, prerequis, configuration, deploiement et demo.
- Aucune valeur secrete n'est presente.
- Les endpoints MVP sont documentes.

### Story 7.2 - Preparer le plan de demo

En tant qu'equipe, je veux un scenario de demo court afin de montrer les points evalues.

Acceptance criteria :
- Le scenario montre Employee puis Finance.
- Le scenario montre une URL S3 pre-signee.
- Le scenario inclut une transition refusee par Lambda.

### Story 7.3 - Preparer le rapport 5 pages

En tant qu'equipe, je veux un rapport concis afin de repondre aux livrables officiels.

Acceptance criteria :
- Le rapport inclut probleme, architecture, DynamoDB, extraits de code et conclusion.
- Le modele DynamoDB liste GSI1, GSI2 et les access patterns MVP.
- Les choix non retenus sont expliques comme ameliorations futures.

## Ameliorations futures hors MVP

- Plusieurs Lambdas par domaine.
- Entites d'audit DynamoDB detaillees.
- GSI3 pour reporting Finance.
- Notifications email.
- OCR des justificatifs.
- Approbation par seuil ou double validation.
- Export comptable.
