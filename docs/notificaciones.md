# Notificaciones

Palabravo ofrece dos preferencias independientes y desactivadas por defecto.

- **Recordatorios de progreso:** la app programa un único aviso local para 48 horas después de salir. Al volver, cancela el aviso pendiente y lo reprograma en la próxima salida.
- **Nuevos retos:** la app se suscribe al tema de Firebase Cloud Messaging `new-challenges-es`. Cada mensaje enlaza al reto semanal vigente.

## Configuración móvil

Android requiere `Palabravo/Platforms/Android/google-services.json`, excluido de Git, con el paquete `com.palabravo.app`. En el primer uso, la app explica ambos tipos de aviso una sola vez; si el jugador elige **Activar**, solicita el permiso nativo (obligatorio desde Android 13) y habilita recordatorios y nuevos retos. Si elige **Ahora no** o rechaza el permiso, ambos permanecen desactivados y puede habilitarlos individualmente más tarde desde Perfil.

La integración de iPhone queda preparada en el servicio compartido, pero el proyecto debe habilitar primero el target `net10.0-ios`. Después se debe incluir `GoogleService-Info.plist`, habilitar Push Notifications y Remote notifications, agregar el entitlement de APNs y cargar una clave APNs en Firebase.

## Configuración del envío

1. Habilitar Firebase Cloud Messaging API v1 en el proyecto Firebase.
2. Crear una cuenta de servicio dedicada con el permiso mínimo necesario para enviar mensajes.
3. Guardar su JSON completo como `FIREBASE_SERVICE_ACCOUNT_JSON` en la configuración de Azure Static Web Apps.
4. Generar un secreto aleatorio largo y guardar el mismo valor como `NOTIFICATION_ADMIN_KEY` en Azure y en GitHub Actions.
5. Publicar el reto antes de enviar su notificación.
6. Ejecutar el workflow manual **Enviar aviso de nuevo reto** desde GitHub Actions.

La Function valida la clave administrativa y limita los tamaños y el formato de los campos. La credencial de Firebase nunca se entrega al navegador ni a la app.
