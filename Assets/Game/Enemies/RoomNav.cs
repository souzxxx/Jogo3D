using Quintessence.Core;
using UnityEngine;
using UnityEngine.AI;

namespace Quintessence.Enemies
{
    /// Chao bom para um inimigo estar: dentro das paredes e ligado por caminho ao heroi.
    /// A malha de navegacao e feita dos colisores da sala, e pedacos dela sobram fora das paredes (atras do fundo,
    /// em cima de pecas). SamplePosition so acha o chao mais perto, entao um ponto sorteado rente a parede podia
    /// cair numa dessas ilhas: o inimigo nascia atras da parede, sem caminho ate o heroi, e a sala nao fechava.
    public static class RoomNav
    {
        static NavMeshPath path;

        /// Dentro da sala, sem a folga de Arena.Contains (que deixa 0,6 m alem das paredes).
        public static bool InsideWalls(Vector3 p) =>
            Mathf.Abs(p.x) <= Arena.HalfX - 0.9f && Mathf.Abs(p.z) <= Arena.HalfZ - 0.9f && Arena.Contains(p);

        /// p esta dentro das paredes e ha caminho completo entre p e from (normalmente o heroi).
        public static bool Reachable(Vector3 p, Vector3 from)
        {
            if (!InsideWalls(p)) return false;
            if (!NavMesh.SamplePosition(from, out var start, 2f, NavMesh.AllAreas)) return true; // heroi no ar (dash, pulo): fica so a checagem das paredes
            path ??= new NavMeshPath();
            return NavMesh.CalculatePath(start.position, p, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete;
        }

        /// Ponto bom mais perto de around (puxado para dentro da sala). Sem nenhum, o chao perto do centro.
        public static bool Nearest(Vector3 around, Vector3 from, out Vector3 point)
        {
            var want = Arena.Clamp(around);
            for (int i = 0; i < 9; i++)
            {
                // tenta o ponto puxado para dentro e, se ele cair numa peca, oito vizinhos a 2 m
                var p = i == 0 ? want : want + Quaternion.Euler(0, i * 45f, 0) * Vector3.forward * 2f;
                if (NavMesh.SamplePosition(Arena.Clamp(p), out var hit, 3f, NavMesh.AllAreas) && Reachable(hit.position, from))
                { point = hit.position; return true; }
            }
            if (NavMesh.SamplePosition(Vector3.zero, out var center, 12f, NavMesh.AllAreas) && InsideWalls(center.position))
            { point = center.position; return true; }
            point = around;
            return false;
        }
    }
}
