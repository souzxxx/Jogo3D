#!/usr/bin/env python3
"""Gera todos os efeitos e as musicas do jogo por sintese, usando so o ffmpeg.
Uso: python3 tools/gen_audio.py [nomes...]   (escreve em Assets/Resources/Audio; sem nomes, gera todos)
Cada som e um grafo de filtros: ondas por expressao (aevalsrc), ruido filtrado (anoisesrc) e eco curto.
"""
import os, subprocess, sys

OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "Resources", "Audio")
SR = 44100
P = "2*PI"

def tone(expr, d):
    return f"aevalsrc='{expr}':d={d}:s={SR}"

def noise(d, color="white", chain=""):
    return f"anoisesrc=c={color}:d={d}:a=0.9:s={SR}" + ("," + chain if chain else "")

def thump(f0, f1, k, decay, amp=0.8):
    """Batida grave: a frequencia cai de f0 para f1 (fase = integral da frequencia)."""
    return f"{amp}*sin({P}*({f1}*t+{f0 - f1}*(1-exp(-{k}*t))/{k}))*exp(-{decay}*t)"

def bell(freq, at, decay=5.0, amp=0.3):
    u = f"(t-{at})"
    return f"{amp}*(sin({P}*{freq}*{u})+0.3*sin({P}*{2 * freq}*{u}))*exp(-{decay}*{u})*gt(t,{at})"

def env(expr):
    return f"volume='{expr}':eval=frame"

ECHO = "aecho=0.8:0.6:55|110:0.3|0.18"

# nome -> (lista de fontes, filtro final opcional)
S = {}
S["cast_fire"] = ([noise(0.4, "pink", "bandpass=f=900:w=1200," + env("min(t*40,1)*exp(-7*t)*1.6")), tone(thump(100, 60, 12, 9, 0.5), 0.4)], "")
S["cast_water"] = ([noise(0.45, "white", "lowpass=f=1400,highpass=f=300," + env("min(t*30,1)*exp(-6*t)*1.3")), tone(f"0.3*sin({P}*(900*t-600*t*t))*exp(-10*t)", 0.45)], "")
S["cast_earth"] = ([tone(thump(95, 45, 14, 7, 0.9), 0.45), noise(0.12, "brown", "lowpass=f=600," + env("exp(-30*t)*1.5"))], "")
S["cast_air"] = ([noise(0.4, "white", "highpass=f=1800,lowpass=f=6000," + env("sin(PI*min(t/0.38,1))*0.8"))], "")
S["skill_fire"] = ([noise(0.9, "pink", "bandpass=f=700:w=1100," + env("min(t*20,1)*exp(-3.2*t)*2")), tone(thump(120, 45, 6, 4, 0.7), 0.9)], ECHO)
S["skill_water"] = ([noise(1.0, "white", "lowpass=f=1100,highpass=f=200," + env("sin(PI*min(t/0.9,1))*1.5")), tone(f"0.3*sin({P}*(500*t-200*t*t))*exp(-4*t)", 1.0)], ECHO)
S["skill_earth"] = ([tone(thump(110, 34, 8, 3.5, 1.0), 1.0), noise(0.5, "brown", "lowpass=f=500," + env("exp(-7*t)*2"))], ECHO)
S["skill_air"] = ([noise(0.9, "white", "highpass=f=1200,lowpass=f=7000," + env("sin(PI*min(t/0.85,1))*1.0")), tone(f"0.12*sin({P}*(400*t+500*t*t))*sin(PI*min(t/0.85,1))", 0.9)], ECHO)
S["hit"] = ([tone(thump(210, 90, 40, 28, 0.8), 0.15), noise(0.05, "white", "highpass=f=2000," + env("exp(-80*t)*0.6"))], "")
S["hit_heavy"] = ([tone(thump(180, 55, 25, 14, 1.0), 0.3), noise(0.08, "white", "highpass=f=1500," + env("exp(-50*t)*0.7"))], "")
S["hurt"] = ([tone(f"0.6*sin({P}*(220*t-200*t*t))*exp(-9*t)", 0.3), noise(0.2, "brown", "lowpass=f=700," + env("exp(-14*t)*1.4"))], "")
S["enemy_die"] = ([noise(0.45, "pink", "lowpass=f=1200," + env("exp(-8*t)*1.2")), tone(f"0.35*sin({P}*(300*t-350*t*t))*exp(-7*t)", 0.4)], "")
S["dash"] = ([noise(0.22, "white", "bandpass=f=1500:w=2000," + env("sin(PI*t/0.22)*0.9"))], "")
S["switch"] = ([tone(bell(660, 0, 14, 0.3) + "+" + bell(990, 0.07, 14, 0.3), 0.4)], "")
S["reaction"] = ([tone(thump(160, 35, 9, 4.5, 1.0), 0.9), noise(0.9, "white", "highpass=f=3000," + env("exp(-5*t)*0.5"))], ECHO)
S["telegraph"] = ([tone(f"0.3*sin({P}*520*t)*exp(-35*t)", 0.12)], "")
S["explode"] = ([tone(thump(200, 30, 7, 3.5, 1.0), 1.0), noise(1.0, "brown", "lowpass=f=900," + env("exp(-4*t)*2.2"))], ECHO)
S["block"] = ([tone(f"0.3*(sin({P}*820*t)+0.7*sin({P}*1370*t)+0.5*sin({P}*2190*t))*exp(-12*t)", 0.4)], "")
S["shield"] = ([tone(f"0.35*(sin({P}*410*t)+0.7*sin({P}*685*t)+0.5*sin({P}*1095*t))*exp(-5*t)", 0.9)], ECHO)
S["shatter"] = ([noise(0.5, "white", "highpass=f=3500," + env("exp(-9*t)*0.7")), tone(f"0.2*(sin({P}*2400*t)+sin({P}*3300*t))*exp(-14*t)", 0.5)], "")
S["quint_on"] = ([tone(f"0.2*(sin({P}*220*t)+sin({P}*277.2*t)+sin({P}*329.6*t)+sin({P}*440*t))*min(t/0.6,1)*exp(-1.5*max(t-0.6,0))", 1.8)], ECHO)
S["quint_off"] = ([tone(f"0.3*sin({P}*(330*t-90*t*t))*exp(-3*t)", 0.8)], "")
S["room_clear"] = ([tone("+".join([bell(523.3, 0), bell(659.3, 0.12), bell(784, 0.24, 3.5)]), 1.4)], ECHO)
S["wave"] = ([tone(thump(120, 50, 20, 9, 0.9) + "+" + f"0.9*sin({P}*(50*(t-0.2)+70*(1-exp(-20*(t-0.2)))/20))*exp(-9*(t-0.2))*gt(t,0.2)", 0.7)], "")
S["door"] = ([noise(0.6, "brown", "bandpass=f=300:w=300," + env("sin(PI*t/0.6)*2.5")), tone(f"0.8*sin({P}*(60*(t-0.42)+60*(1-exp(-25*(t-0.42)))/25))*exp(-12*(t-0.42))*gt(t,0.42)", 0.8)], "")
S["pickup"] = ([tone(bell(784, 0, 6, 0.3) + "+" + bell(1174.7, 0.1, 5, 0.3), 0.8)], ECHO)
S["heal"] = ([tone(bell(440, 0, 5, 0.25) + "+" + bell(554.4, 0.12, 5, 0.25) + "+" + bell(659.3, 0.24, 4, 0.25), 1.0)], ECHO)
S["ui_click"] = ([tone(f"0.3*sin({P}*900*t)*exp(-60*t)", 0.06)], "")
S["boss_spawn"] = ([tone(f"0.5*sin({P}*55*t)*(1+0.5*sin({P}*6*t))*min(t/0.3,1)*exp(-1.2*max(t-1,0))", 2.2), noise(2.2, "brown", "lowpass=f=200," + env("min(t/0.3,1)*exp(-1.5*max(t-0.8,0))*2")), tone(f"0.22*(sin({P}*110*t)+0.5*sin({P}*220*t)+0.33*sin({P}*330*t))*min(t/0.4,1)*exp(-1.5*max(t-1.2,0))", 2.2)], ECHO)
S["boss_phase"] = ([tone(thump(180, 32, 6, 3, 1.0), 1.3), tone(f"0.2*(sin({P}*98*t)+0.5*sin({P}*196*t)+0.33*sin({P}*294*t))*exp(-2.5*t)", 1.3)], ECHO)
S["level_up"] = ([tone("+".join(bell(f, i * 0.07, 4.0, 0.22) for i, f in enumerate([523.3, 659.3, 784, 1046.5, 1318.5])), 1.2)], ECHO)
# despertar da Quintessencia: estouro grave que abre num brilho subindo
S["quint_burst"] = ([tone(thump(150, 38, 5, 2.6, 1.0), 1.4), noise(1.4, "pink", "bandpass=f=1600:w=2400," + env("min(t/0.05,1)*exp(-3.5*t)*1.4")),
    tone("+".join(bell(f, 0.12 + i * 0.05, 3.5, 0.16) for i, f in enumerate([659.3, 987.8, 1318.5])), 1.4)], ECHO)
# feito cumprido: dois sinos claros
S["feat"] = ([tone(bell(880, 0.0, 3.2, 0.3) + "+" + bell(1318.5, 0.14, 3.0, 0.28), 1.1)], ECHO)
# Divisor se partindo: estalo seco e curto
S["split"] = ([tone(thump(320, 110, 30, 16, 0.55), 0.3), noise(0.3, "brown", "lowpass=f=1800," + env("exp(-22*t)*1.6"))], "")
S["champion"] = ([tone(f"0.45*(sin({P}*82*t)+0.6*sin({P}*123*t)+0.3*sin({P}*164*t))*min(t/0.15,1)*exp(-2.2*max(t-0.35,0))", 1.4), noise(1.4, "brown", "lowpass=f=300," + env("min(t/0.1,1)*exp(-3*t)*1.6"))], ECHO)
S["victory"] = ([tone("+".join(bell(f, i * 0.15, 2.2, 0.26) for i, f in enumerate([392, 523.3, 659.3, 784, 1046.5])), 2.8)], ECHO)
S["defeat"] = ([tone("+".join(bell(f, i * 0.32, 2.4, 0.28) for i, f in enumerate([392, 370, 311.1, 261.6])), 2.6)], ECHO)

# ---------- musica ----------

def q(f, length):
    """Arredonda a frequencia para caber um numero inteiro de ciclos no loop (emenda sem estalo)."""
    return round(f * length) / length

def music_ambient(L=32):
    pad = [(73.42, 16), (110.0, 8), (146.83, 32), (174.61, 16)]
    parts = [f"0.07*sin({P}*{q(f, L)}*t)*(0.7+0.3*sin({P}*t/{per}))" for f, per in pad]
    scale = [293.66, 440.0, 349.23, 523.25, 392.0, 440.0, 293.66, 349.23, 587.33, 440.0, 392.0, 523.25, 349.23, 293.66, 440.0, 392.0]
    for i, f in enumerate(scale):
        if i % 4 == 3:
            continue  # respiro: uma pausa a cada quatro notas
        parts.append(bell(f, i * 2.0 + 0.5, 2.2, 0.05))
    return [tone("+".join(parts), L)], "lowpass=f=4200"

def music_boss(L=16):
    m = "mod(t,0.5)"
    kick = f"0.5*sin({P}*(48*{m}+70*(1-exp(-30*{m}))/30))*exp(-9*{m})"
    n = "mod(t,0.25)"
    f = "if(lt(mod(t,8),4),73.42,if(lt(mod(t,8),6),87.31,65.41))"
    bass = f"0.16*(sin({P}*{f}*t)+0.5*sin({P}*2*{f}*t)+0.25*sin({P}*3*{f}*t))*min({n}*200,1)*exp(-7*{n})"
    pad = f"0.05*(sin({P}*{q(220, L)}*t)+sin({P}*{q(293.66, L)}*t))*(0.6+0.4*sin({P}*t*2))"
    return [tone(f"{kick}+{bass}+{pad}", L)], "lowpass=f=5000"

def music_crypt(L=32):
    """Cripta Afogada: do menor, grave e abafado, com gotas agudas espacadas."""
    pad = [(65.41, 16), (98.0, 8), (130.81, 32), (155.56, 16)]
    parts = [f"0.075*sin({P}*{q(f, L)}*t)*(0.7+0.3*sin({P}*t/{per}))" for f, per in pad]
    drips = [(1046.5, 1.3), (1568.0, 4.1), (1244.5, 6.7), (1046.5, 9.9), (1396.9, 13.2), (1174.7, 15.6),
             (1568.0, 18.8), (1244.5, 21.1), (1046.5, 24.9), (1396.9, 27.3), (1174.7, 30.2)]
    for f, at in drips:
        parts.append(bell(f, at, 9.0, 0.035))
    low = [261.63, 311.13, 392.0, 349.23, 311.13, 261.63, 233.08, 261.63]
    for i, f in enumerate(low):
        parts.append(bell(f, i * 4.0 + 2.0, 1.6, 0.045))
    return [tone("+".join(parts), L)], "lowpass=f=2600"

def music_garden(L=32):
    """Jardim de Pedra: sol maior, claro, sinos em pentatonica."""
    pad = [(98.0, 16), (146.83, 8), (196.0, 32), (246.94, 16)]
    parts = [f"0.06*sin({P}*{q(f, L)}*t)*(0.7+0.3*sin({P}*t/{per}))" for f, per in pad]
    scale = [392.0, 440.0, 493.88, 587.33, 659.25, 587.33, 493.88, 440.0, 392.0, 493.88, 587.33, 783.99, 659.25, 587.33, 493.88, 440.0,
             392.0, 440.0, 587.33, 493.88]
    for i, f in enumerate(scale):
        if i % 5 == 4:
            continue
        parts.append(bell(f, i * 1.5 + 0.4, 2.6, 0.048))
    return [tone("+".join(parts), L)], "lowpass=f=5200"

S["music_crypt"] = music_crypt()
S["music_garden"] = music_garden()
S["music_ambient"] = music_ambient()
S["music_boss"] = music_boss()


def render(name, sources, final):
    cmd = ["ffmpeg", "-y", "-loglevel", "error"]
    for s in sources:
        cmd += ["-f", "lavfi", "-i", s]
    graph = "".join(f"[{i}:a]" for i in range(len(sources)))
    graph += f"amix=inputs={len(sources)}:normalize=0:duration=longest" if len(sources) > 1 else "anull"
    if final:
        graph += "," + final
    graph += ",alimiter=limit=0.95[out]"
    cmd += ["-filter_complex", graph, "-map", "[out]", "-ac", "1", "-ar", str(SR), "-c:a", "pcm_s16le", os.path.join(OUT, name + ".wav")]
    r = subprocess.run(cmd, capture_output=True, text=True)
    if r.returncode != 0:
        print("FALHOU", name, r.stderr.strip()[-400:])
        return False
    return True


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    # com nomes na linha de comando, gera so esses (o ruido e aleatorio: regerar tudo muda todos os arquivos)
    names = sys.argv[1:] or list(S)
    ok = sum(render(n, *S[n]) for n in names)
    print(f"{ok}/{len(names)} sons gerados em {os.path.normpath(OUT)}")
    sys.exit(0 if ok == len(names) else 1)
