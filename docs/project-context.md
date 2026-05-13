# Expense Tracker - Contexte projet

Projet scolaire AWS 5ENTAPP / E5WMD.

Objectif : développer une application serverless de gestion de notes de frais.

Stack obligatoire :
- .NET MAUI pour l'IHM mobile/desktop
- Amazon Cognito pour l'authentification et les rôles
- API Gateway comme point d'entrée REST
- AWS Lambda en C#/.NET pour la logique métier
- DynamoDB pour les notes de frais
- S3 pour les justificatifs via URLs pré-signées
- IAM pour les permissions
- CloudWatch pour les logs

Rôles :
- Employee : crée, modifie, soumet et resoumet une note de frais.
- Finance Manager : consulte la file d'attente, approuve ou rejette avec justification.

Workflow :
Draft -> Submitted -> Approved
Draft -> Submitted -> Rejected -> Resubmitted

Contraintes :
- Les transitions d'état doivent être validées côté serveur dans Lambda.
- L'accès aux données doit être filtré selon le rôle Cognito.
- Aucun justificatif S3 ne doit être public.
- Le projet doit être publiable sur GitHub sans secrets.