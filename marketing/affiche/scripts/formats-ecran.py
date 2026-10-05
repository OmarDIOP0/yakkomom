# Génère les sources HTML des formats écran (bannières web, réseaux sociaux) : mêmes éléments que l'affiche
# (marque, accroche, vrai écran de l'app, QR code), composition propre à chaque format.
# Usage : python scripts/formats-ecran.py   (depuis marketing/affiche)
from pathlib import Path

SOURCES = Path(__file__).resolve().parent.parent / 'sources'

ENTETE = '''<!doctype html>
<html lang="fr">
<head>
<meta charset="utf-8">
<title>YakkoMoM — {titre}</title>
<link rel="stylesheet" href="commun.css">
<style>
  /* {titre} : {w} × {h} px. QR code : {qr}. Fichier généré par scripts/formats-ecran.py. */
  html, body {{ width: {w}px; height: {h}px; }}
  .page {{ position: relative; width: {w}px; height: {h}px; overflow: hidden; background: var(--papier); }}
  .a {{ position: absolute; }}
  .accroche span {{ display: block; }}
  .accroche {{ font-family: var(--f-titre); font-weight: 600; letter-spacing: -.035em; line-height: .9; }}
  .accroche .accent {{ color: var(--laterite); }}
  .sous-titre {{ font: italic 380 1em/1.2 var(--f-titre); color: var(--muet); letter-spacing: -.01em; }}
  .appel {{ color: var(--papier); }}
  .appel h2 {{ font-family: var(--f-titre); font-weight: 600; line-height: 1; letter-spacing: -.02em; }}
  .appel .url {{ font-family: var(--f-mono); font-weight: 600; border-top: 1px solid rgb(244 236 221 / .35); }}
  .appel .mono {{ color: var(--ocre); }}
  .fond-encre {{ background: var(--encre); }}
  .telephone {{ z-index: 3; }}
{css}
</style>
</head>
<body>
<main class="page">
{corps}
</main>
<script src="apercu.js"></script>
</body>
</html>
'''

MARQUE = '<div class="a marque"><img src="assets/logo.svg" alt=""><span class="marque__nom">Yakko<em>MoM</em></span></div>'
COURBES = '<img class="a courbes" src="assets/courbes-niveau.svg" alt="">'
SOUS_TITRE = '<p class="a sous-titre">Voyez le terrain avant d\'y aller. Photos, visite 360°, position GPS.</p>'
TELEPHONE = ('<div class="a telephone"><div class="telephone__ecran"><iframe src="ecran/fiche.html" '
             'title="Écran de l\'application YakkoMoM" scrolling="no"></iframe></div></div>')
ARGUMENTS = '''<ol class="a arguments">
    <li><span class="num mono">01</span><strong>Terrains vérifiés</strong><small>Papiers contrôlés avant publication</small></li>
    <li><span class="num mono">02</span><strong>Visite 360° et GPS</strong><small>La parcelle exacte, sur la carte</small></li>
    <li><span class="num mono">03</span><strong>Réponse sur WhatsApp</strong><small>Un conseiller vous répond</small></li>
  </ol>'''


def accroche(quatre_lignes=True):
    if quatre_lignes:
        return ('<h1 class="a accroche"><span>Le bon</span><span>terrain.</span>'
                '<span class="accent">Les vrais</span><span class="accent">papiers.</span></h1>')
    return '<h1 class="a accroche"><span>Le bon terrain.</span><span class="accent">Les vrais papiers.</span></h1>'


def qr(support):
    return f'<div class="a qr"><img src="../qrcodes/qr-{support}.svg" alt="QR code vers yakkomom.onrender.com"></div>'


def appel(mention=True):
    return ('<div class="a appel">' + ('<p class="mono">Gratuit · sans inscription</p>' if mention else '')
            + '<h2>Scannez pour voir les terrains</h2><p class="url">yakkomom.onrender.com</p></div>')


def ecran(largeur):
    """Écran du téléphone : l'iframe (390 × 844 px, taille mobile de l'app) réduite à la largeur voulue."""
    k = largeur / 390
    return (f'.telephone__ecran {{ width: {largeur}px; height: {844 * k:.1f}px; }} '
            f'.telephone__ecran iframe {{ transform: scale({k:.5f}); }}')


FORMATS = {
    'banniere-web': dict(titre='Bannière web', w=1920, h=800, qr='web', css=f'''
  .courbes {{ left: 640px; top: -260px; width: 900px; opacity: .8; }}
  .marque {{ left: 96px; top: 64px; gap: 14px; font-size: 38px; }} .marque img {{ width: 50px; height: 50px; }}
  .accroche {{ left: 90px; top: 178px; font-size: 128px; }}
  .sous-titre {{ left: 96px; top: 438px; font-size: 34px; }}
  .arguments {{ left: 96px; top: 560px; width: 1060px; display: grid; grid-template-columns: repeat(3, 1fr); gap: 32px; list-style: none; }}
  .arguments li {{ border-top: 2px solid var(--filet); padding-top: 18px; }}
  .arguments .num {{ display: block; font-size: 14px; color: var(--laterite); }}
  .arguments strong {{ display: block; margin-top: 10px; font: 650 27px/1.1 var(--f-titre); }}
  .arguments small {{ display: block; margin-top: 6px; font-size: 18px; color: var(--muet); }}
  .fond-encre {{ left: 1290px; top: 0; right: 0; bottom: 0; }}
  .telephone {{ left: 1210px; top: 96px; width: 310px; --bord: 11px; --arrondi: 42px; }}
  {ecran(288)}
  .qr {{ left: 1590px; top: 150px; width: 250px; }}
  .appel {{ left: 1590px; top: 430px; width: 270px; }} .appel .mono {{ font-size: 13px; }}
  .appel h2 {{ margin-top: 12px; font-size: 38px; }} .appel .url {{ margin-top: 20px; padding-top: 16px; font-size: 19px; }}''',
        corps=[COURBES, MARQUE, accroche(False), SOUS_TITRE, ARGUMENTS, '<div class="a fond-encre"></div>', TELEPHONE, qr('web'), appel()]),

    'banniere-mobile': dict(titre='Bannière web mobile', w=1080, h=1350, qr='web', css=f'''
  .courbes {{ right: -260px; top: -200px; width: 820px; opacity: .85; }}
  .marque {{ left: 72px; top: 64px; gap: 14px; font-size: 44px; }} .marque img {{ width: 58px; height: 58px; }}
  .accroche {{ left: 66px; top: 190px; font-size: 150px; }}
  .sous-titre {{ left: 72px; top: 800px; width: 560px; font-size: 38px; }}
  .fond-encre {{ left: 0; right: 0; top: 960px; bottom: 0; }}
  .telephone {{ left: 700px; top: 640px; width: 320px; --bord: 12px; --arrondi: 44px; }}
  {ecran(296)}
  .qr {{ left: 72px; top: 1020px; width: 260px; }}
  .appel {{ left: 372px; top: 1050px; width: 300px; }} .appel .mono {{ font-size: 14px; }}
  .appel h2 {{ margin-top: 12px; font-size: 40px; }} .appel .url {{ margin-top: 22px; padding-top: 16px; font-size: 19px; }}''',
        corps=[COURBES, MARQUE, accroche(), SOUS_TITRE, '<div class="a fond-encre"></div>', TELEPHONE, qr('web'), appel()]),

    'social-carre': dict(titre='Réseaux sociaux, carré', w=1080, h=1080, qr='reseaux', css=f'''
  .courbes {{ right: -300px; top: -260px; width: 820px; opacity: .85; }}
  .marque {{ left: 72px; top: 64px; gap: 14px; font-size: 40px; }} .marque img {{ width: 52px; height: 52px; }}
  .accroche {{ left: 66px; top: 180px; font-size: 136px; }}
  .fond-encre {{ left: 0; right: 0; top: 790px; bottom: 0; }}
  .telephone {{ left: 700px; top: 330px; width: 316px; --bord: 12px; --arrondi: 42px; }}
  {ecran(292)}
  .qr {{ left: 72px; top: 836px; width: 196px; }}
  .appel {{ left: 300px; top: 852px; width: 360px; }}
  .appel h2 {{ font-size: 40px; }} .appel .url {{ margin-top: 18px; padding-top: 14px; font-size: 20px; }}''',
        corps=[COURBES, MARQUE, accroche(), '<div class="a fond-encre"></div>', TELEPHONE, qr('reseaux'), appel(False)]),

    'social-story': dict(titre='Réseaux sociaux, story', w=1080, h=1920, qr='reseaux', css=f'''
  .courbes {{ right: -280px; top: -220px; width: 900px; opacity: .85; }}
  .marque {{ left: 80px; top: 120px; gap: 16px; font-size: 48px; }} .marque img {{ width: 62px; height: 62px; }}
  .accroche {{ left: 74px; top: 270px; font-size: 176px; }}
  .sous-titre {{ left: 80px; top: 960px; width: 480px; font-size: 40px; }}
  .fond-encre {{ left: 0; right: 0; top: 1440px; bottom: 0; }}
  .telephone {{ left: 610px; top: 950px; width: 400px; --bord: 14px; --arrondi: 54px; }}
  {ecran(372)}
  .qr {{ left: 80px; top: 1500px; width: 250px; }}
  .appel-haut {{ left: 370px; top: 1530px; width: 225px; color: var(--papier); font: 600 42px/1 var(--f-titre); letter-spacing: -.02em; }}
  .appel {{ left: 80px; top: 1790px; width: 900px; }} .appel .url {{ border: 0; font-size: 30px; }}''',
        corps=[COURBES, MARQUE, accroche(), SOUS_TITRE, '<div class="a fond-encre"></div>', TELEPHONE, qr('reseaux'),
               '<p class="a appel-haut">Scannez pour voir les terrains</p>',
               '<div class="a appel"><p class="url">yakkomom.onrender.com</p></div>']),
}

for nom, f in FORMATS.items():
    corps = '  ' + '\n  '.join(f['corps'])
    (SOURCES / f'{nom}.html').write_text(ENTETE.format(titre=f['titre'], w=f['w'], h=f['h'], qr=f"qr-{f['qr']}.svg",
                                                       css=f['css'], corps=corps), encoding='utf-8')
    print(f'sources/{nom}.html')
