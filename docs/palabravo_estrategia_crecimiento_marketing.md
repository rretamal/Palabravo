# Palabravo: crecimiento, marketing, contenido y medición desde el día 1

Fecha: 6 de septiembre de 2026. Estado: app aún no publicada, con un camino de 30 retos estáticos. Complemento: `palabravo_estrategia_ads.md`.

## 1. Objetivo y posicionamiento

Lanzar el camino existente, comprobar que los jugadores entienden y disfrutan agrupar 16 palabras en 4 conexiones, y crecer con nuevos contenidos elegidos por evidencia. Promesa inicial: «16 palabras, 4 conexiones. ¿Puedes encontrarlas?».

Audiencia inicial a explorar: adultos hispanohablantes interesados en acertijos, palabras y trivia. El español, los dobles sentidos y las colecciones culturales son hipótesis de diferenciación. No asumir viralidad ni necesidad de ampliar el alcance antes del estreno.

Métrica principal de producto: jugadores semanales que completan al menos un reto en dos días distintos. Acompañarla de activación, retención, calidad de contenido y beneficio, para no confundir actividad con un negocio sostenible.

## 2. Evolución de contenido

| Etapa | Entrega | Decisión siguiente |
| --- | --- | --- |
| Antes de publicar | 30 retos revisados, JSON, IDs estables, caché y telemetría. | Corregir dificultades y pérdida de progreso. |
| Lanzamiento | Un solo camino visible; experiencia sin registro obligatorio. | Detectar abandono y velocidad de avance. |
| Primera novedad | Una expedición de 5–8 retos del país que mejor pueda revisarse. | Medir descubrimiento, inicio, finalización y referencias. |
| Siguiente lote | 10 retos más del principal o segunda expedición. | Elegir según demanda y capacidad editorial. |
| Recurrencia | Piloto semanal; diario solo con reserva y producción sostenible. | Medir retorno y costo por jugador recurrente. |

Los nuevos caminos son independientes del principal y guardan su propio progreso. Cada país está disponible sin terminar los 30; dentro puede haber desbloqueo progresivo. No mostrar caminos vacíos. Argentina, España y Venezuela son candidatos, no tres entregas obligatorias para el lanzamiento.

Extender el principal si los jugadores activos se acercan al final. Corregir onboarding y dificultad si abandonan al comienzo. Ampliar expediciones si atraen jugadores que completan y comparten. Probar recurrencia si terminan pero no vuelven.

Etiquetar «Palabras de Argentina» cuando se pruebe vocabulario y «¿Cuánto sabes de Argentina?» cuando se pruebe conocimiento cultural. Añadir explicación de cada conexión. Una palabra no necesita ser exclusiva de un país: reconocer usos compartidos y evitar respuestas ambiguas.

## 3. Distribución mediante JSON

Propuesta: catálogo remoto estático servido por HTTPS, packs versionados y los 30 originales incluidos en la app. No hace falta inicialmente un CMS ni una API editorial propia.

- Catálogo: `schema_version`, `catalog_version`, `min_app_version`, colecciones, URLs, hashes, revisiones y disponibilidad.
- Colección: `collection_id`, título, descripción, `locale`, `theme_country`, dificultad, imagen y lista ordenada de puzzles.
- Puzzle: `puzzle_id`, `puzzle_version`, cuatro grupos con cuatro palabras y explicación; el cliente mezcla las 16 palabras.
- Calendario futuro: fecha de publicación UTC vinculada al puzzle; el enlace compartido conserva el ID aunque cambie el día.
- Progreso: ID estable y revisión del puzzle, separado del contenido. Guardar primera finalización y mejores resultados, sin reiniciar al actualizar.

Descargar y validar el pack completo antes de sustituir la copia activa; conservar la última válida. No sustituir el tablero a mitad de un intento. Respetar formatos soportados, rangos y compatibilidad; contenido de una mecánica nueva requiere versión de app. Si una corrección cambia sustancialmente la solución, usar nueva revisión, conservar logros previos y separar métricas. La telemetría captura versión de contenido además de versión del binario.

La copia local incluye soluciones: es adecuada para un juego casual, no una defensa antitrampas para premios o competición de alto valor.

Flujo editorial: propuesta → validación estructural → revisión de ambigüedades → revisión regional → prueba de dificultad → publicación → seguimiento. IA puede proponer; no publicar automáticamente. Registrar horas de producción/revisión para conocer el costo real de cada lote.

## 4. Estrategia de adquisición y recomendación

### Lanzamiento acotado

Probar primero con 10 grupos de 3–5 personas que ya se conozcan. Observar si entienden, terminan y se retan sin recordatorios del creador. Pedir feedback sobre conexiones injustas. Esto es investigación cualitativa, no una muestra suficiente para demostrar retención o viralidad.

Preparar ficha de tienda con tablero legible, explicación corta, capturas reales y promesa coherente. Medir visitantes e instalaciones con los reportes de la tienda, separados de primeras aperturas: la app no observa a quienes instalan y nunca abren.

### Contenido para redes

Durante cuatro semanas, probar 4–5 piezas semanales reutilizadas en Reels, TikTok y Shorts. Ajustar la frecuencia al tiempo disponible.

| Formato | Contenido | Resultado a medir |
| --- | --- | --- |
| Miniacertijo | Encontrar cuatro palabras relacionadas. | Visitas que completan un reto. |
| Falso amigo | Una conexión tentadora pero incorrecta y su explicación. | Activación y retención de los atraídos. |
| Duelo | Dos personas resuelven el mismo tablero. | Aperturas de enlaces compartidos. |
| Expedición regional | Una muestra de vocabulario o cultura del pack. | Inicio y finalización de la colección. |

Dar valor dentro del video; enlazar otro reto relacionado. No inventar porcentajes de dificultad o éxito. Probar colaboraciones pequeñas con creadores de acertijos, lengua y trivia, usando un enlace/código por pieza. Registrar gasto y tiempo de producción.

### Recorrido de recomendación

Resultado → «Reta a alguien» → tarjeta sin respuestas y enlace al mismo puzzle → amigo juega → compara → comparte.

La tarjeta muestra ID del reto, resultado, errores y uso de pistas. No requiere acceso a contactos. Comparar intentos asistidos de forma transparente.

En lanzamiento, el enlace puede abrir la app instalada o una página con contexto y acceso a la tienda. Una versión web jugable del mismo reto es una ampliación posterior prioritaria si se observa pérdida por instalación, no un requisito para estrenar. Preparar desde el principio IDs y enlaces estables.

Un enlace normal no garantiza recuperar el puzzle después de instalar. Planificar esa continuidad explícitamente; ofrecer un código corto de reto como respaldo. Medir aperturas directas e instalaciones atribuidas por separado, sin atribución probabilística inventada.

## 5. Arquitectura de medición desde el día 1

**Se pueden crear nuevas métricas sobre eventos existentes sin actualizar la app. No se puede recuperar un comportamiento que nunca se instrumentó.** Configuración remota modifica reglas y eventos ya previstos; sensores, callbacks o funciones nuevas pueden exigir otro binario.

Propuesta inicial de herramientas: Firebase Analytics para producto, exportación de eventos a BigQuery para análisis, Remote Config para parámetros existentes y un servicio de errores compatible con el framework real. AdMob y reportes de compras aportan ingresos. Verificar soporte del SDK antes de elegir bindings; no se presupone una integración MAUI concreta.

Crear una interfaz interna de telemetría con contratos tipados y un adaptador de proveedor. Centralizar navegación, selección de contenido, ciclo de intento, pistas, anuncios, compras y compartir. El SDK gestiona su transporte offline; si se usa un colector propio, implementar cola acotada, TTL, reintentos con backoff y deduplicación. Un fallo de analítica nunca debe bloquear jugar.

Registrar parámetros necesarios desde el estreno y configurar dimensiones de baja cardinalidad en Analytics; mantener IDs de intentos/eventos en datos detallados, evitando convertirlos todos en dimensiones de informes. Firebase admite eventos personalizados y sus parámetros: [documentación](https://firebase.google.com/docs/analytics/android/events). Los parámetros personalizados pueden consultarse en la exportación a BigQuery: [referencia](https://firebase.google.com/docs/analytics/ios/events). Habilitar exportación al lanzar; no contar con reconstruir meses anteriores. Definir presupuesto, retención y controles de consultas.

### Sobre común de evento

| Campo lógico | Propósito |
| --- | --- |
| `event_name`, `event_schema_version` | Contrato estable, independiente de versiones del contenido. |
| `event_id` | UUID por ocurrencia; deduplicación en ingestión/consultas, no garantía automática de GA4. |
| `occurred_at_utc`, `received_at_utc` | Momento del hecho y recepción; el segundo lo añade el sistema receptor. |
| `anonymous_id`, `session_id` | Identidad seudónima y sesión coherente; preferir identidad del SDK cuando aplique. |
| `app_version`, `build`, `platform`, `environment` | Separar versiones, dispositivos y pruebas. |
| `config_version`, `catalog_version` | Reconstruir reglas y catálogo efectivos. |
| `collection_id`, `puzzle_id`, `puzzle_version`, `attempt_id` | Contexto de contenido cuando corresponda. |
| `mode`, `locale`, `theme_country` | Principal, expedición o diario; variante lingüística y temática. |
| `experiment_id`, `variant` | Solo si existe exposición relevante. |

Es un modelo lógico, no una orden de enviar todos los campos como parámetros personalizados en cada llamada: aprovechar campos nativos, derivar metadatos en el almacén y respetar límites vigentes del proveedor. Versionar una lista permitida de parámetros por evento. No usar nombres dinámicos como `puzzle_30_completed`: usar `puzzle_completed` más `puzzle_id`.

País de audiencia y país temático son dimensiones distintas. No inferir nacionalidad del jugador por elegir Venezuela. No recopilar nombres, contactos, emails, texto libre o tokens de compra en eventos. Respetar decisiones de consentimiento y no eludirlas con otra canalización. Documentar población observable; las tasas de analítica pueden excluir usuarios sin medición.

### Eventos base obligatorios

| Evento lógico | Disparador | Datos específicos |
| --- | --- | --- |
| `first_open` | Primera apertura conocida, preferiblemente evento nativo. | Origen atribuido cuando esté disponible. |
| `session_start` | Nueva sesión, coherente con la regla de 30 minutos. | Fuente de entrada. |
| `screen_view` | Pantalla realmente visible. | `screen_id`, `previous_screen_id` |
| `onboarding_step` | Paso mostrado o completado. | `step_id`, `action` |
| `collection_view` | Colección visible. | `collection_id`, `placement_id` |
| `content_select` | Selección de colección o puzzle. | `content_type`, `content_id`, `placement_id` |
| `puzzle_started` | Primer intento o nuevo reintento. | `attempt_id`, `start_reason`, `difficulty` |
| `puzzle_resumed` | Retomar intento existente. | Mismo `attempt_id` |
| `group_submitted` | Envío de una agrupación. | `submission_index`, `correct`, `groups_solved`, `error_count` |
| `hint_requested` | Solicitud de ayuda. | `hint_type`, `source` |
| `hint_granted` | Ayuda realmente entregada. | `hint_type`, `source`, `reward_id` si aplica |
| `puzzle_checkpoint` | Pausa, segundo plano o progreso significativo. | `active_ms`, `groups_solved`, `error_count`, `hints_used` |
| `puzzle_completed` | Resolución completa, una vez por intento. | `active_ms`, `error_count`, `hints_used`, `medal`, `is_first_completion` |
| `puzzle_exited` | Salida explícita detectada. | `reason`, resumen del intento |
| `share_opened` | Apertura del menú de compartir. | `share_id`, `puzzle_id`, `placement_id` |
| `share_result` | Resultado que realmente reporte el sistema. | `status`: `confirmed`, `cancelled` o `unknown` |
| `deep_link_opened` | Enlace recibido por app. | `link_id`, `campaign_id`, `puzzle_id`, `resolution_status` |
| `content_sync_result` | Resultado de actualizar catálogo/pack. | `status`, `error_code`, `latency_ms`, `content_version` |
| `config_applied` | Configuración validada y aplicada. | `config_version`, `source` |
| `experiment_exposure` | Variante efectiva en la experiencia. | `experiment_id`, `variant` |
| `app_error` | Error controlado relevante. | `error_code`, `component`, `recoverable` |

No registrar cada toque de palabra: el envío del grupo suele ser suficiente. No exigir `puzzle_exited` al cierre forzado; puede no ejecutarse. Inferir intento sin finalizar cuando no tenga finalización en 24 horas, marcándolo como observación provisional y recalculando si llega información offline o el jugador regresa.

Duración activa excluye anuncios y segundo plano. Reiniciar crea nuevo `attempt_id`; retomar conserva el anterior. Para evaluar dificultad usar también primeros intentos, evitando sesgo por repeticiones.

Eventos monetarios: ver el documento de ads. `hint_granted` es compartido y se emite una sola vez. Eventos de futuras funciones se implementan al existir la función: el catálogo de nombres no captura por sí solo acciones inexistentes.

## 6. Atribución de marketing

Usar códigos estables `campaign_id`, `creative_id`, `creator_id`, `link_id` y UTMs en páginas web. Conservar first-touch y fuente de la sesión por separado. Guardar gasto por campaña/fecha/moneda en una tabla externa; no hace falta emitirlo desde la app.

Eventos web cuando haya landing: `landing_view`, `store_click`; cuando exista demo: `web_puzzle_started`, `web_puzzle_completed`. Aperturas de bots/previsualizadores no cuentan como jugadores. Vincular web y app solo si hay un mecanismo explícito y fiable; no unir por IP ni asumir que un clic equivale a una instalación.

Abrir el menú de compartir no confirma envío ni lectura. Informar aperturas del menú, envíos confirmados disponibles y visitas reales del enlace como métricas distintas. Si no hay atribución, etiquetar `unknown` en lugar de inventar origen orgánico.

## 7. Tableros y definiciones

Fechas de informe UTC y cohortes por primera apertura observable. D1 y D7 usan días calendario UTC desde esa apertura; no mezclar con ventanas móviles de 24 horas. Incluir únicamente cohortes que hayan alcanzado el horizonte y permitir llegada tardía de datos. Definir activo como jugador con inicio, reanudación, envío de grupo o finalización, excluyendo procesos en segundo plano.

| Métrica | Cálculo / uso |
| --- | --- |
| Activación | Nuevos jugadores que completan su primer reto en 24 h / nuevos jugadores con ventana completa. |
| Retención D1/D7 | Miembros activos en el día 1/7 / miembros de la cohorte madura. |
| DAU y WAU | Activos distintos por día y últimos 7 días. |
| Progresión | Usuarios por mayor nivel completado del principal; expediciones aparte. |
| Finalización de puzzle | Primeros intentos completados en 24 h / primeros intentos iniciados con ventana madura. |
| Dificultad observada | Mediana y percentil 90 de duración, errores y pistas por versión, incluyendo tasa de no finalización. |
| Descubrimiento de expedición | Usuarios que la abren / usuarios a quienes se mostró su entrada. |
| Inicio de expedición | Usuarios que inician un puzzle del pack / usuarios que abren el pack. |
| Finalización de expedición | Jugadores que completan todos los retos de la revisión en 7 días / jugadores que la inician con ventana madura. |
| Propensión a compartir | Jugadores que abren compartir / jugadores que completan un reto, en el mismo período. |
| Activación referida | Invitados que completan un reto en 24 h / visitantes humanos únicos de enlaces medibles. |
| Referidos activados por jugador | Nuevos jugadores referidos activados / jugadores de la cohorte origen, a 7 días. Es una medida observada, no prueba de viralidad autosostenida. |
| Costo por activado | Gasto atribuible / nuevos jugadores atribuidos que completan su primer reto. |
| Costo por retenido D7 | Gasto atribuible / jugadores atribuidos activos en D7. |
| Costo editorial | Horas × tarifa interna + gastos del lote; comparar con jugadores que lo completan y vuelven. |

Dashboards: (1) adquisición y activación; (2) retención y contenido; (3) monetización del documento complementario; (4) salud técnica y calidad de datos. Separar versiones, plataforma, origen, país de audiencia, tema, modo y variante, mostrando tamaños de muestra.

El rendimiento de un camino opcional tiene sesgo de selección: no atribuir causalmente toda diferencia a su temática. Si hay tráfico suficiente, experimentar con exposición/promoción aleatoria. No escalar publicidad pagada hasta tener ingreso por adquirido observado y costos comparables; D30 no representa todavía el ingreso de toda la vida útil.

## 8. Qué cambiar sin publicar otra versión

| Cambio | Sin nuevo binario |
| --- | --- |
| Crear dashboard, embudo o cohorte con datos existentes | Sí. |
| Añadir pack/país con el esquema soportado | Sí, con descarga remota ya implementada. |
| Ajustar frecuencia de anuncios y mostrar una sección soportada | Sí, con parámetros integrados y versionados. |
| Analizar nuevos IDs de contenido | Sí: eventos genéricos más metadatos del catálogo. |
| Registrar un callback o comportamiento nunca instrumentado | No; requiere incorporar captura. |
| Añadir una mecánica, SDK o nuevo tipo de pantalla | Normalmente requiere versión. |
| Obtener datos históricos no recopilados | No. |

Configuración de medición: activar/desactivar eventos opcionales conocidos, nivel de diagnóstico y muestreo de eventos de alto volumen. No muestrear arbitrariamente compras, ingresos o eventos centrales del embudo; si se muestrea, registrar la tasa y no mezclar conteos brutos con completos. Un cambio de definición crea una nueva versión de métrica, no reinterpreta silenciosamente el historial.

## 9. Validación antes del lanzamiento

- [ ] Diccionario de eventos y responsable de mantenerlo definidos.
- [ ] Primera apertura, onboarding, resolver, fallar, pista, pausa, reanudar y repetir verificados en dispositivo.
- [ ] Compra y anuncios verificados según el documento complementario.
- [ ] IDs y versiones sobreviven a actualización de catálogo y reinicio.
- [ ] Enlace válido, inválido, puzzle retirado y app no instalada tienen resultados comprensibles.
- [ ] Pruebas y producción separadas; datos internos excluidos de negocio.
- [ ] Duplicados, colas offline y eventos tardíos revisados; telemetría no bloquea el juego.
- [ ] Exportación y un reporte de activación/retención funcionan antes de captar usuarios.
- [ ] Configuración tiene valores por defecto y reversión.
- [ ] Flujo real observado comparado con sus eventos en DebugView: [guía oficial](https://firebase.google.com/docs/analytics/debugview).

## 10. Rutina de decisión tras publicar

Primera semana: revisar errores, sincronización, recompensas y embudo inicial a diario; corregir antes de producir volumen de contenido. Semanalmente: analizar cohortes maduras, puzzles problemáticos, canales y costo editorial. Elegir un solo experimento prioritario con hipótesis, métrica principal, protección, población, horizonte y criterio de decisión escritos antes de empezar.

Primera entrega posterior: una expedición de 5–8 retos. Mantener una reserva pequeña y no prometer frecuencia diaria hasta poder sostenerla. El lanzamiento requiere medición transversal y un buen camino de 30 retos; web jugable, diario, más países y experimentos complejos se incorporan progresivamente.

Este documento describe trabajo por implementar. No se han configurado SDKs, dashboards, campañas ni automatizaciones en la app.
