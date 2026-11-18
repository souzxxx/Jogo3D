#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Quintessence.Combat;
using Quintessence.Core;
using Quintessence.Enemies;
using Quintessence.Run;
using Quintessence.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Quintessence.Game.PlayTests
{
    /// Rubrica 2 (colisoes): em todos os arranjos, o heroi e jogado contra cada obstaculo, paredes e quinas da sala.
    /// Falha se ele atravessar algo com o dash, entrar dentro de um obstaculo, tremer parado na quina ou nao conseguir sair andando.
    public class CollisionSweepTests
    {
        const float Step = 1f / 30f;
        PlayerController hero;
        ScriptedInput input;
        CharacterController body;
        readonly List<string> problems = new();
        int checks, touched;

        [UnityTest, Timeout(900000)]
        public IEnumerator Hero_Never_Sticks_Or_Passes_Through_Obstacles()
        {
            GameEvents.Clear();
            Time.timeScale = 1f;
            Time.captureFramerate = 30;
            GameFlow.StartElement = ElementType.Air;
            GameFlow.Seed = 3; GameFlow.Layers = 5; GameFlow.Shrines = 1;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Run.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            var director = Object.FindFirstObjectByType<RunDirector>();
            Object.FindFirstObjectByType<WaveSpawner>().enabled = false;
            for (int k = 0; k < 2000 && director.Busy; k++) yield return null;
            hero = Object.FindFirstObjectByType<PlayerController>();
            hero.GetComponent<Health>().Pool.Invulnerable = true;
            hero.GetComponent<PlayerInputReader>().enabled = false;
            input = hero.gameObject.AddComponent<ScriptedInput>();
            hero.Input = input;
            body = hero.GetComponent<CharacterController>();
            Object.FindFirstObjectByType<Hud>().HideTip();

            // paredes e quinas da sala vazia
            director.ApplyLayout(-1);
            for (int k = 0; k < 4; k++) yield return null;
            float hx = Arena.HalfX - 3f, hz = Arena.HalfZ - 3f;
            foreach (var d in Directions())
                yield return Probe("sala vazia", "parede", new Vector3(d.x * hx * 0.6f, 0.05f, d.z * hz * 0.6f), d, null);

            for (int i = 0; i < director.LayoutCount; i++)
            {
                director.ApplyLayout(i);
                for (int k = 0; k < 4; k++) yield return null;
                string layout = director.CurrentLayout;
                var root = GameObject.Find("Layout");
                if (root == null) continue;
                var solids = root.GetComponentsInChildren<Collider>().Where(c => c.enabled && !c.isTrigger).ToList();
                foreach (var c in solids)
                {
                    var b = c.bounds;
                    float reach = Mathf.Max(b.extents.x, b.extents.z) + body.radius + 1.6f;
                    foreach (var d in Directions())
                    {
                        var start = new Vector3(b.center.x, 0.05f, b.center.z) - d * reach;
                        if (!Free(start)) continue; // ponto de partida dentro de outra peca ou fora da sala
                        yield return Probe(layout, c.transform.parent != null ? c.transform.parent.name : c.name, start, d, c);
                    }
                }
            }

            File.WriteAllText("Logs/colisoes.txt", $"{checks} aproximacoes em {director.LayoutCount} arranjos + sala vazia, {touched} com contato (o heroi parou ou deslizou na peca)\n" + string.Join("\n", problems));
            Debug.Log($"COLISOES: {checks} aproximacoes, {touched} com contato, {problems.Count} problemas");
            Assert.Greater(touched, checks / 2, "a maioria das aproximacoes deveria encostar na peca; senao o teste nao esta testando nada");
            Time.captureFramerate = 0;
            Assert.IsEmpty(problems, $"{problems.Count} de {checks} aproximacoes com problema:\n" + string.Join("\n", problems.Take(25)));
        }

        /// Medida (rubrica 1, visibilidade): em quantos pontos andaveis de cada arranjo alguma peca fica entre a camera e o heroi.
        [UnityTest, Explicit, Timeout(600000)]
        public IEnumerator Measure_Camera_Occlusion()
        {
            GameEvents.Clear();
            GameFlow.StartElement = ElementType.Air; GameFlow.Seed = 3; GameFlow.Layers = 5; GameFlow.Shrines = 1;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Run.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            var director = Object.FindFirstObjectByType<RunDirector>();
            Object.FindFirstObjectByType<WaveSpawner>().enabled = false;
            for (int k = 0; k < 2000 && director.Busy; k++) yield return null;
            hero = Object.FindFirstObjectByType<PlayerController>();
            body = hero.GetComponent<CharacterController>();
            var cam = Camera.main;
            var lines = new List<string>();
            int total = 0, hidden = 0;
            for (int i = 0; i < director.LayoutCount; i++)
            {
                director.ApplyLayout(i);
                for (int k = 0; k < 3; k++) yield return null;
                int n = 0, h = 0;
                var names = new HashSet<string>();
                for (float x = -Arena.HalfX + 2f; x <= Arena.HalfX - 2f; x += 1.5f)
                for (float z = -Arena.HalfZ + 2f; z <= Arena.HalfZ - 2f; z += 1.5f)
                {
                    var p = new Vector3(x, 0.05f, z);
                    if (!Free(p)) continue;
                    hero.Teleport(p, Vector3.forward);
                    yield return null;
                    n++;
                    var chest = hero.transform.position + Vector3.up * 1f;
                    var from = cam.transform.position;
                    var hits = Physics.RaycastAll(from, (chest - from).normalized, Vector3.Distance(from, chest) - 0.6f, ~0, QueryTriggerInteraction.Ignore)
                        .Where(r => r.collider.GetComponentInParent<PlayerController>() == null).ToArray();
                    if (hits.Length > 0) { h++; foreach (var r in hits) names.Add(r.collider.transform.root.name + "/" + r.collider.name); }
                }
                total += n; hidden += h;
                lines.Add($"{director.CurrentLayout}: {h} de {n} pontos com o heroi escondido · {string.Join(", ", names.Take(6))}");
            }
            lines.Insert(0, $"total: {hidden} de {total} pontos ({100f * hidden / Mathf.Max(1, total):0.0}%)");
            File.WriteAllText("Logs/oclusao.txt", string.Join("\n", lines));
        }

        /// Rubrica 1 (visibilidade): o cenario nunca some; com o heroi atras de um pilar, o que fica tapado aparece como silhueta
        /// (Core.Silhouette + passadas RenderObjects do renderer). Compara o quadro desenhado com e sem a camada do heroi:
        /// atras da peca o peito do heroi muda de cor; com o heroi a vista, nenhum pixel muda (o corpo nao vira silhueta de si mesmo).
        [UnityTest, Timeout(600000)]
        public IEnumerator Hero_Behind_A_Piece_Shows_As_A_Silhouette()
        {
            GameEvents.Clear();
            Time.captureFramerate = 30;
            GameFlow.StartElement = ElementType.Air; GameFlow.Seed = 3; GameFlow.Layers = 5; GameFlow.Shrines = 1;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Run.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            var director = Object.FindFirstObjectByType<RunDirector>();
            Object.FindFirstObjectByType<WaveSpawner>().enabled = false;
            for (int k = 0; k < 2000 && director.Busy; k++) yield return null;
            hero = Object.FindFirstObjectByType<PlayerController>();
            hero.GetComponent<Health>().Pool.Invulnerable = true;
            body = hero.GetComponent<CharacterController>();
            var cam = Camera.main;
            var model = hero.GetComponent<HeroSkin>().Model;
            int heroLayer = LayerMask.NameToLayer(Silhouette.HeroLayer);
            Assert.GreaterOrEqual(heroLayer, 0, "falta a camada Hero (SilhouetteSetup.Run)");
            Assert.IsTrue(model.GetComponentsInChildren<Renderer>().All(r => r.gameObject.layer == heroLayer), "o modelo do heroi deveria estar na camada da silhueta");

            int tested = 0, shown = 0;
            bool shot = false;
            foreach (var name in new[] { "Pilares", "Clareira", "Arsenal", "Bosque de Pedra" })
            {
                director.ApplyLayout(director.LayoutIndex(name));
                for (int k = 0; k < 3; k++) yield return null;
                var pieces = GameObject.Find("Layout").GetComponentsInChildren<Collider>().Where(c => !c.isTrigger).ToList();
                foreach (var c in pieces.Take(6))
                {
                    // logo ao norte da peca: e onde ela fica entre a camera (ao sul, no alto) e o heroi
                    var p = new Vector3(c.bounds.center.x, 0.05f, c.bounds.max.z + body.radius + 0.4f);
                    if (!Free(p)) continue;
                    hero.Teleport(p, Vector3.forward);
                    for (int k = 0; k < 20; k++) yield return null;
                    var chest = hero.transform.position + Vector3.up * 0.9f;
                    var from = cam.transform.position;
                    bool covered = Physics.RaycastAll(from, (chest - from).normalized, Vector3.Distance(from, chest) - 0.9f, ~0, QueryTriggerInteraction.Ignore)
                        .Any(h => h.collider.GetComponentInParent<PlayerController>() == null);
                    if (!covered) continue;
                    tested++;
                    var (with, without, _) = Compare(cam, model, heroLayer, chest);
                    if (Diff(with, without) > 0.06f) shown++;
                    Assert.IsTrue(c.GetComponentsInChildren<Renderer>().All(r => r.enabled && r.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly), "a peca da frente nao some mais");
                    if (!shot && name == "Pilares")
                    {
                        shot = true;
                        yield return AspectShots.Shoot(cam, "docs/img/s6_silhueta_pilar.png", new Vector2Int(960, 540));
                        Silhouette.Clear(model);
                        yield return AspectShots.Shoot(cam, "docs/img/s6_silhueta_pilar_sem.png", new Vector2Int(960, 540));
                        Silhouette.MarkHero(model);
                    }
                }
            }
            // a vista, sem nada entre a camera e os pes, o peito e a cabeca: o heroi desenhado com e sem a silhueta e identico
            director.ApplyLayout(director.LayoutIndex("Clareira"));
            for (int k = 0; k < 3; k++) yield return null;
            bool open = false;
            for (float z = 0f; z < Arena.HalfZ - 3f && !open; z += 1f)
            {
                hero.Teleport(new Vector3(0, 0.05f, z), Vector3.forward);
                for (int k = 0; k < 20; k++) yield return null;
                open = new[] { 0.1f, 0.9f, 1.8f }.All(hgt =>
                {
                    var at = hero.transform.position + Vector3.up * hgt;
                    var from = cam.transform.position;
                    return !Physics.SphereCastAll(from, 0.5f, (at - from).normalized, Vector3.Distance(from, at), ~0, QueryTriggerInteraction.Ignore)
                        .Any(h => h.collider.GetComponentInParent<PlayerController>() == null && h.collider.bounds.max.y > 0.15f);
                });
            }
            Assert.IsTrue(open, "nenhum ponto livre na Clareira");
            var (_, _, changed) = Compare(cam, model, heroLayer, hero.transform.position + Vector3.up * 0.9f);

            // inimigo atras de um pilar tambem aparece (tijolo apagado); o heroi fica longe, a vista
            director.ApplyLayout(director.LayoutIndex("Pilares"));
            Object.FindFirstObjectByType<WaveSpawner>().enabled = true;
            director.StartEncounter(RoomType.Combat, 1, EliteModifier.None);
            EnemyBrain foe = null;
            for (int k = 0; k < 300 && foe == null; k++) { yield return null; foe = Object.FindFirstObjectByType<EnemyBrain>(); }
            Assert.IsNotNull(foe, "nenhum inimigo apareceu");
            var foeModel = foe.GetComponentInChildren<Animator>().gameObject;
            int enemyLayer = LayerMask.NameToLayer(Silhouette.EnemyLayer);
            // so entra na silhueta depois de sair do chao (o corpo sobe atravessando o piso)
            Assert.AreNotEqual(enemyLayer, foeModel.GetComponentInChildren<SkinnedMeshRenderer>().gameObject.layer, "nascendo, ainda sem silhueta");
            for (float t = 0; t < 3f && foeModel.GetComponentInChildren<SkinnedMeshRenderer>().gameObject.layer != enemyLayer; t += Time.deltaTime) yield return null;
            Assert.IsTrue(foeModel.GetComponentsInChildren<SkinnedMeshRenderer>().All(r => r.gameObject.layer == enemyLayer), "o modelo do inimigo deveria estar na camada da silhueta");
            foreach (var other in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None)) other.enabled = false;
            foe.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;
            // ate cinco pilares: na bateria inteira, as vezes o primeiro deixava o peito do inimigo de fora (outro inimigo na frente,
            // pose de ataque); basta um deles mostrar a silhueta
            var pillars = GameObject.Find("Layout").GetComponentsInChildren<Collider>().Where(c => !c.isTrigger)
                .Where(c => Free(new Vector3(c.bounds.center.x, 0.05f, c.bounds.max.z + 0.9f))).Take(5).ToList();
            Assert.IsNotEmpty(pillars, "nenhum pilar com espaco atras");
            float best = 0f;
            foreach (var pillar in pillars)
            {
                foe.transform.position = new Vector3(pillar.bounds.center.x, 0f, pillar.bounds.max.z + 0.9f);
                hero.Teleport(new Vector3(pillar.bounds.center.x + 6f, 0.05f, pillar.bounds.center.z - 4f), Vector3.forward);
                for (int k = 0; k < 10; k++) yield return null;
                var foeChest = foe.transform.position + Vector3.up * 0.9f;
                var (fa, fb, _) = CompareModel(cam, foeModel, () => Silhouette.MarkEnemy(foe.gameObject), foeChest);
                if (Diff(fa, fb) > best) { best = Diff(fa, fb); yield return AspectShots.Shoot(cam, "docs/img/s6_silhueta_inimigo.png", new Vector2Int(960, 540)); }
                if (best > 0.04f) break;
            }
            Assert.Greater(best, 0.04f, "o inimigo atras do pilar deveria aparecer como silhueta");

            Time.captureFramerate = 0;
            Assert.Greater(tested, 4, "poucos pontos com o heroi atras de uma peca para testar");
            Assert.GreaterOrEqual(shown, tested * 0.8f, $"so {shown} de {tested} pontos tapados mostraram a silhueta");
            // tolera as beiradas finas (adagas do Vey cruzando o redemoinho: ~50 de 130 mil pixels, precisao de profundidade);
            // o corpo inteiro virando silhueta muda centenas
            Assert.Less(changed, 90, $"com o heroi a vista, {changed} pixels mudaram: o corpo esta virando silhueta de si mesmo");
        }

        /// Desenha o quadro com o modelo na camada da silhueta e sem ela; devolve a cor media em volta do ponto nos dois e quantos pixels mudaram no quadro todo.
        static (Color, Color, int) Compare(Camera cam, GameObject model, int heroLayer, Vector3 point)
            => CompareModel(cam, model, () => Silhouette.MarkHero(model), point);

        static (Color, Color, int) CompareModel(Camera cam, GameObject model, System.Action mark, Vector3 point)
        {
            const int W = 480, H = 270;
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            Color32[] Draw()
            {
                cam.targetTexture = rt; cam.Render();
                var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
                RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); RenderTexture.active = null;
                var px = tex.GetPixels32(); Object.Destroy(tex);
                return px;
            }
            cam.targetTexture = rt;
            var sp = cam.WorldToScreenPoint(point);
            var a = Draw();
            Silhouette.Clear(model);
            var b = Draw();
            mark();
            cam.targetTexture = null;
            rt.Release(); Object.Destroy(rt);
            Color Around(Color32[] px)
            {
                Color sum = Color.clear; int n = 0;
                for (int y = (int)sp.y - 3; y <= (int)sp.y + 3; y++)
                for (int x = (int)sp.x - 3; x <= (int)sp.x + 3; x++)
                    if (x >= 0 && y >= 0 && x < W && y < H) { sum += (Color)px[y * W + x]; n++; }
                return n > 0 ? sum / n : Color.clear;
            }
            int changed = 0;
            for (int i = 0; i < a.Length; i++)
                if (Mathf.Abs(a[i].r - b[i].r) + Mathf.Abs(a[i].g - b[i].g) + Mathf.Abs(a[i].b - b[i].b) > 24) changed++;
            return (Around(a), Around(b), changed);
        }

        static float Diff(Color a, Color b) => Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);

        static IEnumerable<Vector3> Directions()
        {
            for (int a = 0; a < 360; a += 45) yield return Quaternion.Euler(0, a, 0) * Vector3.forward;
        }

        bool Free(Vector3 p)
        {
            if (!Arena.Contains(p) || Mathf.Abs(p.x) > Arena.HalfX - 1.5f || Mathf.Abs(p.z) > Arena.HalfZ - 1.5f) return false;
            var a = p + Vector3.up * (body.radius + 0.1f);
            var b = p + Vector3.up * (body.height - body.radius);
            return !Physics.CheckCapsule(a, b, body.radius + 0.1f, ~0, QueryTriggerInteraction.Ignore);
        }

        /// Encosta no alvo vindo de start na direcao dir: dash, empurra, depois tenta sair andando ao contrario.
        IEnumerator Probe(string layout, string piece, Vector3 start, Vector3 dir, Collider target)
        {
            checks++;
            string where = $"{layout} · {piece} · dir {Mathf.RoundToInt(Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg)}°";
            hero.Teleport(start, dir);
            input.Move = Vector2.zero;
            for (int k = 0; k < 3; k++) yield return null;
            var move = new Vector2(dir.x, dir.z);
            input.AimPoint = start + dir * 5f;

            // 1) dash contra o obstaculo: nao pode terminar do outro lado
            input.Move = move;
            input.PressDash();
            var prev = hero.transform.position;
            bool crossed = false;
            for (float t = 0; t < 0.35f; t += Step)
            {
                yield return null;
                crossed |= Crosses(target, prev, hero.transform.position);
                prev = hero.transform.position;
            }
            if (crossed) problems.Add($"{where}: o dash atravessou");

            // 2) empurra por 1 s: sem tremer (andar muito sem sair do lugar), sem entrar no obstaculo, sem subir nele
            float path = 0f;
            var last = hero.transform.position;
            var mark = last;
            for (float t = 0; t < 1f; t += Step)
            {
                yield return null;
                var p = hero.transform.position;
                if (Crosses(target, last, p)) { problems.Add($"{where}: atravessou andando"); break; }
                if (t > 0.5f) path += Vector3.Distance(new Vector3(p.x, 0, p.z), new Vector3(last.x, 0, last.z));
                else mark = p;
                last = p;
                if (p.y > 0.6f || p.y < -0.3f) { problems.Add($"{where}: altura {p.y:0.00} (subiu ou afundou)"); break; }
                if (!Arena.Contains(p)) { problems.Add($"{where}: saiu da sala"); break; }
            }
            float net = Vector3.Distance(new Vector3(last.x, 0, last.z), new Vector3(mark.x, 0, mark.z));
            // andando livre o heroi faria uns 3 m em meio segundo: bem menos que isso e porque encostou em algo
            if (net < 1.5f) touched++;
            if (path > 1.2f && net < path * 0.25f) problems.Add($"{where}: tremendo (andou {path:0.0} m e saiu {net:0.0} m do lugar)");
            if (target != null && Inside(target)) problems.Add($"{where}: dentro do obstaculo");

            // 3) sai andando ao contrario
            var stuck = hero.transform.position;
            input.Move = -move;
            for (float t = 0; t < 0.6f; t += Step) yield return null;
            float escaped = Vector3.Distance(new Vector3(stuck.x, 0, stuck.z), new Vector3(hero.transform.position.x, 0, hero.transform.position.z));
            if (escaped < 1f) problems.Add($"{where}: preso (andou {escaped:0.00} m tentando sair)");
            input.Move = Vector2.zero;
        }

        /// O trecho andado neste quadro (na altura do peito) corta o colisor: o heroi passou por dentro dele.
        static bool Crosses(Collider target, Vector3 from, Vector3 to)
        {
            if (target == null) return false;
            var a = from + Vector3.up; var b = to + Vector3.up;
            var d = b - a;
            float len = d.magnitude;
            if (len < 0.001f) return false;
            return target.Raycast(new Ray(a, d / len), out _, len) && target.Raycast(new Ray(b, -d / len), out _, len);
        }

        bool Inside(Collider target)
        {
            var p = hero.transform.position;
            float r = body.radius * 0.5f;
            var a = p + Vector3.up * (body.radius + 0.1f);
            var b = p + Vector3.up * (body.height - body.radius);
            return Physics.OverlapCapsule(a, b, r, ~0, QueryTriggerInteraction.Ignore).Contains(target);
        }
    }
}
#endif
