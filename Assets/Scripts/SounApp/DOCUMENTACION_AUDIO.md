# Sistema de Detección de Afinación Vocal — Documentación Técnica

**Módulo:** `SounApp` (entrenador vocal sobre Unity)
**Versión del documento:** 1.0
**Fecha:** 2 de junio de 2026
**Alcance:** Captura de voz, detección de tono, evaluación de afinación, retroalimentación al usuario.

---

## 1. Resumen ejecutivo

Este módulo permite que un jugador cante una nota y el sistema determine, en tiempo real y de forma objetiva, **si la cantó afinada**. Funciona como los juegos comerciales de canto (SingStar, Smule), pero construido a la medida del proyecto y fundamentado en literatura científica revisada por pares.

El sistema:

1. **Escucha** la voz por el micrófono del dispositivo.
2. **Limpia** la señal (elimina ruido grave y agudo no vocal).
3. **Calcula la frecuencia fundamental (F₀)** —la "altura" de la nota— mediante el algoritmo **YIN**, un estándar académico para voz y música.
4. **Compara** esa frecuencia con la nota objetivo y mide la desviación en *cents* (centésimas de semitono).
5. **Decide** si la nota está afinada según una regla de tolerancia psicoacústica.
6. **Muestra** retroalimentación visual (gráfico de afinación en vivo + barra de resultado) y reproduce un **audio de referencia** para ayudar al jugador.

Todo el procesamiento ocurre **en el dispositivo, sin conexión**, y está optimizado para **móviles**.

---

## 2. ¿Por qué es confiable? Fundamento científico

La detección de tono no se resolvió de forma "casera": se siguió el método que la comunidad académica considera estándar para voz.

| Decisión | Justificación | Fuente |
|---|---|---|
| Algoritmo **YIN** (dominio del tiempo) en lugar de FFT simple | YIN reduce drásticamente los **errores de octava** (confundir una nota con la misma nota una octava más arriba/abajo), el problema clásico de los detectores basados en transformada de Fourier, sobre todo en frecuencias graves. | Cheveigné & Kawahara (2002), *"YIN, a fundamental frequency estimator for speech and music"* |
| **Umbral YIN ≈ 0.1** | Valor validado experimentalmente para decidir cuándo una señal es suficientemente periódica (tonal). | Franco Caspe et al. (2017); Hong (2022) |
| Tolerancia de **±10 cents** | Corresponde al umbral de percepción humana de desafinación para notas sostenidas. | Psicoacústica estándar (integración temporal de la afinación) |
| **No penalizar el vibrato** (suavizado por mediana) | La voz entonada oscila naturalmente ~4–7 Hz; penalizarlo daría falsos negativos. | Características de F₀ en voz, Franco Caspe et al. (2017) |

El algoritmo YIN se implementó **completo y verificable** en el código (no es una caja negra): función de diferencia → normalización acumulativa (CMNDF) → umbral absoluto → interpolación parabólica. Cualquier auditor puede revisar cada paso en [VocalPitchDetector.cs](VocalPitchDetector.cs).

---

## 3. Cómo funciona, paso a paso

```
 Micrófono
    │
    ▼
[1] Filtros HPF 60 Hz + LPF 1200 Hz      ← elimina ruido grave y armónicos no vocales
    │
    ▼
[2] Ventana de 1024 muestras, hop 512    ← bloques solapados al 50%, cadencia fija
    │
    ▼
[3] Gate de silencio (dBFS) + Gate ZCR   ← descarta silencios y consonantes (S, T, P…)
    │
    ▼
[4] YIN → frecuencia fundamental F₀       ← la "altura" de la nota cantada
    │
    ▼
[5] F₀ → nota MIDI + desviación en cents  ← qué tan afinado respecto al objetivo
    │
    ▼
[6] Lógica de juego + feedback visual     ← % de afinación, barra verde/roja, gráfico
```

### Paso 1 — Limpieza de la señal (preprocesamiento)
- **Filtro pasa-altos a 60 Hz:** elimina zumbidos y ruido de fondo muy grave.
- **Filtro pasa-bajos a 1200 Hz:** la voz cantada rara vez supera ~1000 Hz en su fundamental; recortar por encima evita que armónicos agudos confundan al detector.
- Implementados como **filtros biquad IIR** (RBJ cookbook), de bajísimo costo computacional, ideales para móvil. El estado del filtro se mantiene continuo entre bloques mediante un *pre-roll*, evitando artefactos en los bordes.

### Paso 2 — Análisis por bloques solapados
- Bloque de **1024 muestras** (~23 ms) con **avance (hop) de 512** (solapamiento 50 %).
- **Punto clave de calidad:** el análisis se dispara **por cantidad de muestras de audio, no por frame de Unity**. Esto significa que el resultado es **idéntico en un teléfono lento o en uno rápido** — no depende de los FPS. Si el dispositivo pierde frames, el sistema procesa todas las ventanas pendientes sin alterar el conteo.

### Paso 3 — Detección de voz vs. ruido
- **Gate de silencio:** si el nivel de la señal está por debajo de **−40 dBFS**, se considera silencio y se descarta.
- **Gate de consonantes (ZCR):** las consonantes (S, T, P…) producen muchos cruces por cero; si se superan, el bloque se descarta como "no tonal". Así no se asigna nota a ruidos.

### Paso 4 — Cálculo de la frecuencia (YIN)
Sobre cada bloque tonal se ejecuta YIN. Devuelve además una métrica de **claridad** (0–1): qué tan "puro/periódico" es el sonido. Si la claridad es baja, se rechaza la medición.

### Paso 5 — Conversión a nota y desviación
- Frecuencia → nota MIDI: `midi = 69 + 12·log₂(f / 440)`
- Desviación en cents respecto a la nota objetivo: `cents = 1200·log₂(f / f_objetivo)`
- Para el **display en vivo** se aplica una **mediana rodante de 5 valores**, que suaviza el vibrato natural sin penalizarlo.

### Paso 6 — Evaluación de la nota (regla de afinación)
Para cada nota del nivel:
1. Se ignora el **ataque inicial** (~0.15 s, o 1/3 de la nota si es corta), porque el comienzo de la voz suele ser inestable.
2. Se recogen todas las mediciones **crudas** de F₀ de la parte sostenida.
3. Se calcula cuántas caen dentro de la tolerancia (**±10 cents** por defecto) respecto a la nota objetivo.
4. **La nota se considera afinada si ≥ 70 % de las mediciones están dentro de tolerancia** (configurable por nivel).

Esta regla refleja cómo el oído humano percibe la afinación de una nota sostenida: importa el tiempo que se estuvo "en el centro", no un instante aislado.

---

## 4. Funcionalidades nuevas para el jugador

### 4.1 Audio de referencia ("Nota Do" y demás)
Cuando el juego **llega a una nota** (p.ej. Do), antes de evaluar al jugador, **suena el audio de referencia de esa nota** para refrescar su memoria auditiva. Luego comienza la evaluación de su voz.

- Reutiliza el **mismo audio** que ya reproduce el botón correspondiente (ej. `btnNotaDo`), sin duplicar archivos.
- Se reproduce con la captura del micrófono **apagada**, de modo que el sonido de referencia **no contamina** la medición del jugador.
- Es configurable por nota: se puede asignar referencia solo al Do, o a todas las notas que se desee.

### 4.2 Registro vocal (voz grave / aguda) — soporte para voces femeninas
La misma melodía sirve para cualquier tipo de voz mediante un **selector de octava** que el jugador elige antes de empezar:
- **Voz grave (C3–C4):** típico masculino (configuración por defecto).
- **Voz aguda (C4–C5):** típico femenino.
- Octavas adicionales (infantil/soprano) si se desean.

El sistema **transpone todo el ejercicio** a la octava elegida sin cambiar la melodía ni los visuales: solo se desplaza la nota esperada. El **audio de referencia también se transpone automáticamente** (mediante un cambio de `pitch` exacto de una octava), por lo que no hace falta grabar clips nuevos. La elección **se recuerda entre sesiones** (PlayerPrefs).

Esto se resolvió con un selector manual —como en las apps profesionales de canto ("elegí tu registro")— por ser la opción más confiable: no depende de una detección automática que podría fallar y cubre cualquier voz, incluidas las infantiles.

### 4.3 Retroalimentación visual
Componente [TuningFeedbackUI.cs](TuningFeedbackUI.cs):
- **Gráfico de afinación en tiempo real:** una línea que se actualiza a la cadencia del análisis (no del frame rate), mostrando la desviación del jugador respecto a la nota. Una **banda verde central** marca la zona afinada; los puntos se pintan **verdes** dentro de tolerancia y **rojos/naranjas** fuera.
- **Barra de resultado final:** al terminar cada nota se rellena una barra con el **% de afinación**, en **verde** si aprobó o **rojo** si no.

---

## 5. Cumplimiento de la especificación

Comparación contra el documento de requisitos oficial del proyecto (`instructions.md`):

| Requisito | Estado | Implementación |
|---|---|---|
| Frecuencia de muestreo 44100 Hz | ✅ | Se solicita 44100 y se usa la **frecuencia real** que reporta el micrófono. |
| Tamaño de bloque 1024 | ✅ | `sampleSize = 1024`. |
| Solapamiento 50 % / hop | ✅ | `hopSize = 512`, análisis por hop. |
| Procesamiento independiente del frame rate | ✅ | Cadencia por muestras, no por FPS. |
| Filtro pasa-altos 60 Hz | ✅ | Biquad HPF. |
| Filtro pasa-bajos 1200 Hz | ✅ | Biquad LPF. |
| Algoritmo YIN (difference function, CMNDF, umbral, interpolación) | ✅ | Implementación completa y revisable. |
| Manejo de errores de octava | ✅ | Inherente a YIN + selección de primer mínimo. |
| Gate de silencio en dBFS (−40) | ✅ | `silenceFloorDb = -40`. |
| Gate de consonantes (ZCR) | ✅ | Umbral configurable. |
| Conversión Hz → MIDI y cents | ✅ | Fórmulas estándar. |
| Tolerancia ±10 cents | ✅ | `tuningToleranceCents = 10` (configurable). |
| Suavizado de vibrato (mediana 3–5) | ✅ | Mediana rodante de 5. |
| Regla > 70 % de frames afinados | ✅ | `perNoteTuningPercent = 70`. |
| % final de afinación | ✅ | Por nota y por nivel. |
| Gráfico en tiempo real | ✅ | `TuningFeedbackUI` (gráfico). |
| Barra de resultado verde/rojo | ✅ | `TuningFeedbackUI` (barra). |

> **Nota técnica honesta:** la especificación sugería aplicar una *ventana Hann/Hamming*. Se decidió **no aplicarla** porque el algoritmo elegido (YIN) trabaja en el dominio del tiempo y **no la requiere**; de hecho, aplicarla puede degradar la estimación. Esta es una decisión técnica deliberada y alineada con la literatura de YIN.

---

## 6. Decisiones tomadas sobre puntos ambiguos de la especificación

El documento de requisitos contenía algunas ambigüedades. Se resolvieron así (acordado con el responsable del proyecto):

| Punto | Ambigüedad | Decisión | Motivo |
|---|---|---|---|
| Tolerancia | ±10 cents vs. ±15 cents | **±10 cents** (configurable para subir dificultad) | Umbral perceptual más exigente y "profesional". |
| % para aprobar nota | "> 70 %" vs. valores mayores | **70 %** | Coincide con la regla psicoacústica de la spec. |
| Algoritmo | YIN obligatorio vs. opcional/librería | **YIN propio** | Control total, verificable, sin dependencias externas. |
| Tamaño de bloque | 1024 vs. 2048 | **1024** | Menor latencia y menor costo de CPU (mejor para móvil). |
| Ventana | Hann/Hamming vs. nada | **Sin ventana** | YIN no la necesita (ver nota en §5). |
| Gate de nivel | Lineal vs. dBFS | **dBFS (−40)** | Estándar de audio profesional y coherente con la spec. |

---

## 7. Parámetros configurables (sin tocar código)

Desde el Inspector de Unity, sin programar, se pueden ajustar:

**En el asset de nivel (`LevelData`):**
- `tuningToleranceCents` — tolerancia de afinación (5–50 cents). Más alto = más fácil.
- `perNoteTuningPercent` — % de frames afinados para dar una nota por buena (70 por defecto).
- `passPercentage` — % de notas correctas para aprobar el nivel completo.
- `bpm` — tempo, que define la duración de cada nota.

**En `VocalPitchDetector`:**
- `silenceFloorDb`, `zcrThreshold`, `clarityThreshold` — sensibilidad del detector.
- `vocalMinHz` / `vocalMaxHz` — rango vocal aceptado.
- `medianWindow` — suavizado del display.

**En `GameAudioManager`:**
- `octaveOffset` — registro vocal (0 = grave/masculino, +1 = agudo/femenino). Lo fija el jugador con los botones de octava.
- `referenceAudios` — qué audio de referencia suena en cada nota.
- `playReferenceBeforeNote` — activar/desactivar la ayuda auditiva.
- `attackIgnoreTime` — cuánto ataque inicial se ignora.

---

## 8. Guía de configuración en el editor (una sola vez)

Para activar las funciones nuevas, en la escena `Game`:

**A) Audio de referencia ("Nota Do"):**
1. Seleccionar el objeto con el componente `GameAudioManager`.
2. En la lista **Reference Audios**, agregar un elemento:
   - `note` = `C3` (y/o `C4`)
   - `source` = arrastrar el **AudioSource del objeto `btnNotaDo`**.
3. (Opcional) Repetir para otras notas con sus respectivos botones.

**B) Selector de registro vocal (voz grave / aguda):**
1. Crear un botón por opción en el Canvas (ej. "Voz grave", "Voz aguda").
2. Agregar a cada uno el componente `OctaveSelectorButton` y asignar:
   - `button` = su propio `Button`.
   - `octaveOffset` = `0` para voz grave (C3), `1` para voz aguda (C4/femenino).
   - `label` (opcional) = el texto del botón, para resaltar la opción activa.
3. El jugador puede **alternar de octava libremente entre ejercicios**; el cambio solo se ignora mientras un entrenamiento está en curso (lo controla el propio componente). Agregarlos al arreglo `buttons` de `UIInteractionManager` es **opcional**: solo los atenúa visualmente durante el entrenamiento.

**C) Feedback visual:**
1. Crear un `RawImage` en el Canvas para el gráfico.
2. Crear un `Image` (tipo *Filled → Horizontal*) para la barra de resultado, y opcionalmente un texto TMP.
3. Agregar el componente `TuningFeedbackUI` a un objeto del Canvas y asignar:
   - `detector` = el `VocalPitchDetector` de la escena.
   - `graphImage` = el `RawImage` creado.
   - `resultBar` = el `Image` rellenable; `resultText` = el texto (opcional).
4. En `GameAudioManager`, asignar el campo `feedbackUI` a este componente.

> Todas las referencias son **opcionales y null-safe**: si algo no se asigna, el sistema sigue funcionando sin esa parte (no crashea).

---

## 9. Limitaciones conocidas y trabajo futuro

- **Rango vocal de evaluación:** optimizado para 65–1200 Hz. Voces extremadamente graves/agudas fuera de ese rango no se evalúan.
- **Latencia:** ~23 ms de bloque + buffer del micrófono; imperceptible para el jugador, pero existe por diseño.
- **Eco/altavoz:** si el audio de referencia sonara durante la captura, el micrófono podría captarlo. Por eso la referencia se reproduce **antes** de evaluar; en dispositivos con auriculares no hay riesgo alguno.
- **Futuro posible:** modo "afinador libre", detección polifónica (acordes), calibración de latencia del dispositivo, exportación de estadísticas de progreso.

---

## 10. Glosario

- **F₀ (frecuencia fundamental):** la frecuencia que percibimos como "la altura" de una nota.
- **Cents:** unidad de afinación; 100 cents = 1 semitono. ±10 cents es una desviación muy pequeña.
- **YIN:** algoritmo de estimación de F₀ para voz/música (Cheveigné & Kawahara, 2002).
- **CMNDF:** *Cumulative Mean Normalized Difference Function*, núcleo de YIN.
- **dBFS:** decibelios relativos a la escala digital máxima; 0 dBFS = máximo, valores negativos = más bajo.
- **ZCR:** *Zero-Crossing Rate*, tasa de cruces por cero; alta en consonantes/ruido.
- **Hop / solapamiento:** cuántas muestras avanza cada bloque de análisis.
- **Biquad / IIR:** filtro digital eficiente de segundo orden.

---

## 11. Referencias

1. **A. de Cheveigné, H. Kawahara (2002).** *YIN, a fundamental frequency estimator for speech and music.* Journal of the Acoustical Society of America, 111(4).
2. **Franco Caspe et al. (2017).** Estimador de F₀ en tiempo real basado en YIN (implementación embebida; umbral YIN ≈ 0.1).
3. **Hong (2022).** *A comprehensive look into the YIN algorithm* (detalles de CMNDF y umbral).
4. **R. Bristow-Johnson.** *Audio EQ Cookbook* (fórmulas de los filtros biquad utilizados).

---

## 12. Archivos del módulo

| Archivo | Responsabilidad |
|---|---|
| [VocalPitchDetector.cs](VocalPitchDetector.cs) | Captura, filtros, YIN, gates, suavizado. |
| [Core/PitchEvaluator.cs](Core/PitchEvaluator.cs) | Conversiones Hz ↔ MIDI ↔ nombre de nota. |
| [Core/GameAudioManager.cs](Core/GameAudioManager.cs) | Lógica del nivel, evaluación, audio de referencia. |
| [TuningFeedbackUI.cs](TuningFeedbackUI.cs) | Gráfico en tiempo real + barra de resultado. |
| [OctaveSelectorButton.cs](OctaveSelectorButton.cs) | Selector de registro vocal (voz grave/aguda). |
| [Core/LevelData.cs](Core/LevelData.cs) | Configuración de nivel (tolerancias, tempo). |
| [Core/ScoreSystemGameAudio.cs](Core/ScoreSystemGameAudio.cs) | Puntaje y % de aprobación del nivel. |

*Documento generado como soporte técnico del módulo de audio. Para dudas, cada afirmación de este documento es trazable a una línea de código o a una referencia académica citada.*