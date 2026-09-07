# FactorDemo — ein echtes, lauffähiges Faktorisierungs-Demo

Standalone **net8.0**-Konsolenprojekt. Faktorisiert Ganzzahlen mit den klassischen
Verfahren, die tatsächlich funktionieren, und zeigt, *welche* Art von Modul mit
*welcher* Methode fällt.

## Wichtig: Kein Riemann-Zusammenhang

Dieses Projekt entstand aus der Frage, ob Claudes Zeta-Nullstellen-Resultat
(Anteil der Nullstellen auf der kritischen Geraden von ~41,6 % → 67,2 %, durch
Kombination von Montgomerys Paarkorrelation mit den unbedingten
Paarkorrelations-Resultaten von Baluyot–Goldston–Suriajaya–Turnage-Butterbaugh)
sich in einen RSA-Angriff übersetzen lässt.

**Nein — und dieses Tool enthält bewusst keinen solchen Schritt.** Die
Riemannsche Vermutung beschreibt, *wie viele* Primzahlen unter x liegen bzw. wie
regelmäßig sie im Mittel verteilt sind. Sie sagt nichts darüber, *welche* Zahl
ein gegebenes N teilt. Es gibt keine bekannte Reduktion von RH/GRH auf
schnelleres Faktorisieren; das beste allgemeine Verfahren (GNFS) wird unter RH
nicht schneller. Was Ganzzahlen faktorisiert, sind die Algorithmen unten.

## Enthaltene Verfahren

| Methode           | Gut gegen                                             |
|-------------------|------------------------------------------------------|
| Trial division    | kleine Primfaktoren                                  |
| **Fermat**        | zwei Primfaktoren nahe beieinander (**schwacher RNG**) |
| **Pollard p−1**   | ein Faktor p, bei dem p−1 glatt (B-smooth) ist        |
| **Pollard ρ (Brent)** | allgemeiner Arbeitspferd-Algorithmus             |
| Miller–Rabin      | Primzahltest (deterministisch < 3,3·10²⁴, dann Zufallsbasen) |

Der rekursive Zerleger schält erst kleine Faktoren ab, prüft auf Primalität und
sucht sonst einen nicht-trivialen Teiler in der Reihenfolge „billig/eventuell
sofort" → „allgemein". Jede Zerlegung wird gegen `Produkt == N` verifiziert.

## Die eine sicherheitsrelevante Lehre

Fermat zerlegt **jedes** RSA-Modul sofort, dessen beide Primzahlen zu nah
beieinander erzeugt wurden — unabhängig von der Bitlänge. Das ist der reale
Angriffsvektor: nicht die Zahlentheorie der Primverteilung, sondern ein
**schlechter Primzahl-Generator**. `weak 512` demonstriert das: ein 511-Bit-Modul
fällt in Millisekunden.

## Bauen & Ausführen

> Hinweis: Die lokale `Directory.Build.props` schirmt dieses Projekt bewusst von
> der Solution-Wurzel ab (die auf net472 + .NET-Framework-Referenzpakete zwingt).
> MSBuild importiert nur die nächstgelegene `Directory.Build.props`.

```bash
dotnet build FactorDemo.csproj -c Release

# Demo-Suite (jede Methode an ihrem passenden Modul):
dotnet run -c Release

# beliebige Zahl faktorisieren:
dotnet run -c Release -- 805989520929183022144333

# zufälliges 80-Bit-Semiprime erzeugen und faktorisieren:
dotnet run -c Release -- gen 80

# schwacher RNG: nahe Primzahlen -> Fermat sofort, selbst bei 512 Bit:
dotnet run -c Release -- weak 512
```

## Grenzen (ehrlich)

- Faires RSA ab ~100 Bit ist mit Pollard ρ nicht mehr praktikabel; für größere
  Module bräuchte man das quadratische Sieb / GNFS. Dieses Demo zielt auf das
  Verständnis der Verfahren und auf *schwache* Module, nicht auf das Brechen
  echter RSA-Schlüssel.
- LibRincewind (das übrige Projekt in diesem Verzeichnis) ist **symmetrische**
  Krypto (AES-CTR + Argon2id) — es gibt dort kein N = p·q, also nichts zu
  faktorisieren. Für dessen Sicherheit zählen andere Tests (RNG-/Entropiequalität,
  Schlüsselableitung/Nonce-Handling, der RC4Plus-Combiner).
