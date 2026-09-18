# Palabravo: estrategia de anuncios y monetización

Fecha: 6 de septiembre de 2026. Estado: plan previo al lanzamiento; no hay datos reales de producción. Los precios, límites y experimentos son hipótesis iniciales.

Actualización de alcance, 9 de septiembre: BigQuery y Looker Studio quedan para una etapa posterior. El lanzamiento utiliza los reportes de AdMob, Firebase Analytics, Google Play y App Store; no requiere un tablero externo ni exportación a BigQuery.

## 1. Decisión de negocio

Lanzar gratis con los 30 retos actuales, pistas mediante anuncios voluntarios, intersticiales moderados y una compra única «Palabravo sin anuncios» con precio de referencia US$2,99, localizado por la tienda. No incorporar suscripción, monedas, battle pass ni skins al lanzamiento.

Optimizar el ingreso acumulado por jugador conservando la retención, no el número de anuncios por sesión. La compra elimina todos los anuncios; propuesta inicial: incluye 3 pistas gratuitas diarias. Este beneficio debe comunicarse antes de pagar y conservarse para quienes compraron; no reducirlo mediante configuración remota. Reintentar y avanzar no requieren anuncios. Los 30 retos pueden completarse el mismo día, sin bloqueos temporales ni anuncios obligatorios para desbloquear niveles.

Los packs premium serían una decisión posterior. La compra sin anuncios no implica automáticamente futuros packs de pago; si se incorporan, comunicarlo claramente.

## 2. Reglas iniciales de anuncios

| Elemento | Regla propuesta |
| --- | --- |
| Pistas | Algunas pistas gratuitas de introducción; después, oferta voluntaria con recompensa explícita. |
| Recompensado | Un video por una pista parcial, solo tras aceptación. No resolver todo el tablero. |
| Intersticial | Primera oportunidad al completar el tercer reto, sin tiempo mínimo inicial; después, al menos 3 retos completados desde el último intersticial mostrado, respetando separación y límite por sesión. |
| Separación | Al menos 180 segundos de juego activo desde el último anuncio mostrado, también si fue recompensado. Si no se ha mostrado ninguno, no hay espera inicial. |
| Límite | Máximo 2 intersticiales por sesión. Sesión: reinicio después de 30 minutos en segundo plano/inactividad. Persistir contadores para que reiniciar el proceso no eluda los límites. |
| Ubicación | Transición natural al finalizar un reto, antes de habilitar la acción de continuar. Nunca durante selección de palabras. |
| Nuevo invitado | Primera partida referida sin intersticial, aunque otras condiciones lo permitan. |
| Sin disponibilidad | Continuar normalmente; no bloquear por falta de inventario. En pistas, informar y permitir seguir o reintentar después. |
| Comprador | Sin solicitudes ni visualización de anuncios tras confirmar el derecho adquirido. |
| Banners y apertura | No incluir. |

Si un anuncio no está listo en su oportunidad, omitirlo y reevaluar al completar el siguiente reto; nunca mostrarlo tardíamente dentro del siguiente puzzle. El contador de separación entre retos se actualiza solo al registrar una impresión. Precargar no equivale a mostrar ni a generar ingresos. No colocar un intersticial inmediatamente después de una pista recompensada.

AdMob recomienda las pausas entre niveles como ubicación natural y advierte sobre anuncios inesperados junto al botón de continuar: [guía oficial](https://support.google.com/admob/answer/6201350?hl=en).

## 3. Implementación configurable desde el día 1

Centralizar decisiones en un servicio de monetización, con adaptadores para anuncios, compras y telemetría. Las pantallas no deciden individualmente las reglas. Configuración remota con valores locales seguros, validación de rangos, caché, revisión y reversión:

```json
{
  "config_version": "launch-1",
  "banner_enabled": true,
  "interstitial_enabled": true,
  "rewarded_enabled": true,
  "interstitial_every_completed": 3,
  "interstitial_grace_completed": 2,
  "min_active_seconds_between_ads": 180,
  "max_interstitials_per_session": 2,
  "first_referred_attempt_ad_free": true,
  "remove_ads_product_id": "palabravo_remove_ads"
}
```

Aplicar cambios de frecuencia en límites de sesión; mantener la versión efectiva durante el intento. Un interruptor de apagado puede tener efecto inmediato. Si falla una actualización, conservar la última configuración válida. La configuración no ejecuta código remoto ni añade formatos ausentes del binario. El precio mostrado procede de la tienda, no de un JSON.

Firebase Remote Config permite ajustar parámetros ya integrados sin publicar un binario nuevo: [documentación](https://firebase.google.com/docs/remote-config). La selección final del SDK/adaptador debe verificarse contra el framework real de la app; este documento no presupone un paquete MAUI específico.

## 4. Contrato de eventos de monetización

El contrato común, identidad, sesiones y reglas de análisis se definen en `palabravo_estrategia_crecimiento_marketing.md`. Aquí se añaden los eventos monetarios. Nombres lógicos: el adaptador debe mapearlos a nombres compatibles y evitar duplicar eventos automáticos del proveedor.

| Evento | Cuándo emitir | Parámetros específicos |
| --- | --- | --- |
| `ad_opportunity` | Al evaluar una transición o una solicitud de pista. | `placement_id`, `format`, `eligible`, `reason` |
| `ad_request` | Solicitud real al SDK. | `ad_request_id`, `placement_id`, `format` |
| `ad_load_result` | Resultado de carga. | `ad_request_id`, `status`, `error_code`, `latency_ms` |
| `reward_offer_view` | Oferta de pista efectivamente visible. | `placement_id`, `reward_type`, `offer_id` |
| `reward_offer_accept` | Aceptación explícita. | `offer_id`, `placement_id` |
| `ad_impression` | Callback real de impresión. | `ad_instance_id`, `placement_id`, `format`, `ad_source` |
| `ad_show_failed` | Fallo al intentar mostrar. | `ad_instance_id`, `error_code` |
| `ad_dismissed` | Cierre confirmado por SDK. | `ad_instance_id` |
| `ad_revenue` | Callback de valor monetario. | `ad_instance_id`, `value_micros`, `currency`, `precision` |
| `reward_earned` | Callback de recompensa del SDK. | `ad_instance_id`, `reward_id`, `reward_type` |
| `hint_granted` | Pista entregada efectivamente. | `reward_id` si aplica, `source`, `hint_type` |
| `purchase_offer_view` | Oferta visible, no solo pantalla cargada. | `product_id`, `placement_id`, `currency`, `price` |
| `purchase_started` | Apertura del flujo de compra. | `product_id` |
| `purchase_result` | Cambio confirmado de estado. | `product_id`, `status`, `error_code` si aplica |
| `entitlement_changed` | Derecho verificado, restaurado o revocado. | `product_id`, `status`, `source` |

Estados de compra: `pending`, `cancelled`, `failed`, `verified`. Una compra pendiente no es ingreso confirmado. Conservar transacciones verificadas en el registro de compras y reconciliar devoluciones; no enviar tokens de compra a analítica general. Una restauración no es una nueva venta. Entrega de pistas idempotente por `reward_id`: no duplicar recompensas al recibir dos callbacks ni perderlas si el callback de cierre llega antes.

El SDK de AdMob expone valor por impresión, moneda y precisión. Convertir micros dividiendo por 1.000.000; no interpretar un valor desconocido como ingreso confirmado. [Documentación de ingresos por impresión](https://developers.google.com/admob/android/impression-level-ad-revenue).

Elegir una fuente canónica para ingreso publicitario: si Firebase/AdMob ya registra `ad_impression` con ingresos, no sumar otra vez `ad_revenue`. Los eventos auxiliares sirven para diagnóstico. Reconciliar estimaciones de telemetría con reportes finalizados del proveedor.

## 5. Tablero de monetización

| Métrica | Definición |
| --- | --- |
| Ingreso publicitario diario | Suma canónica de ingresos por impresiones, misma moneda y fecha UTC. |
| ARPDAU publicitario | Ingreso publicitario / jugadores activos del día. |
| ARPDAU total | Ingreso publicitario + compras verificadas del día, dividido por jugadores activos; señalar si es bruto o neto. |
| eCPM | Ingreso publicitario × 1.000 / impresiones reales. Separar formato y país de audiencia. |
| Carga exitosa | Solicitudes cargadas / solicitudes al SDK. Es una métrica propia; no asumir que reproduce el fill rate de AdMob. |
| Impresiones por activo | Impresiones / jugadores activos. |
| Aceptación de pista con anuncio | Ofertas aceptadas / ofertas visibles, deduplicadas por `offer_id`. |
| Entrega de recompensa | Recompensas entregadas / recompensas ganadas, por `reward_id`. |
| Conversión de oferta | Usuarios con compra verificada / usuarios que vieron la oferta, en 7 días. |
| Conversión D30 de cohorte | Nuevos jugadores compradores en 30 días / nuevos jugadores de esa cohorte madura. |
| Continuidad posterior | Intentos con otro inicio de reto en 10 minutos activos posteriores al cierre del anuncio / intentos con anuncio cerrado. |
| Ingreso D7/D30 por adquirido | Ingreso acumulado de la cohorte hasta ese horizonte / usuarios adquiridos de la cohorte. No llamarlo LTV completo. |

El abandono posterior a un anuncio es asociación, no prueba causal. Comparar variantes aleatorias para estimar el efecto. Separar compradores, no compradores, versión de app, país de audiencia, plataforma, origen, formato, placement y variante. No sumar importes de monedas distintas sin una conversión documentada.

Ejemplo ilustrativo, no pronóstico: 1.000 activos diarios × 1,5 impresiones diarias × 30 × US$3 de eCPM / 1.000 = US$135 mensuales en ads. El resultado depende de audiencia, demanda, retención e inventario. Las compras únicas no son MRR.

Beneficio operativo: ingresos cobrados menos comisiones y ajustes no descontados previamente, infraestructura, adquisición, herramientas y costo editorial. No descontar dos veces comisiones ya incluidas en un importe neto.

## 6. Experimentación y decisiones

Primero verificar datos y construir una línea base. Después probar una sola diferencia: solo recompensados frente a recompensados más intersticiales moderados. Asignar variante estable por instalación, registrar exposición al activarse la política y analizar compradores aparte. No asignar cada sesión una variante distinta.

Métrica principal: ingreso D7 por nuevo jugador expuesto. Protección: retención D1/D7, finalización del primer reto, errores y entrega de recompensas. Comparar cohortes maduras; con poco tráfico priorizar observación y pruebas de usuarios, sin afirmar significancia. Definir antes el efecto mínimo relevante y tamaño necesario; no detener el test solo por un pico favorable.

Si suben ingresos por sesión pero cae el ingreso por adquirido o la retención, reducir presión. Si las recompensas fallan, apagar ese placement y resolverlo. No aumentar dificultad para forzar pistas.

## 7. Criterios para publicar

- [ ] Unidades de prueba separadas de producción; las pruebas no generan ingresos reales.
- [ ] Consentimiento, opciones de privacidad y declaraciones de la tienda coherentes con la recopilación efectiva; verificar requisitos vigentes al implementar.
- [ ] Eventos de oportunidad, impresión, recompensa e ingresos comprobados en dispositivo.
- [ ] Contadores, cooldown, ausencia de red y callbacks duplicados comprobados.
- [ ] Compra, cancelación, estado pendiente, restauración y derecho sin anuncios verificados.
- [ ] Ingresos automáticos y manuales no se duplican.
- [ ] Configuración inválida no rompe el juego y existe apagado remoto.
- [ ] Reportes de AdMob, Firebase Analytics y ambas tiendas verificados; BigQuery y Looker Studio quedan para después.

Este documento es una especificación propuesta, no una integración ya ejecutada.
