---
project: expense-tracker-aws
artifact: Strategie demo tests README rapport
status: final
source:
  - docs/project-context.md
  - docs/source/Project_ExpenseTracker_5ENTAPP.pdf
---

# Strategie demo, tests, README et rapport

Ce document decrit la strategie finale de soutenance pour la release `v1.0.1`.

## 1. Strategie de demo

Objectif : demontrer en moins de 8 minutes les exigences centrales du sujet, sans s'ecarter du parcours MVP.

Scenario recommande :

1. Lancer l'application MAUI.
2. Se connecter avec un compte Employee.
3. Creer une note de frais.
4. Ouvrir le detail et generer une URL pre-signee d'upload de justificatif.
5. Soumettre la note : `Draft -> Submitted`.
6. Se deconnecter puis se connecter en Finance Manager.
7. Ouvrir la file Finance et consulter la note.
8. Rejeter avec justification : `Submitted -> Rejected`.
9. Revenir Employee, afficher le motif et resoumettre : `Rejected -> Resubmitted`.
10. Revenir Finance et approuver : `Resubmitted -> Approved`.
11. Montrer un test backend ou un appel refuse pour prouver que le serveur bloque les transitions invalides.

Points defensifs a preparer :

- Employee qui tente un endpoint Finance : refus.
- Rejet sans justification : refus.
- Transition impossible comme `Approved -> Rejected` : refus.
- Objet S3 sans URL pre-signee : inaccessible.

## 2. Strategie de tests

Commande principale :

```powershell
dotnet test src\ExpenseTracker.sln
```

Couverture existante :

- machine d'etats ;
- politiques RBAC et ownership ;
- routeur API ;
- handlers Employee, Finance, `/me` et receipts ;
- mapping Cognito ;
- mapping et requetes DynamoDB ;
- generation d'URLs pre-signees S3 ;
- construction de cles de justificatifs.

Tests manuels MAUI :

- login Employee ;
- creation note ;
- upload URL justificatif ;
- soumission ;
- affichage statut ;
- login Finance ;
- rejet ;
- resoumission ;
- approbation.

## 3. Presentation GitHub

Le README final sert de point d'entree. Il couvre :

- contexte ;
- fonctionnalites ;
- architecture ;
- endpoints ;
- prerequis ;
- build/test/run ;
- deploiement AWS ;
- configuration MAUI ;
- parcours de demonstration ;
- limites connues ;
- decisions techniques ;
- securite.

Regles :

- Ne pas publier de secret AWS.
- Ne pas publier de mot de passe reel.
- Utiliser les scripts et outputs CloudFormation pour recuperer les valeurs d'environnement.

## 4. Rapport final

Le rapport final Markdown est disponible dans `docs/report/report.md`.

Il est structure pour tenir en environ 5 pages une fois converti :

1. contexte et objectif ;
2. architecture AWS ;
3. modele DynamoDB ;
4. implementation et securite ;
5. tests, limites et conclusion.

## 5. Definition of Done documentaire

- README a jour avec l'etat final.
- Contexte projet finalise.
- Rapport Markdown present.
- Artefacts BMAD indexes et coherents avec la release.
- Scripts infra documentes.
- Limitations et pistes futures explicites.
- Aucune modification de code applicatif necessaire pour cette passe.
