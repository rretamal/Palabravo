# Edición de retos para la compilación 7

## Resultado de la revisión

Se han implementado cambios sobre los cuatro ejes del informe del 20/10/2026. La calibración de dificultad sigue siendo editorial: no se presenta como resultado de un estudio con jugadores.

- **Justicia:** reemplazados los tableros 20/24/27 y la conexión final, y precisadas las preguntas 9/25. Las respuestas escritas 8/22 ahora indican una familia o un inicio concreto. Sus explicaciones reconocen los sinónimos que no completan esa forma, en lugar de declararlos incorrectos en general. Se toleran tildes omitidas al responder y se muestran las grafías correctas.
- **Progresión:** conservados los cinco primeros retos como introducción. Reescritos los tramos posteriores con interpretación de contexto, condiciones simultáneas, función gramatical y relaciones entre forma y sentido. Las etiquetas se ajustaron a la exigencia estimada; ya no se presenta una pregunta básica como Máxima por estar al final. Los rangos del jugador siguen reflejando avance, no un nivel certificado de español.
- **Identidad lingüística:** explicaciones para los 40 grupos del camino; preguntas de polisemia, expresiones, conectores y comprensión. Tres paneles originales sustituyen las repeticiones de Castro. Se empaquetan con la app, se muestran sin recortar y tienen descripción accesible con la información que ve un jugador. Las fichas de palabras permiten hasta tres líneas.
- **Renovación:** 14 diarios completos, con IDs propios y fechas UTC, del 21/10 al 03/11/2026. Después se ofrece contenido identificado como Del archivo. Cuatro semanales completos del 26/10 al 23/11, con tableros, cuatro preguntas finales, explicaciones, pistas, insignia y textos para compartir.

## Compatibilidad y conservación del progreso

El archivo antiguo `Resources/Raw/puzzles.json` estaba excluido del paquete: el informe inicial dedujo incorrectamente una divergencia activa a partir de ese archivo. Se eliminó el archivo obsoleto. El proyecto empaqueta directamente el mismo JSON remoto de la edición nueva, y una prueba comprueba el origen.

El endpoint `/content/puzzles.json` se conserva íntegro para la versión 6. La versión 7 usa `/content/v2/puzzles.json` y una caché distinta. No se alteran IDs ni órdenes del camino, ni se borran medallas, desbloqueos, rachas o puntajes. Las medallas previas conservan su crédito y se identifican como Medalla anterior. Cada nuevo resultado registra la versión de contenido; si mejora una medalla, esa mejor marca queda atribuida a la nueva edición. El ranking del camino conserva su criterio de mejores medallas, sin inventar una tabla de tiempos comparable entre ediciones.

Antes del 21/10 la versión 7 conserva el diario de la edición anterior. Desde el 21 usa el calendario independiente incluido o descargado. Las entradas ya incluidas en un calendario son inmutables, incluso si su fecha aún no llegó; el servidor puede añadir fechas sin reescribirlas. Un calendario inválido o inaccesible deja disponible el último válido o el incluido. Si falta una fecha posterior al inicio, solo se seleccionan ejercicios de fechas pasadas y se marca Del archivo: nunca se adelanta contenido futuro. La primera selección de cada fecha se guarda localmente para que una recuperación de red no cambie a mitad del día el ejercicio que se empezó sin conexión. Una caché semanal antigua tampoco puede ocultar eventos futuros incluidos con la actualización.

La versión 6 seguirá usando su ciclo diario anterior hasta actualizarse. Por ello la web y la pantalla diaria no prometen que clientes de ediciones diferentes resuelvan necesariamente el mismo ejercicio. No se ha añadido un ranking diario entre ediciones.

El semanal Chile activo se conserva exactamente. Hay una prueba con su copia congelada. Los cuatro eventos futuros tienen IDs de evento, puzzle e insignia distintos y ventanas consecutivas, sin solapamientos:

| Evento | Ventana de Santiago | UTC |
|---|---|---|
| Sentidos de cada día | 26/10–02/11 | 03:00 Z a 03:00 Z |
| Más allá de lo literal | 02/11–09/11 | 03:00 Z a 03:00 Z |
| Un idioma, muchas voces | 09/11–16/11 | 03:00 Z a 03:00 Z |
| La forma da pistas | 16/11–23/11 | 03:00 Z a 03:00 Z |

No se enviaron nuevas pushes. Publicar fechas futuras no significa que ya estén activas: las selecciona la ventana de cada evento. Se mantienen compatibles con la estructura de la versión 6; no requieren los nuevos paneles de la versión 7.

## Fuentes lingüísticas y gráficos

Se verificaron acepciones puntuales en fuentes primarias; los textos del juego son explicaciones propias, no artículos copiados:

- https://dle.rae.es/bebedero — acepción adjetiva admitida.
- https://dle.rae.es/titular — jugador habitual, persona a cuyo nombre está una cuenta, encabezamiento.
- https://dle.rae.es/guagua — transporte en distintas regiones y bebé en Chile, entre otros países.
- https://dle.rae.es/pasta — acepciones alimentarias y económicas.
- https://dle.rae.es/lana y https://dle.rae.es/guita — usos coloquiales económicos.
- https://dle.rae.es/chaval, https://dle.rae.es/chamaco, https://dle.rae.es/pibe y https://dle.rae.es/gur%C3%AD — usos para niño o joven, con sus marcas regionales.
- https://www.asale.org/damer/cabrita, https://www.asale.org/damer/pochoclo y https://www.asale.org/damer/cotufa — maíz reventado en Chile, Argentina y Venezuela. Los usos no se presentan como exclusivos ni universales.
- https://cvc.cervantes.es/LENGUA/anuario/anuario_06-07/pdf/presente_09.pdf — variación del léxico para autobús.

Los paneles de mercado, estación y recorrido son diagramas originales producidos por `Web/scripts/render-learning-panels.py`. No contienen fotografías de terceros. El script permite configurar la fuente mediante PALABRAVO_PANEL_FONT; se conservan tanto los PNG web como los del paquete móvil.

## Comprobaciones y publicación

Las pruebas comprueban resolución de todos los retos, calendario, límites temporales, archivo sin anticipos, rechazo de revisiones del diario, identidades, preservación de medallas, equivalencia de origen empaquetado y recursos visuales. La compilación web ejecuta también `validate-content.mjs` para impedir publicar contenido incompleto.

La aplicación registra, solo con el consentimiento de estadísticas ya existente, inicio/final de retos con formato, edición, errores, pistas y tiempo. No registra respuestas escritas ni texto del usuario. Esto permite calibrar con uso real; no sustituye una prueba ciega editorial con personas de distintas variedades del español.

Se necesita distribuir la compilación 7 y comprobarla en el teléfono, incluyendo actualización SIN desinstalar ni borrar datos. La compilación local no sustituye ese smoke. La firma de carga de Play sigue siendo la del proceso habitual del propietario; no se instaló un paquete con firma distinta ni se desinstaló la app.

### Resultado de validación

- .NET: 108 pruebas aprobadas; repetición específica de 22 pruebas de catálogo después de la revisión final del tutorial, también aprobada.
- Web: compilación correcta y 14 pruebas aprobadas. API: 51 pruebas aprobadas.
- Android Release AAB: compilación correcta, cero errores; 34 advertencias conocidas de callbacks dinámicos de AdMob.
- Se abrió el AAB y se compararon por hash sus assets puzzles.json, daily.json y weekly.json con los archivos fuente: coincidencia exacta en los tres. Se comprobaron también los recursos de los tres paneles.
- La validación automatizada prueba estructura, invariantes y resultados declarados; no acredita una dificultad empírica ni la ausencia absoluta de interpretaciones alternativas en todas las variedades del español.
