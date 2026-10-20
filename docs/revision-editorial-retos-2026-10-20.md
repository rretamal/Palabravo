# Revisión editorial de retos — 20 de octubre de 2026

## Corrección tras inspeccionar el empaquetado

El archivo antiguo de Resources/Raw no se incluye: Palabravo.csproj lo excluía y empaquetaba el JSON web. La diferencia de archivos detectada abajo no constituía una divergencia activa del binario. En la implementación posterior se elimina el archivo obsoleto y se añade verificación del origen empaquetado.

## Dictamen y alcance

El catálogo permite un lanzamiento gradual, pero necesita una segunda edición antes de una campaña amplia centrada en aprendizaje de español. Las prioridades son justicia de las respuestas, coherencia de dificultad y renovación del contenido. Esta revisión no modifica ni publica retos.

Se revisaron los 30 retos del catálogo web (40 grupos de conexiones y 48 preguntas), sus pistas, explicaciones, respuestas alternativas y el especial Chile entre líneas con sus cuatro preguntas finales. El catálogo público descargado coincide por SHA-256 con `Web/public/content/puzzles.json`: `b2180a9870d108396aea9198972bc45c1d67f6047aecf287922744f468098261`. También se inspeccionaron catálogo incluido, repositorio, selector diario, validadores y motor.

Hay 10 conexiones, 4 puentes, 4 trivias, 4 de piezas, 3 intrusos, 3 visuales y 2 de respuesta escrita. Las valoraciones de dificultad son hipótesis editoriales para un adulto hispanohablante; no son resultados de pruebas con una muestra de jugadores ni niveles certificados de español.

Prioridades: P1 corregir antes de promoción amplia; P2 siguiente edición; P3 mejora de presentación. Las propuestas siguientes requieren validación editorial y prueba ciega antes de publicarse.

## 1. Ambigüedades y respuestas alternativas

### P1 — Conexiones con alternativas defendibles

- **Reto 20, Medidas relativas.** Las categorías dependen de anteponer MEDIA, ALTA, BAJO y GRAN. PRESIÓN aparece en BAJO, pero «alta presión» también es una expresión corriente. «Media presión» o «bajo mar» no tienen el mismo carácter de expresión fijada; no todo sintagma imaginable cuenta como alternativa. Revisar la matriz de cruces y sustituir palabras puente accidentales. «Gran final» no es por sí mismo una falta: final puede ser masculino al referirse al desenlace; en una competición se usa «la gran final». La categoría debe explicar qué sentido busca.
- **Reto 24, Expresiones de enlace.** DUDA, TIEMPO, SUERTE y FIN permiten «sin duda», «sin tiempo», «sin suerte» y «sin fin», pero el motor marca esa selección como error porque no coincide con un grupo declarado. Es una alternativa razonable ante una consigna genérica de relación, aunque no se ha demostrado aquí otra partición completa de las 16 palabras. Decisión recomendada: reconstruir el tablero con locuciones fijas y una consigna precisa; comprobar cruces con todas las preposiciones usadas. No basta con declarar que solo vale la solución del autor.
- **Reto 27, Palabras con nombre propio.** ROSA, VIOLETA, LUNA y SOL también forman un grupo defendible de nombres asociados a naturaleza. Los grupos «colores», «cielo y naturaleza» y «asociación religiosa» se solapan semánticamente. Rehacer con reglas más delimitadas o trasladar el objetivo a preguntas con contexto. No tratar una asociación cultural discutible como deducción única.
- **Semanal Chile.** «Sabores con apellido» es una etiqueta evocadora, pero demasiado amplia para justificar por sí sola una única selección. Hay otras palabras comestibles en el tablero. Es un señuelo legítimo si la relación final se delimita con ejemplos completos. La categoría «Animales que describen personas» depende de polisemia y registro; debe explicitar el sentido local y evitar que parezca que «loco» solo significa un animal. No lo marco como error léxico: existe el molusco llamado loco. Mantener el semanal vigente intacto durante la competición y aplicar mejoras al siguiente evento.

El motor de conexiones solo acepta los cuatro conjuntos declarados. No soporta grupos alternativos ni evalúa relaciones en lenguaje natural. Por tanto, las ambigüedades deben resolverse editorialmente; aceptar automáticamente cualquier combinación «que suene bien» no es una solución implementable ni verificable.

### P1/P2 — Preguntas y correcciones precisas

- **Reto 9:** «___ del río» no contextualiza bien banco. Propuesta: «Un ___ de arena · un ___ para sentarse» con Banco, Sillón, Cauce y Puente. La explicación distingue depósito de arena y asiento.
- **Reto 25:** «titular de una noticia · titular de un periódico» repite prácticamente el mismo sentido periodístico. Propuesta: «Jugador ___ · ___ de una cuenta», con Titular como respuesta. La explicación debe distinguir integrante habitual del equipo y persona a cuyo nombre está la cuenta. Es una propuesta, no un reto ya ensayado.
- **Reto 8:** no eliminar «bebedero» por parecer extraño: el DLE registra el adjetivo para un líquido bueno de beber. La explicación actual no menciona todas las alternativas que el juego acepta. Añadir una explicación breve de esa acepción o acotar la consigna a la forma más habitual sin penalizar variantes válidas.
- **Reto 22:** las consignas muy abiertas admiten más variantes que la lista finita. «Si un texto se puede leer» no es exactamente lo mismo que que sea comprensible, claro o inteligible. Separar visibilidad de las letras y comprensión del contenido. Propuesta: «Las letras se distinguen y permiten leer el texto: el texto es…». Mantener variantes genuinas y hacer que las pistas describan el significado antes de dar iniciales.
- **Reto 6:** explicación «para y aguas» frente a piezas PARA + GUAS. La concatenación funciona, pero esas piezas son segmentos de juego, no el análisis morfológico de la palabra. Explicitarlo. Mejor usar PARA + AGUAS y una regla real de unión, o escoger un compuesto que encaje sin ajuste en el motor actual.
- **Reto 21:** CIEM + PIÉS es una segmentación de juego; explicar la grafía de ciempiés, sin enseñar que CIEM sea una palabra autónoma. No cambiar el motor de piezas a mitad de la edición.

### Fuentes y límites de verificación

- DLE, bebedero: https://dle.rae.es/bebedero — confirma la acepción adjetiva, además de las nominales.
- DLE, titular: https://dle.rae.es/titular — distingue los sentidos de jugador habitual, titular de un documento/cuenta y encabezamiento periodístico.
- DLE, final: https://dle.rae.es/final — distingue desenlace masculino y competición femenina.

Se resumieron acepciones; no se reprodujeron artículos completos. Los señalamientos restantes proceden del contenido y del contrato del motor; no se atribuye certificación de la RAE a todo el catálogo.

## 2. Progresión de dificultad: revisión de los 30 retos

Dificultad editorial provisional: inicial = reconocimiento directo; media = contexto o polisemia; alta = combinar condiciones o discriminar alternativas cercanas. «Rehacer» implica conservar una copia histórica, no cambiar a ciegas el ID publicado.

| N.º | Reto | Etiqueta actual | Estimación editorial y decisión |
|---|---|---|---|
| 1 | Primeras conexiones | Fácil | Inicial. Buen tutorial. Mantener; explicar por qué LIMA pertenece a capitales y no al grupo de cítricos previsto. |
| 2 | Una palabra, dos caminos | Fácil | Inicial/media. Buen segundo formato. Mantener y mejorar ejemplos de uso. |
| 3 | Curiosidades cotidianas | Fácil | Inicial. Mantener como introducción; Tierra y termómetro son muy directos. |
| 4 | Sonidos y movimiento | Fácil | Inicial. Categorías muy separadas. Mantener, con menos redundancia musical respecto al 1. |
| 5 | Uno no encaja | Fácil | Inicial. La consigna revela la regla y solo exige reconocer el intruso. Adecuado al inicio. |
| 6 | Piezas que forman palabras | Media | Inicial. Revisar segmentación de paraguas; no justifica un salto de dificultad. |
| 7 | Vuelo y oficio | Media | Inicial. Aves, países, herramientas y días siguen siendo categorías directas. Simplificar etiqueta o enriquecer. |
| 8 | No te confíes: los sentidos | Media | Media. Buen potencial de vocabulario; acotar variantes y explicar todas las aceptadas. |
| 9 | Conexiones escondidas | Media | Inicial/media. Mejorar banco del río; la segunda pregunta es directa. |
| 10 | El engaño de Mercurio | Media | Media. Buen señuelo metal/planeta. Mantener y explicar el contexto decisivo. |
| 11 | Observa la costa | Media alta | Inicial/media. Observación de pilotes y madera. Una imagen adecuada basta; revisar legibilidad móvil. |
| 12 | Busca la regla | Media alta | Media. Agudas aporta contenido lingüístico; litro es mucho más fácil. Igualar exigencia. |
| 13 | Mitos y constelaciones | Media alta | Inicial/media. Repite exactamente los planetas del 10 y no contiene una categoría de constelaciones. Cambiar título y grupo repetido. |
| 14 | Construye con sentido | Media alta | Inicial. Rompecabezas y girasol son evidentes con esas piezas. Mover objetivo al inicio o sustituir. |
| 15 | Palabras y relatos | Media alta | Inicial/media. Protagonista, abundante y ¿ tienen exigencias distintas. Sustituir la última por interpretación contextual. |
| 16 | Una palabra cambia todo | Difícil | Inicial/media. Punta y dorso no requieren varios pasos. Añadir contexto y distractores cercanos. |
| 17 | Del cuerpo al paisaje | Difícil | Media. La polisemia de boca/brazo/cuello/pie es buena dirección; las otras categorías son directas. |
| 18 | Lee el paisaje | Difícil | Inicial. Repite la imagen del 11 y pide observaciones obvias. Cambiar imagen y objetivo. |
| 19 | Ciencia en pocas palabras | Difícil | Inicial/media. Corazón y gravedad no sostienen la etiqueta para adultos. Reubicar objetivo o sustituir por comprensión de un pequeño texto científico. |
| 20 | Medidas relativas | Difícil | Media/alta después de resolver cruces. Rehacer reglas, no aumentar dificultad con ambigüedad. |
| 21 | Compuestos ingeniosos | Muy difícil | Inicial/media. Abrelatas es directo; ciempiés evalúa ortografía. Añadir contraste y contexto. |
| 22 | No te confíes: cambia la raíz | Muy difícil | Media. Puede ser exigente para estudiantes, pero no por eso «muy difícil» para todos. Revisar justicia de corrección abierta. |
| 23 | La excepción precisa | Muy difícil | Inicial/media. Primos añade matemáticas; canto/infinitivos es reconocimiento básico. Sustituir por una excepción lingüística contextual. |
| 24 | Expresiones de enlace | Muy difícil | No calibrable con justicia hasta eliminar alternativas. Rehacer antes de promocionarlo como reto experto. |
| 25 | Puentes de significado | Muy difícil | Inicial/media. Nota tiene dos sentidos claros; titular necesita otro sentido genuino. Reescribir. |
| 26 | Reconoce Chiloé | Máxima | Conocimiento regional, no máxima complejidad. Tercera aparición de la misma imagen. Mover el enfoque al contenido cultural y renovar el visual. |
| 27 | Palabras con nombre propio | Máxima | No calibrable por solapamiento de categorías. Rehacer. |
| 28 | Últimas piezas | Máxima | Inicial/media. Vaivén aporta ortografía; mediodía es muy directo. Rehacer como cierre con varios contrastes. |
| 29 | El desafío del saber | Máxima | Media. Personificación e hiato son útiles; la respuesta correcta del rectángulo explica mucho más que los distractores. Equilibrarlos. |
| 30 | La conexión final | Máxima | Media/alta. Buena dirección con expresiones, pero necesita auditoría de cruces y cierre con explicación de todos los grupos. |

No recomiendo permutar de inmediato los IDs para «ordenar mejor»: están ligados al avance y mejores resultados de jugadores. Preparar una edición nueva y una política de migración. A corto plazo se pueden corregir textos y etiquetas de forma controlada; no confundir un reto corregido sustancialmente con el mismo desempeño histórico.

### Criterio para la siguiente edición

- Retos 1–5: tutorial y reconocimiento; 6–10: polisemia cotidiana; 11–15: contexto y vocabulario; 16–20: expresiones y contrastes; 21–25: deducción con varias condiciones; 26–30: síntesis, sin trivia oscura como sustituto de dificultad.
- Rangos de progresión no equivalen a niveles MCER ni a dominio del idioma.
- Prueba ciega con hispanohablantes de más de un país y estudiantes que ya entiendan español. Registrar finalización sin pistas, errores, tiempo activo y reclamaciones de ambigüedad por formato. Los resultados servirán para calibrar; no fijar dificultad solo con la opinión del autor.
- Corregir primero injusticias; no conservar una pregunta ambigua solo porque produce muchos errores.

## 3. Expresiones y polisemia: dirección editorial

Los puentes 2, 9, 16 y 25, el cuerpo aplicado al paisaje en 17 y las expresiones de 20/24/30 son la identidad más aprovechable. Darles contexto y explicaciones aumenta el valor frente a preguntas enciclopédicas sueltas.

Propuesta de equilibrio del nuevo contenido: alrededor de la mitad dedicada a vocabulario, polisemia y expresiones; el resto a deducción, comprensión y cultura. Es una pauta de diseño, no una cuota aplicada al catálogo actual.

Ejemplos de preguntas candidatas:

1. «Al salir, Ana echó una mano con las cajas. ¿Qué hizo?» → Ayudó. Contrastar con señalar, despedirse y contar; explicar el sentido figurado.
2. «Luis estaba en las nubes durante la reunión. ¿Qué le ocurría?» → Estaba distraído. Evitar presentar la frase como ubicación literal.
3. «La vela se apagó · El barco desplegó la vela»: identificar qué cambia de significado en una misma palabra. Usar dos escenas claras.
4. «La planta del pie · Una planta del jardín»: distinguir sentidos con una frase breve en cada caso.
5. «En Chile, te dicen “vuelvo al tiro”. ¿Qué quieren decir?» → De inmediato. Etiquetar país, uso coloquial y contexto; no suponer universalidad.
6. Comparar un uso literal y uno figurado de «romper el hielo» con frases completas. Evitar que dos opciones expresen aproximadamente la misma intención social.

Cada ficha editorial debe incluir objetivo, país/registro si corresponde, respuesta, alternativas válidas, razón de descarte de cada distractor, pista parcial, explicación y fuente cuando la afirmación sea regional o discutible. Estos ejemplos son candidatos pendientes de revisión ciega, no contenido aprobado.

Faltan explicaciones en los 40 grupos de conexiones del camino; los grupos del semanal sí las tienen. La pantalla GamePage ya enlaza Explanation para cada grupo resuelto; añadir explicaciones al catálogo aprovecharía esa presentación existente, pendiente de comprobar legibilidad en móvil.

Los 3 retos visuales y sus 6 preguntas reutilizan `weekly_palafitos_castro.png`. Diversificar con escenas útiles para el lenguaje: un mercado, señales de una estación, objetos de una cocina. Si la pregunta exige observar, la respuesta debe ser legible en un teléfono y no depender de reconocer un lugar no identificable en la imagen. Revisar origen y licencia de cada imagen.

## 4. Diario independiente y reserva de semanales

### Estado actual y riesgos

- `DailyPuzzleSchedule` usa el día UTC módulo número de puzzles; el diario y el camino comparten catálogo. Con 30 entradas, repite cada 30 días. Añadir un puzzle cambia la selección futura; no existe un calendario editorial independiente.
- El diario cambia según la fecha UTC, no a medianoche chilena. La política horaria debe ser explícita y coherente entre app, enlaces y comunicación.
- Solo hay un semanal publicado, del 19 al 26/10/2026. No hay un siguiente evento en el archivo revisado.
- El catálogo incluido en la app contiene 30 conexiones, mientras el remoto contiene siete formatos. Sin red y sin caché, una misma ID puede representar otro reto. Las pruebas actuales cargan el remoto, no certifican equivalencia con el incluido. P1: sincronizar la edición incluida y la remota, y comprobarlo antes de distribuir otra compilación.

### Diseño recomendado, pendiente de implementación

1. Separar catálogo de camino, calendario diario y calendario semanal.
2. Diario: IDs propios (`daily-2026-…`), fecha explícita, contenido versionado y publicado por adelantado; caché y un conjunto de respaldo incluido. No usar un índice módulo del camino.
3. El validador actual exige IDs `puzzle-NN` y al menos 30 retos: no sirve directamente para un catálogo diario independiente. Crear su contrato y pruebas, y adaptar repositorio, enlaces y modo diario. Exige una nueva versión móvil; subir un `daily.json` por sí solo no activa la función en la 6.
4. Mantener inmutable el contenido de una competición una vez abierta. Corregir algo que altera dificultad implica decidir cómo tratar resultados, no solo aumentar `contentVersion`.
5. Semanales futuros: IDs de evento, puzzle y badge únicos; ventanas sin solapamiento. El enlace `/semanal` debe seguir abriendo el vigente. Publicar contenido antes de enviar su push.
6. No reescribir el progreso local ni reordenar IDs existentes para aplicar esta revisión. Documentar migración de la siguiente edición y probar actualización con datos conservados.

### Reserva propuesta de 14 diarios

Borrador de programación relativo al inicio de la nueva edición; sin fechas de producción ni publicación. No es aún un lote de JSON jugable.

| Día | Formato | Objetivo |
|---|---|---|
| 1 | Puente | Dos sentidos de planta y vela, con contexto |
| 2 | Trivia | Comprender echar una mano y estar en las nubes |
| 3 | Intruso | Palabra que no encaja en una frase concreta |
| 4 | Conexiones | Objetos de cocina, con categorías delimitadas |
| 5 | Visual | Leer un cartel de estación con imagen nueva |
| 6 | Piezas | Compuestos transparentes sin ajustes de unión |
| 7 | Trivia | Sentido literal frente a figurado |
| 8 | Puente | Polisemia cotidiana con escenas nuevas |
| 9 | Conexiones | Expresiones revisadas para evitar cruces |
| 10 | Visual | Vocabulario de un mercado, imagen nueva |
| 11 | Intruso | Registro formal e informal en contexto |
| 12 | Trivia | Regionalismo etiquetado con país y ejemplo |
| 13 | Recall | Recuperación léxica con consigna acotada y variantes |
| 14 | Mixto por temática | Síntesis de vocabulario nuevo, usando un formato soportado; no inventar un modo técnico nuevo sin implementarlo |

### Reserva propuesta de cuatro semanales

Ventanas consecutivas propuestas a medianoche de Santiago (UTC−3 en estas fechas), no activadas. La disponibilidad real dependerá de completar y aprobar cada lote.

| Ventana local | Tema | Objetivo y condiciones de preparación |
|---|---|---|
| 26/10–02/11 | Palabras con dos vidas | Polisemia cotidiana; cuatro grupos sin solapamientos y cuatro preguntas contextuales. Evitar repetir los ejemplos del camino. |
| 02/11–09/11 | Expresiones en contexto | Locuciones de uso amplio; cuatro grupos auditados y cuatro escenas que distingan sentido literal/figurado. |
| 09/11–16/11 | Viaje por el español | Diferencias regionales con países explícitos; fuentes de cada acepción y alternativas que no penalicen otros dialectos. |
| 16/11–23/11 | Lo que dicen las palabras | Compuestos, familias y cambios de sentido; alternar deducción con comprensión y un visual nuevo con licencia revisada. |

Para cada semanal faltan redactar el tablero definitivo de 16 palabras, las preguntas y distractores, explicaciones, pistas, badge, textos de compartir y push. No se consideran listos por tener título y fecha. Si el primero no supera revisión a tiempo, preparar un especial sencillo y justo antes que publicar uno ambiguo para cumplir calendario.

## Orden de trabajo recomendado

1. P1: rehacer 20/24/27 y corregir 9/25; revisar variantes abiertas de 8/22. Preparar cambios en borrador, sin sustituir una competición activa.
2. P1: eliminar discrepancia del catálogo incluido/remoto en la siguiente compilación. Probar primera apertura sin red y actualización conservando progreso.
3. P2: editar todos los retos de la tabla, incorporar explicaciones a conexiones y calibrar con jugadores. Mantener trazabilidad entre edición y resultados.
4. P2: completar y aprobar cuatro semanales y catorce diarios; solo entonces anunciar una cadencia de contenido nuevo.
5. Implementar diario independiente y migración en una versión móvil posterior. No bloquear el camino actual por la ausencia de nuevos servicios.

## Verificación y límites

Se ejecutaron las pruebas PuzzleCatalogTests y WeeklyChallengeTests: 13 aprobadas, cero fallidas. Las pruebas estructurales del catálogo comprueban formato y que las respuestas declaradas ganen la partida; no demuestran unicidad semántica, interés ni dificultad. Esta revisión no ha publicado cambios, alterado tablas de ranking ni enviado notificaciones. El dictamen es editorial; no sustituye pruebas con jugadores ni una revisión lexicográfica completa.
