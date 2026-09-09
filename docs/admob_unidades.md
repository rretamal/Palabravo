# Unidades de AdMob de Palabravo

Crear **dos aplicaciones en AdMob**, una Android y otra iOS, y **cuatro unidades en total**. Usar `com.palabravo.app` como identificador de paquete/bundle de la app correspondiente.

| App | Nombre sugerido de la unidad | Formato | Campo de configuración |
| --- | --- | --- | --- |
| Android | `palabravo_android_pista` | Con recompensa (Rewarded) | `androidRewardedId` |
| Android | `palabravo_android_fin_reto` | Intersticial | `androidInterstitialId` |
| iOS | `palabravo_ios_pista` | Con recompensa (Rewarded) | `iosRewardedId` |
| iOS | `palabravo_ios_fin_reto` | Intersticial | `iosInterstitialId` |

Para ambas unidades con recompensa:

- Cantidad de recompensa: **1**.
- Nombre/tipo de recompensa: **pista**.
- Seleccionar **Con recompensa**, no el formato «Intersticial con recompensa».
- La app solicita aceptación explícita y entrega una pista parcial tras recibir el callback de recompensa.

Los intersticiales se utilizan exclusivamente al finalizar retos, con frecuencia, cooldown y límites controlados por la app. No crear banners ni unidades de apertura.

## Identificadores que se necesitan

1. Los **cuatro Ad Unit IDs**, con formato `ca-app-pub-…/…`, se guardan en `Palabravo/monetization.json` en los campos indicados.
2. El **App ID Android**, con formato `ca-app-pub-…~…`, sustituye el ID de muestra de `com.google.android.gms.ads.APPLICATION_ID` en `Platforms/Android/AndroidManifest.xml`.
3. El **App ID iOS**, también con `~`, sustituye el ID de muestra de `GADApplicationIdentifier` en `Platforms/iOS/Info.plist`.
4. El Publisher ID `pub-…` se configura como `ADMOB_PUBLISHER_ID` para generar `app-ads.txt` en el sitio.

Los App IDs y Ad Unit IDs cumplen funciones distintas; no intercambiarlos. Las pruebas conservan `testAds: true` y usan las unidades de demostración del SDK. No hace falta crear cuatro unidades extra para desarrollo.

Configurar además los mensajes de privacidad de AdMob. Tener unidades creadas no sustituye la configuración de consentimiento, Firebase ni compras.

Referencia: [crear una unidad con recompensa en AdMob](https://support.google.com/admob/answer/7311747?hl=es).
