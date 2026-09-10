# Explication de la solution

## 1. Organisation générale

La solution sépare quatre responsabilités :

- **Domain** : agrégat `Blast`, entité `Hole`, règles métier et événements immuables.
- **Application** : commandes, requêtes, handlers et interfaces.
- **Infrastructure** : event store en mémoire, repository event-sourcé, projection de lecture et horloge.
- **Api** : contrats HTTP, endpoints Minimal API et traduction des exceptions en statuts HTTP.

## 2. Event Sourcing

Chaque blast possède un flux identifié par son `Guid`. Ce flux contient, dans l’ordre :

- `BlastCreated`
- `HoleAdded`
- `HoleCharged`
- `HoleMarkedReady` pour le bonus
- `BlastFired`

L’état courant n’est jamais enregistré directement. Lorsqu’une commande arrive, le repository charge les événements du flux et appelle `Blast.Rehydrate(...)`. L’agrégat applique alors chaque événement pour reconstruire son état.

Les méthodes métier ne modifient pas les champs directement. Elles vérifient les invariants puis lèvent un événement avec `Raise(...)`. La mutation privée est centralisée dans les méthodes `Apply(...)`.

## 3. Version et concurrence

La version d’un flux correspond au nombre d’événements déjà persistés :

- flux vide : version `0` ;
- premier événement : version `1` ;
- deuxième événement : version `2`, etc.

Lors de la sauvegarde, l’event store compare `expectedVersion` avec la version actuelle. Si deux requêtes ont chargé la même version et tentent d’écrire, la seconde reçoit une `OptimisticConcurrencyException`, transformée en HTTP `409 Conflict`.

## 4. Frontière d’agrégat

`Blast` est la racine d’agrégat et possède les trous. Les événements des trous sont conservés dans le même flux que le blast.

Ce choix permet à `FireBlast` de vérifier atomiquement l’état de tous les trous. Avec un flux séparé par trou, cette règle nécessiterait une coordination multi-flux ou un process manager.

`Hole` reste event-sourcé : son état est initialisé et modifié uniquement par `HoleAdded`, `HoleCharged` et `HoleMarkedReady`.

## 5. CQRS

Chaque action d’écriture possède son propre objet et son propre handler :

- `CreateBlastCommandHandler`
- `AddHoleCommandHandler`
- `ChargeHoleCommandHandler`
- `MarkHoleReadyCommandHandler`
- `FireBlastCommandHandler`

Les lectures sont séparées :

- `GetBlastQueryHandler` lit une projection préconstruite ;
- `GetBlastHistoryQueryHandler` lit directement le flux d’événements ordonné.

Aucun MediatR ni framework CQRS n’est utilisé. Les handlers sont injectés directement dans les endpoints.

## 6. Projection de lecture

`BlastReadModelProjection` s’abonne aux événements ajoutés dans l’event store. Elle maintient une vue destinée aux requêtes `GET /blasts/{blastId}`.

Dans cet exercice, la projection est mise à jour de façon synchrone : la lecture est immédiatement cohérente après une commande. Dans un système distribué, elle serait généralement asynchrone et devrait gérer les offsets, la reprise, l’idempotence et le rejeu.

## 7. Règles métier principales

- Un nom de blast et un nom de trou sont obligatoires.
- La direction doit être comprise entre `0` et `360` degrés.
- Un trou ne peut être chargé qu’à partir de `Planned`.
- Un trou ne peut devenir `Ready` qu’à partir de `Charged`.
- Un blast déjà tiré ne peut plus être modifié ni retiré.
- Un blast doit contenir au moins un trou avant le tir.
- Par défaut, tous les trous doivent être `Charged` ou `Ready` avant le tir.
- En mode bonus strict, tous les trous doivent être `Ready`.

## 8. Statuts HTTP

- `400 Bad Request` : invariant métier violé ;
- `404 Not Found` : blast ou trou inconnu ;
- `409 Conflict` : conflit de version optimiste ;
- `201 Created` : création d’un blast ou ajout d’un trou ;
- `204 No Content` : commande exécutée sans corps de réponse ;
- `200 OK` : requête de lecture réussie.

## 9. Fichiers à regarder en priorité

- `src/BlastManagement.Api/Domain/Blasts/Blast.cs`
- `src/BlastManagement.Api/Infrastructure/EventStore/InMemoryEventStore.cs`
- `src/BlastManagement.Api/Infrastructure/Projections/BlastReadModelProjection.cs`
- `src/BlastManagement.Api/Api/Endpoints/BlastEndpoints.cs`
- `src/BlastManagement.Api/Program.cs`
- `tests/BlastManagement.Tests/`
