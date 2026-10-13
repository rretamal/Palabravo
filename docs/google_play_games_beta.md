# Inicio de sesión con Google Play Games en beta

El SDK se inicializa desde `MainApplication.OnCreate`. En el primer uso de Android se ofrece vincular Google una sola vez, con la alternativa de seguir como invitado. Si se omite o falla, puede reintentarse desde Inicio, Ranking o Perfil. Ranking muestra un aviso mientras no haya vinculación. Los botones de Inicio, Ranking y Perfil usan el mismo servicio de vinculación con PlayFab. El cliente web se obtiene del recurso `google_play_games_web_client_id`, sin duplicarlo en código.

Si funciona instalado localmente pero falla desde Google Play, revisar:

1. En Play Console → Integridad de la aplicación, copiar el SHA-1 del certificado de **firma de aplicaciones de Google Play**. El certificado de carga es distinto.
2. En Play Games Services → Configuración, verificar una credencial Android para `com.palabravo.app` con ese SHA-1. Conservar una credencial separada para desarrollo local si se necesita.
3. Verificar que el ID de proyecto de Play Games coincida con `google_play_games_app_id`.
4. Verificar una credencial de servidor de juego con el cliente web de `Platforms/Android/Resources/values/google_play_games.xml`. Configurar el mismo cliente web y su secreto en PlayFab, título `153ECF`. El secreto nunca se incluye en la app.
5. Si Play Games no está publicado, agregar la cuenta de prueba en **Testers de Play Games** o habilitar el canal de pruebas correspondiente. Participar en la beta de la app no garantiza acceso a Play Games. Publicar los cambios de Play Games cuando corresponda.
6. Instalar desde el canal beta e iniciar sesión con esa cuenta. El botón debe mostrar confirmación de vinculación; al volver a Inicio debe desaparecer. Si falla, registrar el mensaje y código mostrado, sin guardar códigos OAuth ni tokens.

El código 10 señala una configuración inválida; revisar credenciales, paquete y firma. Un error de PlayFab sobre OAuth o el código de servidor exige revisar la credencial web y su configuración en PlayFab. Cambiar el código de la app no corrige por sí solo estas configuraciones externas.

Referencias: [integración Android](https://developer.android.com/games/pgs/android/android-signin), [configuración y testers](https://developer.android.com/games/pgs/console/setup), [diagnóstico](https://developer.android.com/games/pgs/android/troubleshooting).
