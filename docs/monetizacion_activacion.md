# Monetización: activación y validación

Actualizado el 9 de septiembre de 2026. BigQuery y Looker Studio quedan para una etapa posterior. El lanzamiento utiliza los reportes de AdMob, Firebase Analytics, Google Play y App Store.

La monetización permanece desactivada en `Palabravo/monetization.json` hasta completar la configuración externa y las pruebas de tienda. No se han publicado binarios ni efectuado compras reales.

## Implementación

- Reglas y persistencia local independientes del progreso: bienvenida, cupo diario, recompensas idempotentes, primera oportunidad al tercer reto sin espera inicial; después, mínimo tres retos desde el último intersticial y 180 segundos activos desde cualquier anuncio. Máximo dos intersticiales por sesión; si falta inventario, reevaluar al completar el siguiente reto.
- Pistas distintas y parciales; una recompensa cuya pantalla no se confirmó se recupera en el siguiente intento.
- Pausas compartidas entre intentos para consentimiento, anuncios, modales y segundo plano. Iniciar otro reto durante una pausa no activa su cronómetro.
- Adaptadores de AdMob, UMP, Firebase, Google Play Billing y StoreKit 2; verificación y restauración mediante API autenticada con PlayFab.
- Antes de una solicitud o visualización encolada se vuelve a comprobar consentimiento, compra y configuración, evitando anuncios después de confirmar el derecho.
- Borrado de referencias de comprador en Azure, solicitud de eliminación histórica de Analytics y limpieza local. Se conserva el beneficio ya comprado y los registros de transacción necesarios para restauraciones/devoluciones. Un marcador impide que verificaciones concurrentes recreen referencias a la cuenta eliminada.
- Enlaces de reto y códigos de respaldo, opciones de privacidad y oferta sin anuncios.

## Verificación

- Núcleo: 76 pruebas aprobadas.
- API: compilación TypeScript y 21 pruebas aprobadas.
- Web: compilación y 6 pruebas aprobadas.
- Android: se resolvió el conflicto de clases duplicadas alineando Firebase Analytics y toda la familia Measurement en `123.2.0.1`; Release empaqueta. El SDK de anuncios emite advertencias sobre registro dinámico de callbacks: comprobarlos en dispositivo.
- iOS: compilación Release sin firma con puente Swift StoreKit 2. La ausencia de perfil de aprovisionamiento genera una advertencia esperada en este modo; no equivale a una validación de tienda.

## Configuración externa

1. Registrar las dos apps y las cuatro unidades descritas en [admob_unidades.md](admob_unidades.md).
2. Conectar Firebase para `com.palabravo.app`. Descargar `google-services.json` a `Palabravo/Platforms/Android/` y `GoogleService-Info.plist` a `Palabravo/Platforms/iOS/`. Estos archivos se excluyen de Git.
3. Configurar mensajes UMP y opciones de privacidad. Analytics y Crashlytics se habilitan únicamente con la preferencia opcional del usuario. Declarar en las tiendas los datos que recopila la versión finalmente probada.
4. Crear el producto no consumible `palabravo_remove_ads` en ambas tiendas, con precio localizado de referencia US$2,99. Beneficios permanentes: ningún anuncio, tres pistas por día UTC no acumulables y máximo dos pistas por intento. Restauración dentro de cada tienda.
5. Crear `PalabravoPurchases` y `PalabravoWeekly` en Azure Table Storage y configurar las variables de `Web/api/local.settings.example.json` como secretos de ejecución. Ambas usan `PURCHASE_STORAGE_CONNECTION`; las Functions no crean infraestructura automáticamente.
6. Separar sandbox y producción mediante `PURCHASE_ENVIRONMENT`. Configurar credenciales de Google Play y Apple, certificados raíz oficiales de Apple y permisos de acceso al producto. Nunca registrar tokens de compra ni cabeceras de autorización.
7. Configurar Apple Server Notifications en `/api/purchases/notifications/apple` y Google RTDN mediante Pub/Sub push autenticado hacia `/api/purchases/notifications/google`, con audiencia exacta y correo de cuenta de servicio autorizado.
8. Para el borrado histórico de Analytics, configurar `ANALYTICS_PROPERTY_ID` y `ANALYTICS_SERVICE_ACCOUNT_JSON`, habilitar Analytics Admin API y conceder a esa cuenta los permisos necesarios sobre la propiedad. No necesita BigQuery. El endpoint de borrado solicita primero el procesamiento de Analytics y limpia las referencias Azure, antes de invalidar la sesión PlayFab. Si falla, el usuario puede reintentar.
9. Completar las seis unidades (banner, intersticial y recompensado para Android e iOS) en `Palabravo/monetization.json`. Mantener `testAds: true` en QA. Sustituir también los App IDs de muestra del manifiesto Android y `Info.plist` iOS al preparar producción.
10. Configurar Remote Config con la clave JSON `monetization_policy` y los parámetros de la estrategia. Incluir `banner_enabled: true`; permite apagar los banners sin publicar una versión nueva. Para la frecuencia actual, publicar `min_active_seconds_between_ads: 180`; una política remota o cacheada con 300 conserva la espera anterior. Los cambios de frecuencia se aplican al iniciar una nueva sesión después de recibirlos. La app conserva la última configuración válida, congela la política durante el intento y aplica el apagado al recibirlo. Consulta en primer plano y durante interacciones con caché mínima de 60 segundos; la red no garantiza entrega instantánea.
11. Configurar `ANDROID_CERT_SHA256` (Play App Signing), `APPLE_TEAM_ID` y `ADMOB_PUBLISHER_ID` en el build web. Se generan las asociaciones `.well-known` y `app-ads.txt`. `MONETIZATION_RELEASE=true` impide generar una versión web de monetización sin estos datos. Agregar el enlace real de App Store cuando exista.

## Reportes para el lanzamiento

- AdMob: impresiones e ingresos publicitarios por plataforma y formato.
- Firebase Analytics: inicios/finalizaciones, ofertas de pistas, recompensas y resultados de compra. Los usuarios sin consentimiento quedan fuera de esta medición.
- Google Play y App Store: ventas verificadas, devoluciones y reportes financieros. No inferir ingresos de una compra pendiente ni sumar monedas diferentes.
- `ad_impression` automático es la fuente publicitaria canónica. `ad_impression_diagnostic` sirve para verificar callbacks y nunca se vuelve a sumar como ingreso.

Las consultas de `monetizacion_tablero.sql` se conservan como material futuro. No desplegar BigQuery, no crear Looker Studio y no bloquear el lanzamiento por ellos. El experimento de presión publicitaria sigue apagado hasta tener una línea base.

## Pruebas que requieren servicios y dispositivos

- UMP requerido/no requerido, denegación, cambios de privacidad, ausencia de red e inventario.
- Anuncios de prueba: impresión, cierre, recompensa, callbacks duplicados/desordenados, interrupción del proceso y entrega única.
- Intersticiales en 3/6/9, cooldown activo, límite de sesión, intento referido y apagado remoto.
- Compra, cancelación, pendiente, aprobación posterior, restauración, reinstalación, devolución y cero solicitudes de anuncios después de la compra.
- Entrega y reintentos de notificaciones de ambas tiendas, permisos Azure y solicitud de borrado de Analytics.
- Enlaces universales/App Links con firma de distribución y los informes nativos del proveedor.

Las credenciales y unidades no están configuradas en este repositorio. Estas comprobaciones no pueden darse por realizadas con una compilación local.

## Comandos

```sh
dotnet test Palabravo.Tests/Palabravo.Tests.csproj
dotnet build Palabravo/Palabravo.csproj -f net10.0-android -c Release
dotnet build Palabravo/Palabravo.csproj -f net10.0-ios -r ios-arm64 -c Release -p:EnableCodeSigning=false
```

En `Web/api` y en `Web`: `npm run build` y `npm test`. El ejecutor .NET necesita permiso para abrir un puerto local. Los builds sin firma no validan compras reales ni publicación.
