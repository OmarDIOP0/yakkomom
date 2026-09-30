namespace Yakkomom.Data.Seed;

/// <summary>
/// Découpage administratif du Sénégal : 14 régions, 46 départements (Keur Massar inclus, créé en 2021).
/// Les communes fournies sont une sélection (zones les plus demandées) ; les autres s'ajoutent
/// depuis l'admin. Pour un département sans liste détaillée, on crée sa commune chef-lieu.
/// </summary>
internal static class DonneesLocalites
{
    public static readonly (string Region, (string Departement, string[] Communes)[] Departements)[] Regions =
    [
        ("Dakar",
        [
            ("Dakar", ["Dakar-Plateau", "Médina", "Fann-Point E-Amitié", "Mermoz-Sacré-Cœur", "Ouakam", "Ngor", "Yoff", "Grand Yoff", "Parcelles Assainies"]),
            ("Guédiawaye", ["Golf Sud", "Sam Notaire", "Ndiarème Limamoulaye", "Wakhinane Nimzatt", "Médina Gounass"]),
            ("Keur Massar", ["Keur Massar Nord", "Keur Massar Sud", "Malika", "Yeumbeul Nord", "Yeumbeul Sud", "Jaxaay-Parcelles-Niakoul Rab"]),
            ("Pikine", ["Pikine Est", "Pikine Nord", "Pikine Ouest", "Thiaroye-sur-Mer", "Mbao"]),
            ("Rufisque", ["Rufisque", "Bargny", "Sébikotane", "Diamniadio", "Sangalkam", "Bambilor", "Yène", "Sendou", "Tivaouane Peulh-Niaga"]),
        ]),
        ("Diourbel",
        [
            ("Bambey", []),
            ("Diourbel", []),
            ("Mbacké", ["Mbacké", "Touba Mosquée"]),
        ]),
        ("Fatick",
        [
            ("Fatick", []),
            ("Foundiougne", ["Foundiougne", "Sokone"]),
            ("Gossas", []),
        ]),
        ("Kaffrine",
        [
            ("Birkelane", []),
            ("Kaffrine", []),
            ("Koungheul", []),
            ("Malem Hodar", []),
        ]),
        ("Kaolack",
        [
            ("Guinguinéo", []),
            ("Kaolack", []),
            ("Nioro du Rip", []),
        ]),
        ("Kédougou",
        [
            ("Kédougou", []),
            ("Salémata", []),
            ("Saraya", []),
        ]),
        ("Kolda",
        [
            ("Kolda", []),
            ("Médina Yoro Foulah", []),
            ("Vélingara", []),
        ]),
        ("Louga",
        [
            ("Kébémer", []),
            ("Linguère", []),
            ("Louga", []),
        ]),
        ("Matam",
        [
            ("Kanel", []),
            ("Matam", []),
            ("Ranérou-Ferlo", ["Ranérou"]),
        ]),
        ("Saint-Louis",
        [
            ("Dagana", ["Dagana", "Richard-Toll"]),
            ("Podor", []),
            ("Saint-Louis", []),
        ]),
        ("Sédhiou",
        [
            ("Bounkiling", []),
            ("Goudomp", []),
            ("Sédhiou", []),
        ]),
        ("Tambacounda",
        [
            ("Bakel", []),
            ("Goudiry", []),
            ("Koumpentoum", []),
            ("Tambacounda", []),
        ]),
        ("Thiès",
        [
            ("Mbour", ["Mbour", "Saly Portudal", "Nguékokh", "Joal-Fadiouth", "Somone", "Ngaparou", "Popenguine-Ndayane", "Sindia", "Malicounda", "Thiadiaye", "Diass", "Nguéniène", "Sandiara"]),
            ("Thiès", ["Thiès Est", "Thiès Nord", "Thiès Ouest", "Pout", "Kayar", "Keur Moussa", "Fandène", "Diender Guedj", "Notto Diobass", "Tassette"]),
            ("Tivaouane", ["Tivaouane", "Mboro", "Méckhé", "Pire Goureye", "Taïba Ndiaye"]),
        ]),
        ("Ziguinchor",
        [
            ("Bignona", ["Bignona", "Kafountine"]),
            ("Oussouye", ["Oussouye", "Diembéring"]),
            ("Ziguinchor", []),
        ]),
    ];
}
