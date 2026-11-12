using System.Collections.Generic;
using UnityEngine;

namespace Quintessence.Game
{
    /// Dicas de primeira vez: cada uma aparece uma unica vez por jogador (guardado no navegador).
    public static class Tips
    {
        public const string DefaultKey = "quintessence.tips";
        static string key = DefaultKey;
        static HashSet<string> seen;

        /// Troca a chave (os testes usam uma propria).
        public static void UseKey(string newKey) { key = newKey; seen = null; }

        static HashSet<string> Seen => seen ??= new HashSet<string>(PlayerPrefs.GetString(key, "").Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries));

        public static bool WasSeen(string id) => Seen.Contains(id);

        /// Devolve true se a dica ainda nao tinha aparecido (e marca como vista).
        public static bool Consume(string id)
        {
            if (!Seen.Add(id)) return false;
            PlayerPrefs.SetString(key, string.Join(";", Seen));
            PlayerPrefs.Save();
            return true;
        }

        public static void ResetAll() { PlayerPrefs.DeleteKey(key); seen = null; }

        public static string Text(string id) => id switch
        {
            "start" => "Clique esquerdo ataca. As teclas 1 a 4 soltam as skills dos elementos. Espaço desvia.",
            "level" => "Subiu de nível: escolha um talento com 1, 2 ou 3.",
            "shrine" => "Santuário: desperte um elemento novo ou aprofunde uma skill que você já tem.",
            "react" => "Duas skills! Marque com uma e detone com a outra: isso é uma reação.",
            "elite" => "Elite: inimigos com um traço especial e um inimigo forte no fim. Paga mais.",
            "shop" => "Loja: gaste Essência em cura, vida máxima, bênçãos e talentos.",
            "pact" => "Pacto: um ganho forte com um custo que dura a run toda. Dá para recusar.",
            "terrain" => "A poça marca quem está dentro com o elemento do trecho: uma skill de outro elemento detona a reação.",
            "siege" => "Cerco: as ondas não param. Aguente até o tempo acabar; quem sobrar desmorona e você ganha uma bênção.",
            "trial" => "Provação: cumpra o desafio do letreiro e ganhe um talento extra. Falhar só perde o prêmio.",
            "fusion" => "Três elementos: lance a skill do terceiro numa poça de Magma ou numa nuvem de Vapor. É uma Fusão.",
            "affinity" => "Elementais resistem ao próprio elemento e são fracos a outro: Água vence Fogo, Ar vence Pedra.",
            "quint" => "Quatro elementos despertos: cause reações para encher a Quintessência e aperte R.",
            _ => "",
        };
    }
}
