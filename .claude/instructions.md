Instrucciones del Agente de Audio de Unity
Resumen Ejecutivo: Este archivo define las instrucciones actualizadas para el agente de audio de Unity. Enfatiza el alcance restringido (solo Assets/Scripts/SounApp), la arquitectura del módulo (entrada de audio, detección de tono vía YIN/PYIN, mapeo de notas, lógica de juego) y los parámetros técnicos clave (Fs=44100 Hz; tamaño de bloque 1024/2048 muestras; solapamiento 50–75 %; filtros pasabajos 1200 Hz y pasaaltos 60 Hz; umbrales de RMS y ZCR; umbral YIN ≈0.1; suavizado por mediana; tolerancia ±10 cents, etc.). Incluye ejemplos de código C#, pseudocódigo para evaluación de notas de 1s, tablas comparativas (bloque/solapamiento/latencia) y un diagrama mermaid del flujo: Audio→Preprocesamiento→Detección→Postprocesamiento→Lógica. Al final, un checklist de tareas pendientes y referencias científicas (priorizando fuentes primarias y papers sobre YIN/PYIN y psicoacústica).

Restricción Principal
Ámbito: El agente solo puede leer y modificar código dentro de Assets/Scripts/SounApp.
Prohibido: No analizar ni cambiar ningún código fuera de esa carpeta (scripts de Unity por defecto, Packages/, ProjectSettings/, etc.).
Contexto Permitido
Solo se leerán archivos fuente en Assets/Scripts/SounApp. Cualquier otro recurso (assets, configuración de proyecto, etc.) está fuera de alcance y debe ignorarse.
Objetivo del Módulo
Este módulo implementa un sistema educativo de audio para detectar notas musicales basadas en la voz del usuario. El agente debe asegurar que el código maneje correctamente el flujo de audio (captura, detección de tono, mapeo de nota y lógica del juego) según los criterios técnicos y psicoacústicos requeridos.

Arquitectura del Sistema
El sistema sigue una tubería típica de reconocimiento de afinación:

mermaid
Copiar
graph LR
    AudioInput([Entrada de Audio]) --> Preprocessing([Preprocesamiento])
    Preprocessing --> PitchDetection([Detección de Tono (YIN/PYIN)])
    PitchDetection --> Postprocessing([Postprocesamiento y Análisis])
    Postprocessing --> GameLogic([Lógica del Juego])
Entrada de audio: Audio en tiempo real desde micrófono.
Preprocesamiento: Filtrado y preparación de la señal (ventaneo, normalización, filtros).
Detección de tono: Cálculo de F₀ usando YIN/PYIN (dominando en tiempo); no usar FFT simple debido a baja resolución en frecuencias graves.
Mapeo de notas: Convertir la frecuencia detectada a nota MIDI y calcular desviación (cents).
Lógica de juego: Decidir si la nota canta está afinada y retroalimentar al usuario (barra gráfica, puntaje).
Parámetros Recomendados
Frecuencia de muestreo (Fs): 44100 Hz (estándar para voz).
Tamaño de bloque: 1024 o 2048 muestras (~23 ms o ~46 ms por bloque). Véase tabla abajo.
Solapamiento (Overlap): 50%–75%. Ejemplo: con 2048 muestras y 50% se avanza 1024 muestras (≈23 ms); con 75%, 512 muestras (≈11.6 ms). Esto aumenta la resolución temporal y evita “puntos ciegos” al cambiar notas. Siempre usar ventanas Hann/Hamming para aplicar solapamiento consistente.
Tamaño de bloque (muestras)	Duración aprox.	Hop 50% (muestras)	Duración hop (ms)
1024	~23 ms	512	~11.6 ms
2048	~46 ms	1024	~23.2 ms

Ventana: Hann o Hamming; Hop size (desplazamiento) = tamaño_bloque × (1 - solapamiento). Ej. 50% overlap → hop = 0.5×block.
Preprocesamiento de Señal
Filtro pasaaltos: Corte en ~60 Hz para eliminar ruido de fondo muy bajo.
Filtro pasabajo: Corte en ~1200 Hz (aprox. Do6). La mayoría de cantantes no tienen F₀ por encima de ~1000 Hz, por lo que esto elimina armónicos innecesarios.
Normalización y ventana: Aplicar ventana (Hann/Hamming) a cada bloque para reducir artefactos en extremos.
Detección de Tono (YIN/PYIN)
Algoritmo YIN: Usar YIN (o versión probabilística pYIN) en el dominio del tiempo. Esto supera métodos basados en FFT o cepstrum, reduciendo errores de octava.
Función de diferencia autocorrelacionada (d(τ)): Calcular la función de diferencia en cada bloque.
CMNDF (Cumulative Mean Normalized Difference): Normalizar acumulativamente como en la fórmula de YIN.
Umbral absoluto: Seleccionar el primer mínimo de d′(τ) por debajo de un umbral (~0.1). Si no hay valores < umbral, escoger el mínimo global. Esto evita confundir armónicos (salto de octava).
Interpolación parabolica (opcional): Refinar el pico mínimo con interpolación parabolica para mayor precisión (a medio cuadro).
Configuración: No implementar YIN “desde cero” obligatoriamente; se pueden usar bibliotecas confiables (por ejemplo, Aubio en C#/C++, librosa.pyin en Python, PitchFinder.js en JS).
Ventanas Deslizantes y Solapamiento
Análisis continuo: No procesar bloques de 1 segundo fijos como un todo. Usar bloques pequeños superpuestos (sliding windows) con avance constante.
Ejemplo: 1 segundo (44100 muestras) puede segmentarse en bloques de 2048 muestras con 50–75% de overlap. Esto produce ≈43–86 mediciones de frecuencia por segundo en tiempo real.
Beneficios: El solapamiento evita “puntos ciegos” en las fronteras de bloques y mejora la suavidad del gráfico de afinación. Por ejemplo, con 50% de solapamiento, se obtiene una lectura de afinación cada ~23 ms en lugar de cada ~46 ms.
Detección Voiced/Unvoiced (Silencio y Consonantes)
Puerta de silencio: Calcular RMS del bloque. Si está por debajo de un umbral (ej. –40 dBFS), considerar silencio o ruido de fondo y descartar el bloque.
Tasa de cruces por cero (ZCR): Las consonantes (S, T, P, etc.) producen alta ZCR. Si el ZCR supera un umbral alto típico, descartar el bloque como no afinado (ruido percusivo, no tono estable).
Resumen: Así se evita que el algoritmo intente asignar nota a ruidos o pausas.
Cuantización y Error en Cents
Convertir F₀ a nota MIDI:
csharp
Copiar
double midiVal = 69 + 12 * Math.Log(frequencyHz / 440.0, 2);
Nota objetivo: Redondear midiVal al entero más cercano (por ejemplo, 69 = La4).
Error en cents:
csharp
Copiar
double centsError = (midiVal - Math.Round(midiVal)) * 100;
Esto da la desviación en cents respecto a la nota más cercana. Ej. F0 = 442.3 Hz → midiVal ≈ 69.09 → desviación ≈ +9 cents (ligeramente agudo).
Suavizado y Vibrato
Vibrato: La voz entonada presenta vibrato natural (oscilaciones de F₀ ~4–7 Hz). No se debe penalizar el micro-vibrato involuntario.
Filtro de mediana: Mantener un buffer de los últimos ~3–5 valores de F₀ calculados y usar la mediana como frecuencia central. La mediana atenúa valores atípicos (picos de ataque o gallos) y refleja la frecuencia en la que el cantante pasó más tiempo.
Filtro de media móvil (opcional): Alternativamente, un filtro promedio móvil suave la señal de F₀ en tiempo real.
Reglas de Negocio para Afinación
Margen humano: Considerar afinada la nota si el error absoluto está en ±10 cents (umbral de tolerancia típico).
Porcentaje de tiempo en tono: Calcular el porcentaje de frames en los que la desviación ≤15 cents (o umbral deseado). Ejemplo de lógica:
text
Copiar
// Pseudocódigo para 1 segundo de audio: frecuencias_validas = [] for cada frame en 1s: if no es silencio ni ruido: f = calcularF0_YIN(frame) frecuencias_validas.append(f) if frecuencias_validas.empty: notaMalCantada();

f_central = mediana(frecuencias_validas) midiObjetivo = Round(69 + 12log2(f_central/440)) framesAfinados = count de f en frecuencias_validas tal que |(69+12log2(f/440)) - midiObjetivo|*100 ≤ 15 porcentajeAfinacion = framesAfinados / frecuencias_validas.Count * 100 if porcentajeAfinacion > 70: resultado = "Nota afinada" else: resultado = "Nota desafinada o glissando"

php
Copiar
- **Decisión final:** Si el cantante estuvo >70% del tiempo dentro del rango tolerado, se considera que la nota se percibe afinada. De lo contrario, se clasifica como desafinada o transición continua.  

## Salidas y Retroalimentación  
- **Porcentaje de afinación:** Calcular y mostrar al usuario qué porcentaje del tiempo estuvo en rango.  
- **Gráfico en tiempo real:** Dibujar la fluctuación de F₀ sobre tiempo (actualizando cada bloque, p.ej. cada 23 ms) para mostrar la línea de entonación (como en SingStar/Smule).  
- **Indicador final:** Al terminar la nota (1s), pintar barra de color verde/rojo según porcentaje de afinación.  

## Ejemplos de Código

- **Conversión Hertz a MIDI y cents (C#):**  
```csharp
double frequencyHz = /* frecuencia detectada por YIN */;
double midiVal = 69 + 12 * Math.Log(frequencyHz / 440.0, 2);
int midiNote = (int)Math.Round(midiVal);
double centsError = (midiVal - midiNote) * 100;
// centsError en [-50,50]: negativo=grave, positivo=agudo
Mediana de un conjunto de frames (C#):

csharp
Copiar
List<double> freqs = /* lista de F0 de frames válidos */;
freqs.Sort();
double mediana = freqs[freqs.Count/2];
Pseudocódigo de evaluación de 1s: (como arriba en Reglas de Negocio).

Tablas Comparativas
Tamaño de bloque vs latencia: Ya mostrada arriba. Un bloque más grande reduce la latencia de procesamiento pero añade retraso de al menos el tamaño de bloque; solapamiento incrementa frecuencia de actualización.

Opciones de solapamiento:

Solapamiento	Hop (muestras)	Latencia efectiva
0%	Muestras completas	Bloque completo (e.g. 46 ms con M=2048)
50%	50% de M	50% del tamaño de bloque (p.ej. ≈23 ms)
75%	25% de M	25% del tamaño de bloque (p.ej. ≈11.5 ms)

Filtros pasabandas:

Filtro	Frecuencia de corte	Objetivo
Pasaaltos	60 Hz	Eliminar ruidos muy graves
Pasabajo	1200 Hz	Limitar F₀ humanos (hasta Do6)

Checklist de Tareas Pendientes
 Confirmar que la frecuencia de muestreo esté en 44100 Hz en la configuración de audio.
 Verificar tamaños de buffer: 1024/2048 muestras con solapamiento adecuado.
 Implementar (o ajustar) filtros pasabajo y pasaaltos (60 Hz–1200 Hz).
 Añadir detector de silencio (puerta RMS -40 dB) y detector de consonantes (alto ZCR).
 Implementar algoritmo YIN/PYIN con CMNDF y umbral ~0.1.
 Aplicar ventana Hann/Hamming con solapamiento en cada bloque.
 Calcular mediana de últimas ~3–5 frecuencias para suavizado de vibrato.
 Convertir F₀ a nota MIDI y calcular desviación en cents (±10c margen).
 Contar frames afinados (>70%) y determinar nota afinada/desafinada.
 Generar gráficos y resultados (porcentaje, barra de feedback).
 Escribir ejemplos de código en C# en Assets/Scripts/SounApp (métodos utilitarios).
 NO modificar scripts fuera de SounApp; el agente debe informar si detecta referencias fuera de alcance.
Referencias
Cheveigné & Kawahara (2002), “YIN, a fundamental frequency estimator for speech and music” (algoritmo YIN, supera métodos clásicos).
Franco Caspe et al. (2017), Estimador de F0 en tiempo real basado en YIN (implementación embebida de YIN; umbral YIN ≈0.1).
Franco Caspe et al. (2017), Fundamentos de F0 en voz (características de la voz: consonantes inarmónicas, vibrato natural).
Hong (2022), “A comprehensive look into the YIN algorithm” (detalles de CMNDF y umbral).
Nota: Las referencias citadas guían la implementación de YIN/PYIN y las consideraciones psicoacústicas (integración temporal, tolerancias humanas, vibrato). Use estas fuentes para validar fórmulas y parámetros.