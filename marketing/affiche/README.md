# Affiche promotionnelle YakkoMoM

Affiche, panneau, bannières web et visuels réseaux sociaux, générés à partir de sources HTML/CSS,
dans l'identité de l'application : papier, encre, latérite, Fraunces, angles droits, filets.

```
marketing/affiche/
├── sources/           HTML/CSS de chaque format
│   ├── commun.css            identité commune (couleurs, polices, téléphone, QR)
│   ├── affiche-portrait.css  mise en page commune A3 / A2
│   ├── a3.html, a2.html      affiches imprimées
│   ├── panneau.html          panneau 4 × 3 m (échelle 1/10)
│   ├── banniere-web.html, banniere-mobile.html, social-carre.html, social-story.html
│   ├── ecran/                vrai écran « fiche terrain » de l'application, figé (voir plus bas)
│   └── assets/               logo, police Fraunces, courbes de niveau
├── qrcodes/           QR codes SVG (vectoriels) + PNG 2000 px, un par support
├── exports/
│   ├── print/         A3 et A2 : PDF (fond perdu 3 mm), PNG 300 dpi, aperçu, HTML autonome
│   ├── panneau/       4 × 3 m : PDF vectoriel à l'échelle 1/10, PNG haute définition
│   ├── web/           bannières 1920 × 800 et 1080 × 1350 : WebP + PNG
│   └── social/        carré 1080 × 1080 et story 1080 × 1920 : PNG
└── scripts/           génération et contrôles
```

## Régénérer les exports

Prérequis : Node.js et Microsoft Edge (ou Chrome : définir `CHROME_PATH`). Python seulement pour `formats-ecran.py`.

```bash
cd marketing/affiche
npm install                      # une seule fois
node scripts/qrcodes.mjs         # QR codes + vérification de lecture
python scripts/formats-ecran.py  # sources des bannières et visuels réseaux (si modifiés)
node scripts/exporter.mjs        # tous les formats (ou : node scripts/exporter.mjs a3 panneau …)
node scripts/autonome.mjs a3 a2  # versions HTML « une seule page », sans fichier lié
node scripts/verifier-exports.mjs   # relit le QR code de chaque image exportée
```

Aperçu à l'écran : ouvrir un fichier de `sources/` dans un navigateur ; il s'ajuste à la fenêtre.
Ajouter `?guides` pour afficher la coupe (bleu) et la zone de sécurité (rose).

## Changer l'adresse du QR code

1. Modifier `SITE` dans `scripts/config.mjs` (ex. `https://yakkomom.sn`).
2. Modifier l'adresse affichée en clair dans les sources (`yakkomom.onrender.com`).
3. Relancer `qrcodes.mjs`, `exporter.mjs`, `autonome.mjs`, puis `verifier-exports.mjs`.
4. Copier le nouveau `qrcodes/qr-web.svg` dans `Yakkomom/wwwroot/img/` et l'A3 dans
   `Yakkomom/wwwroot/telechargements/` (bannière du site).

Les QR codes pointent vers des adresses courtes, qui redirigent vers la liste des terrains avec les paramètres UTM :

| Support | Adresse du QR | Paramètres ajoutés |
|---|---|---|
| Affiches A3 / A2 | `/affiche` | `utm_source=affiche&utm_medium=print` |
| Panneau 4 × 3 m | `/panneau` | `utm_source=panneau&utm_medium=outdoor` |
| Bannières web | `/web` | `utm_source=site&utm_medium=web` |
| Réseaux sociaux | `/reseaux` | `utm_source=reseaux&utm_medium=social` |

La redirection est définie dans `Yakkomom/Controllers/CampagnesController.cs`. On peut changer la page de
destination sans réimprimer : l'adresse imprimée ne change jamais.

## QR codes

- Correction d'erreur **H** (30 %), indispensable avec le logo au centre (moins de 5 % de la surface).
- Modules arrondis **jointifs** : des modules séparés (points ronds) ne passaient pas le contrôle de lecture.
- Zone de silence de 4 modules, modules encre `#1C2A21` sur blanc.
- `qrcodes.mjs` décode chaque QR à 1200, 400 et 180 px ; `verifier-exports.mjs` relit le QR de chaque export,
  cadré (téléphone qui vise) et en vue d'ensemble (photo de loin). Les deux s'arrêtent en erreur si un QR est illisible.

## Écran du téléphone

Le téléphone affiche le **vrai** écran de l'application, et non une imitation : `scripts/capturer-ecran.mjs`
ouvre la fiche d'un terrain dans l'app (version mobile, thème clair) et la fige dans `sources/ecran/`,
avec sa feuille de style, ses icônes et sa photo. Il reste vectoriel dans les PDF.

Pour changer de terrain (par exemple un vrai terrain publié) : lancer l'application en local, puis

```bash
YK_FICHE=/terrains/yk-0042-… node scripts/capturer-ecran.mjs
```

Le nom affiché dans l'écran vient des paramètres du site de la base locale (« YakkoMoM »).

## Impression

- **A3 / A2** : PDF au format fini + **3 mm de fond perdu** (303 × 426 mm et 426 × 600 mm). Marge de sécurité
  de 14 mm en A3 (20 mm en A2). Préciser à l'imprimeur : format fini A3 (ou A2), fond perdu inclus, sans traits de coupe.
- **Panneau 4 × 3 m** : PDF vectoriel à l'**échelle 1/10** (400 × 300 mm + 5 mm de fond perdu, soit 5 cm réels).
  À agrandir à 1000 %. QR code de 1,20 m (40 % de la hauteur).
- Couleurs en RVB (l'imprimeur convertit en CMJN) ; la police Fraunces est incorporée au PDF sous forme de tracés.
- Les photos d'illustration de l'écran proviennent des terrains de démonstration (photo CC0).

## Sur le site

Le composant `Yakkomom/Views/Shared/_BanniereAffiche.cshtml` (styles : `.banniere-affiche` dans `site.css`) reprend
l'accroche, propose le téléchargement de l'affiche A3 en PDF et le partage du site. Le QR code s'affiche sur
ordinateur seulement. Il est placé sur la page d'accueil, avant le bandeau de contact ; il peut aussi être ajouté
ailleurs avec `<partial name="_BanniereAffiche" />`.
