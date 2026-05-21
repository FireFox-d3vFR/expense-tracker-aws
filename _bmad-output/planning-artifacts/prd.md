---
project: expense-tracker-aws
artifact: PRD
status: final
source:
  - docs/project-context.md
  - docs/source/Project_ExpenseTracker_5ENTAPP.pdf
---

# PRD - Expense Tracker AWS

Etat final : les exigences MVP Must sont implementees dans la release `v1.0.1`. Les exigences d'historique avance restent volontairement limitees a un audit minimal.

## 1. Vision produit

Expense Tracker AWS est une application serverless de gestion de notes de frais. Elle permet a un employe de creer une note de frais, joindre un justificatif, soumettre une demande de remboursement, puis suivre son etat. Elle permet a un Finance Manager de consulter les demandes en attente, verifier les details et le justificatif, puis approuver ou rejeter avec justification.

Le projet repond a un probleme industriel classique : les notes de frais gerees par email, fichiers Excel ou pieces jointes disperses entrainent des retards, des pertes de justificatifs, des risques de double remboursement et une faible auditabilite.

## 2. Objectifs

- Fournir une application fonctionnelle mobile/desktop avec .NET MAUI.
- Implementer une architecture AWS serverless basee sur Cognito, API Gateway, Lambda C#/.NET, DynamoDB, S3, IAM et CloudWatch.
- Garantir que les transitions d'etat sont controlees cote serveur.
- Appliquer un controle d'acces par role a partir des groupes ou claims Cognito.
- Stocker les justificatifs S3 sans exposition publique, uniquement via URLs pre-signees.
- Produire un repository publiable sur GitHub sans secret.
- Preparer une demonstration orale defensive : architecture, DynamoDB, RBAC, workflow et S3.

## 3. Utilisateurs et roles

### Employee

L'Employee cree, modifie, soumet et resoumet ses propres notes de frais.

Capacites attendues :
- Se connecter via Cognito.
- Creer une note en brouillon.
- Renseigner montant, devise, categorie, date de depense et description.
- Demander une URL pre-signee pour televerser un justificatif.
- Soumettre une note Draft.
- Consulter uniquement ses propres notes.
- Modifier une note Draft ou Rejected.
- Resoumettre une note Rejected.

### Finance Manager

Le Finance Manager traite les notes soumises.

Capacites attendues :
- Se connecter via Cognito.
- Consulter la file des notes Submitted et Resubmitted.
- Voir les details d'une note et obtenir une URL pre-signee de consultation du justificatif.
- Approuver une note.
- Rejeter une note avec justification obligatoire.
- Consulter les informations de decision disponibles dans le detail : statut, date de revue, reviewer et motif de rejet.

## 4. Workflow metier

Etats supportes :
- Draft
- Submitted
- Rejected
- Resubmitted
- Approved

Transitions autorisees :
- Draft -> Submitted
- Submitted -> Approved
- Submitted -> Rejected
- Rejected -> Resubmitted
- Resubmitted -> Approved
- Resubmitted -> Rejected

Regles :
- Le client MAUI ne decide jamais seul des transitions.
- Chaque transition est validee dans Lambda.
- Un Employee ne peut agir que sur ses propres notes.
- Un Finance Manager ne peut pas modifier le contenu metier d'une note, seulement approuver ou rejeter.
- Une note Approved est terminale.
- Une rejection requiert une justification non vide.

## 5. Exigences fonctionnelles

| ID | Exigence | Priorite |
| --- | --- | --- |
| FR-01 | Authentifier les utilisateurs via Cognito. | Must |
| FR-02 | Distinguer Employee et Finance Manager via groupes ou claims Cognito. | Must |
| FR-03 | Creer une note de frais en Draft. | Must |
| FR-04 | Modifier une note Draft ou Rejected appartenant a l'Employee. | Must |
| FR-05 | Generer une URL pre-signee S3 pour upload de justificatif. | Must |
| FR-06 | Soumettre une note Draft. | Must |
| FR-07 | Resoumettre une note Rejected. | Must |
| FR-08 | Lister les notes de l'Employee connecte. | Must |
| FR-09 | Lister la file Finance des notes Submitted/Resubmitted. | Must |
| FR-10 | Generer une URL pre-signee S3 pour consultation du justificatif. | Must |
| FR-11 | Approuver une note Submitted/Resubmitted. | Must |
| FR-12 | Rejeter une note Submitted/Resubmitted avec justification. | Must |
| FR-13 | Enregistrer les dates, auteurs et decisions pour audit minimal. | Should |
| FR-14 | Afficher un historique simple du cycle de vie. | Partiel |
| FR-15 | Prevenir les secrets dans le code et la configuration versionnee. | Must |

## 6. Exigences non fonctionnelles

- Securite : JWT Cognito obligatoire sur les endpoints API Gateway.
- RBAC : controle cote Lambda, pas seulement cote UI.
- Confidentialite : aucun objet S3 public.
- Observabilite : logs CloudWatch pour les appels critiques et erreurs.
- Maintenabilite : Lambdas C# structurees avec services metier testables.
- Simplicite : architecture adaptee a un projet scolaire, sans complexite inutile.
- Auditabilite : chaque changement d'etat garde une trace minimale.
- Portabilite : instructions de deploiement reproductibles.

## 7. Definition du MVP

Le MVP est atteint lorsque :
- Un Employee peut se connecter, creer une note avec justificatif, la soumettre et suivre son statut.
- Un Finance Manager peut se connecter, voir les notes en attente, approuver ou rejeter.
- Les transitions invalides sont refusees par Lambda.
- Les requetes sont filtrees selon le role et l'identite Cognito.
- Les justificatifs passent uniquement par URLs pre-signees.
- Le README explique architecture, prerequis, configuration, deploiement et demonstration.

## 8. Hors perimetre initial

- Paiement ou remboursement bancaire reel.
- OCR des justificatifs.
- Notifications email.
- Regles avancees de seuils d'approbation.
- Multi-organisation.
- Back-office administrateur complet.

## 9. Criteres d'acceptation globaux

- Le projet compile et peut etre lance localement pour la partie MAUI.
- Les Lambdas C# peuvent etre testees ou invoquees avec exemples de payload.
- Les ressources AWS sont decrites par scripts ou instructions claires.
- Le modele DynamoDB supporte les access patterns presentes dans le rapport.
- Le rapport peut expliquer clairement chaque service obligatoire et chaque GSI.
