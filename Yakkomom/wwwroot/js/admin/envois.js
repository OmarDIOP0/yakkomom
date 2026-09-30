// File d'envoi résistante aux connexions instables.
// - Les fichiers (déjà compressés) sont conservés dans IndexedDB jusqu'à confirmation du serveur :
//   une coupure, un rechargement ou même la fermeture de l'onglet ne perdent rien.
// - Un envoi à la fois (la 3G ne gagne rien à paralléliser), avec progression réelle.
// - Nouvelles tentatives automatiques (2 s, 5 s, 15 s, 30 s, 60 s), attente du réseau si hors ligne.
// - Chaque envoi porte un identifiant unique : le serveur ignore les doublons.

const BASE = 'yakkomom-envois';
const MAGASIN = 'envois';
const DELAIS = [2000, 5000, 15000, 30000, 60000];

function ouvrirBase() {
  return new Promise((resoudre, rejeter) => {
    const req = indexedDB.open(BASE, 1);
    req.onupgradeneeded = () => req.result.createObjectStore(MAGASIN, { keyPath: 'id' }).createIndex('file', 'file');
    req.onsuccess = () => resoudre(req.result);
    req.onerror = () => rejeter(req.error);
  });
}

async function transaction(mode, action) {
  const db = await ouvrirBase();
  return new Promise((resoudre, rejeter) => {
    const tx = db.transaction(MAGASIN, mode);
    const resultat = action(tx.objectStore(MAGASIN));
    tx.oncomplete = () => resoudre(resultat?.result);
    tx.onerror = () => rejeter(tx.error);
  });
}

export function nouvelId() {
  return crypto.randomUUID?.() ?? ([1e7] + -1e3 + -4e3 + -8e3 + -1e11).replace(/[018]/g, (c) =>
    (c ^ (crypto.getRandomValues(new Uint8Array(1))[0] & (15 >> (c / 4)))).toString(16));
}

export class FileEnvois {
  /**
   * @param {{ file: string, jeton: string, surEtat: (e: object) => void }} options
   *   file : identifiant de la file (ex. « photos-12 »), jeton : anti-falsification,
   *   surEtat : appelé à chaque changement (etat, progression, message, reponse).
   */
  constructor({ file, jeton, surEtat }) {
    this.file = file;
    this.jeton = jeton;
    this.surEtat = surEtat;
    this.attente = [];
    this.enCours = false;
    this.memoireSeule = !('indexedDB' in window);
    window.addEventListener('online', () => this.#suivant());
  }

  /** Reprend les envois interrompus lors d'une visite précédente. */
  async reprendre() {
    if (this.memoireSeule) return [];
    try {
      const tous = await transaction('readonly', (s) => s.index('file').getAll(this.file));
      for (const e of tous) {
        this.attente.push(e);
        this.surEtat({ ...e, etat: 'attente', progression: 0, message: 'Reprise de l’envoi interrompu' });
      }
      this.#suivant();
      return tous;
    } catch {
      this.memoireSeule = true;
      return [];
    }
  }

  /** @param {{ id: string, url: string, nom: string, champs: object, fichiers: Record<string, Blob> }} envoi */
  async ajouter(envoi) {
    const e = { ...envoi, file: this.file, tentatives: 0, creeLe: Date.now() };
    if (!this.memoireSeule) {
      try { await transaction('readwrite', (s) => s.put(e)); } catch { this.memoireSeule = true; }
    }
    this.attente.push(e);
    this.surEtat({ ...e, etat: 'attente', progression: 0 });
    this.#suivant();
  }

  /** Relance manuelle après un échec. */
  relancer(id) {
    const e = this.attente.find((x) => x.id === id);
    if (!e) return;
    e.tentatives = 0;
    e.bloque = false;
    this.#suivant();
  }

  async abandonner(id) {
    this.attente = this.attente.filter((x) => x.id !== id);
    if (!this.memoireSeule) await transaction('readwrite', (s) => s.delete(id)).catch(() => {});
  }

  get nombreEnAttente() { return this.attente.length; }

  async #suivant() {
    if (this.enCours) return;
    const e = this.attente.find((x) => !x.bloque && !x.planifie);
    if (!e) return;
    if (!navigator.onLine) {
      this.surEtat({ ...e, etat: 'hors-ligne', message: 'Hors ligne : l’envoi reprendra automatiquement.' });
      return;
    }

    this.enCours = true;
    try {
      const reponse = await this.#envoyer(e);
      this.attente = this.attente.filter((x) => x !== e);
      if (!this.memoireSeule) await transaction('readwrite', (s) => s.delete(e.id)).catch(() => {});
      this.surEtat({ ...e, etat: 'reussi', progression: 1, reponse });
    } catch (err) {
      if (err.definitif) {
        // Refus du serveur (format, taille…) : inutile de réessayer.
        await this.abandonner(e.id);
        this.surEtat({ ...e, etat: 'erreur', definitif: true, message: err.message });
      } else if (e.tentatives < DELAIS.length) {
        const delai = DELAIS[e.tentatives++];
        e.planifie = true;
        this.surEtat({ ...e, etat: 'attente', message: `Connexion interrompue, nouvel essai dans ${Math.round(delai / 1000)} s…` });
        setTimeout(() => { e.planifie = false; this.#suivant(); }, delai);
      } else {
        e.bloque = true;
        this.surEtat({ ...e, etat: 'erreur', message: 'Envoi impossible pour le moment.' });
      }
    } finally {
      this.enCours = false;
      this.#suivant();
    }
  }

  #envoyer(e) {
    return new Promise((resoudre, rejeter) => {
      const donnees = new FormData();
      for (const [k, v] of Object.entries(e.champs ?? {})) if (v != null) donnees.append(k, v);
      for (const [k, blob] of Object.entries(e.fichiers ?? {})) donnees.append(k, blob, blob.name ?? `${k}${extension(blob.type)}`);

      const xhr = new XMLHttpRequest();
      xhr.open('POST', e.url);
      xhr.timeout = 180000;
      xhr.setRequestHeader('RequestVerificationToken', this.jeton);
      xhr.setRequestHeader('Accept', 'application/json');
      xhr.upload.onprogress = (ev) => {
        if (ev.lengthComputable) this.surEtat({ ...e, etat: 'envoi', progression: ev.loaded / ev.total });
      };
      xhr.onload = () => {
        let corps = null;
        try { corps = JSON.parse(xhr.responseText); } catch { /* réponse vide */ }
        if (xhr.status >= 200 && xhr.status < 300) return resoudre(corps);
        const erreur = new Error(corps?.erreur ?? (xhr.status === 401 || xhr.status === 302
          ? 'Session expirée : reconnectez-vous, les fichiers seront renvoyés.' : `Erreur ${xhr.status}`));
        // 4xx = refus définitif (sauf 408/429 : réessayer), 5xx = réessayer
        erreur.definitif = xhr.status >= 400 && xhr.status < 500 && ![401, 408, 429].includes(xhr.status);
        rejeter(erreur);
      };
      xhr.onerror = () => rejeter(new Error('Réseau indisponible'));
      xhr.ontimeout = () => rejeter(new Error('Délai dépassé'));
      this.surEtat({ ...e, etat: 'envoi', progression: 0 });
      xhr.send(donnees);
    });
  }
}

function extension(type) {
  return { 'image/webp': '.webp', 'image/jpeg': '.jpg', 'image/png': '.png', 'application/pdf': '.pdf' }[type] ?? '';
}
