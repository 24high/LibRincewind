# LibRincewindHoney — Deniability, die auch für Passwörter hält

Behebt eine gemessene Lücke zwischen dem, was LibRincewind verspricht, und dem,
was es für **strukturierte** Klartexte — allen voran gespeicherte Passwörter —
tatsächlich leistet.

## Das Problem

`CRincewind` verspricht: Brute Force liefert kein Signal. Das gilt aber nur,
solange der *Klartext selbst* gleichverteilt über das 95-Zeichen-Alphabet
aussieht. Ein Passwort ist das nicht.

Gemessen an der unveränderten Bibliothek (32 menschliche Passwörter, je 400
falsche Rateversuche, bewertet von einem Bigramm-Modell auf einem **disjunkten**
Passwort-Korpus):

```
ohne DTE:   richtiges Passwort auf Rang #1 in 31 von 32 Fällen
```

Der Grund ist logisch, nicht implementierungsbedingt: Entschlüsseln mit dem
richtigen Schlüssel *muss* den Klartext liefern. Ist der Klartext erkennbar, ist
der Schlüssel erkennbar. **Kein Cipher kann das allein beheben** — die Lösung muss
ändern, *was* verschlüsselt wird.

## Die Lösung: ein DTE (Honey Encryption)

`PasswordDte` kodiert ein Passwort in einen **144 Zeichen langen Seed** über
demselben Alphabet. Entscheidend ist, dass `Decode` **total** ist: *jeder* der
95^144 möglichen Seeds ergibt ein plausibles Passwort. Ein falscher Schlüssel
liefert damit keinen Müll mehr, sondern Köder:

```
"Princess2768"   "hockey141"   "Master2004?"   "p@$$w0rd123"   "dragon436089"
```

Vorher an derselben Stelle: `hx~FFmjzz%xqQo,B3.}qLJE`

Das ist Honey Encryption (Juels & Ristenpart 2014) mit einem Passwort-Tresor-DTE
(vgl. Chatterjee et al. 2015). **Kerckhoffs-konform:** das Modell ist vollständig
öffentlich und enthält kein Geheimnis.

### Warum kein Header, keine Längenangabe, kein Padding-Check

Jedes davon wäre ein Gültigkeitstest — und ein Gültigkeitstest ist genau das
Orakel, das die Konstruktion beseitigen soll, denn er würde nur beim richtigen
Schlüssel aufgehen. Deshalb: fester Seed von 144 Zeichen für *jedes* Passwort
(das verbirgt zugleich die Passwortlänge), der Decoder liest nur so viele Zeichen,
wie seine Entscheidungen brauchen, und **schaut den Rest nie an**. Der ungenutzte
Schwanz wird beim Kodieren mit frischem Zufall gefüllt und ist unbeschränkt — dort
gibt es nichts zu verifizieren, also auch nichts zu testen.

## Gemessene Ergebnisse

Angreifer: Bigramm-Modell mit Rand-Token, trainiert auf einem **disjunkten**
Korpus (sieht die DTE-Tabellen nie). Bewertet wird **zweiseitig** — auf dem
letzten Rang zu landen verrät genauso wie auf dem ersten.

| Lauf | mittlerer Rang | #1 getroffen | Tails (ideal 20 %) |
|---|---|---|---|
| **Baseline, ohne DTE** (Wort+Ziffern) | **1 / 401** | **31/32** | 100 % |
| **mit DTE** (Wort+Ziffern) | 100 / 401 | 0/32 | 6 % |
| **mit DTE** (gemischter Tresor) | **163 / 401** | 0/61 | 15 % |

Ideal für einen perfekten DTE: mittlerer Rang 201, ~20 % in den Tails.

Weitere geprüfte Invarianten: 494 Passwörter (inkl. Sonderfälle, Mixed Case,
Symbole, 64-Zeichen-Passphrasen, 300 zufällige Hochentropie-Strings) durchlaufen
`Decode(Encode(p)) == p` exakt; jeder Seed ist exakt 144 Zeichen lang.

## Ehrliche Grenzen

- **Restsignal bleibt.** Mittlerer Rang 100 statt 201 im engen Testfall heißt: das
  echte Passwort liegt im besseren Viertel, nicht in der Mitte. Der Angriffsaufwand
  steigt von „1 Versuch" auf „~100 Kandidaten prüfen" — eine deutliche, aber keine
  vollständige Verbesserung.
- **Die Ursache ist Modell-Mismatch, nicht die Konstruktion.** Sobald die
  Passwort-Mischung des Tresors der des Modells ähnelt (Zeile 3), nähert sich der
  Rang dem Ideal (163 von 201). Der wirksamste Hebel ist daher, `Words` durch eine
  echte, frequenzsortierte Liste aus einem Leak-Korpus zu ersetzen und die
  Template-Gewichte an den realen Bestand anzupassen.
- **„Modell gegen Modell" ist prinzipiell.** Ist das Passwortmodell des Angreifers
  schärfer als dieses, bleibt eine Trennung möglich. Das ist Honey Encryption
  inhärent — publizierte Tresor-Verfahren wurden genau deshalb verbessert und
  erneut gebrochen (Golla et al. 2016). Es gibt hier keinen Beweis, nur eine
  messbar höhere Latte.
- **Weiterhin keine Integrität.** Wie der Rest der Bibliothek bewusst
  unauthentifiziert: Chiffretext ist formbar, Manipulation entschlüsselt still zu
  Müll. Ein MAC wäre wieder genau das Schlüssel-Orakel, das hier entfernt wird.

## Verwendung

```csharp
var vault = new HoneyVault();                       // AES-CTR + OS-CSPRNG
string env = vault.Protect("dragon2011", k1, k2);
string pw  = vault.Reveal(env, k1, k2);             // falscher Key -> plausibler Köder
```

Oder direkt, wenn der Cipher schon woanders sitzt:

```csharp
string envelope = rincewind.encryptString(PasswordDte.Encode(pw), k1, k2);
string recovered = PasswordDte.Decode(rincewind.decryptString(envelope, k1, k2));
```

## Dateien

| Datei | Inhalt |
|---|---|
| `PasswordDte.cs` | der DTE: Kodiertabellen, PCFG-Modell, `Encode` / `Decode` |
| `HoneyVault.cs` | Wrapper, der DTE und `CRincewind` verbindet |
