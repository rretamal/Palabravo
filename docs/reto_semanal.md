# Reto semanal: operación

La app y el sitio usan `Web/public/content/weekly.json` como fuente administrable. Ese mismo archivo se incluye en el binario como respaldo sin conexión. Al publicar el sitio, la app consulta `https://palabravo.app/content/weekly.json`, conserva la última versión válida y solo muestra el evento entre `startsAt` (incluido) y `endsAt` (excluido), en UTC.

El ranking visible en la app es semanal y recibe solamente resultados completados desde el reto semanal, para que todas las personas compitan con el mismo tablero. Antes de llamar a la API, la app guarda localmente el mejor resultado pendiente y vuelve a enviarlo al abrir el ranking. Una interrupción o una API temporalmente indisponible no pierde el resultado.

Para cambiar el evento se editan sus metadatos, las cuatro conexiones, el badge y los textos de compartir. Cada evento y cada puzzle deben tener IDs nuevos y estables. El puzzle debe contener exactamente cuatro grupos de cuatro palabras únicas. Las imágenes remotas deben usar HTTPS.

## Azure Table

Crear, en la misma Storage Account usada por compras, una segunda tabla llamada exactamente `PalabravoWeekly`. Las Functions usan `PURCHASE_STORAGE_CONNECTION`; no hace falta otra cadena de conexión.

La tabla almacena:

- el mejor resultado de cada jugador, en una partición por evento;
- tokens de desafío de seis caracteres, con vencimiento de 31 días;
- alias, medalla, errores, pistas, tiempo y score necesarios para la landing y el ranking.

Los IDs de jugador se transforman en un hash antes de persistirse. No se guardan tokens de sesión de PlayFab. La API autentica la publicación de resultados y la creación de desafíos; la consulta de un desafío es pública porque el token forma parte del enlace compartido.

## Rutas

- Contenido: `/content/weekly.json`
- Landing: `/semanal`
- Crear desafío: `POST /api/challenges`
- Resolver desafío: `GET /api/challenges/{token}`
- Publicar resultado: `POST /api/weekly/results`
- Ranking: `GET /api/weekly/{weeklyId}/ranking`

Antes de publicar se deben probar los App Links/Universal Links con las firmas de distribución, crear `PalabravoWeekly` y desplegar juntos el sitio y sus Functions. Si la API social no está disponible, el puzzle y el badge siguen funcionando; compartir utiliza `/semanal` como respaldo.
