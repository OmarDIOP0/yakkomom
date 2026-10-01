# Mise en ligne de Yakkomom

Hébergement 100 % gratuit pour démarrer :

| Rôle | Service | Offre gratuite |
|---|---|---|
| Application (conteneur Docker) | [Render](https://render.com) | 512 Mo de RAM, mise en veille après 15 min sans visite |
| Base PostgreSQL | [Neon](https://neon.tech) | 0,5 Go, ne s'efface pas (contrairement à la base gratuite de Render, supprimée après 30 jours) |
| Photos et documents | [Cloudinary](https://cloudinary.com) | 25 crédits par mois (environ 25 Go de stockage et de trafic) |
| Réveil automatique | [UptimeRobot](https://uptimerobot.com) ou [cron-job.org](https://cron-job.org) | Une vérification toutes les 5 minutes |

Aucun secret n'est écrit dans le dépôt : tout passe par les variables d'environnement de Render.

---

## 1. Base de données : Neon

1. Créez un compte sur neon.tech, puis un projet `yakkomom`, région **AWS Europe Central (Frankfurt)**, la même que Render.
2. Dans **Connect**, décochez « Connection pooling » et copiez l'URL. Elle ressemble à
   `postgresql://utilisateur:motdepasse@ep-xxxx.eu-central-1.aws.neon.tech/neondb?sslmode=require`.
   La connexion directe, sans pooling, est nécessaire pour que les migrations s'appliquent au démarrage.
3. Gardez cette URL pour l'étape 4 (variable `DATABASE_URL`). Ne la partagez pas.

Les tables, les régions et communes du Sénégal et les services sont créés automatiquement au premier démarrage.

## 2. Photos et documents : Cloudinary

1. Créez un compte sur cloudinary.com.
2. Dans **Dashboard → API Keys**, copiez la variable d'environnement `CLOUDINARY_URL`.
   Elle a la forme `cloudinary://123456:abcdef@nom-du-cloud`.
3. Gardez-la pour l'étape 4.

Les photos publiques sont servies par Cloudinary, qui les redimensionne et choisit le meilleur format selon le téléphone.
Les documents fonciers privés sont stockés en mode « authenticated » : ils ne sont lisibles
que par un lien signé valable 5 minutes, généré pour un admin connecté.

## 3. Code sur GitHub

Render déploie depuis le dépôt GitHub `OmarDIOP0/yakkomom`, branche `main`.
Chaque `git push` sur `main` redéploie automatiquement le site.

## 4. Application : Render

1. Créez un compte sur render.com et connectez votre compte GitHub.
2. **New → Blueprint**, choisissez le dépôt `yakkomom`. Render lit `render.yaml` à la racine.
3. Renseignez les valeurs demandées :

   | Variable | Valeur |
   |---|---|
   | `DATABASE_URL` | l'URL Neon de l'étape 1 |
   | `CLOUDINARY_URL` | l'URL Cloudinary de l'étape 2 |
   | `AdminInitial__Email` | l'e-mail du premier SuperAdmin |
   | `AdminInitial__MotDePasse` | un mot de passe solide (10 caractères minimum, dont au moins un chiffre) |

   Les autres variables sont déjà remplies par `render.yaml` :

   - `Stockage__Fournisseur=Cloudinary`
   - `Demo__Terrains=true` : crée les 3 terrains de démonstration
   - `Site__Indexable=false` : Google n'indexe pas le site de démonstration

4. **Apply**. La première construction prend 5 à 10 minutes. Le site est ensuite disponible à
   l'adresse `https://yakkomom.onrender.com`, ou `https://yakkomom-xxxx.onrender.com` si le nom est déjà pris.
5. Vérifiez que `https://…onrender.com/health` affiche `ok`.

### Premiers réglages
1. Connectez-vous sur `/admin/connexion` avec l'e-mail et le mot de passe de l'étape 4.
2. **Paramètres du site** : saisissez le ou les numéros WhatsApp, l'adresse, les horaires et les réseaux sociaux.
   Sans numéro WhatsApp, les boutons WhatsApp ne peuvent pas fonctionner.
3. **Utilisateurs** : créez les comptes de l'équipe. Chaque personne reçoit un mot de passe provisoire à changer.

## 5. Éviter la mise en veille

Sur l'offre gratuite, Render met le site en veille après 15 minutes sans visite. Le visiteur suivant attend alors
30 à 60 secondes. Pour l'éviter :

1. Créez un compte sur uptimerobot.com (ou cron-job.org).
2. Ajoutez une sonde **HTTP(s)** sur `https://…onrender.com/health`, **toutes les 5 minutes**.

`/health` ne touche pas à la base de données, donc la sonde ne réveille pas inutilement la base Neon.
Un seul service allumé en permanence consomme environ 720 heures par mois, dans la limite des 750 heures gratuites de Render.

## 6. Passer de la démonstration au vrai site

1. Dans Render, supprimez la variable `Demo__Terrains` (ou mettez-la à `false`).
2. Dans l'admin, passez les 3 terrains « (démo) » en **Archivé**, ou supprimez-les (réservé au SuperAdmin).
3. Mettez `Site__Indexable=true` (ou supprimez la variable).
4. Nom de domaine :
   - dans Render : **Settings → Custom Domains**, ajoutez par exemple `yakkomom.sn`, puis créez chez votre registraire l'enregistrement DNS indiqué ;
   - ajoutez ensuite `Site__UrlPublique=https://yakkomom.sn` pour que les liens partagés et le sitemap utilisent ce domaine.
5. Faites relire la page « Acheter en toute sécurité » par un notaire.

## Variables d'environnement (référence)

| Variable | Rôle | Défaut |
|---|---|---|
| `DATABASE_URL` ou `ConnectionStrings__DefaultConnection` | Connexion PostgreSQL | obligatoire |
| `Stockage__Fournisseur` | `Local` (disque) ou `Cloudinary` | `Local` |
| `CLOUDINARY_URL` | Accès Cloudinary | |
| `Stockage__DossierCloudinary` | Dossier racine dans Cloudinary | `yakkomom` |
| `AdminInitial__Email`, `AdminInitial__MotDePasse`, `AdminInitial__Nom` | Premier SuperAdmin, créé seulement s'il n'en existe aucun | |
| `Site__UrlPublique` | Domaine officiel (liens canoniques, partages, sitemap) | domaine de la requête |
| `Site__Indexable` | `false` : robots.txt interdit tout et chaque page porte `X-Robots-Tag: noindex` | `true` |
| `Demo__Terrains` | `true` : crée les terrains de `Demo/terrains.json` s'ils n'existent pas | `false` |
| `Database__MigrerAuDemarrage` | Applique les migrations au démarrage | `true` |
| `PORT` | Port d'écoute (fourni par Render) | 10000 dans l'image Docker |

## Dépannage
- **Le déploiement échoue au démarrage** : dans Render, onglet **Logs**. Le message « Aucune chaîne de connexion » signifie que `DATABASE_URL` est absente.
- **Les photos disparaissent après un redéploiement** : `Stockage__Fournisseur` n'est pas `Cloudinary`. Le disque de Render est effacé à chaque déploiement.
- **Mot de passe SuperAdmin oublié** : un autre SuperAdmin peut le réinitialiser depuis **Utilisateurs**.
  Changer `AdminInitial__MotDePasse` n'a aucun effet si le compte existe déjà : c'est voulu.

## Tests automatiques

```bash
dotnet test
```

- **Tests unitaires** (sans base) : formats FCFA et téléphones, saisies, URL, liens et messages WhatsApp, complétude des fiches, surfaces GPS, reconnaissance des fichiers envoyés, politique de sécurité.
- **Tests d'intégration** : l'application complète, sur une base PostgreSQL jetable créée puis supprimée automatiquement. Ils vérifient notamment :
  - les documents privés, jamais accessibles sans connexion admin ;
  - les filtres et les brouillons, jamais visibles du public ;
  - les clics WhatsApp, sans donnée personnelle et sans doublon ;
  - l'admin, le jeton anti-falsification et les en-têtes de sécurité.

La connexion au serveur PostgreSQL de test est lue dans la variable `YK_TESTS_DB` (chaîne Npgsql, avec le droit de créer des bases), ou à défaut dans les user-secrets de développement.
